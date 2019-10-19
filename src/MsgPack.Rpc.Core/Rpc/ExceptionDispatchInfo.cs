using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace MsgPack.Rpc.Core {
	internal sealed class ExceptionDispatchInfo {
		private static readonly Type[] _constructorParameterStringException = new[] { typeof(string), typeof(Exception) };
		private static readonly PropertyInfo _exceptionHResultProperty = typeof(Exception).GetProperty("HResult", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo _safeCreateMatroshikaMethod = typeof(ExceptionDispatchInfo).GetMethod("SafeCreateMatroshika", BindingFlags.Static | BindingFlags.NonPublic);
		private static readonly MethodInfo _safeCreateWrapperWin32ExceptionMethod = typeof(ExceptionDispatchInfo).GetMethod("SafeCreateWrapperWin32Exception", BindingFlags.Static | BindingFlags.NonPublic);

		private readonly Exception _source;

		private ExceptionDispatchInfo(Exception source) {
			if (source == null) {
				throw new ArgumentNullException("source");
			}

			Contract.EndContractBlock();

			_source = source;
			if (source is IStackTracePreservable preservable) {
				preservable.PreserveStackTrace();
			}
		}

		[ContractInvariantMethod]
		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "ContractInvariantMethod")]
		private void ObjectInvariant() {
			Contract.Invariant(_source != null);
		}

		public void Throw() {
			if (_source is IStackTracePreservable) {
				throw _source;
			}
			else {
				throw CreateMatroshika(_source);
			}
		}

		internal static Exception CreateMatroshika(Exception inner) {
			Contract.Requires(inner != null);
			Contract.Ensures(Contract.Result<Exception>() != null);

			var result = HandleKnownWin32Exception(inner);
			if (result != null) {
				return result;
			}

			result = TryCreateMatroshikaWithExternalExceptionMatroshka(inner);
			if (result != null) {
				return result;
			}

			result = HandleExternalExceptionInPartialTrust(inner);
			if (result != null) {
				return result;
			}

			return GetMatroshika(inner) ?? new TargetInvocationException(inner.Message, inner);
		}
		private static Exception HandleKnownWin32Exception(Exception inner) {
			if (inner is SocketException asSocketException) {
				var result = new WrapperSocketException(asSocketException);
				SetMatroshika(inner);
				return result;
			}

			if (inner is HttpListenerException asHttpListenerException) {
				var result = new WrapperHttpListenerException(asHttpListenerException);
				SetMatroshika(inner);
				return result;
			}

			if (inner is NetworkInformationException asNetworkInformationException) {
				var result = new WrapperNetworkInformationException(asNetworkInformationException);
				SetMatroshika(inner);
				return result;
			}

			if (inner is Win32Exception asWin32Exception) {
				if (_safeCreateWrapperWin32ExceptionMethod.IsSecuritySafeCritical) {
					var result = SafeCreateWrapperWin32Exception(asWin32Exception);
					return result;
				}
				else {
					return new TargetInvocationException(asWin32Exception.Message, asWin32Exception);
				}
			}

			return null;
		}

		private static Exception TryCreateMatroshikaWithExternalExceptionMatroshka(Exception inner) {
			// Try matroshika with HResult setting(requires full trust).
			if (_safeCreateMatroshikaMethod.IsSecuritySafeCritical) {
				if (inner is ExternalException asExternalException) {
					var matroshika = SafeCreateMatroshika(asExternalException);
					if (matroshika != null) {
						return matroshika;
					}
					else {
						// Fallback.
						return new TargetInvocationException(inner.Message, inner);
					}
				}
			}

			return null;
		}

		private static Exception HandleExternalExceptionInPartialTrust(Exception inner) {
			if (inner is COMException asCOMException) {
				var result = new WrapperCOMException(asCOMException.Message, asCOMException);
				SetMatroshika(inner);
				return result;
			}

			if (inner is SEHException asSEHException) {
				var result = new WrapperSEHException(asSEHException.Message, asSEHException);
				SetMatroshika(inner);
				return result;
			}

			if (inner is ExternalException asExternalException) {
				var result = new WrapperExternalException(asExternalException.Message, asExternalException);
				SetMatroshika(inner);
				return result;
			}

			return null;
		}

		[SecuritySafeCritical]
		private static Exception SafeCreateMatroshika(ExternalException inner) {
			var result = GetMatroshika(inner);
			if (result != null) {
				_exceptionHResultProperty.SetValue(result, Marshal.GetHRForException(inner), null);
			}

			return result;
		}

		[SecuritySafeCritical]
		private static WrapperWin32Exception SafeCreateWrapperWin32Exception(Win32Exception inner) {
			var result = new WrapperWin32Exception(inner.Message, inner);
			SetMatroshika(inner);
			return result;
		}

		private static Exception GetMatroshika(Exception inner) {
			var ctor = inner.GetType().GetConstructor(_constructorParameterStringException);
			if (ctor == null) {
				return null;
			}
			var result = ctor.Invoke(new object[] { inner.Message, inner }) as Exception;
			SetMatroshika(inner);
			return result;
		}
		private static void SetMatroshika(Exception exception) {
			exception.Data[ExceptionModifiers.IsMatrioshkaInner] = null;
		}

		public static ExceptionDispatchInfo Capture(Exception source) {
			// TODO: Capture Watson information.
			return new ExceptionDispatchInfo(source);
		}

		[Serializable]
		private sealed class WrapperExternalException : ExternalException {
			public WrapperExternalException(string message, ExternalException inner)
				: base(message, inner) {
				HResult = inner.ErrorCode;
			}

			private WrapperExternalException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		}

		[Serializable]
		private sealed class WrapperCOMException : COMException {
			public WrapperCOMException(string message, COMException inner)
				: base(message, inner) {
				HResult = inner.ErrorCode;
			}

			private WrapperCOMException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		}

		[Serializable]
		private sealed class WrapperSEHException : SEHException {
			public WrapperSEHException(string message, SEHException inner)
				: base(message, inner) {
				HResult = inner.ErrorCode;
			}

			private WrapperSEHException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		}

		[Serializable]
		[SecuritySafeCritical]
		private sealed class WrapperWin32Exception : Win32Exception {
			public WrapperWin32Exception(string message, Win32Exception inner)
				: base(message, inner) {
				HResult = inner.ErrorCode;
			}

			private WrapperWin32Exception(SerializationInfo info, StreamingContext context) : base(info, context) { }
		}

		[Serializable]
		private sealed class WrapperHttpListenerException : HttpListenerException {
			private readonly string _innerStackTrace;

			public sealed override string StackTrace => string.Join(
							_innerStackTrace,
							"   --- End of preserved stack trace ---",
							Environment.NewLine,
							base.StackTrace
						);

			public WrapperHttpListenerException(HttpListenerException inner)
				: base(inner.ErrorCode) {
				_innerStackTrace = inner.StackTrace;
			}

			private WrapperHttpListenerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		}

		[Serializable]
		private sealed class WrapperNetworkInformationException : NetworkInformationException {
			private readonly string _innerStackTrace;

			public sealed override string StackTrace => string.Join(
							_innerStackTrace,
							"   --- End of preserved stack trace ---",
							Environment.NewLine,
							base.StackTrace
						);

			public WrapperNetworkInformationException(NetworkInformationException inner)
				: base(inner.ErrorCode) {
				_innerStackTrace = inner.StackTrace;
			}

			private WrapperNetworkInformationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		}

		[Serializable]
		private sealed class WrapperSocketException : SocketException {
			private readonly string _innerStackTrace;

			public sealed override string StackTrace => string.Join(
							_innerStackTrace,
							"   --- End of preserved stack trace ---",
							Environment.NewLine,
							base.StackTrace
						);

			public WrapperSocketException(SocketException inner)
				: base(inner.ErrorCode) {
				_innerStackTrace = inner.StackTrace;
			}

			private WrapperSocketException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		}
	}
}