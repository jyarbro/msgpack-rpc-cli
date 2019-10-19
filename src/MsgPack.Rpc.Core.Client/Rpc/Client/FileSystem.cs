using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MsgPack.Rpc.Core.Client {
	internal static class FileSystem {
		private static readonly Regex _invalidPathChars =
			new Regex(
				"[" + Regex.Escape(String.Join(String.Empty, Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).Distinct())) + "]",
#if !SILVERLIGHT
				 RegexOptions.Compiled |
#endif
				 RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Singleline
			);

		public static string EscapeInvalidPathChars(string value, string replacement) {
			if (value == null) {
				throw new ArgumentNullException("value");
			}

#if !SILVERIGHT
			return _invalidPathChars.Replace(value, replacement ?? String.Empty);
#else
			return "." + Path.DirectorySepartorChar + _invalidPathChars.Replace( value, replacement ?? String.Empty );
#endif
		}
	}
}
