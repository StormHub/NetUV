// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using NetUV.Core.Common;

    abstract class ArrayBuffer<T> : ReferenceCounted, IArrayBuffer<T>
    {
        internal ArrayBuffer(int capacity)
            : base(capacity)
        { }

        public T[] Array { get; internal set; }

        public int Offset { get; internal set; }

        public int Count { get; internal set; }

        internal int Length => this.Array?.Length ?? 0;

        public abstract IArrayBufferAllocator<T> Allocator { get; }

        internal abstract IArrayBuffer<T> AdjustCapacity(int newCapacity);

        public IArrayBuffer<T> Copy() => 
            this.Count > 0 ? this.Copy(0, this.Count) : this;

        public IArrayBuffer<T> Copy(int index, int length)
        {
            if (length <= 0 
                || index < 0
                || index > this.Count - length - this.Offset)
            {
                throw new IndexOutOfRangeException(
                    $"{nameof(index)}:{index}, {nameof(length)}:{length} (expected: range(0, {this.Count - this.Offset})");
            }

            IArrayBuffer<T> copy = this.Allocator.Buffer(length, this.Capacity);
            this.Array.CopyBlock(copy.Array, this.Offset + index, length);

            return copy;
        }
    }
}
