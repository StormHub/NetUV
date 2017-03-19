// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Text;
    using NetUV.Core.Common;
    using NetUV.Core.Logging;

    sealed class PooledArrayBufferAllocator<T> : ArrayBufferAllocator<T>
    {
        readonly PoolArena<T>[] heapArenas;
        readonly IReadOnlyList<IPoolArenaMetric> heapArenaMetrics;
        readonly PoolThreadLocalCache threadCache;

        internal PooledArrayBufferAllocator() 
            : this(
                  PoolOptions.DefaultNumHeapArena,
                  PoolOptions.DefaultPageSize,
                  PoolOptions.DefaultMaxOrder)
        { }

        internal PooledArrayBufferAllocator(int nHeapArena, int pageSize, int maxOrder) 
            : this(
                 nHeapArena, 
                 pageSize, 
                 maxOrder,
                 PoolOptions.DefaultTinyCacheSize,
                 PoolOptions.DefaultSmallCacheSize,
                 PoolOptions.DefaultNormalCacheSize,
                 PoolOptions.DefaultDirectMemoryCacheAlignment)
        { }

        internal PooledArrayBufferAllocator(
            int nHeapArena,
            int pageSize,
            int maxOrder,
            int tinyCacheSize,
            int smallCacheSize,
            int normalCacheSize,
            int directMemoryCacheAlignment)
        {
            Contract.Requires(nHeapArena >= 0);

            this.threadCache = new PoolThreadLocalCache(this);
            this.TinyCacheSize = tinyCacheSize;
            this.SmallCacheSize = smallCacheSize;
            this.NormalCacheSize = normalCacheSize;

            int chunkSize = PoolOptions.ValidateAndCalculateChunkSize(pageSize, maxOrder);
            int pageShifts = PoolOptions.ValidateAndCalculatePageShifts(pageSize);

            if (nHeapArena > 0)
            {
                this.heapArenas = NewArenaArray(nHeapArena);
                var metrics = new List<IPoolArenaMetric>(this.heapArenas.Length);
                for (int i = 0; i < this.heapArenas.Length; i++)
                {
                    var arena = new PoolArena<T>(
                        this, 
                        pageSize,
                        maxOrder, 
                        pageShifts, 
                        chunkSize, 
                        directMemoryCacheAlignment);
                    this.heapArenas[i] = arena;
                    metrics.Add(arena);
                }
                this.heapArenaMetrics = metrics.AsReadOnly();
            }
            else
            {
                this.heapArenas = null;
                this.heapArenaMetrics = new List<IPoolArenaMetric>();
            }
        }

        static PoolArena<T>[] NewArenaArray(int size) => new PoolArena<T>[size];

        protected override IArrayBuffer<T> NewBuffer(int initialCapacity, int capacity)
        {
            PoolThreadCache<T> cache = this.threadCache.Value;
            PoolArena<T> heapArena = cache.HeapArena;

            ArrayBuffer<T> buffer;
            if (heapArena != null)
            {
                buffer = heapArena.Allocate(cache, initialCapacity, capacity);
            }
            else
            {
                buffer = new UnpooledArrayBuffer<T>(this, initialCapacity, capacity);
            }

            return ToLeakAwareBuffer(buffer);
        }

        sealed class PoolThreadLocalCache : FastThreadLocal<PoolThreadCache<T>>
        {
            readonly PooledArrayBufferAllocator<T> owner;

            public PoolThreadLocalCache(PooledArrayBufferAllocator<T> owner)
            {
                this.owner = owner;
            }

            protected override PoolThreadCache<T> GetInitialValue()
            {
                lock (this)
                {
                    PoolArena<T> heapArena = GetLeastUsedArena(this.owner.heapArenas);

                    return new PoolThreadCache<T>(
                        heapArena,
                        this.owner.TinyCacheSize,
                        this.owner.SmallCacheSize,
                        this.owner.NormalCacheSize,
                        PoolOptions.DefaultMaxCachedBufferCapacity,
                        PoolOptions.DefaultCacheTrimInterval);
                }
            }

            protected override void OnRemoval(PoolThreadCache<T> threadCache) => threadCache.Free();

            static PoolArena<T> GetLeastUsedArena(PoolArena<T>[] arenas)
            {
                if (arenas == null
                    || arenas.Length == 0)
                {
                    return null;
                }

                PoolArena<T> minArena = arenas[0];
                for (int i = 1; i < arenas.Length; i++)
                {
                    PoolArena<T> arena = arenas[i];
                    if (arena.NumThreadCaches < minArena.NumThreadCaches)
                    {
                        minArena = arena;
                    }
                }

                return minArena;
            }
        }

        // Return the number of heap arenas.
        public int NumHeapArenas() => this.heapArenaMetrics.Count;

        // Return a {@link List} of all heap {@link PoolArenaMetric}s that are provided by this pool.
        public IReadOnlyList<IPoolArenaMetric> HeapArenas() => this.heapArenaMetrics;

        // Return the number of thread local caches used by this
        public int NumThreadLocalCaches()
        {
            PoolArena<T>[] arenas = this.heapArenas;
            if (arenas == null)
            {
                return 0;
            }

            int total = 0;
            foreach (PoolArena<T> arena in arenas)
            {
                total += arena.NumThreadCaches;
            }

            return total;
        }

        /// Return the size of the tiny cache.
        public int TinyCacheSize { get; }

        /// Return the size of the small cache.
        public int SmallCacheSize { get; }

        /// Return the size of the normal cache.
        public int NormalCacheSize { get; }

        internal PoolThreadCache<T> ThreadCache() => this.threadCache.Value;

        /// Returns the status of the allocator (which contains all metrics) as string. 
        /// Be aware this may be expensive and so should not called too frequently.
        public string DumpStats()
        {
            int length = this.heapArenas?.Length ?? 0;
            StringBuilder buf = new StringBuilder(512)
                    .Append(length)
                    .Append(" heap arena(s):")
                    .Append(Environment.NewLine);

            if (length > 0)
            {
                // ReSharper disable once PossibleNullReferenceException
                foreach (PoolArena<T> a in this.heapArenas)
                {
                    buf.Append(a);
                }
            }

            return buf.ToString();
        }
    }

    static class PoolOptions
    {
        static readonly ILog Log = LogFactory.ForContext(typeof(PoolOptions).Name);

        internal static readonly int DefaultNumHeapArena;
        internal static readonly int DefaultPageSize;
        internal static readonly int DefaultMaxOrder; // 8192 << 11 = 16 MiB per chunk
        internal static readonly int DefaultTinyCacheSize;
        internal static readonly int DefaultSmallCacheSize;
        internal static readonly int DefaultNormalCacheSize;
        internal static readonly int DefaultMaxCachedBufferCapacity;
        internal static readonly int DefaultCacheTrimInterval;
        internal static readonly int DefaultChunkSize;
        internal static readonly int DefaultDirectMemoryCacheAlignment;

        const int MinPageSize = 4096;
        const int MaxChunkSize = (int)((int.MaxValue + 1L) / 2);

        static PoolOptions()
        {
            int defaultPageSize;
            Exception pageSizeFallbackCause = null;
            if (!Configuration.TryGetValue($"{nameof(defaultPageSize)}", out defaultPageSize))
            {
                defaultPageSize = 8192;
            }
            else
            {
                try
                {
                    ValidateAndCalculatePageShifts(defaultPageSize);
                }
                catch (Exception exception)
                {
                    pageSizeFallbackCause = exception;
                    defaultPageSize = 8192;
                }
            }
            DefaultPageSize = defaultPageSize;

            int defaultMaxOrder;
            Exception maxOrderFallbackCause = null;
            if (!Configuration.TryGetValue($"{nameof(defaultMaxOrder)}", out defaultMaxOrder))
            {
                defaultMaxOrder = 11;
            }
            else
            {
                try
                {
                    ValidateAndCalculateChunkSize(DefaultPageSize, defaultMaxOrder);
                }
                catch (Exception t)
                {
                    maxOrderFallbackCause = t;
                    defaultMaxOrder = 11;
                }
            }
            DefaultMaxOrder = defaultMaxOrder;
            DefaultChunkSize = DefaultPageSize << DefaultMaxOrder;

            // Determine reasonable default for nHeapArena and nDirectArena.
            // Assuming each arena has 3 chunks, the pool should not consume more than 50% of max memory.

            // Use 2 * cores by default to reduce contention as we use 2 * cores for the number of EventLoops
            // in NIO and EPOLL as well. If we choose a smaller number we will run into hotspots as allocation and
            // deallocation needs to be synchronized on the PoolArena.
            // See https://github.com/netty/netty/issues/3888
            int defaultMinNumArena;
            if (!Configuration.TryGetValue($"{nameof(defaultMinNumArena)}", out defaultMinNumArena))
            {
                defaultMinNumArena = Environment.ProcessorCount * 2;
            }
            DefaultNumHeapArena = Math.Max(0, defaultMinNumArena);

            // cache sizes
            int tinyCacheSize;
            if (!Configuration.TryGetValue($"{nameof(tinyCacheSize)}", out tinyCacheSize))
            {
                tinyCacheSize = 512;
            }
            DefaultTinyCacheSize = tinyCacheSize;

            int smallCacheSize;
            if (!Configuration.TryGetValue($"{nameof(smallCacheSize)}", out smallCacheSize))
            {
                smallCacheSize = 256;
            }
            DefaultSmallCacheSize = smallCacheSize;

            int normalCacheSize;
            if (!Configuration.TryGetValue($"{nameof(normalCacheSize)}", out normalCacheSize))
            {
                normalCacheSize = 64;
            }
            DefaultNormalCacheSize = normalCacheSize;

            // 32 kb is the default maximum capacity of the cached buffer. Similar to what is explained in
            // 'Scalable memory allocation using jemalloc'
            int maxCachedBufferCapacity;
            if (!Configuration.TryGetValue($"{nameof(maxCachedBufferCapacity)}", out maxCachedBufferCapacity))
            {
                maxCachedBufferCapacity = 32 * 1024;
            }
            DefaultMaxCachedBufferCapacity = maxCachedBufferCapacity;

            // the number of threshold of allocations when cached entries will be freed up if not frequently used
            int cacheTrimInterval;
            if (!Configuration.TryGetValue(nameof(cacheTrimInterval), out cacheTrimInterval))
            {
                cacheTrimInterval = 8192;
            }
            DefaultCacheTrimInterval = cacheTrimInterval;

            int defaultDirectMemoryCacheAlignment;
            if (!Configuration.TryGetValue(nameof(defaultDirectMemoryCacheAlignment), out defaultDirectMemoryCacheAlignment))
            {
                defaultDirectMemoryCacheAlignment = 64;
            }
            DefaultDirectMemoryCacheAlignment = defaultDirectMemoryCacheAlignment;

            if (!Log.IsDebugEnabled)
            {
                return;
            }

            Log.Debug($"{nameof(DefaultNumHeapArena)} = {DefaultNumHeapArena}");
            Log.Debug($"{nameof(DefaultPageSize)} = {DefaultPageSize}"
                + (pageSizeFallbackCause != null ? $" {pageSizeFallbackCause}" : string.Empty));
            Log.Debug($"{nameof(DefaultMaxOrder)} = {DefaultMaxOrder}"
                + (maxOrderFallbackCause != null ? $" {maxOrderFallbackCause}" : string.Empty));
            Log.Debug($"{nameof(DefaultChunkSize)} = {DefaultChunkSize}");
            Log.Debug($"{nameof(DefaultTinyCacheSize)} = {DefaultTinyCacheSize}");
            Log.Debug($"{nameof(DefaultSmallCacheSize)} = {DefaultSmallCacheSize}");
            Log.Debug($"{nameof(DefaultNormalCacheSize)} = {DefaultNormalCacheSize}");
            Log.Debug($"{nameof(DefaultMaxCachedBufferCapacity)} = {DefaultMaxCachedBufferCapacity}");
            Log.Debug($"{nameof(DefaultCacheTrimInterval)} = {DefaultCacheTrimInterval}");
            Log.Debug($"{nameof(DefaultDirectMemoryCacheAlignment)} = {DefaultDirectMemoryCacheAlignment}");
        }

        internal static int ValidateAndCalculatePageShifts(int pageSize)
        {
            Contract.Requires(pageSize >= MinPageSize);
            Contract.Requires((pageSize & pageSize - 1) == 0, "Expected power of 2");

            // Logarithm base 2. At this point we know that pageSize is a power of two.
            return IntegerExtensions.Log2(pageSize);
        }

        internal static int ValidateAndCalculateChunkSize(int pageSize, int maxOrder)
        {
            if (maxOrder > 14)
            {
                throw new ArgumentException($"maxOrder: {maxOrder} (expected: 0-14)");
            }

            // Ensure the resulting chunkSize does not overflow.
            int chunkSize = pageSize;
            for (int i = maxOrder; i > 0; i--)
            {
                if (chunkSize > MaxChunkSize >> 1)
                {
                    throw new ArgumentException(
                        $"pageSize ({pageSize}) << maxOrder ({maxOrder}) must not exceed {MaxChunkSize}");
                }

                chunkSize <<= 1;
            }

            return chunkSize;
        }
    }
}
