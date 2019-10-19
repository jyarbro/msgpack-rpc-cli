using System;

namespace MsgPack.Rpc {
	internal static class ExceptionModifiers {
		public static readonly object IsMatrioshkaInner = String.Intern(typeof(ExceptionModifiers).FullName + ".IsMatrioshkaInner");
	}
}
