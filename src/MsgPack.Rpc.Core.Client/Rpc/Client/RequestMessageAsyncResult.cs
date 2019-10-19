using MsgPack.Rpc.Client.Protocols;
using System;
using System.Threading;

namespace MsgPack.Rpc.Client {
	/// <summary>
	///		<see cref="IAsyncResult"/> implementation for async RPC.
	/// </summary>
	internal sealed class RequestMessageAsyncResult : MessageAsyncResult {
		private ResultHolder _result;

		/// <summary>
		///		Gets a response data.
		/// </summary>
		/// <value>
		///		A response data.
		/// </value>
		public ResultHolder Result {
			get { return this._result; }
		}

		/// <summary>
		///		Processes asynchronous operation completion logic.
		/// </summary>
		/// <param name="context">The response context which holds response data.</param>
		/// <param name="exception">The exception occured.</param>
		/// <param name="completedSynchronously">When operation is completed same thread as initiater then <c>true</c>; otherwise, <c>false</c>.</param>
		public void OnCompleted(ClientResponseContext context, Exception exception, bool completedSynchronously) {
			if (exception != null) {
				base.OnError(exception, completedSynchronously);
			}
			else {
				var error = ErrorInterpreter.UnpackError(context);
				if (!error.IsSuccess) {
					base.OnError(error.ToException(), completedSynchronously);
				}
				else {
					Interlocked.CompareExchange(ref this._result, new ResultHolder(Unpacking.UnpackObject(context.ResultBuffer)), null);
					base.Complete(completedSynchronously);
				}
			}

			var callback = this.AsyncCallback;
			if (callback != null) {
				callback(this);
			}
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="RequestMessageAsyncResult"/> class.
		/// </summary>
		/// <param name="owner">
		///		The owner of asynchrnous invocation. This value will not be null.
		/// </param>
		/// <param name="messageId">The ID of message.</param>
		/// <param name="asyncCallback">
		///		The callback of asynchrnous invocation which should be called in completion.
		///		This value can be null.
		/// </param>
		/// <param name="asyncState">
		///		The state object of asynchrnous invocation which will be passed to <see cref="AsyncCallback"/>.
		///		This value can be null.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="owner"/> is null.
		/// </exception>
		public RequestMessageAsyncResult(Object owner, int messageId, AsyncCallback asyncCallback, object asyncState)
			: base(owner, messageId, asyncCallback, asyncState) { }

		public sealed class ResultHolder {
			private readonly MessagePackObject _value;

			public MessagePackObject Value {
				get { return this._value; }
			}

			public ResultHolder(MessagePackObject value) {
				this._value = value;
			}
		}
	}
}
