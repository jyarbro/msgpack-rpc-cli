using System;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace MsgPack.Rpc
{
	/// <summary>
	///		Marks the type represents service contract for the MessagePack-RPC.
	/// </summary>
	[AttributeUsage( AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = true )]
	public sealed class MessagePackRpcServiceContractAttribute : Attribute
	{
		private string _name;

		/// <summary>
		///		Gets the name of the RPC procedure.
		/// </summary>
		/// <value>
		///		The name of the RPC procedure.
		///		If the value is <c>null</c>, empty or consisted by whitespace characters only, the qualified type name will be used.
		/// </value>
		public string Name
		{
			get { return this._name; }
			set { this._name = value; }
		}

		/// <summary>
		///		Gets or sets the version of the RPC procedure.
		/// </summary>
		/// <value>
		///		The version of the RPC procedure.
		/// </value>
		public int Version { get; set; }

		/// <summary>
		///		Initializes a new instance of the <see cref="MessagePackRpcServiceContractAttribute"/> class.
		/// </summary>
		public MessagePackRpcServiceContractAttribute() { }

		internal string ToServiceId( Type serviceType )
		{
			return
				ServiceIdentifier.CreateServiceId(
					String.IsNullOrWhiteSpace( this._name ) ? ServiceIdentifier.TruncateGenericsSuffix( serviceType.Name ) : this._name,
					this.Version
				);
		}
	}
}