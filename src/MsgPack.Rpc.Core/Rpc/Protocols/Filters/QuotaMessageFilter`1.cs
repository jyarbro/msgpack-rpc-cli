using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace MsgPack.Rpc.Core.Protocols.Filters {
	/// <summary>
	///		Implements common functionalities of quota filter for inbound message.
	/// </summary>
	/// <typeparam name="T">The type of <see cref="InboundMessageContext"/>.</typeparam>
	public abstract class QuotaMessageFilter<T> : MessageFilter<T>
			where T : InboundMessageContext {
		private readonly long _quota;

		/// <summary>
		///		Gets the quota.
		/// </summary>
		/// <value>
		///		The quota.
		/// </value>
		public long Quota {
			get { return this._quota; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="QuotaMessageFilter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="quota">The quota. <c>0</c> means no quota (infinite).</param>
		/// <exception cref="ArgumentOutOfRangeException">
		///		The value of <paramref name="quota"/> is negative.
		/// </exception>
		protected QuotaMessageFilter(long quota) {
			if (quota < 0) {
				throw new ArgumentOutOfRangeException("quota", "Quota cannot be negative.");
			}

			Contract.EndContractBlock();

			this._quota = quota;
		}

		/// <summary>
		///		Applies this filter to the specified message.
		/// </summary>
		/// <param name="context">The message context. This value is not <c>null</c>.</param>
		protected override void ProcessMessageCore(T context) {
			if (this._quota == 0) {
				// Infinite.
				return;
			}

			long length = context.ReceivedData.Sum(s => (long)s.Count);

			if (length > this._quota) {
				throw RpcError.MessageTooLargeError.ToException(
					new MessagePackObject(
						new MessagePackObjectDictionary(2)
						{
							{ "ActualLength", length },
							{ "Quota", this._quota }
						},
						true
					)
				);
			}
		}
	}
}
