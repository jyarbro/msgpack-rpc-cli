using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MsgPack.Rpc
{
	partial class RpcException : IStackTracePreservable
	{
		private List<string> _preservedStackTrace;

		[SuppressMessage( "Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "Infrastracture." )]
		void IStackTracePreservable.PreserveStackTrace()
		{
			if ( this._preservedStackTrace == null )
			{
				this._preservedStackTrace = new List<string>();
			}

			this._preservedStackTrace.Add(
#if !SILVERLIGHT
				new StackTrace( this, true )
#else
				new StackTrace( this )
#endif
				.ToString() );
		}

		/// <summary>
		///		Gets a string representation of the immediate frames on the call stack.
		/// </summary>
		/// <returns>A string that describes the immediate frames of the call stack.</returns>
		public override string StackTrace
		{
			get
			{
				if ( this._preservedStackTrace == null || this._preservedStackTrace.Count == 0 )
				{
					return base.StackTrace;
				}

				var buffer = new StringBuilder();
				foreach ( var preserved in this._preservedStackTrace )
				{
					buffer.Append( preserved );
					buffer.AppendLine( "   --- End of preserved stack trace ---" );
				}

				buffer.Append( base.StackTrace );
				return buffer.ToString();
			}
		}
	}
}
