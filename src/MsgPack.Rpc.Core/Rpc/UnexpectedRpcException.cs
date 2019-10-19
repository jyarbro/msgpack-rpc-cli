﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace MsgPack.Rpc {
	/// <summary>
	///		Exception in unexpected error.
	/// </summary>
	/// <remarks>
	///		If server returns error but its structure is not compatible with de-facto standard, client library will throw this exception.
	/// </remarks>
#if !SILVERLIGHT
	[Serializable]
#endif
	[SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly", Justification = "Using ISafeSerializationData.")]
	[SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification = "Using ISafeSerializationData.")]
	public sealed class UnexpectedRpcException : RpcException {
		private const string _errorKey = "Error";
		private const string _errorDetailKey = "ErrorDetail";

		// NOT readonly for safe deserialization
		private MessagePackObject _error;

		/// <summary>
		///		Get the value of error field of response.
		/// </summary>
		/// <value>
		///		Value of error field of response.
		///		This value is not nil, but its content is arbitary.
		/// </value>
		public MessagePackObject Error {
			get { return this._error; }
		}

		// NOT readonly for safe deserialization
		private MessagePackObject _errorDetail;

		/// <summary>
		///		Get the value of return field of response in error.
		/// </summary>
		/// <value>
		///		Value of return field of response in error.
		///		This value may be nil, but server can set any value.
		/// </value>
		public MessagePackObject ErrorDetail {
			get { return this._errorDetail; }
		}

		/// <summary>
		///		Initialize new instance.
		/// </summary>
		/// <param name="error">
		///		Value of error field of response.
		///		This value is not nil, but its content is arbitary.
		/// </param>
		/// <param name="errorDetail">
		///		Value of return field of response in error.
		///		This value may be nil, but server can set any value.
		/// </param>
		public UnexpectedRpcException(MessagePackObject error, MessagePackObject errorDetail)
			: base(RpcError.Unexpected, RpcError.Unexpected.DefaultMessage, null) {
			this._error = error;
			this._errorDetail = errorDetail;
		}

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
		///		Or <see cref="P:Error"/> is <c>null</c>.
		///		Or <see cref="P:ErrorDetail"/> is <c>null</c>.
		/// </exception>
		/// <permission cref="System.Security.Permissions.SecurityPermission"><c>LinkDemand</c>, <c>Flags=SerializationFormatter</c></permission>
		[SecurityPermission( SecurityAction.LinkDemand, SerializationFormatter = true )]
		private UnexpcetedRpcException( SerializationInfo info, StreamingContext context )
			: base( info, context )
		{
			this._error = ( MessagePackObject )info.GetValue( _errorKey, typeof( MessagePackObject ) );
			this._errorDetail = ( MessagePackObject )info.GetValue( _errorDetailKey, typeof( MessagePackObject ) );
		}

		/// <summary>
		///		When overridden in a derived class, sets the <see cref="SerializationInfo"/> with information about the exception.
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
		/// <permission cref="System.Security.Permissions.SecurityPermission"><c>LinkDemand</c>, <c>Flags=SerializationFormatter</c></permission>
		[SecurityPermission( SecurityAction.LinkDemand, SerializationFormatter = true )]
		public override void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			base.GetObjectData( info, context );

			info.AddValue( _errorKey, this._error );
			info.AddValue( _errorDetailKey, this._errorDetail );
		}
#endif

#if !SILVERLIGHT && !MONO
		/// <summary>
		///		When overridden on the derived class, handles <see cref="E:Exception.SerializeObjectState"/> event to add type-specified serialization state.
		/// </summary>
		/// <param name="sender">The <see cref="Exception"/> instance itself.</param>
		/// <param name="e">
		///		The <see cref="System.Runtime.Serialization.SafeSerializationEventArgs"/> instance containing the event data.
		///		The overriding method adds its internal state to this object via <see cref="M:SafeSerializationEventArgs.AddSerializedState"/>.
		///	</param>
		/// <seealso cref="ISafeSerializationData"/>
		protected override void OnSerializeObjectState(object sender, SafeSerializationEventArgs e) {
			base.OnSerializeObjectState(sender, e);
			e.AddSerializedState(
				new SerializedState() {
					Error = this._error,
					ErrorDetail = this._errorDetail
				}
			);
		}

		[Serializable]
		private sealed class SerializedState : ISafeSerializationData {
			public MessagePackObject Error;
			public MessagePackObject ErrorDetail;

			public void CompleteDeserialization(object deserialized) {
				var enclosing = deserialized as UnexpectedRpcException;
				enclosing._error = this.Error;
				enclosing._errorDetail = this.ErrorDetail;
			}
		}
#endif
	}
}