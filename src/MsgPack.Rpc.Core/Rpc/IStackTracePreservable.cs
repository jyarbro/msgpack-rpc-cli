using System;

namespace MsgPack.Rpc {
	/// <summary>
	///		Marks the <see cref="Exception"/> can preserve its stack trace.
	/// </summary>
#if NET_4_5
	[Obsolete( "This interface is no longer used." )]
#endif
	public interface IStackTracePreservable {
		/// <summary>
		///		Preserves the current stack trace.
		/// </summary>
		void PreserveStackTrace();
	}
}
