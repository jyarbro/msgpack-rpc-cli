using MsgPack.Rpc.Core.Diagnostics;
using System;
using System.Diagnostics.Contracts;

namespace MsgPack.Rpc.Core.Protocols.Filters {
	/// <summary>
	///		Implements common functionalities of providers for <see cref="StreamLoggingMessageFilter{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of <see cref="InboundMessageContext"/>.</typeparam>
	public abstract class StreamLoggingMessageFilterProvider<T> : MessageFilterProvider<T>
		where T : InboundMessageContext {
		private readonly IMessagePackStreamLogger _logger;

		/// <summary>
		///		Gets the logger which is the <see cref="IMessagePackStreamLogger"/> which will be log sink.
		/// </summary>
		/// <value>
		///		The logger which is the <see cref="IMessagePackStreamLogger"/> which will be log sink.
		/// </value>
		protected IMessagePackStreamLogger Logger {
			get { return this._logger; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="StreamLoggingMessageFilter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="logger">The <see cref="IMessagePackStreamLogger"/> which will be log sink.</param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="logger"/> is <c>null</c>.
		/// </exception>
		protected StreamLoggingMessageFilterProvider(IMessagePackStreamLogger logger) {
			if (logger == null) {
				throw new ArgumentNullException("logger");
			}

			Contract.EndContractBlock();

			this._logger = logger;
		}
	}
}
