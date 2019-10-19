﻿using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Scope = "member", Target = "MsgPack.Rpc.Core.ObjectPoolConfiguration.#ToString`1(!!0,System.Text.StringBuilder)", Justification = "Boolean value should be lower case.")]

namespace MsgPack.Rpc.Core {
	/// <summary>
	///		Represents configuratin of the <see cref="ObjectPool{T}"/>.
	/// </summary>
	public sealed partial class ObjectPoolConfiguration : FreezableObject {
		private static readonly ObjectPoolConfiguration _default = new ObjectPoolConfiguration().AsFrozen();

		/// <summary>
		///		Gets the default frozen instance.
		/// </summary>
		/// <value>
		///		The default frozen instance.
		///		This value will not be <c>null</c>.
		/// </value>
		public static ObjectPoolConfiguration Default {
			get { return _default; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectPoolConfiguration"/> class.
		/// </summary>
		public ObjectPoolConfiguration() { }

		/// <summary>
		///		Clones all of the fields of this instance.
		/// </summary>
		/// <returns>
		///		The shallow copy of this instance.
		/// </returns>
		public ObjectPoolConfiguration Clone() {
			return this.CloneCore() as ObjectPoolConfiguration;
		}

		/// <summary>
		///		Freezes this instance.
		/// </summary>
		/// <returns>
		///		This instance.
		/// </returns>
		public ObjectPoolConfiguration Freeze() {
			return this.FreezeCore() as ObjectPoolConfiguration;
		}

		/// <summary>
		/// Gets the frozen copy of this instance.
		/// </summary>
		/// <returns>
		/// This instance if it is already frozen.
		/// Otherwise, frozen copy of this instance.
		/// </returns>
		public ObjectPoolConfiguration AsFrozen() {
			return this.AsFrozenCore() as ObjectPoolConfiguration;
		}
	}
}
