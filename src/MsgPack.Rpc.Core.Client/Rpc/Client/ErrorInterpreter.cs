using MsgPack.Rpc.Core.Client.Protocols;
using System.Diagnostics.Contracts;
using System.Linq;

namespace MsgPack.Rpc.Core.Client {
	/// <summary>
	///		Interprets error stream.
	/// </summary>
	internal static class ErrorInterpreter {
		/// <summary>
		///		Unpacks <see cref="RpcErrorMessage"/> from stream in the specified context.
		/// </summary>
		/// <param name="context"><see cref="ClientResponseContext"/> which stores serialized error.</param>
		/// <returns>An unpacked <see cref="RpcErrorMessage"/>.</returns>
		internal static RpcErrorMessage UnpackError(ClientResponseContext context) {
			Contract.Assert(context != null);
			Contract.Assert(context.ErrorBuffer != null);
			Contract.Assert(context.ErrorBuffer.Length > 0);
			Contract.Assert(context.ResultBuffer != null);
			Contract.Assert(context.ResultBuffer.Length > 0);

			MessagePackObject error;
			try {
				error = Unpacking.UnpackObject(context.ErrorBuffer);
			}
			catch (UnpackException) {
				error = new MessagePackObject(context.ErrorBuffer.GetBuffer().SelectMany(segment => segment.AsEnumerable()).ToArray());
			}

			if (error.IsNil) {
				return RpcErrorMessage.Success;
			}

			bool isUnknown = false;
			RpcError errorIdentifier;
			if (error.IsTypeOf<string>().GetValueOrDefault()) {
				var asString = error.AsString();
				errorIdentifier = RpcError.FromIdentifier(asString, null);
				// Check if the error is truely Unexpected error.
				isUnknown = errorIdentifier.ErrorCode == RpcError.Unexpected.ErrorCode && asString != RpcError.Unexpected.Identifier;
			}
			else if (error.IsTypeOf<int>().GetValueOrDefault()) {
				errorIdentifier = RpcError.FromIdentifier(null, error.AsInt32());
			}
			else {
				errorIdentifier = RpcError.Unexpected;
				isUnknown = true;
			}

			MessagePackObject detail;
			if (context.ResultBuffer.Length == 0) {
				detail = MessagePackObject.Nil;
			}
			else {
				try {
					detail = Unpacking.UnpackObject(context.ResultBuffer);
				}
				catch (UnpackException) {
					detail = new MessagePackObject(context.ResultBuffer.GetBuffer().SelectMany(segment => segment.AsEnumerable()).ToArray());
				}
			}

			if (isUnknown) {
				// Unknown error, the error should contain original Error field as message.
				if (detail.IsNil) {
					return new RpcErrorMessage(errorIdentifier, error.AsString(), null);
				}
				else {
					var details = new MessagePackObjectDictionary(2);
					details[RpcException.MessageKeyUtf8] = error;
					details[RpcException.DebugInformationKeyUtf8] = detail;
					return new RpcErrorMessage(errorIdentifier, new MessagePackObject(details, true));
				}
			}
			else {
				return new RpcErrorMessage(errorIdentifier, detail);
			}
		}
	}
}
