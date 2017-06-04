// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Common;

    class PooledArrayBuffer<T> : AbstractReferenceCountedArrayBuffer<T>
    {
        static readonly ThreadLocalPool<PooledArrayBuffer<T>> Recycler = 
            new ThreadLocalPool<PooledArrayBuffer<T>>(handle => new PooledArrayBuffer<T>(handle, 0));

        readonly ThreadLocalPool.Handle recyclerHandle;

        protected internal PoolChunk<T> Chunk;
        protected internal long Handle;
        protected internal T[] Memory;
        protected internal int Offset;
        protected internal int Length;
        internal int MaxLength;
        internal PoolThreadCache<T> Cache;
        PooledArrayBufferAllocator<T> allocator;

        internal static PooledArrayBuffer<T> NewInstance(int maxCapacity)
        {
            PooledArrayBuffer<T> buf = Recycler.Take();
            buf.SetReferenceCount(1);
            buf.MaxCapacity = maxCapacity;
            buf.SetIndex(0, 0);
            buf.DiscardMarkers();
            return buf;
        }

        protected PooledArrayBuffer(ThreadLocalPool.Handle recyclerHandle, int maxCapacity)
            : base(maxCapacity)
        {
            this.recyclerHandle = recyclerHandle;
        }

        internal void Init(PoolChunk<T> chunk, long handle, int offset, int length, int maxLength,
            PoolThreadCache<T> cache)
        {
            this.Init0(chunk, handle, offset, length, maxLength, cache);
            this.DiscardMarkers();
        }

        internal void InitUnpooled(PoolChunk<T> chunk, int length) => this.Init0(chunk, 0, 0, length, length, null);

        void Init0(PoolChunk<T> chunk, long handle, int offset, int length, int maxLength, 
            PoolThreadCache<T> cache)
        {
            Contract.Assert(handle >= 0);
            Contract.Assert(chunk != null);

            this.Chunk = chunk;
            this.Memory = chunk.Memory;
            this.allocator = chunk.Arena.Parent;
            this.Cache = cache;
            this.Handle = handle;
            this.Offset = offset;
            this.Length = length;
            this.MaxLength = maxLength;
            this.SetIndex(0, 0);
        }

        public override int Capacity => this.Length;

        public sealed override IArrayBuffer<T> AdjustCapacity(int newCapacity)
        {
            this.CheckNewCapacity(newCapacity);

            // If the request capacity does not require reallocation, just update the length of the memory.
            if (this.Chunk.Unpooled)
            {
                if (newCapacity == this.Length)
                {
                    return this;
                }
            }
            else
            {
                if (newCapacity > this.Length)
                {
                    if (newCapacity <= this.MaxLength)
                    {
                        this.Length = newCapacity;
                        return this;
                    }
                }
                else if (newCapacity < this.Length)
                {
                    if (newCapacity > this.MaxLength.RightUShift(1))
                    {
                        if (this.MaxLength <= 512)
                        {
                            if (newCapacity > this.MaxLength - 16)
                            {
                                this.Length = newCapacity;
                                this.SetIndex(Math.Min(this.ReaderIndex, newCapacity), Math.Min(this.WriterIndex, newCapacity));
                                return this;
                            }
                        }
                        else
                        {
                            // > 512 (i.e. >= 1024)
                            this.Length = newCapacity;
                            this.SetIndex(Math.Min(this.ReaderIndex, newCapacity), Math.Min(this.WriterIndex, newCapacity));
                            return this;
                        }
                    }
                }
                else
                {
                    return this;
                }
            }

            // Reallocation required.
            this.Chunk.Arena.Reallocate(this, newCapacity, true);
            return this;
        }

        public sealed override IArrayBufferAllocator<T> Allocator => this.allocator;

        public sealed override IArrayBuffer<T> Unwrap() => null;

        protected sealed override void Deallocate()
        {
            if (this.Handle >= 0)
            {
                long handle = this.Handle;
                this.Handle = -1;
                this.Memory = default(T[]);
                this.Chunk.Arena.Free(this.Chunk, handle, this.MaxLength, this.Cache);
                this.Chunk = null;
                this.allocator = null;
                this.Recycle();
            }
        }

        void Recycle() => this.recyclerHandle.Release(this);

        protected int Idx(int index) => this.Offset + index;

        public override T Get(int index)
        {
            this.CheckIndex(index);
            return this.Array[this.Idx(index)];
        }

        public override IArrayBuffer<T> Get(int index, IArrayBuffer<T> dst, int dstIndex, int length)
        {
            this.CheckDstIndex(index, length, dstIndex, dst.Capacity);

            if (dst.HasArray)
            {
                this.Get(index, dst.Array, dst.ArrayOffset + dstIndex, length);
            }
            else
            {
                dst.Get(dstIndex, this.Memory, this.Idx(index), length);
            }

            return this;
        }

        public override IArrayBuffer<T> Get(int index, T[] dst, int dstIndex, int length)
        {
            this.CheckDstIndex(index, length, dstIndex, dst.Length);
            System.Array.Copy(this.Memory, this.Idx(index), dst, dstIndex, length);
            return this;
        }

        public override IArrayBuffer<T> Set(int index, T value)
        {
            this.CheckIndex(index);
            this.Memory[this.Idx(index)] = value;
            return this;
        }

        public override IArrayBuffer<T> Set(int index, IArrayBuffer<T> src, int srcIndex, int length)
        {
            this.CheckSrcIndex(index, length, srcIndex, src.Capacity);
            if (src.HasArray)
            {
                this.Set(index, src.Array, src.ArrayOffset + srcIndex, length);
            }
            else
            {
                src.Set(srcIndex, this.Memory, this.Idx(index), length);
            }
            return this;
        }

        public override IArrayBuffer<T> Set(int index, T[] src, int srcIndex, int length)
        {
            this.CheckSrcIndex(index, length, srcIndex, src.Length);
            System.Array.Copy(src, srcIndex, this.Memory, this.Idx(index), length);
            return this;
        }

        public override IArrayBuffer<T> Copy(int index, int length)
        {
            this.CheckIndex(index, length);
            IArrayBuffer<T> copy = this.Allocator.Buffer(length, this.MaxCapacity);
            copy.Write(this.Memory, this.Idx(index), length);
            return copy;
        }

        public override bool HasArray => true;

        public override T[] Array
        {
            get
            {
                this.EnsureAccessible();
                return this.Memory;
            }
        }

        public override int ArrayOffset => this.Offset;
    }
}
