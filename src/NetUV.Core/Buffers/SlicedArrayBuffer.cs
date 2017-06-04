// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;

    // Forked from https://github.com/Azure/DotNetty
    sealed class SlicedArrayBuffer<T> : AbstractDerivedArrayBuffer<T>
    {
        readonly IArrayBuffer<T> buffer;
        readonly int adjustment;
        readonly int sliceLength;

        public SlicedArrayBuffer(IArrayBuffer<T> buffer, int index, int length)
            : base(length)
        {
            if (index < 0 
                || index > buffer.Capacity - length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"{buffer}.slice({index}, {length})");
            }

            var slicedByteBuf = buffer as SlicedArrayBuffer<T>;
            if (slicedByteBuf != null)
            {
                this.buffer = slicedByteBuf.buffer;
                this.adjustment = slicedByteBuf.adjustment + index;
            }
            else if (buffer is DuplicatedArrayBuffer<T>)
            {
                this.buffer = buffer.Unwrap();
                this.adjustment = index;
            }
            else
            {
                this.buffer = buffer;
                this.adjustment = index;
            }
            this.sliceLength = length;
            this.SetWriterIndex(length);
        }

        public override IArrayBuffer<T> Unwrap() => this.buffer;

        public override IArrayBufferAllocator<T> Allocator => this.buffer.Allocator;

        public override int Capacity => this.sliceLength;

        public override IArrayBuffer<T> AdjustCapacity(int newCapacity)
        {
            throw new NotSupportedException($"{nameof(SlicedArrayBuffer<T>)}");
        }

        public override bool HasArray => this.buffer.HasArray;

        public override T[] Array => this.buffer.Array;

        public override int ArrayOffset => this.buffer.ArrayOffset + this.adjustment;

        public override IArrayBuffer<T> Duplicate()
        {
            IArrayBuffer<T> duplicate = this.buffer.Slice(this.adjustment, this.sliceLength);
            duplicate.SetIndex(this.ReaderIndex, this.WriterIndex);
            return duplicate;
        }

        public override IArrayBuffer<T> Copy(int index, int length)
        {
            this.CheckIndex(index, length);
            return this.buffer.Copy(index + this.adjustment, length);
        }

        public override IArrayBuffer<T> Slice(int index, int length)
        {
            this.CheckIndex(index, length);
            if (length == 0)
            {
                return this.Allocator.EmptyBuffer;
            }

            return this.buffer.Slice(index + this.adjustment, length);
        }

        public override T Get(int index)
        {
            this.CheckIndex(index);
            return this.buffer.Get(index + this.adjustment);
        }

        public override IArrayBuffer<T> Get(int index, IArrayBuffer<T> dst, int dstIndex, int length)
        {
            this.CheckIndex(index, length);
            this.buffer.Get(index + this.adjustment, dst, dstIndex, length);
            return this;
        }

        public override IArrayBuffer<T> Get(int index, T[] dst, int dstIndex, int length)
        {
            this.CheckIndex(index, length);
            this.buffer.Get(index + this.adjustment, dst, dstIndex, length);
            return this;
        }

        public override IArrayBuffer<T> Set(int index, T[] src, int srcIndex, int length)
        {
            this.CheckIndex(index, length);
            this.buffer.Set(index + this.adjustment, src, srcIndex, length);
            return this;
        }

        public override IArrayBuffer<T> Set(int index, T value)
        {
            this.CheckIndex(index);
            this.buffer.Set(index + this.adjustment, value);
            return this;
        }

        public override IArrayBuffer<T> Set(int index, IArrayBuffer<T> src, int srcIndex, int length)
        {
            this.CheckIndex(index, length);
            this.buffer.Set(index + this.adjustment, src, srcIndex, length);
            return this;
        }
    }
}
