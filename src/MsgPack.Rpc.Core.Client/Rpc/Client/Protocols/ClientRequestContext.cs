using MsgPack.Rpc.Core.Protocols;
using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;

namespace MsgPack.Rpc.Core.Client.Protocols {
	/// <summary>
	///		Represents context information for the client side request including notification.
	/// </summary>
	public sealed class ClientRequestContext : OutboundMessageContext {
		/// <summary>
		///		Constant part of the request header.
		/// </summary>
		private static readonly ArraySegment<byte> _requestHeader =
			new ArraySegment<byte>(new byte[] { 0x94, 0x00 }); // [FixArray4], [Request:0]

		/// <summary>
		///		Constant part of the request header.
		/// </summary>
		private static readonly ArraySegment<byte> _notificationHeader =
			new ArraySegment<byte>(new byte[] { 0x94, 0x02 }); // [FixArray4], [Notification:2]

		/// <summary>
		///		Empty array of <see cref="ArraySegment{T}"/> of <see cref="byte"/>.
		/// </summary>
		private static readonly ArraySegment<byte> _emptyBuffer =
			new ArraySegment<byte>(new byte[0], 0, 0);

		private MessageType _messageType;

		/// <summary>
		///		Gets the type of the message.
		/// </summary>
		/// <value>
		///		The type of the message.
		/// </value>
		/// <remarks>
		///		This value can be set via <see cref="SetRequest"/> or <see cref="SetNotification"/> method.
		/// </remarks>
		public MessageType MessageType {
			get {
				Contract.Ensures(Contract.Result<MessageType>() == Rpc.Core.Protocols.MessageType.Request || Contract.Result<MessageType>() == Rpc.Core.Protocols.MessageType.Notification);

				return _messageType;
			}
		}

		/// <summary>
		///		Gets the <see cref="Packer"/> to pack arguments array.
		/// </summary>
		/// <value>
		///		The <see cref="Packer"/> to pack arguments array.
		///		This value will not be <c>null</c>.
		/// </value>
		public Packer ArgumentsPacker { get; private set; }

		/// <summary>
		///		Gets the name of the calling method.
		/// </summary>
		/// <value>
		///		The name of the calling method.
		///		This value will be <c>null</c> if both of <see cref="SetRequest"/> and <see cref="SetNotification"/> have not been called after previous cleanup or initialization.
		/// </value>
		/// <remarks>
		///		This value can be set via <see cref="SetRequest"/> or <see cref="SetNotification"/> method.
		/// </remarks>
		public string MethodName { get; private set; }

		/// <summary>
		///		The reusable buffer to pack method name.
		///		This value will not be <c>null</c>.
		/// </summary>
		private readonly MemoryStream methodNameBuffer;

		/// <summary>
		///		The reusable buffer to pack arguments.
		///		This value will not be <c>null</c>.
		/// </summary>
		private readonly MemoryStream argumentsBuffer;

		/// <summary>
		///		The resusable buffer to hold sending response data.
		/// </summary>
		/// <remarks>
		///		Each segment corresponds to the message segment.
		///		<list type="table">
		///			<listheader>
		///				<term>Index</term>
		///				<description>Content</description>
		///			</listheader>
		///			<item>
		///				<term>0</term>
		///				<description>
		///					Common response header, namely array header and message type.
		///					Do not change this element.
		///				</description>
		///			</item>
		///			<item>
		///				<term>1</term>
		///				<description>
		///					Message ID to correpond the response to the request.
		///				</description>
		///			</item>
		///			<item>
		///				<term>2</term>
		///				<description>
		///					Error identifier.
		///				</description>
		///			</item>
		///			<item>
		///				<term>3</term>
		///				<description>
		///					Return value.
		///				</description>
		///			</item>
		///		</list>
		/// </remarks>
		internal readonly ArraySegment<byte>[] SendingBuffer;

		/// <summary>
		///		Gets the callback delegate which will be called when the notification is sent.
		/// </summary>
		/// <value>
		///		The callback delegate which will be called when the notification is sent.
		///		The 1st argument is an <see cref="Exception"/> which represents sending error, or <c>null</c> for success.
		///		The 2nd argument indicates that the operation is completed synchronously.
		///		This value will be <c>null</c> if both of <see cref="SetRequest"/> and <see cref="SetNotification"/> have not been called after previous cleanup or initialization.
		/// </value>
		/// <remarks>
		///		This value can be set via <see cref="SetNotification"/> method.
		/// </remarks>
		public Action<Exception, bool> NotificationCompletionCallback { get; private set; }

		/// <summary>
		///		Gets the callback delegate which will be called when the response is received.
		/// </summary>
		/// <value>
		///		The callback delegate which will be called when the notification sent.
		///		The 1st argument is a <see cref="ClientResponseContext"/> which stores any information of the response, it will not be <c>null</c>.
		///		The 2nd argument is an <see cref="Exception"/> which represents sending error, or <c>null</c> for success.
		///		The 3rd argument indicates that the operation is completed synchronously.
		///		This value will be <c>null</c> if both of <see cref="SetRequest"/> and <see cref="SetNotification"/> have not been called after previous cleanup or initialization.
		/// </value>
		/// <remarks>
		///		This value can be set via <see cref="SetRequest"/> method.
		/// </remarks>
		public Action<ClientResponseContext, Exception, bool> RequestCompletionCallback { get; private set; }

		private readonly Stopwatch _stopwatch;

		internal TimeSpan ElapsedTime => _stopwatch.Elapsed;

		/// <summary>
		///		Initializes a new instance of the <see cref="ClientRequestContext"/> class with default settings.
		/// </summary>
		public ClientRequestContext()
			: this(null) {
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="ClientRequestContext"/> class with specified configuration.
		/// </summary>
		/// <param name="configuration">
		///		An <see cref="RpcClientConfiguration"/> to tweak this instance initial state.
		/// </param>
		public ClientRequestContext(RpcClientConfiguration configuration) {
			methodNameBuffer = new MemoryStream((configuration ?? RpcClientConfiguration.Default).InitialMethodNameBufferLength);
			argumentsBuffer = new MemoryStream((configuration ?? RpcClientConfiguration.Default).InitialArgumentsBufferLength);
			SendingBuffer = new ArraySegment<byte>[4];
			ArgumentsPacker = Packer.Create(argumentsBuffer, false);
			_messageType = MessageType.Response;
			_stopwatch = new Stopwatch();
		}

		internal override void StartWatchTimeout(TimeSpan timeout) {
			base.StartWatchTimeout(timeout);
			_stopwatch.Restart();
		}

		internal override void StopWatchTimeout() {
			_stopwatch.Stop();
			base.StopWatchTimeout();
		}

		/// <summary>
		///		Set ups this context for request message.
		/// </summary>
		/// <param name="messageId">The message id which identifies request/response and associates request and response.</param>
		/// <param name="methodName">Name of the method to be called.</param>
		/// <param name="completionCallback">
		///		The callback which will be called when the response is received.
		///		For details, see <see cref="RequestCompletionCallback"/>.
		///	</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		///		Or <paramref name="completionCallback"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is empty.
		/// </exception>
		public void SetRequest(int messageId, string methodName, Action<ClientResponseContext, Exception, bool> completionCallback) {
			if (methodName == null) {
				throw new ArgumentNullException("methodName");
			}

			if (methodName.Length == 0) {
				throw new ArgumentException("Method name cannot be empty.", "methodName");
			}

			if (completionCallback == null) {
				throw new ArgumentNullException("completionCallback");
			}

			Contract.Ensures(MessageType == Rpc.Core.Protocols.MessageType.Request);
			Contract.Ensures(MessageId != null);
			Contract.Ensures(!string.IsNullOrEmpty(MethodName));
			Contract.Ensures(RequestCompletionCallback != null);
			Contract.Ensures(NotificationCompletionCallback == null);

			_messageType = Rpc.Core.Protocols.MessageType.Request;
			MessageId = messageId;
			MethodName = methodName;
			RequestCompletionCallback = completionCallback;
			NotificationCompletionCallback = null;
		}

		/// <summary>
		///		Set ups this context for notification message.
		/// </summary>
		/// <param name="methodName">Name of the method to be called.</param>
		/// <param name="completionCallback">
		///		The callback which will be called when the response is received.
		///		For details, see <see cref="NotificationCompletionCallback"/>.
		///	</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="methodName"/> is <c>null</c>.
		///		Or <paramref name="completionCallback"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		<paramref name="methodName"/> is empty.
		/// </exception>
		public void SetNotification(string methodName, Action<Exception, bool> completionCallback) {
			if (methodName == null) {
				throw new ArgumentNullException("methodName");
			}

			if (methodName.Length == 0) {
				throw new ArgumentException("Method name cannot be empty.", "methodName");
			}

			if (completionCallback == null) {
				throw new ArgumentNullException("completionCallback");
			}

			Contract.Ensures(MessageType == Rpc.Core.Protocols.MessageType.Notification);
			Contract.Ensures(MessageId == null);
			Contract.Ensures(!string.IsNullOrEmpty(MethodName));
			Contract.Ensures(RequestCompletionCallback == null);
			Contract.Ensures(NotificationCompletionCallback != null);

			_messageType = Rpc.Core.Protocols.MessageType.Notification;
			MessageId = null;
			MethodName = methodName;
			NotificationCompletionCallback = completionCallback;
			RequestCompletionCallback = null;
		}

		/// <summary>
		///		Prepares this instance to send request or notification message.
		/// </summary>
		internal void Prepare(bool canUseChunkedBuffer) {
			if (_messageType == MessageType.Response) {
				throw new InvalidOperationException("MessageType is not set.");
			}

			Contract.Assert(MethodName != null);

			using (var packer = Packer.Create(methodNameBuffer, false)) {
				packer.Pack(MethodName);
			}

			if (_messageType == MessageType.Request) {
				Contract.Assert(MessageId != null);
				Contract.Assert(RequestCompletionCallback != null);

				SendingBuffer[0] = _requestHeader;
				SendingBuffer[1] = GetPackedMessageId();
				SendingBuffer[2] = new ArraySegment<byte>(methodNameBuffer.GetBuffer(), 0, unchecked((int)methodNameBuffer.Length));
				SendingBuffer[3] = new ArraySegment<byte>(argumentsBuffer.GetBuffer(), 0, unchecked((int)argumentsBuffer.Length));
			}
			else {
				Contract.Assert(NotificationCompletionCallback != null);

				SendingBuffer[0] = _notificationHeader;
				SendingBuffer[1] = new ArraySegment<byte>(methodNameBuffer.GetBuffer(), 0, unchecked((int)methodNameBuffer.Length));
				SendingBuffer[2] = new ArraySegment<byte>(argumentsBuffer.GetBuffer(), 0, unchecked((int)argumentsBuffer.Length));
				SendingBuffer[3] = _emptyBuffer;
			}

			SocketContext.SetBuffer(null, 0, 0);
			SocketContext.BufferList = SendingBuffer;
		}

		internal override void ClearBuffers() {
			methodNameBuffer.SetLength(0);
			argumentsBuffer.SetLength(0);
			SocketContext.BufferList = null;
			ArgumentsPacker.Dispose();
			ArgumentsPacker = Packer.Create(argumentsBuffer, false);
			SendingBuffer[0] = new ArraySegment<byte>();
			SendingBuffer[1] = new ArraySegment<byte>();
			SendingBuffer[2] = new ArraySegment<byte>();
			SendingBuffer[3] = new ArraySegment<byte>();
			base.ClearBuffers();
		}

		/// <summary>
		///		Clears this instance internal buffers for reuse.
		/// </summary>
		internal sealed override void Clear() {
			ClearBuffers();
			MethodName = null;
			_messageType = MessageType.Response; // Invalid.
			RequestCompletionCallback = null;
			NotificationCompletionCallback = null;
			base.Clear();
		}
	}
}
