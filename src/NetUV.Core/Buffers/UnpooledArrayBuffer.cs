// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Common;

    sealed class UnpooledArrayBuffer<T> : ArrayBuffer<T>
    {
        public UnpooledArrayBuffer(IArrayBufferAllocator<T> allocator, int initialCapacity, int capacity)
            : this(allocator, new T[initialCapacity], capacity)
        { }

        public UnpooledArrayBuffer(IArrayBufferAllocator<T> allocator, T[] initialArray, int capacity)
            : this(allocator, initialArray, 0, capacity)
        { }

        public UnpooledArrayBuffer(IArrayBufferAllocator<T> allocator, T[] initialArray, int offset, int count)
            : base(count)
        {
            Contract.Requires(initialArray != null && initialArray.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= initialArray.Length);

            this.Allocator = allocator;
            this.Array = initialArray;
            this.Offset = offset;
            this.Count = count;
        }

        public override IArrayBufferAllocator<T> Allocator { get; }

        internal override IArrayBuffer<T> AdjustCapacity(int newCapacity)
        {
            Contract.Requires(newCapacity >= 0 && newCapacity <= this.Capacity);

            int oldCapacity = this.Length;
            var newArray = new T[newCapacity];
            this.Array.CopyBlock(newArray, 0, Math.Min(oldCapacity, newCapacity));
            this.Array = newArray;

            return this;
        }

        protected override void Deallocate()
        {
            this.Array = null;
            this.Offset = 0;
            this.Count = 0;
        }
    }
}
