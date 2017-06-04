// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    // Forked from https://github.com/Azure/DotNetty
    sealed class DuplicatedArrayBuffer<T> : AbstractDerivedArrayBuffer<T>
    {
        readonly IArrayBuffer<T> buffer;

        public DuplicatedArrayBuffer(IArrayBuffer<T> source)
            : base(source.MaxCapacity)
        {
            var asDuplicate = source as DuplicatedArrayBuffer<T>;
            this.buffer = asDuplicate != null ? asDuplicate.buffer : source;
            this.SetIndex(source.ReaderIndex, source.WriterIndex);
        }

        public override int Capacity => this.buffer.Capacity;

        public override IArrayBuffer<T> AdjustCapacity(int newCapacity) => this.buffer.AdjustCapacity(newCapacity);

        public override IArrayBufferAllocator<T> Allocator => this.buffer.Allocator;

        public override T Get(int index)
        {
            return this.buffer.Get(index);
        }

        public override IArrayBuffer<T> Get(int index, IArrayBuffer<T> destination, int dstIndex, int length)
        {
            this.buffer.Get(index, destination, dstIndex, length);
            return this;
        }

        public override IArrayBuffer<T> Get(int index, T[] destination, int dstIndex, int length)
        {
            this.buffer.Get(index, destination, dstIndex, length);
            return this;
        }

        public override IArrayBuffer<T> Get(int index, IArrayBuffer<T> destination)
        {
            this.buffer.Get(index, destination);
            return this;
        }

        public override IArrayBuffer<T> Set(int index, T value)
        {
            this.buffer.Set(index, value);
            return this;
        }

        public override IArrayBuffer<T> Set(int index, IArrayBuffer<T> src, int srcIndex, int length)
        {
            this.buffer.Set(index, src, srcIndex, length);
            return this;
        }

        public override IArrayBuffer<T> Set(int index, T[] src, int srcIndex, int length)
        {
            this.buffer.Set(index, src, srcIndex, length);
            return this;
        }

        public override bool HasArray => this.buffer.HasArray;

        public override T[] Array => this.buffer.Array;

        public override IArrayBuffer<T> Copy(int index, int length) => this.buffer.Copy(index, length);

        public override int ArrayOffset => this.buffer.ArrayOffset;

        public override IArrayBuffer<T> Unwrap() => this.buffer;
    }
}
