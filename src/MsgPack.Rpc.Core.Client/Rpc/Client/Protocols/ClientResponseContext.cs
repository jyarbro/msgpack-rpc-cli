using MsgPack.Rpc.Core.Protocols;
using System;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc.Core.Client.Protocols {
	/// <summary>
	///		Represents context information for the client side response.
	/// </summary>
	public sealed class ClientResponseContext : InboundMessageContext {
		/// <summary>
		///		Next (that is, resuming) process on the deserialization pipeline.
		/// </summary>
		internal Func<ClientResponseContext, bool> NextProcess;

		internal long ErrorStartAt;

		/// <summary>
		///		Subtree <see cref="Unpacker"/> to parse error value as opaque sequence.
		/// </summary>
		internal ByteArraySegmentStream ErrorBuffer;

		internal long ResultStartAt;

		/// <summary>
		///		Subtree <see cref="Unpacker"/> to parse return value as opaque sequence.
		/// </summary>
		internal ByteArraySegmentStream ResultBuffer;

		/// <summary>
		///		Initializes a new instance of the <see cref="ClientResponseContext"/> class with default settings.
		/// </summary>
		public ClientResponseContext()
			: this(null) {
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="ClientResponseContext"/> class with specified configuration.
		/// </summary>
		/// <param name="configuration">
		///		An <see cref="RpcClientConfiguration"/> to tweak this instance initial state.
		/// </param>
		public ClientResponseContext(RpcClientConfiguration configuration)
			: base((configuration ?? RpcClientConfiguration.Default).InitialReceiveBufferLength) {
			ErrorStartAt = -1;
			ResultStartAt = -1;
		}

		internal long? SkipResultSegment() {
#if DEBUG
			Contract.Assert(ResultStartAt > -1);
#endif
			return SkipHeader(ResultStartAt);
		}

		internal long? SkipErrorSegment() {
#if DEBUG
			Contract.Assert(ErrorStartAt > -1);
#endif
			return SkipHeader(ErrorStartAt);
		}

		private long? SkipHeader(long origin) {
			var result = HeaderUnpacker.Skip();
			if (result == null) {
				// Revert buffer position to handle next attempt.
				UnpackingBuffer.Position = origin;
			}

			return result;
		}

		/// <summary>
		///		Sets the bound <see cref="ClientTransport"/>.
		/// </summary>
		/// <param name="transport">The binding transport.</param>
		internal void SetTransport(ClientTransport transport) {
			Contract.Requires(transport != null);

			NextProcess = transport.UnpackResponseHeader;
			base.SetTransport(transport);
		}

		private static bool InvalidFlow(ClientResponseContext context) {
			throw new InvalidOperationException("Invalid state transition.");
		}

		/// <summary>
		///		Clears this instance internal buffers for reuse.
		/// </summary>
		internal sealed override void Clear() {
			ClearBuffers();
			NextProcess = InvalidFlow;
			base.Clear();
		}

		/// <summary>
		///		Clears the buffers to deserialize message.
		/// </summary>
		internal override void ClearBuffers() {
			if (ErrorBuffer != null) {
				ErrorBuffer.Dispose();
				ErrorBuffer = null;
			}

			if (ResultBuffer != null) {
				ResultBuffer.Dispose();
				ResultBuffer = null;
			}

			ErrorStartAt = -1;
			ResultStartAt = -1;

			base.ClearBuffers();
		}
	}
}
