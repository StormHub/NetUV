// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System.Diagnostics.Contracts;

    class UnpooledArrayBuffer<T> : AbstractReferenceCountedArrayBuffer<T>
    {
        T[] array;

        public UnpooledArrayBuffer(IArrayBufferAllocator<T> allocator, int initialCapacity, int maxCapacity)
            : this(allocator, new T[initialCapacity], 0, 0, maxCapacity)
        {
        }

        public UnpooledArrayBuffer(IArrayBufferAllocator<T> allocator, T[] initialArray, int maxCapacity)
            : this(allocator, initialArray, 0, initialArray.Length, maxCapacity)
        {
        }
        public UnpooledArrayBuffer(IArrayBufferAllocator<T> allocator, T[] initialArray, 
            int readerIndex, int writerIndex, int maxCapacity)
            : base(maxCapacity)
        {
            Contract.Requires(allocator != null);
            Contract.Requires(initialArray != null);
            Contract.Requires(initialArray.Length <= maxCapacity);

            this.Allocator = allocator;
            this.SetArray(initialArray);
            this.SetIndex(readerIndex, writerIndex);
        }

        protected void SetArray(T[] initialArray) => this.array = initialArray;

        public override IArrayBufferAllocator<T> Allocator { get; }

        public override int Capacity
        {
            get
            {
                this.EnsureAccessible();
                return this.array.Length;
            }
        }

        public override IArrayBuffer<T> AdjustCapacity(int newCapacity)
        {
            this.CheckNewCapacity(newCapacity);

            int oldCapacity = this.array.Length;
            if (newCapacity > oldCapacity)
            {
                var newArray = new T[newCapacity];
                System.Array.Copy(this.array, 0, newArray, 0, this.array.Length);
                this.SetArray(newArray);
            }
            else if (newCapacity < oldCapacity)
            {
                var newArray = new T[newCapacity];
                int readerIndex = this.ReaderIndex;
                if (readerIndex < newCapacity)
                {
                    int writerIndex = this.WriterIndex;
                    if (writerIndex > newCapacity)
                    {
                        this.SetWriterIndex(writerIndex = newCapacity);
                    }
                    System.Array.Copy(this.array, readerIndex, newArray, readerIndex, writerIndex - readerIndex);
                }
                else
                {
                    this.SetIndex(newCapacity, newCapacity);
                }
                this.SetArray(newArray);
            }
            return this;
        }

        public override bool HasArray => true;

        public override T[] Array
        {
            get
            {
                this.EnsureAccessible();
                return this.array;
            }
        }

        public override int ArrayOffset => 0;

        public override T Get(int index)
        {
            this.EnsureAccessible();
            return this.array[index];
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
                dst.Get(dstIndex, this.array, index, length);
            }

            return this;
        }

        public override IArrayBuffer<T> Get(int index, T[] dst, int dstIndex, int length)
        {
            this.CheckDstIndex(index, length, dstIndex, dst.Length);
            System.Array.Copy(this.array, index, dst, dstIndex, length);
            return this;
        }

        public override IArrayBuffer<T> Set(int index, T value)
        {
            this.EnsureAccessible();
            this.array[index] = value;
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
                src.Set(srcIndex, this.array, index, length);
            }

            return this;
        }

        public override IArrayBuffer<T> Set(int index, T[] src, int srcIndex, int length)
        {
            this.CheckSrcIndex(index, length, srcIndex, src.Length);
            System.Array.Copy(src, srcIndex, this.array, index, length);
            return this;
        }

        public override IArrayBuffer<T> Copy(int index, int length)
        {
            this.CheckIndex(index, length);
            var copiedArray = new T[length];
            System.Array.Copy(this.array, index, copiedArray, 0, length);
            return new UnpooledArrayBuffer<T>(this.Allocator, copiedArray, this.MaxCapacity);
        }

        protected override void Deallocate() => this.array = null;

        public override IArrayBuffer<T> Unwrap() => null;
    }
}
