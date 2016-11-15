// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Text;
    using System.Threading;
    using NetUV.Core.Common;

    enum SizeClass
    {
        Tiny,
        Small,
        Normal
    }

    /// <summary>
    /// Forked and adapted from https://github.com/Azure/DotNetty
    /// </summary>
    class PoolArena<T> : IPoolArenaMetric
    {
        internal const int NumTinySubpagePools = 512 >> 4;

        internal readonly PooledArrayBufferAllocator<T> Parent;

        readonly int maxOrder;
        internal readonly int PageSize;
        internal readonly int PageShifts;
        internal readonly int ChunkSize;
        internal readonly int SubpageOverflowMask;
        internal readonly int NumSmallSubpagePools;
        readonly PoolSubpage<T>[] tinySubpagePools;
        readonly PoolSubpage<T>[] smallSubpagePools;

        readonly PoolChunkList<T> q050;
        readonly PoolChunkList<T> q025;
        readonly PoolChunkList<T> q000;
        readonly PoolChunkList<T> qInit;
        readonly PoolChunkList<T> q075;
        readonly PoolChunkList<T> q100;

        readonly List<IPoolChunkListMetric> chunkListMetrics;

        // Metrics for allocations and deallocations
        // We need to use the LongCounter here as this is not guarded via synchronized block.
        long allocationsHuge;
        long activeBytesHuge;

        // We need to use the LongCounter here as this is not guarded via synchronized block.
        long deallocationsHuge;

        // Number of thread caches backed by this arena.
        int numThreadCaches;

        internal PoolArena(PooledArrayBufferAllocator<T> parent, int pageSize, int maxOrder, int pageShifts, int chunkSize)
        {
            this.Parent = parent;
            this.PageSize = pageSize;
            this.maxOrder = maxOrder;
            this.PageShifts = pageShifts;
            this.ChunkSize = chunkSize;
            this.SubpageOverflowMask = ~(pageSize - 1);
            this.tinySubpagePools = NewSubpagePoolArray(NumTinySubpagePools);
            for (int i = 0; i < this.tinySubpagePools.Length; i++)
            {
                this.tinySubpagePools[i] = NewSubpagePoolHead(pageSize);
            }

            this.NumSmallSubpagePools = pageShifts - 9;
            this.smallSubpagePools = NewSubpagePoolArray(this.NumSmallSubpagePools);
            for (int i = 0; i < this.smallSubpagePools.Length; i++)
            {
                this.smallSubpagePools[i] = NewSubpagePoolHead(pageSize);
            }

            this.q100 = new PoolChunkList<T>(null, 100, int.MaxValue, chunkSize);
            this.q075 = new PoolChunkList<T>(this.q100, 75, 100, chunkSize);
            this.q050 = new PoolChunkList<T>(this.q075, 50, 100, chunkSize);
            this.q025 = new PoolChunkList<T>(this.q050, 25, 75, chunkSize);
            this.q000 = new PoolChunkList<T>(this.q025, 1, 50, chunkSize);
            this.qInit = new PoolChunkList<T>(this.q000, int.MinValue, 25, chunkSize);

            this.q100.PrevList(this.q075);
            this.q075.PrevList(this.q050);
            this.q050.PrevList(this.q025);
            this.q025.PrevList(this.q000);
            this.q000.PrevList(null);
            this.qInit.PrevList(this.qInit);

            this.chunkListMetrics = new List<IPoolChunkListMetric>(6)
            {
                this.qInit,
                this.q000,
                this.q025,
                this.q050,
                this.q075,
                this.q100
            };
        }

        public int NumThreadCaches => Volatile.Read(ref this.numThreadCaches);

        public void RegisterThreadCache() => Interlocked.Increment(ref this.numThreadCaches);

        public void DeregisterThreadCache() => Interlocked.Decrement(ref this.numThreadCaches);

        static PoolSubpage<T> NewSubpagePoolHead(int pageSize)
        {
            var head = new PoolSubpage<T>(pageSize);
            head.Prev = head;
            head.Next = head;
            return head;
        }

        static PoolSubpage<T>[] NewSubpagePoolArray(int size) => new PoolSubpage<T>[size];

        internal PooledArrayBuffer<T> Allocate(PoolThreadCache<T> cache, int reqCapacity, int maxCapacity)
        {
            PooledArrayBuffer<T> buf = this.NewByteBuf(maxCapacity);
            this.Allocate(cache, buf, reqCapacity);
            return buf;
        }

        internal static int TinyIdx(int normCapacity) => normCapacity.RightUShift(4);

        internal static int SmallIdx(int normCapacity)
        {
            int tableIdx = 0;
            int i = normCapacity.RightUShift(10);
            while (i != 0)
            {
                i = i.RightUShift(1);
                tableIdx++;
            }
            return tableIdx;
        }

        // capacity < pageSize
        internal bool IsTinyOrSmall(int normCapacity) => (normCapacity & this.SubpageOverflowMask) == 0;

        // normCapacity < 512
        internal static bool IsTiny(int normCapacity) => (normCapacity & 0xFFFFFE00) == 0;

        void Allocate(PoolThreadCache<T> cache, PooledArrayBuffer<T> buf, int reqCapacity)
        {
            int normCapacity = this.NormalizeCapacity(reqCapacity);
            if (this.IsTinyOrSmall(normCapacity))
            {
                // capacity < pageSize
                int tableIdx;
                PoolSubpage<T>[] table;
                bool tiny = IsTiny(normCapacity);
                if (tiny)
                {
                    // < 512
                    if (cache.AllocateTiny(this, buf, reqCapacity, normCapacity))
                    {
                        // was able to allocate out of the cache so move on
                        return;
                    }
                    tableIdx = TinyIdx(normCapacity);
                    table = this.tinySubpagePools;
                }
                else
                {
                    if (cache.AllocateSmall(this, buf, reqCapacity, normCapacity))
                    {
                        // was able to allocate out of the cache so move on
                        return;
                    }
                    tableIdx = SmallIdx(normCapacity);
                    table = this.smallSubpagePools;
                }

                PoolSubpage<T> head = table[tableIdx];

                /**
                 * Synchronize on the head. This is needed as {@link PoolSubpage#allocate()} and
                 * {@link PoolSubpage#free(int)} may modify the doubly linked list as well.
                 */
                lock (head)
                {
                    PoolSubpage<T> s = head.Next;
                    if (s != head)
                    {
                        Contract.Assert(s.DoNotDestroy && s.ElemSize == normCapacity);
                        long handle = s.Allocate();
                        Contract.Assert(handle >= 0);
                        s.Chunk.InitBufWithSubpage(buf, handle, reqCapacity);

                        if (tiny)
                        {
                            ++this.NumTinyAllocations;
                        }
                        else
                        {
                            ++this.NumSmallAllocations;
                        }
                        return;
                    }
                }

                this.AllocateNormal(buf, reqCapacity, normCapacity);
                return;
            }

            if (normCapacity <= this.ChunkSize)
            {
                if (cache.AllocateNormal(this, buf, reqCapacity, normCapacity))
                {
                    // was able to allocate out of the cache so move on
                    return;
                }
                this.AllocateNormal(buf, reqCapacity, normCapacity);
            }
            else
            {
                // Huge allocations are never served via the cache so just call allocateHuge
                this.AllocateHuge(buf, reqCapacity);
            }
        }

        void AllocateNormal(PooledArrayBuffer<T> buf, int reqCapacity, int normCapacity)
        {
            lock (this)
            {
                if (this.q050.Allocate(buf, reqCapacity, normCapacity) 
                    || this.q025.Allocate(buf, reqCapacity, normCapacity)
                    || this.q000.Allocate(buf, reqCapacity, normCapacity) 
                    || this.qInit.Allocate(buf, reqCapacity, normCapacity)
                    || this.q075.Allocate(buf, reqCapacity, normCapacity))
                {
                    ++this.NumNormalAllocations;
                    return;
                }

                // Add a new chunk.
                PoolChunk<T> c = this.NewChunk(this.PageSize, this.maxOrder, this.PageShifts, this.ChunkSize);
                long handle = c.Allocate(normCapacity);
                ++this.NumNormalAllocations;
                Contract.Assert(handle > 0);
                c.InitBuf(buf, handle, reqCapacity);
                this.qInit.Add(c);
            }
        }

        void AllocateHuge(PooledArrayBuffer<T> buf, int reqCapacity)
        {
            PoolChunk<T> chunk = this.NewUnpooledChunk(reqCapacity);
            Interlocked.Add(ref this.activeBytesHuge, chunk.ChunkSize);
            buf.InitUnpooled(chunk, reqCapacity);
            Interlocked.Increment(ref this.allocationsHuge);
        }

        internal void Free(PoolChunk<T> chunk, long handle, int normCapacity, PoolThreadCache<T> cache)
        {
            if (chunk.Unpooled)
            {
                int size = chunk.ChunkSize;
                this.DestroyChunk(chunk);
                Interlocked.Add(ref this.activeBytesHuge, -size);
                Interlocked.Decrement(ref this.deallocationsHuge);
            }
            else
            {
                SizeClass sc = this.SizeClass(normCapacity);
                if (cache != null && cache.Add(this, chunk, handle, normCapacity, sc))
                {
                    // cached so not free it.
                    return;
                }

                this.FreeChunk(chunk, handle, sc);
            }
        }

        SizeClass SizeClass(int normCapacity)
        {
            if (!this.IsTinyOrSmall(normCapacity))
            {
                return Buffers.SizeClass.Normal;
            }
            return IsTiny(normCapacity) ? Buffers.SizeClass.Tiny : Buffers.SizeClass.Small;
        }

        internal void FreeChunk(PoolChunk<T> chunk, long handle, SizeClass sizeClass)
        {
            bool mustDestroyChunk;
            lock (this)
            {
                switch (sizeClass)
                {
                    case Buffers.SizeClass.Normal:
                        ++this.NumNormalDeallocations;
                        break;
                    case Buffers.SizeClass.Small:
                        ++this.NumSmallDeallocations;
                        break;
                    case Buffers.SizeClass.Tiny:
                        ++this.NumTinyDeallocations;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                mustDestroyChunk = !chunk.Parent.Free(chunk, handle);
            }
            if (mustDestroyChunk)
            {
                // destroyChunk not need to be called while holding the synchronized lock.
                this.DestroyChunk(chunk);
            }
        }

        internal PoolSubpage<T> FindSubpagePoolHead(int elemSize)
        {
            int tableIdx;
            PoolSubpage<T>[] table;
            if (IsTiny(elemSize))
            {
                // < 512
                tableIdx = elemSize.RightUShift(4);
                table = this.tinySubpagePools;
            }
            else
            {
                tableIdx = 0;
                elemSize = elemSize.RightUShift(10);
                while (elemSize != 0)
                {
                    elemSize = elemSize.RightUShift(1);
                    tableIdx++;
                }
                table = this.smallSubpagePools;
            }

            return table[tableIdx];
        }

        internal int NormalizeCapacity(int reqCapacity)
        {
            Contract.Requires(reqCapacity >= 0);

            if (reqCapacity >= this.ChunkSize)
            {
                return reqCapacity;
            }

            if (!IsTiny(reqCapacity))
            {
                // >= 512
                // Doubled

                int normalizedCapacity = reqCapacity;
                normalizedCapacity--;
                normalizedCapacity |= normalizedCapacity.RightUShift(1);
                normalizedCapacity |= normalizedCapacity.RightUShift(2);
                normalizedCapacity |= normalizedCapacity.RightUShift(4);
                normalizedCapacity |= normalizedCapacity.RightUShift(8);
                normalizedCapacity |= normalizedCapacity.RightUShift(16);
                normalizedCapacity++;

                if (normalizedCapacity < 0)
                {
                    normalizedCapacity = normalizedCapacity.RightUShift(1);
                }

                return normalizedCapacity;
            }

            // Quantum-spaced
            if ((reqCapacity & 15) == 0)
            {
                return reqCapacity;
            }

            return (reqCapacity & ~15) + 16;
        }

        internal void Reallocate(PooledArrayBuffer<T> buf, int newCapacity, bool freeOldMemory)
        {
            Contract.Requires(newCapacity >= 0 && newCapacity <= buf.Capacity);

            int oldCapacity = buf.Capacity;
            if (oldCapacity == newCapacity)
            {
                return;
            }

            PoolChunk<T> oldChunk = buf.Chunk;
            long oldHandle = buf.Handle;
            T[] oldMemory = buf.Array;
            int oldOffset = buf.Offset;
            int oldMaxLength = buf.Length;

            this.Allocate(this.Parent.ThreadCache(), buf, newCapacity);
            if (newCapacity > oldCapacity)
            {
                this.MemoryCopy(
                    oldMemory, oldOffset,
                    buf.Array, buf.Offset, oldCapacity);
            }
            else if (newCapacity < oldCapacity)
            {
                this.MemoryCopy(
                    oldMemory, oldOffset,
                    buf.Array, buf.Offset, newCapacity);
            }

            if (freeOldMemory)
            {
                this.Free(oldChunk, oldHandle, oldMaxLength, buf.Cache);
            }
        }

        public int NumTinySubpages => this.tinySubpagePools.Length;

        public int NumSmallSubpages => this.smallSubpagePools.Length;

        public int NumChunkLists => this.chunkListMetrics.Count;

        public IReadOnlyList<IPoolSubpageMetric> TinySubpages => SubPageMetricList(this.tinySubpagePools);

        public IReadOnlyList<IPoolSubpageMetric> SmallSubpages => SubPageMetricList(this.smallSubpagePools);

        public IReadOnlyList<IPoolChunkListMetric> ChunkLists => this.chunkListMetrics;

        static List<IPoolSubpageMetric> SubPageMetricList(PoolSubpage<T>[] pages)
        {
            var metrics = new List<IPoolSubpageMetric>();
            for (int i = 1; i < pages.Length; i++)
            {
                PoolSubpage<T> head = pages[i];
                if (head.Next == head)
                {
                    continue;
                }
                PoolSubpage<T> s = head.Next;
                for (;;)
                {
                    metrics.Add(s);
                    s = s.Next;
                    if (s == head)
                    {
                        break;
                    }
                }
            }
            return metrics;
        }

        public long NumAllocations => 
            this.NumTinyAllocations + this.NumSmallAllocations + this.NumNormalAllocations + this.NumHugeAllocations;

        public long NumTinyAllocations { get; set; }

        public long NumSmallAllocations { get; set; }

        public long NumNormalAllocations { get; set; }

        public long NumDeallocations => 
            this.NumTinyDeallocations + this.NumSmallDeallocations + this.NumNormalAllocations + this.NumHugeDeallocations;

        public long NumTinyDeallocations { get; set; }

        public long NumSmallDeallocations { get; set; }

        public long NumNormalDeallocations { get; set; }

        public long NumHugeAllocations => Volatile.Read(ref this.allocationsHuge);

        public long NumHugeDeallocations => Volatile.Read(ref this.deallocationsHuge);

        public long NumActiveAllocations => Math.Max(this.NumAllocations - this.NumDeallocations, 0);

        public long NumActiveTinyAllocations => Math.Max(this.NumTinyAllocations - this.NumTinyDeallocations, 0);

        public long NumActiveSmallAllocations => Math.Max(this.NumSmallAllocations - this.NumSmallDeallocations, 0);

        public long NumActiveNormalAllocations
        {
            get
            {
                long val;
                lock (this)
                {
                    val = this.NumNormalAllocations - this.NumNormalDeallocations;
                }
                return Math.Max(val, 0);
            }
        }

        public long NumActiveHugeAllocations => Math.Max(this.NumHugeAllocations - this.NumHugeDeallocations, 0);

        public long NumActiveBytes
        {
            get
            {
                long val = Volatile.Read(ref this.activeBytesHuge);
                lock (this)
                {
                    foreach (IPoolChunkListMetric chunkListMetric in this.chunkListMetrics)
                    {
                        foreach (IPoolChunkMetric m in chunkListMetric)
                        {
                            val += m.ChunkSize;
                        }
                    }
                }

                return Math.Max(0, val);
            }
        }

        protected PoolChunk<T> NewChunk(int newPageSize, int newMaxOrder, int newPageShifts, int newChunkSize) => 
            new PoolChunk<T>(this, new T[newChunkSize], newPageSize, newMaxOrder, newPageShifts, newChunkSize);

        protected PoolChunk<T> NewUnpooledChunk(int capacity) =>
            new PoolChunk<T>(this, new T[capacity], capacity);

        protected PooledArrayBuffer<T> NewByteBuf(int maxCapacity) =>
            PooledArrayBuffer<T>.NewInstance(maxCapacity);

        protected void MemoryCopy(T[] src, int srcOffset, T[] dst, int dstOffset, int length)
        {
            if (length == 0)
            {
                return;
            }

            Array.Copy(src, srcOffset, dst, dstOffset, length);
        }

        protected void DestroyChunk(PoolChunk<T> chunk)
        {
            // Rely on GC.
        }

        public override string ToString()
        {
            lock (this)
            {
                StringBuilder buf = new StringBuilder()
                    .Append("Chunk(s) at 0~25%:")
                    .Append(Environment.NewLine)
                    .Append(this.qInit)
                    .Append(Environment.NewLine)
                    .Append("Chunk(s) at 0~50%:")
                    .Append(Environment.NewLine)
                    .Append(this.q000)
                    .Append(Environment.NewLine)
                    .Append("Chunk(s) at 25~75%:")
                    .Append(Environment.NewLine)
                    .Append(this.q025)
                    .Append(Environment.NewLine)
                    .Append("Chunk(s) at 50~100%:")
                    .Append(Environment.NewLine)
                    .Append(this.q050)
                    .Append(Environment.NewLine)
                    .Append("Chunk(s) at 75~100%:")
                    .Append(Environment.NewLine)
                    .Append(this.q075)
                    .Append(Environment.NewLine)
                    .Append("Chunk(s) at 100%:")
                    .Append(Environment.NewLine)
                    .Append(this.q100)
                    .Append(Environment.NewLine)
                    .Append("tiny subpages:");
                for (int i = 1; i < this.tinySubpagePools.Length; i++)
                {
                    PoolSubpage<T> head = this.tinySubpagePools[i];
                    if (head.Next == head)
                    {
                        continue;
                    }

                    buf.Append(Environment.NewLine)
                        .Append(i)
                        .Append(": ");
                    PoolSubpage<T> s = head.Next;
                    for (;;)
                    {
                        buf.Append(s);
                        s = s.Next;
                        if (s == head)
                        {
                            break;
                        }
                    }
                }
                buf.Append(Environment.NewLine)
                    .Append("small subpages:");
                for (int i = 1; i < this.smallSubpagePools.Length; i++)
                {
                    PoolSubpage<T> head = this.smallSubpagePools[i];
                    if (head.Next == head)
                    {
                        continue;
                    }

                    buf.Append(Environment.NewLine)
                        .Append(i)
                        .Append(": ");
                    PoolSubpage<T> s = head.Next;
                    for (;;)
                    {
                        buf.Append(s);
                        s = s.Next;
                        if (s == head)
                        {
                            break;
                        }
                    }
                }
                buf.Append(Environment.NewLine);

                return buf.ToString();
            }
        }
    }

    public interface IPoolArenaMetric
    {
        /// Returns the number of thread caches backed by this arena.
        int NumThreadCaches { get; }

        /// Returns the number of tiny sub-pages for the arena.
        int NumTinySubpages { get; }

        /// Returns the number of small sub-pages for the arena.
        int NumSmallSubpages { get; }

        /// Returns the number of chunk lists for the arena.
        int NumChunkLists { get; }

        /// Returns an unmodifiable {@link List} which holds {@link PoolSubpageMetric}s for tiny sub-pages.
        IReadOnlyList<IPoolSubpageMetric> TinySubpages { get; }

        /// Returns an unmodifiable {@link List} which holds {@link PoolSubpageMetric}s for small sub-pages.
        IReadOnlyList<IPoolSubpageMetric> SmallSubpages { get; }

        /// Returns an unmodifiable {@link List} which holds {@link PoolChunkListMetric}s.
        IReadOnlyList<IPoolChunkListMetric> ChunkLists { get; }

        /// Return the number of allocations done via the arena. This includes all sizes.
        long NumAllocations { get; }

        /// Return the number of tiny allocations done via the arena.
        long NumTinyAllocations { get; }

        /// Return the number of small allocations done via the arena.
        long NumSmallAllocations { get; }

        /// Return the number of normal allocations done via the arena.
        long NumNormalAllocations { get; }

        /// Return the number of huge allocations done via the arena.
        long NumHugeAllocations { get; }

        /// Return the number of deallocations done via the arena. This includes all sizes.
        long NumDeallocations { get; }

        /// Return the number of tiny deallocations done via the arena.
        long NumTinyDeallocations { get; }

        /// Return the number of small deallocations done via the arena.
        long NumSmallDeallocations { get; }

        /// Return the number of normal deallocations done via the arena.
        long NumNormalDeallocations { get; }

        /// Return the number of huge deallocations done via the arena.
        long NumHugeDeallocations { get; }

        /// Return the number of currently active allocations.
        long NumActiveAllocations { get; }

        /// Return the number of currently active tiny allocations.
        long NumActiveTinyAllocations { get; }

        /// Return the number of currently active small allocations.
        long NumActiveSmallAllocations { get; }

        /// Return the number of currently active normal allocations.
        long NumActiveNormalAllocations { get; }

        /// Return the number of currently active huge allocations.
        long NumActiveHugeAllocations { get; }

        /// Return the number of active bytes that are currently allocated by the arena.
        long NumActiveBytes { get; }
    }
}
