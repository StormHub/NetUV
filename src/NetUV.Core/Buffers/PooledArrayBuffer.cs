// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System.Diagnostics.Contracts;
    using NetUV.Core.Common;

    sealed class PooledArrayBuffer<T> : ArrayBuffer<T>
    {
        static readonly ThreadLocalPool<PooledArrayBuffer<T>> Recycler =
            new ThreadLocalPool<PooledArrayBuffer<T>>(handle => new PooledArrayBuffer<T>(handle, 0));

        readonly ThreadLocalPool.Handle recyclerHandle;

        internal PoolChunk<T> Chunk;
        internal long Handle;
        internal PoolThreadCache<T> Cache;

        internal static PooledArrayBuffer<T> NewInstance(int capacity)
        {
            PooledArrayBuffer<T> buf = Recycler.Take();
            buf.Recycle(capacity);

            return buf;
        }

        internal PooledArrayBuffer(ThreadLocalPool.Handle recyclerHandle, int capacity)
            : base(capacity)
        {
            this.recyclerHandle = recyclerHandle;
            this.Capacity = capacity;
        }

        internal void Init(PoolChunk<T> chunk, long handle, int offset, int length, PoolThreadCache<T> cache)
        {
            Contract.Assert(handle >= 0);
            Contract.Assert(chunk != null);

            this.Chunk = chunk;
            this.Handle = handle;
            // ReSharper disable once PossibleNullReferenceException
            this.Array = chunk.Memory;
            this.Offset = offset;
            this.Count = length;
            this.Cache = cache;
        }

        internal void InitUnpooled(PoolChunk<T> chunk, int length)
        {
            Contract.Assert(chunk != null);

            this.Chunk = chunk;
            this.Handle = 0;
            // ReSharper disable once PossibleNullReferenceException
            this.Array = chunk.Memory;
            this.Offset = 0;
            this.Count = length;
            this.Cache = null;
        }

        public override IArrayBufferAllocator<T> Allocator => this.Chunk.Arena.Parent;

        internal override IArrayBuffer<T> AdjustCapacity(int newCapacity)
        {
            // If the request capacity does not require reallocation, just update the length of the memory.
            if (newCapacity == this.Length)
            {
                return this;
            }

            if (newCapacity < this.Length)
            {
                if (newCapacity > this.Length.RightUShift(1))
                {
                    if (this.Length <= 512)
                    {
                        if (newCapacity > this.Length - 16)
                        {
                            this.Count = newCapacity;
                            return this;
                        }
                    }
                    else
                    {
                        // > 512 (i.e. >= 1024)
                        this.Count = newCapacity;
                        return this;
                    }
                }
            }

            // Reallocation required.
            this.Chunk.Arena.Reallocate(this, newCapacity, true);
            return this;
        }

        protected override void Deallocate()
        {
            if (this.Handle < 0)
            {
                return;
            }

            long handle = this.Handle;
            this.Handle = -1;
            this.Array = default(T[]);
            this.Chunk.Arena.Free(this.Chunk, handle, this.Count, this.Cache);
            this.Release();
        }

        void Release() => this.recyclerHandle.Release(this);
    }
}
