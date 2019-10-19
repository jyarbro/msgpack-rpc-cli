﻿using MsgPack.Rpc.Core.Protocols.Filters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

[module: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "MsgPack.Rpc.Core.Client.RpcClientConfiguration.#UdpTransportPoolProvider")]
[module: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "MsgPack.Rpc.Core.Client.RpcClientConfiguration.#RequestContextPoolProvider")]
[module: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "MsgPack.Rpc.Core.Client.RpcClientConfiguration.#TcpTransportPoolProvider")]
[module: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "MsgPack.Rpc.Core.Client.RpcClientConfiguration.#ResponseContextPoolProvider")]
[module: SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Scope = "member", Target = "MsgPack.Rpc.Core.Client.RpcClientConfiguration.#ToString`1(!!0,System.Text.StringBuilder)", Justification = "Boolean value should be lower case.")]
namespace MsgPack.Rpc.Core.Client {
	/// <summary>
	///		Represents client side configuration settings.
	/// </summary>
	public sealed partial class RpcClientConfiguration : FreezableObject {
		private static readonly RpcClientConfiguration _default = new RpcClientConfiguration().Freeze();

		/// <summary>
		///		Gets the default frozen instance.
		/// </summary>
		/// <value>
		///		The default frozen instance.
		///		This value will not be <c>null</c>.
		/// </value>
		public static RpcClientConfiguration Default {
			get {
				Contract.Ensures(Contract.Result<RpcClientConfiguration>() != null);

				return RpcClientConfiguration._default;
			}
		}

		private IList<MessageFilterProvider> _filterProviders = new List<MessageFilterProvider>();

		/// <summary>
		///		Gets the filter providers collection.
		/// </summary>
		/// <value>
		///		The filter providers collection. Default is empty.
		/// </value>
		public IList<MessageFilterProvider> FilterProviders {
			get { return this._filterProviders; }
		}

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcClientConfiguration"/> class.
		/// </summary>
		public RpcClientConfiguration() { }

		/// <summary>
		///		Creates the <see cref="ObjectPoolConfiguration"/> for the transport pool corresponds to values of this instance.
		/// </summary>
		/// <returns>
		///		The <see cref="ObjectPoolConfiguration"/> for the transport pool corresponds to values of this instance.
		///		This value will not be <c>null</c>.
		/// </returns>
		public ObjectPoolConfiguration CreateTransportPoolConfiguration() {
			Contract.Ensures(Contract.Result<ObjectPoolConfiguration>() != null);

			return new ObjectPoolConfiguration() { ExhausionPolicy = ExhausionPolicy.BlockUntilAvailable, MaximumPooled = this.MaximumConcurrentRequest, MinimumReserved = this.MinimumConcurrentRequest };
		}

		/// <summary>
		///		Creates the <see cref="ObjectPoolConfiguration"/> for the <see cref="MsgPack.Rpc.Core.Client.Protocols.ClientRequestContext"/> pool corresponds to values of this instance.
		/// </summary>
		/// <returns>
		///		The <see cref="ObjectPoolConfiguration"/> for the <see cref="MsgPack.Rpc.Core.Client.Protocols.ClientRequestContext"/> pool corresponds to values of this instance.
		///		This value will not be <c>null</c>.
		/// </returns>
		public ObjectPoolConfiguration CreateRequestContextPoolConfiguration() {
			Contract.Ensures(Contract.Result<ObjectPoolConfiguration>() != null);

			return new ObjectPoolConfiguration() { ExhausionPolicy = ExhausionPolicy.BlockUntilAvailable, MaximumPooled = this.MaximumConcurrentRequest, MinimumReserved = this.MinimumConcurrentRequest };
		}

		/// <summary>
		///		Creates the <see cref="ObjectPoolConfiguration"/> for the <see cref="MsgPack.Rpc.Core.Client.Protocols.ClientResponseContext"/> pool corresponds to values of this instance.
		/// </summary>
		/// <returns>
		///		The <see cref="ObjectPoolConfiguration"/> for the <see cref="MsgPack.Rpc.Core.Client.Protocols.ClientResponseContext"/> pool corresponds to values of this instance.
		///		This value will not be <c>null</c>.
		/// </returns>
		public ObjectPoolConfiguration CreateResponseContextPoolConfiguration() {
			Contract.Ensures(Contract.Result<ObjectPoolConfiguration>() != null);

			return new ObjectPoolConfiguration() { ExhausionPolicy = ExhausionPolicy.BlockUntilAvailable, MaximumPooled = this.MaximumConcurrentRequest, MinimumReserved = this.MinimumConcurrentRequest };
		}

		/// <summary>
		///		Clones all of the fields of this instance.
		/// </summary>
		/// <returns>
		///		The shallow copy of this instance.
		/// </returns>
		public RpcClientConfiguration Clone() {
			Contract.Ensures(Contract.Result<RpcClientConfiguration>() != null);
			Contract.Ensures(!Object.ReferenceEquals(Contract.Result<RpcClientConfiguration>(), this));
			Contract.Ensures(Contract.Result<RpcClientConfiguration>().IsFrozen == this.IsFrozen);

			return this.CloneCore() as RpcClientConfiguration;
		}

		/// <summary>
		///		Freezes this instance.
		/// </summary>
		/// <returns>
		///		This instance.
		/// </returns>
		public RpcClientConfiguration Freeze() {
			Contract.Ensures(Object.ReferenceEquals(Contract.Result<RpcClientConfiguration>(), this));
			Contract.Ensures(this.IsFrozen);

			return this.FreezeCore() as RpcClientConfiguration;
		}

		/// <summary>
		/// Gets the frozen copy of this instance.
		/// </summary>
		/// <returns>
		/// This instance if it is already frozen.
		/// Otherwise, frozen copy of this instance.
		/// </returns>
		public RpcClientConfiguration AsFrozen() {
			Contract.Ensures(Contract.Result<RpcClientConfiguration>() != null);
			Contract.Ensures(!Object.ReferenceEquals(Contract.Result<RpcClientConfiguration>(), this));
			Contract.Ensures(Contract.Result<RpcClientConfiguration>().IsFrozen);
			Contract.Ensures(this.IsFrozen == Contract.OldValue(this.IsFrozen));

			return this.AsFrozenCore() as RpcClientConfiguration;
		}

		/// <summary>
		///		Clones all of the fields of this instance.
		/// </summary>
		/// <returns>
		///		The shallow copy of this instance. Returned instance always is not frozen.
		/// </returns>
		protected override FreezableObject CloneCore() {
			var result = base.CloneCore() as RpcClientConfiguration;
			result._filterProviders = new List<MessageFilterProvider>(result._filterProviders);
			return result;
		}

		/// <summary>
		///		Freezes this instance.
		/// </summary>
		/// <returns>
		///		This instance.
		/// </returns>
		protected override FreezableObject FreezeCore() {
			var result = base.FreezeCore() as RpcClientConfiguration;
			result._filterProviders = new ReadOnlyCollection<MessageFilterProvider>(result._filterProviders);
			return result;
		}
	}
}
