using MsgPack.Rpc.Protocols.Filters;
using System;

namespace MsgPack.Rpc.Client.Protocols.Filters {
	/// <summary>
	///		<see cref="QuotaMessageFilterProvider{T}"/> for <see cref="ClientResponseContext"/>.
	/// </summary>
	public sealed class ClientQuotaMessageFilterProvider : QuotaMessageFilterProvider<ClientResponseContext> {
		private readonly ClientQuotaMessageFilter _filterInstance;

		/// <summary>
		///		Initializes a new instance of the <see cref="ClientQuotaMessageFilterProvider"/> class.
		/// </summary>
		/// <param name="quota">The quota. <c>0</c> means no quota (infinite).</param>
		/// <exception cref="ArgumentOutOfRangeException">
		///		The value of <paramref name="quota"/> is negative.
		/// </exception>
		public ClientQuotaMessageFilterProvider(long quota)
			: base(quota) {
			this._filterInstance = new ClientQuotaMessageFilter(this.Quota);
		}

		/// <summary>
		///		Returns a <see cref="MessageFilter{T}"/> instance.
		/// </summary>
		/// <param name="location">The location of the filter to be applied.</param>
		/// <returns>A <see cref="MessageFilter{T}"/> instance.</returns>
		public override MessageFilter<ClientResponseContext> GetFilter(MessageFilteringLocation location) {
			if (location != MessageFilteringLocation.BeforeDeserialization) {
				return null;
			}
			else {
				return this._filterInstance;
			}
		}
	}
}
