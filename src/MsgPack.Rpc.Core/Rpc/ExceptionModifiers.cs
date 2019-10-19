using System;

namespace MsgPack.Rpc.Core {
	internal static class ExceptionModifiers {
		public static readonly object IsMatrioshkaInner = String.Intern(typeof(ExceptionModifiers).FullName + ".IsMatrioshkaInner");
	}
}
