using MsgPack.Rpc.Core.StandardObjectPoolTracing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Threading;

namespace MsgPack.Rpc.Core {
	/// <summary>
	///		Implements standard <see cref="ObjectPool{T}"/>.
	/// </summary>
	/// <typeparam name="T">
	///		The type of objects to be pooled.
	/// </typeparam>
	internal sealed class StandardObjectPool<T> : ObjectPool<T>
		where T : class {
		private static readonly bool _isDisposableTInternal = typeof(IDisposable).IsAssignableFrom(typeof(T));

		// name for debugging purpose, explicitly specified, or automatically constructed.
		private readonly string _name;

		internal TraceSource TraceSource { get; }

		private readonly ObjectPoolConfiguration _configuration;

		private int _isCorrupted;
		private bool IsCorrupted => Interlocked.CompareExchange(ref _isCorrupted, 0, 0) != 0;

		private readonly Func<T> _factory;
		private readonly BlockingCollection<T> _pool;
		private readonly TimeSpan _borrowTimeout;

		// Debug
		internal int PooledCount => _pool.Count;

		private readonly BlockingCollection<WeakReference> _leases;
		private readonly ReaderWriterLockSlim _leasesLock;

		internal int LeasedCount => _leases.Count;

		// TODO: Timer might be too heavy.
		private readonly Timer _evictionTimer;
		private readonly int? _evictionIntervalMilliseconds;

		/// <summary>
		/// Initializes a new instance of the <see cref="StandardObjectPool&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="factory">
		///		The factory delegate to create <typeparamref name="T"/> type instance.
		///	</param>
		/// <param name="configuration">
		///		The <see cref="ObjectPoolConfiguration"/> which contains various settings of this object pool.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="factory"/> is <c>null</c>.
		/// </exception>
		public StandardObjectPool(Func<T> factory, ObjectPoolConfiguration configuration) {
			if (factory == null) {
				throw new ArgumentNullException("factory");
			}

			Contract.EndContractBlock();

			var safeConfiguration = (configuration ?? ObjectPoolConfiguration.Default).AsFrozen();

			if (string.IsNullOrWhiteSpace(safeConfiguration.Name)) {
				TraceSource = new TraceSource(GetType().FullName);
				_name = GetType().FullName + "@" + GetHashCode().ToString("X", CultureInfo.InvariantCulture);
			}
			else {
				TraceSource = new TraceSource(safeConfiguration.Name);
				_name = safeConfiguration.Name;
			}

			if (configuration == null && TraceSource.ShouldTrace(StandardObjectPoolTrace.InitializedWithDefaultConfiguration)) {
				TraceSource.TraceEvent(
					StandardObjectPoolTrace.InitializedWithDefaultConfiguration,
					"Initialized with default configuration. { \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X} }",
					_name,
					GetType(),
					GetHashCode()
				);
			}
			else if (TraceSource.ShouldTrace(StandardObjectPoolTrace.InitializedWithConfiguration)) {
				TraceSource.TraceEvent(
					StandardObjectPoolTrace.InitializedWithConfiguration,
					"Initialized with specified configuration. { \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Configuration\" : {3} }",
					_name,
					GetType(),
					GetHashCode(),
					configuration
				);
			}

			_configuration = safeConfiguration;
			_factory = factory;
			_leasesLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			_borrowTimeout = safeConfiguration.BorrowTimeout ?? TimeSpan.FromMilliseconds(Timeout.Infinite);
			_pool =
				new BlockingCollection<T>(
					new ConcurrentStack<T>()
				);

			if (safeConfiguration.MaximumPooled == null) {
				_leases = new BlockingCollection<WeakReference>(new ConcurrentQueue<WeakReference>());
			}
			else {
				_leases = new BlockingCollection<WeakReference>(new ConcurrentQueue<WeakReference>(), safeConfiguration.MaximumPooled.Value);
			}

			for (var i = 0; i < safeConfiguration.MinimumReserved; i++) {
				if (!AddToPool(factory(), 0)) {
					TraceSource.TraceEvent(
						StandardObjectPoolTrace.FailedToAddPoolInitially,
						"Failed to add item. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X} }}",
						_name,
						GetType(),
						GetHashCode()
					);
				}
			}

			_evictionIntervalMilliseconds = safeConfiguration.EvitionInterval == null ? default(int?) : unchecked((int)safeConfiguration.EvitionInterval.Value.TotalMilliseconds);

			if (safeConfiguration.MaximumPooled != null
						 && safeConfiguration.MinimumReserved != safeConfiguration.MaximumPooled.GetValueOrDefault()
						 && _evictionIntervalMilliseconds != null) {
				_evictionTimer = new Timer(OnEvictionTimerElapsed, null, _evictionIntervalMilliseconds.Value, Timeout.Infinite);
			}
			else {
				_evictionTimer = null;
			}
		}

		private bool AddToPool(T value, int millisecondsTimeout) {
			var result = false;
			try { }
			finally {
				if (_pool.TryAdd(value, millisecondsTimeout)) {
					if (_leases.TryAdd(new WeakReference(value))) {
						result = true;
					}
				}
			}

			return result;
		}

		protected sealed override void Dispose(bool disposing) {
			if (disposing) {
				_pool.Dispose();
				if (_evictionTimer != null) {
					_evictionTimer.Dispose();
				}
				_leasesLock.Dispose();

				if (TraceSource.ShouldTrace(StandardObjectPoolTrace.Disposed)) {
					TraceSource.TraceEvent(
						StandardObjectPoolTrace.Disposed,
						"Object pool is disposed. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X} }}",
						_name,
						GetType(),
						GetHashCode()
					);
				}
			}
			else {
				if (TraceSource.ShouldTrace(StandardObjectPoolTrace.Finalized)) {
					TraceSource.TraceEvent(
						StandardObjectPoolTrace.Finalized,
						"Object pool is finalized. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X} }}",
						_name,
						GetType(),
						GetHashCode()
					);
				}
			}

			base.Dispose(disposing);
		}

		private void VerifyIsNotCorrupted() {
			if (IsCorrupted) {
				throw new ObjectPoolCorruptedException();
			}
		}

		private void SetIsCorrupted() {
			Interlocked.Exchange(ref _isCorrupted, 1);
		}

		private void OnEvictionTimerElapsed(object state) {
			EvictExtraItemsCore(false);

			Contract.Assert(_evictionIntervalMilliseconds.HasValue);

			if (IsCorrupted) {
				return;
			}

			if (!_evictionTimer.Change(_evictionIntervalMilliseconds.Value, Timeout.Infinite)) {
				TraceSource.TraceEvent(
					StandardObjectPoolTrace.FailedToRefreshEvictionTImer,
					"Failed to refresh evition timer. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X} }}",
					_name,
					GetType(),
					GetHashCode()
				);
			}
		}

		/// <summary>
		///		Evicts the extra items from current pool.
		/// </summary>
		public sealed override void EvictExtraItems() {
			EvictExtraItemsCore(true);
		}

		private void EvictExtraItemsCore(bool isInduced) {
			var remains = _pool.Count - _configuration.MinimumReserved;
			var evicting = remains / 2 + remains % 2;
			if (evicting > 0) {
				if (isInduced && TraceSource.ShouldTrace(StandardObjectPoolTrace.EvictingExtraItemsInduced)) {
					TraceSource.TraceEvent(
						StandardObjectPoolTrace.EvictingExtraItemsInduced,
						"Start induced eviction. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Evicting\" : {3} }}",
						_name,
						GetType(),
						GetHashCode(),
						evicting
					);
				}
				else if (TraceSource.ShouldTrace(StandardObjectPoolTrace.EvictingExtraItemsPreiodic)) {
					TraceSource.TraceEvent(
						StandardObjectPoolTrace.EvictingExtraItemsPreiodic,
						"Start periodic eviction. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Evicting\" : {3} }}",
						_name,
						GetType(),
						GetHashCode(),
						evicting
					);
				}

				var disposed = EvictItems(evicting);

				if (isInduced && TraceSource.ShouldTrace(StandardObjectPoolTrace.EvictedExtraItemsInduced)) {
					TraceSource.TraceEvent(
						StandardObjectPoolTrace.EvictedExtraItemsInduced,
						"Finish induced eviction. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Evicted\" : {3} }}",
						_name,
						GetType(),
						GetHashCode(),
						disposed.Count
					);
				}
				else if (TraceSource.ShouldTrace(StandardObjectPoolTrace.EvictedExtraItemsPreiodic)) {
					TraceSource.TraceEvent(
						StandardObjectPoolTrace.EvictedExtraItemsPreiodic,
						"Finish periodic eviction. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Evicted\" : {3} }}",
						_name,
						GetType(),
						GetHashCode(),
						disposed.Count
					);
				}

				CollectLeases(disposed);
			}
			else {
				// Just GC
				CollectLeases(new List<T>(0));
			}
		}

		private List<T> EvictItems(int count) {
			var disposed = new List<T>(count);
			for (var i = 0; i < count; i++) {
				T disposing;
				if (!_pool.TryTake(out disposing, 0)) {
					// Race, cancel eviction now.
					return disposed;
				}

				DisposeItem(disposing);
				disposed.Add(disposing);
			}

			return disposed;
		}

		private void CollectLeases(List<T> disposed) {
			var isSuccess = false;
			try {
				_leasesLock.EnterWriteLock();
				try {
					var buffer = new List<WeakReference>(_leases.Count + Environment.ProcessorCount * 2);
					WeakReference dequeud;
					while (_leases.TryTake(out dequeud)) {
						buffer.Add(dequeud);
					}

					var isFlushed = false;
					var freed = 0;
					foreach (var item in buffer) {
						if (!isFlushed && item.IsAlive && !disposed.Exists(x => Object.ReferenceEquals(x, SafeGetTarget(item)))) {
							if (!_leases.TryAdd(item)) {
								// Just evict
								isFlushed = true;
								freed++;
							}
						}
						else {
							freed++;
						}
					}

					if (freed - disposed.Count > 0 && TraceSource.ShouldTrace(StandardObjectPoolTrace.GarbageCollectedWithLost)) {
						TraceSource.TraceEvent(
							StandardObjectPoolTrace.GarbageCollectedWithLost,
							"Garbage items are collected, but there may be lost items. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Collected\" : {3}, \"MayBeLost\" : {4} }}",
							_name,
							GetType(),
							GetHashCode(),
							freed,
							freed - disposed.Count
						);
					}
					else if (freed > 0 && TraceSource.ShouldTrace(StandardObjectPoolTrace.GarbageCollectedWithoutLost)) {
						TraceSource.TraceEvent(
							StandardObjectPoolTrace.GarbageCollectedWithoutLost,
							"Garbage items are collected. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Collected\" : {3} }}",
							_name,
							GetType(),
							GetHashCode(),
							freed
						);
					}
				}
				finally {
					_leasesLock.ExitWriteLock();
				}

				isSuccess = true;
			}
			finally {
				if (!isSuccess) {
					SetIsCorrupted();
				}
			}
		}

		private static T SafeGetTarget(WeakReference item) {
			try {
				return item.Target as T;
			}
			catch (InvalidOperationException) {
				return null;
			}
		}

		protected sealed override T BorrowCore() {
			VerifyIsNotCorrupted();

			T result;
			while (true) {
				if (_pool.TryTake(out result, 0)) {
					if (TraceSource.ShouldTrace(StandardObjectPoolTrace.BorrowFromPool)) {
						TraceBorrow(result);
					}

					return result;
				}

				_leasesLock.EnterReadLock(); // TODO: Timeout
				try {
					if (_leases.Count < _leases.BoundedCapacity) {
						var newObject = _factory();
						Contract.Assume(newObject != null);

						if (_leases.TryAdd(new WeakReference(newObject), 0)) {
							if (TraceSource.ShouldTrace(StandardObjectPoolTrace.ExpandPool)) {
								TraceSource.TraceEvent(
									StandardObjectPoolTrace.ExpandPool,
									"Expand the pool. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"NewCount\" : {3} }}",
									_name,
									GetType(),
									GetHashCode(),
									_pool.Count
								);
							}

							TraceBorrow(newObject);
							return newObject;
						}
						else {
							if (TraceSource.ShouldTrace(StandardObjectPoolTrace.FailedToExpandPool)) {
								TraceSource.TraceEvent(
									StandardObjectPoolTrace.FailedToExpandPool,
									"Failed to expand the pool. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"NewCount\" : {3} }}",
									_name,
									GetType(),
									GetHashCode(),
									_pool.Count
								);
							}

							DisposeItem(newObject);
						}
					}
				}
				finally {
					_leasesLock.ExitReadLock();
				}

				// Wait or exception
				break;
			}

			if (TraceSource.ShouldTrace(StandardObjectPoolTrace.PoolIsEmpty)) {
				TraceSource.TraceEvent(
					StandardObjectPoolTrace.PoolIsEmpty,
					"Pool is empty. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X} }}",
					_name,
					GetType(),
					GetHashCode()
				);
			}

			if (_configuration.ExhausionPolicy == ExhausionPolicy.ThrowException) {
				throw new ObjectPoolEmptyException();
			}
			else {
				if (!_pool.TryTake(out result, _borrowTimeout)) {
					throw new TimeoutException(string.Format(CultureInfo.CurrentCulture, "The object borrowing is not completed in the time out {0}.", _borrowTimeout));
				}

				if (TraceSource.ShouldTrace(StandardObjectPoolTrace.BorrowFromPool)) {
					TraceBorrow(result);
				}

				return result;
			}
		}

		private void TraceBorrow(T result) {
			TraceSource.TraceEvent(
				StandardObjectPoolTrace.BorrowFromPool,
				"Borrow the value from the pool. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"HashCode\" : 0x{2:X}, \"Evicted\" : 0x{2:X}, \"Resource\" : \"{3}\", \"HashCodeOfResource\" : 0x{4:X} }}",
				_name,
				GetType(),
				GetHashCode(),
				result,
				result.GetHashCode()
			);
		}

		private static void DisposeItem(T item) {
			if (_isDisposableTInternal) {
				((IDisposable)item).Dispose();
			}
		}

		protected sealed override void ReturnCore(T value) {
			if (!_pool.TryAdd(value)) {
				TraceSource.TraceEvent(
					StandardObjectPoolTrace.FailedToReturnToPool,
					"Failed to return the value to the pool. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"Value\" : 0x{2:X}, \"Resource\" : \"{3}\", \"HashCodeOfResource\" : 0x{4:X} }}",
					_name,
					GetType(),
					GetHashCode(),
					value,
					value.GetHashCode()
				);
				SetIsCorrupted();
				throw new ObjectPoolCorruptedException("Failed to return the value to the pool.");
			}
			else {
				if (TraceSource.ShouldTrace(StandardObjectPoolTrace.ReturnToPool)) {
					TraceSource.TraceEvent(
						StandardObjectPoolTrace.ReturnToPool,
						"Return the value to the pool. {{ \"Name\" : \"{0}\", \"Type\" : \"{1}\", \"Value\" : 0x{2:X}, \"Resource\" : \"{3}\", \"HashCodeOfResource\" : 0x{4:X} }}",
						_name,
						GetType(),
						GetHashCode(),
						value,
						value.GetHashCode()
					);
				}
			}
		}
	}
}
