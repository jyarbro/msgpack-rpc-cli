using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace MsgPack.Rpc.Core {
	[DebuggerTypeProxy(typeof(DebuggerProxy))]
	internal sealed class ByteArraySegmentStream : Stream {
		readonly IList<ArraySegment<byte>> _segments;

		int _segmentIndex;
		int _offsetInCurrentSegment;

		public sealed override bool CanRead => true;

		public sealed override bool CanSeek => true;

		public sealed override bool CanWrite => false;

		public sealed override long Length => _segments.Sum(item => (long)item.Count);

		long _position;

		public sealed override long Position {
			get {
				return _position;
			}
			set {
				if (value < 0) {
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				Seek(value - _position);
			}
		}

		public ByteArraySegmentStream(IList<ArraySegment<byte>> underlying) {
			_segments = underlying;
		}

		public sealed override int Read(byte[] buffer, int offset, int count) {
			var remains = count;
			var result = 0;
			while (0 < remains && _segmentIndex < _segments.Count) {
				var copied = _segments[_segmentIndex].CopyTo(_offsetInCurrentSegment, buffer, offset + result, remains);
				result += copied;
				remains -= copied;
				_offsetInCurrentSegment += copied;

				if (_offsetInCurrentSegment == _segments[_segmentIndex].Count) {
					_segmentIndex++;
					_offsetInCurrentSegment = 0;
				}

				_position += copied;
			}

			return result;
		}

		public sealed override long Seek(long offset, SeekOrigin origin) {
			var length = Length;
			long offsetFromCurrent;
			switch (origin) {
				case SeekOrigin.Begin: {
					offsetFromCurrent = offset - _position;
					break;
				}
				case SeekOrigin.Current: {
					offsetFromCurrent = offset;
					break;
				}
				case SeekOrigin.End: {
					offsetFromCurrent = length + offset - _position;
					break;
				}
				default: {
					throw new ArgumentOutOfRangeException(nameof(origin));
				}
			}

			if (offsetFromCurrent + _position < 0 || length < offsetFromCurrent + _position) {
				throw new ArgumentOutOfRangeException(nameof(offset));
			}

			Seek(offsetFromCurrent);
			return _position;
		}

		void Seek(long offsetFromCurrent) {
#if DEBUG
			Contract.Assert(0 <= offsetFromCurrent + _position, offsetFromCurrent + _position + " < 0");
			Contract.Assert(offsetFromCurrent + _position <= Length, Length + " <= " + offsetFromCurrent + _position);
#endif

			if (offsetFromCurrent < 0) {
				for (long i = 0; offsetFromCurrent < i; i--) {
					if (_offsetInCurrentSegment == 0) {
						_segmentIndex--;
						Contract.Assert(0 <= _segmentIndex);
						_offsetInCurrentSegment = _segments[_segmentIndex].Count - 1;
					}
					else {
						_offsetInCurrentSegment--;
					}

					_position--;
				}
			}
			else {
				for (long i = 0; i < offsetFromCurrent; i++) {
					if (_offsetInCurrentSegment == _segments[_segmentIndex].Count - 1) {
						_segmentIndex++;
						Contract.Assert(_segmentIndex <= _segments.Count);
						_offsetInCurrentSegment = 0;
					}
					else {
						_offsetInCurrentSegment++;
					}

					_position++;
				}
			}
		}

		public IList<ArraySegment<byte>> GetBuffer() {
			return _segments;
		}

		public IList<ArraySegment<byte>> GetBuffer(long start, long length) {
			if (start < 0) {
				throw new ArgumentOutOfRangeException(nameof(start));
			}

			if (length < 0) {
				throw new ArgumentOutOfRangeException(nameof(length));
			}

			var result = new List<ArraySegment<byte>>(_segments.Count);
			long taken = 0;
			var toBeSkipped = start;
			foreach (var segment in _segments) {
				var skipped = 0;
				if (toBeSkipped > 0) {
					if (segment.Count <= toBeSkipped) {
						toBeSkipped -= segment.Count;
						continue;
					}

					skipped = unchecked((int)toBeSkipped);
					toBeSkipped = 0;
				}

				var available = segment.Count - skipped;
				var required = length - taken;
				if (required <= available) {
					taken += required;
					result.Add(new ArraySegment<byte>(segment.Array, segment.Offset + skipped, unchecked((int)required)));
					break;
				}
				else {
					taken += available;
					result.Add(new ArraySegment<byte>(segment.Array, segment.Offset + skipped, available));
				}
			}

			return result;
		}

		public byte[] ToArray() {
			if (_segments.Count == 0) {
				return Array.Empty<byte>();
			}

			var result = _segments[0].AsEnumerable();
			for (var i = 1; i < _segments.Count; i++) {
				result = result.Concat(_segments[i].AsEnumerable());
			}

			return result.ToArray();
		}

		public sealed override void Flush() {
			// nop
		}

		public override void SetLength(long value) {
			throw new NotSupportedException();
		}

		public sealed override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
			throw new NotSupportedException();
		}

		public sealed override void EndWrite(IAsyncResult asyncResult) {
			throw new NotSupportedException();
		}

		public sealed override void Write(byte[] buffer, int offset, int count) {
			throw new NotSupportedException();
		}

		public sealed override void WriteByte(byte value) {
			throw new NotSupportedException();
		}

		internal sealed class DebuggerProxy {
			readonly ByteArraySegmentStream _source;

			public bool CanSeek => _source.CanSeek;

			public bool CanRead => _source.CanRead;

			public bool CanWrite => _source.CanWrite;

			public bool CanTimeout => _source.CanTimeout;

			public int ReadTimeout {
				get { return _source.ReadTimeout; }
				set { _source.ReadTimeout = value; }
			}

			public int WriteTimeout {
				get { return _source.WriteTimeout; }
				set { _source.WriteTimeout = value; }
			}

			public long Position {
				get { return _source.Position; }
				set { _source.Position = value; }
			}

			public long Length => _source.Length;

			public IList<ArraySegment<byte>> Segments => _source._segments ?? Array.Empty<ArraySegment<byte>>();

			public string Data => "[" +
						string.Join(
							",",
							Segments.Select(
								s => s.AsEnumerable().Select(b => b.ToString("X2"))
							).Aggregate((current, subsequent) => current.Concat(subsequent))
						) + "]";

			public DebuggerProxy(ByteArraySegmentStream source) {
				_source = source;
			}
		}
	}
}
