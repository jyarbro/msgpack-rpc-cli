using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using MsgPack.Rpc.Core.Protocols;

namespace MsgPack.Rpc.Core.Client.Protocols {
	partial class ClientTransport {
		/// <summary>
		///		Unpack response message array header.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		internal bool UnpackResponseHeader(ClientResponseContext context) {
			Contract.Assert(context != null);

			if (context.RootUnpacker == null) {
				context.UnpackingBuffer = new ByteArraySegmentStream(context.ReceivedData);
				context.RootUnpacker = Unpacker.Create(context.UnpackingBuffer, false);
				context.RenewSessionId();
			}

			if (!context.ReadFromRootUnpacker()) {
				MsgPackRpcClientProtocolsTrace.TraceEvent(MsgPackRpcClientProtocolsTrace.NeedRequestHeader, "Array header is needed. {{ \"SessionID\" : {0} }}", context.SessionId);
				return false;
			}

			if (!context.RootUnpacker.IsArrayHeader) {
				HandleDeserializationError(context, "Invalid response message stream. Message must be array.", () => context.UnpackingBuffer.ToArray());
				return context.NextProcess(context);
			}

			if (context.RootUnpacker.ItemsCount != 4) {
				HandleDeserializationError(
					context,
					string.Format(
						CultureInfo.CurrentCulture,
						"Invalid response message stream. Message must be valid size array. Actual size is {0}.",
						context.RootUnpacker.ItemsCount
					),
					() => context.UnpackingBuffer.ToArray()
				);
				return context.NextProcess(context);
			}

			context.HeaderUnpacker = context.RootUnpacker.ReadSubtree();
			context.NextProcess = UnpackMessageType;
			return context.NextProcess(context);
		}

		/// <summary>
		///		Unpack Message Type part on response message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackMessageType(ClientResponseContext context) {
			if (!context.ReadFromHeaderUnpacker()) {
				MsgPackRpcClientProtocolsTrace.TraceEvent(MsgPackRpcClientProtocolsTrace.NeedMessageType, "Message Type is needed. {{ \"SessionID\" : {0} }}", context.SessionId);
				return false;
			}

			int numericType;
			try {
				numericType = context.HeaderUnpacker.LastReadData.AsInt32();
			}
			catch (InvalidOperationException) {
				HandleDeserializationError(context, "Invalid response message stream. Message Type must be Int32 compatible integer.", () => context.UnpackingBuffer.ToArray());
				return context.NextProcess(context);
			}

			var type = (MessageType)numericType;
			if (type != MessageType.Response) {
				HandleDeserializationError(
					context,
					string.Format(CultureInfo.CurrentCulture, "Unknown message type '{0:x8}'", numericType),
					() => context.UnpackingBuffer.ToArray()
				);
				return context.NextProcess(context);
			}

			context.NextProcess = UnpackMessageId;

			return context.NextProcess(context);
		}

		/// <summary>
		///		Unpack Message ID part on response message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackMessageId(ClientResponseContext context) {
			if (!context.ReadFromHeaderUnpacker()) {
				MsgPackRpcClientProtocolsTrace.TraceEvent(MsgPackRpcClientProtocolsTrace.NeedMessageId, "Message ID is needed. {{ \"SessionID\" : {0} }}", context.SessionId);
				return false;
			}

			try {
				context.MessageId = unchecked((int)context.HeaderUnpacker.LastReadData.AsUInt32());
			}
			catch (InvalidOperationException) {
				HandleDeserializationError(
					context,
					"Invalid response message stream. ID must be UInt32 compatible integer.",
					() => context.UnpackingBuffer.ToArray()
				);
				return context.NextProcess(context);
			}

			context.NextProcess = UnpackError;
			return context.NextProcess(context);
		}

		/// <summary>
		///		Unpack error part on response message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackError(ClientResponseContext context) {
			Contract.Assert(context.UnpackingBuffer.CanSeek);
			if (context.ErrorStartAt == -1) {
				context.ErrorStartAt = context.UnpackingBuffer.Position;
			}

			var skipped = context.SkipErrorSegment();
			if (skipped == null) {
				MsgPackRpcClientProtocolsTrace.TraceEvent(MsgPackRpcClientProtocolsTrace.NeedError, "Error value is needed. {{ \"SessionID\" : {0} }}", context.SessionId);
				return false;
			}

			context.ErrorBuffer = new ByteArraySegmentStream(context.UnpackingBuffer.GetBuffer(context.ErrorStartAt, context.UnpackingBuffer.Position - context.ErrorStartAt));
			context.NextProcess = UnpackResult;

			return context.NextProcess(context);
		}

		/// <summary>
		///		Unpack result part on response message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool UnpackResult(ClientResponseContext context) {
			Contract.Assert(context.UnpackingBuffer.CanSeek);
			if (context.ResultStartAt == -1) {
				context.ResultStartAt = context.UnpackingBuffer.Position;
			}

			var skipped = context.SkipResultSegment();
			if (skipped == null) {
				MsgPackRpcClientProtocolsTrace.TraceEvent(MsgPackRpcClientProtocolsTrace.NeedResult, "Result value is needed. {{ \"SessionID\" : {0} }}", context.SessionId);
				return false;
			}

			context.ResultBuffer = new ByteArraySegmentStream(context.UnpackingBuffer.GetBuffer(context.ResultStartAt, context.UnpackingBuffer.Position - context.ResultStartAt));
			context.NextProcess = Dispatch;

			return context.NextProcess(context);
		}

		/// <summary>
		///		Dispatch response message.
		/// </summary>
		/// <param name="context">Context information.</param>
		/// <returns>
		///		<c>true</c>, if the pipeline is finished;
		///		<c>false</c>, the pipeline is interruppted because extra data is needed.
		/// </returns>
		private bool Dispatch(ClientResponseContext context) {
			Contract.Assert(context.MessageId != null);

			try {
				Action<ClientResponseContext, Exception, bool> handler = null;
				try {
					pendingRequestTable.TryRemove(context.MessageId.Value, out handler);
				}
				finally {
					// Best effort to rescue from ThreadAbortException...
					if (handler != null) {
						handler(context, null, context.CompletedSynchronously);
					}
					else {
						HandleOrphan(context);
					}
				}
			}
			finally {
				context.ClearBuffers();
				OnProcessFinished();
			}

			if (context.UnpackingBuffer.Length > 0) {
				// Subsequent request is already arrived.
				context.NextProcess = UnpackResponseHeader;
				return context.NextProcess(context);
			}
			else {
				// Try receive subsequent.
				return true;
			}
		}

		internal bool DumpCorrupttedData(ClientResponseContext context) {
			if (context.BytesTransferred == 0) {
				context.Clear();
				return false;
			}

			if (Manager.Configuration.DumpCorruptResponse) {
				using var dumpStream = OpenDumpStream(context.SessionStartedAt, context.RemoteEndPoint, context.SessionId, MessageType.Response, context.MessageId);

				dumpStream.Write(context.CurrentReceivingBuffer, context.CurrentReceivingBufferOffset, context.BytesTransferred);
				dumpStream.Flush();
			}

			context.ShiftCurrentReceivingBuffer();

			return true;
		}
	}
}
