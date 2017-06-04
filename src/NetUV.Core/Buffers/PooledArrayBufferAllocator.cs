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

    sealed class PooledArrayBufferAllocator<T> : AbstractArrayBufferAllocator<T>
    {
        readonly PoolArena<T>[] heapArenas;
        readonly IReadOnlyList<IPoolArenaMetric> heapArenaMetrics;
        readonly PoolThreadLocalCache threadCache;

        public PooledArrayBufferAllocator()
            : this(PoolOptions.DefaultNumHeapArena, 
                  PoolOptions.DefaultPageSize, 
                  PoolOptions.DefaultMaxOrder)
        {
        }

        public PooledArrayBufferAllocator(int heapArenaCount, int pageSize, int maxOrder)
            : this(heapArenaCount, pageSize, maxOrder, 
                  PoolOptions.DefaultTinyCacheSize, 
                  PoolOptions.DefaultSmallCacheSize, 
                  PoolOptions.DefaultNormalCacheSize)
        {
        }

        public PooledArrayBufferAllocator(int heapArenaCount, int pageSize, int maxOrder,
            int tinyCacheSize, int smallCacheSize, int normalCacheSize)
            : this(heapArenaCount, pageSize, maxOrder, tinyCacheSize, smallCacheSize, normalCacheSize, int.MaxValue)
        {
        }

        public PooledArrayBufferAllocator(long maxMemory) : 
            this(
                PoolOptions.DefaultNumHeapArena, 
                PoolOptions.DefaultPageSize, 
                PoolOptions.DefaultMaxOrder, 
                PoolOptions.DefaultTinyCacheSize,
                PoolOptions.DefaultSmallCacheSize, 
                PoolOptions.DefaultNormalCacheSize,
                Math.Max(1, (int)Math.Min(maxMemory / PoolOptions.DefaultNumHeapArena / (PoolOptions.DefaultPageSize << PoolOptions.DefaultMaxOrder), int.MaxValue)))
        {
        }

        public PooledArrayBufferAllocator(int heapArenaCount, int pageSize, int maxOrder,
            int tinyCacheSize, int smallCacheSize, int normalCacheSize, int maxChunkCountPerArena)
        {
            Contract.Requires(heapArenaCount >= 0);

            this.threadCache = new PoolThreadLocalCache(this);
            this.TinyCacheSize = tinyCacheSize;
            this.SmallCacheSize = smallCacheSize;
            this.NormalCacheSize = normalCacheSize;
            int chunkSize = PoolOptions.ValidateAndCalculateChunkSize(pageSize, maxOrder);

            int pageShifts = PoolOptions.ValidateAndCalculatePageShifts(pageSize);

            if (heapArenaCount > 0)
            {
                this.heapArenas = NewArenaArray(heapArenaCount);
                var metrics = new List<IPoolArenaMetric>(this.heapArenas.Length);
                for (int i = 0; i < this.heapArenas.Length; i++)
                {
                    var arena = new PoolArena<T>(this, pageSize, maxOrder, pageShifts, chunkSize, maxChunkCountPerArena);
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

        protected override IArrayBuffer<T> NewBuffer(int initialCapacity, int maxCapacity)
        {
            PoolThreadCache<T> cache = this.threadCache.Value;
            PoolArena<T> heapArena = cache.HeapArena;

            IArrayBuffer<T> buf;
            if (heapArena != null)
            {
                buf = heapArena.Allocate(cache, initialCapacity, maxCapacity);
            }
            else
            {
                buf = new UnpooledArrayBuffer<T>(this, initialCapacity, maxCapacity);
            }

            return ToLeakAwareBuffer(buf);
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
                if (arenas == null || arenas.Length == 0)
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

        public int NumHeapArenas() => this.heapArenaMetrics.Count;

        public IReadOnlyList<IPoolArenaMetric> HeapArenas() => this.heapArenaMetrics;

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

        /// Returns the status of the allocator (which contains all metrics) as string. Be aware this may be expensive
        /// and so should not called too frequently.
        public string DumpStats()
        {
            StringBuilder buf = new StringBuilder(512)
                .Append(this.heapArenas?.Length ?? 0)
                .Append(" heap arena(s):")
                .Append(Environment.NewLine);

            if (this.heapArenas != null)
            {
                foreach (PoolArena<T> arena in this.heapArenas)
                {
                    buf.Append(arena);
                }
            }

            return buf.ToString();
        }
    }

    static class PoolOptions
    {
        static readonly ILog Log = LogFactory.ForContext(nameof(PoolOptions));

        internal static readonly int DefaultNumHeapArena;
        internal static readonly int DefaultPageSize;
        internal static readonly int DefaultMaxOrder; // 8192 << 11 = 16 MiB per chunk
        internal static readonly int DefaultTinyCacheSize;
        internal static readonly int DefaultSmallCacheSize;
        internal static readonly int DefaultNormalCacheSize;
        internal static readonly int DefaultMaxCachedBufferCapacity;
        internal static readonly int DefaultCacheTrimInterval;

        const int MinPageSize = 4096;
        const int MaxChunkSize = (int)((int.MaxValue + 1L) / 2);

        static PoolOptions()
        {
            int defaultPageSize = Configuration.TryGetValue("allocator.pageSize", 8192);
            Exception pageSizeFallbackCause = null;
            try
            {
                ValidateAndCalculatePageShifts(defaultPageSize);
            }
            catch (Exception t)
            {
                pageSizeFallbackCause = t;
                defaultPageSize = 8192;
            }
            DefaultPageSize = defaultPageSize;

            int defaultMaxOrder = Configuration.TryGetValue("allocator.maxOrder", 11);
            Exception maxOrderFallbackCause = null;
            try
            {
                ValidateAndCalculateChunkSize(DefaultPageSize, defaultMaxOrder);
            }
            catch (Exception t)
            {
                maxOrderFallbackCause = t;
                defaultMaxOrder = 11;
            }
            DefaultMaxOrder = defaultMaxOrder;

            // Assuming each arena has 3 chunks, the pool should not consume more than 50% of max memory.

            // Use 2 * cores by default to reduce contention as we use 2 * cores for the number of EventLoops
            // in NIO and EPOLL as well. If we choose a smaller number we will run into hotspots as allocation and
            // deallocation needs to be synchronized on the PoolArena.
            // See https://github.com/netty/netty/issues/3888
            int defaultMinNumArena = Environment.ProcessorCount * 2;
            //int defaultChunkSize = DEFAULT_PAGE_SIZE << DEFAULT_MAX_ORDER;
            DefaultNumHeapArena = Math.Max(0, Configuration.TryGetValue("allocator.numHeapArenas", defaultMinNumArena));

            // cache sizes
            DefaultTinyCacheSize = Configuration.TryGetValue("allocator.tinyCacheSize", 512);
            DefaultSmallCacheSize = Configuration.TryGetValue("allocator.smallCacheSize", 256);
            DefaultNormalCacheSize = Configuration.TryGetValue("allocator.normalCacheSize", 64);

            // 32 kb is the default maximum capacity of the cached buffer. Similar to what is explained in
            // 'Scalable memory allocation using jemalloc'
            DefaultMaxCachedBufferCapacity = Configuration.TryGetValue("allocator.maxCachedBufferCapacity", 32 * 1024);

            // the number of threshold of allocations when cached entries will be freed up if not frequently used
            DefaultCacheTrimInterval = Configuration.TryGetValue("allocator.cacheTrimInterval", 8192);

            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("allocator.numHeapArenas: {0}", DefaultNumHeapArena);
                if (pageSizeFallbackCause == null)
                {
                    Log.DebugFormat("allocator.pageSize: {0}", DefaultPageSize);
                }
                else
                {
                    Log.DebugFormat("allocator.pageSize: {0}", DefaultPageSize, pageSizeFallbackCause);
                }
                if (maxOrderFallbackCause == null)
                {
                    Log.DebugFormat("allocator.maxOrder: {0}", DefaultMaxOrder);
                }
                else
                {
                    Log.DebugFormat("allocator.maxOrder: {0}", DefaultMaxOrder, maxOrderFallbackCause);
                }
                Log.DebugFormat("allocator.chunkSize: {0}", DefaultPageSize << DefaultMaxOrder);
                Log.DebugFormat("allocator.tinyCacheSize: {0}", DefaultTinyCacheSize);
                Log.DebugFormat("allocator.smallCacheSize: {0}", DefaultSmallCacheSize);
                Log.DebugFormat("allocator.normalCacheSize: {0}", DefaultNormalCacheSize);
                Log.DebugFormat("allocator.maxCachedBufferCapacity: {0}", DefaultMaxCachedBufferCapacity);
                Log.DebugFormat("allocator.cacheTrimInterval: {0}", DefaultCacheTrimInterval);
            }
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
                throw new ArgumentException("maxOrder: " + maxOrder + " (expected: 0-14)");
            }

            // Ensure the resulting chunkSize does not overflow.
            int chunkSize = pageSize;
            for (int i = maxOrder; i > 0; i--)
            {
                if (chunkSize > MaxChunkSize >> 1)
                {
                    throw new ArgumentException($"pageSize ({pageSize}) << maxOrder ({maxOrder}) must not exceed {MaxChunkSize}");
                }
                chunkSize <<= 1;
            }
            return chunkSize;
        }


    }
}
