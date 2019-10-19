using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace MsgPack.Rpc.Protocols.Filters
{
	/// <summary>
	///		Implements common functionalities of providers for <see cref="QuotaMessageFilter{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of <see cref="InboundMessageContext"/>.</typeparam>
	public abstract class QuotaMessageFilterProvider<T> : MessageFilterProvider<T>
		where T : InboundMessageContext
	{
		private readonly long _quota;

		/// <summary>
		///		Gets the quota.
		/// </summary>
		/// <value>
		///		The quota.
		/// </value>
		public long Quota
		{
			get { return this._quota; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="QuotaMessageFilter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="quota">The quota. <c>0</c> means no quota (infinite).</param>
		/// <exception cref="ArgumentOutOfRangeException">
		///		The value of <paramref name="quota"/> is negative.
		/// </exception>
		protected QuotaMessageFilterProvider( long quota )
		{
			if ( quota < 0 )
			{
				throw new ArgumentOutOfRangeException( "quota", "Quota cannot be negative." );
			}

			Contract.EndContractBlock();

			this._quota = quota;
		}
	}
}
