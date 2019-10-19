﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace MsgPack.Rpc {
	/// <summary>
	///		Exception thrown when server is (maybe temporaly) unavailable.
	/// </summary>
#if !SILVERLIGHT
	[Serializable]
#endif
	[SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly", Justification = "Using ISafeSerializationData.")]
	[SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification = "Using ISafeSerializationData.")]
	public sealed class RpcServerUnavailableException : RpcException {
		/// <summary>
		///		Initializes a new instance of the <see cref="RpcServerUnavailableException"/> class with the default error message.
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="RpcError.RemoteRuntimeError"/> is used.
		///	</param>
		public RpcServerUnavailableException(RpcError rpcError) : this(rpcError, null, null, null) { }

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcServerUnavailableException"/> class with a specified error message.
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="RpcError.RemoteRuntimeError"/> is used.
		///	</param>
		/// <param name="message">
		///		Error message to desribe condition. Note that this message should not include security related information.
		///	</param>
		/// <param name="debugInformation">
		///		Debug information of error.
		///		This value can be null for security reason, and its contents are for developers, not end users.
		/// </param>
		/// <remarks>
		///		<para>
		///			For example, if some exception is occurred in server application,
		///			the value of <see cref="Exception.ToString()"/> should specify for <paramref name="debugInformation"/>.
		///			And then, user-friendly, safe message should be specified to <paramref name="message"/> like 'Internal Error."
		///		</para>
		///		<para>
		///			MessagePack-RPC for CLI runtime does not propagate <see cref="RpcException.DebugInformation"/> for remote endpoint.
		///			So you should specify some error handler to instrument it (e.g. logging handler).
		///		</para>
		/// </remarks>		
		public RpcServerUnavailableException(RpcError rpcError, string message, string debugInformation)
			: this(rpcError, message, debugInformation, null) { }

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcServerUnavailableException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception. 
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="RpcError.RemoteRuntimeError"/> is used.
		///	</param>
		/// <param name="message">
		///		Error message to desribe condition. Note that this message should not include security related information.
		///	</param>
		/// <param name="debugInformation">
		///		Debug information of error.
		///		This value can be null for security reason, and its contents are for developers, not end users.
		/// </param>
		/// <param name="inner">
		///		Exception which caused this error.
		/// </param>
		/// <remarks>
		///		<para>
		///			For example, if some exception is occurred in server application,
		///			the value of <see cref="Exception.ToString()"/> should specify for <paramref name="debugInformation"/>.
		///			And then, user-friendly, safe message should be specified to <paramref name="message"/> like 'Internal Error."
		///		</para>
		///		<para>
		///			MessagePack-RPC for CLI runtime does not propagate <see cref="RpcException.DebugInformation"/> for remote endpoint.
		///			So you should specify some error handler to instrument it (e.g. logging handler).
		///		</para>
		/// </remarks>
		public RpcServerUnavailableException(RpcError rpcError, string message, string debugInformation, Exception inner)
			: base(rpcError ?? RpcError.ServerError, message, debugInformation, inner) { }

		/// <summary>
		///		Initializes a new instance of the <see cref="RpcServerUnavailableException"/> class with the unpacked data.
		/// </summary>
		/// <param name="rpcError">
		///		Metadata of error. If you specify null, <see cref="RpcError.RemoteRuntimeError"/> is used.
		///	</param>
		/// <param name="unpackedException">
		///		Exception data from remote MessagePack-RPC server.
		///	</param>
		/// <exception cref="SerializationException">
		///		Cannot deserialize instance from <paramref name="unpackedException"/>.
		/// </exception>
		internal RpcServerUnavailableException(RpcError rpcError, MessagePackObject unpackedException) : base(rpcError, unpackedException) { }

#if MONO
		/// <summary>
		///		Initializes a new instance with serialized data. 
		/// </summary>
		/// <param name="info">
		///		The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown. 
		/// </param>
		/// <param name="context">
		///		The <see cref="StreamingContext"/> that contains contextual information about the source or destination.
		/// </param>
		/// <exception cref="T:System.ArgumentNullException">
		///   <paramref name="info"/><paramref name="info"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="T:System.Runtime.Serialization.SerializationException">
		///		The class name is <c>null</c>.
		///		Or <see cref="P:System.Exception.HResult"/> is zero(0).
		/// </exception>
		/// <permission cref="System.Security.Permissions.SecurityPermission"><c>LinkDemand</c>, <c>Flags=SerializationFormatter</c></permission>
		[SecurityPermission( SecurityAction.LinkDemand, SerializationFormatter = true )]
		private RpcServerUnavailableException( SerializationInfo info, StreamingContext context )
			: base( info, context ) { }
#endif
	}
}