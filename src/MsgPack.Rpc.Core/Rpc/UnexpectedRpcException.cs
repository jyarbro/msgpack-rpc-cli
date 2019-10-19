using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace MsgPack.Rpc.Core {
	/// <summary>
	///		Exception in unexpected error.
	/// </summary>
	/// <remarks>
	///		If server returns error but its structure is not compatible with de-facto standard, client library will throw this exception.
	/// </remarks>
	[Serializable]
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
		public MessagePackObject Error => _error;

		// NOT readonly for safe deserialization
		private MessagePackObject _errorDetail;

		/// <summary>
		///		Get the value of return field of response in error.
		/// </summary>
		/// <value>
		///		Value of return field of response in error.
		///		This value may be nil, but server can set any value.
		/// </value>
		public MessagePackObject ErrorDetail => _errorDetail;

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
			_error = error;
			_errorDetail = errorDetail;
		}

		/// <summary>
		///		When overridden on the derived class, handles <see cref="E:Exception.SerializeObjectState"/> event to add type-specified serialization state.
		/// </summary>
		/// <param name="sender">The <see cref="Exception"/> instance itself.</param>
		/// <param name="e">
		///		The <see cref="SafeSerializationEventArgs"/> instance containing the event data.
		///		The overriding method adds its internal state to this object via <see cref="M:SafeSerializationEventArgs.AddSerializedState"/>.
		///	</param>
		/// <seealso cref="ISafeSerializationData"/>
		protected override void OnSerializeObjectState(object sender, SafeSerializationEventArgs e) {
			base.OnSerializeObjectState(sender, e);
			e.AddSerializedState(
				new SerializedState() {
					Error = _error,
					ErrorDetail = _errorDetail
				}
			);
		}

		[Serializable]
		private sealed class SerializedState : ISafeSerializationData {
			public MessagePackObject Error;
			public MessagePackObject ErrorDetail;

			public void CompleteDeserialization(object deserialized) {
				var enclosing = deserialized as UnexpectedRpcException;
				enclosing._error = Error;
				enclosing._errorDetail = ErrorDetail;
			}
		}
	}
}
