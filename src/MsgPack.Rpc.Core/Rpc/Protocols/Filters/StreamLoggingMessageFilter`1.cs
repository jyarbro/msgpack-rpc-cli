using MsgPack.Rpc.Diagnostics;
using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace MsgPack.Rpc.Protocols.Filters {
	/// <summary>
	///		Implements common functionalities of inbound message stream logging filter.
	/// </summary>
	/// <typeparam name="T">The type of <see cref="InboundMessageContext"/>.</typeparam>
	public abstract class StreamLoggingMessageFilter<T> : MessageFilter<T>
		where T : InboundMessageContext {
		private readonly IMessagePackStreamLogger _logger;

		/// <summary>
		///		Initializes a new instance of the <see cref="StreamLoggingMessageFilter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="logger">The <see cref="IMessagePackStreamLogger"/> which will be log sink.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="logger"/> is <c>null</c>.
		/// </exception>
		protected StreamLoggingMessageFilter(IMessagePackStreamLogger logger) {
			if (logger == null) {
				throw new ArgumentNullException("logger");
			}

			Contract.EndContractBlock();

			this._logger = logger;
		}

		/// <summary>
		///		Applies this filter to the specified message.
		/// </summary>
		/// <param name="context">The message context. This value is not <c>null</c>.</param>
		protected override void ProcessMessageCore(T context) {
			this._logger.Write(context.SessionStartedAt, context.RemoteEndPoint, context.ReceivedData.SelectMany(s => s.AsEnumerable()));
		}
	}
}