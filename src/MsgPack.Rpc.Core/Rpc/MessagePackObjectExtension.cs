using System;

namespace MsgPack.Rpc.Core {
	internal static class MessagePackObjectExtension {
		public static string GetString(this MessagePackObject source, MessagePackObject key) {
			if (source.IsDictionary) {
				MessagePackObject value;
				if (source.AsDictionary().TryGetValue(key, out value) && value.IsTypeOf<string>().GetValueOrDefault()) {
					return value.AsString();
				}
			}

			return null;
		}

		public static TimeSpan? GetTimeSpan(this MessagePackObject source, MessagePackObject key) {
			if (source.IsDictionary) {
				MessagePackObject value;
				if (source.AsDictionary().TryGetValue(key, out value) && value.IsTypeOf<Int64>().GetValueOrDefault()) {
					return new TimeSpan(value.AsInt64());
				}
			}

			return null;
		}
	}
}
