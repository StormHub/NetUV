// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System.Diagnostics.Contracts;

    class WrappedByteBuffer<T> : IArrayBuffer<T>
    {
        readonly IArrayBuffer<T> buffer;

        internal WrappedByteBuffer(IArrayBuffer<T> buffer)
        {
            Contract.Requires(buffer != null);

            this.buffer = buffer;
        }

        public int ReferenceCount => this.buffer.ReferenceCount;

        public int Capacity => this.buffer.Capacity;

        public T[] Array => this.buffer.Array;

        public int Offset => this.buffer.Offset;

        public int Count => this.buffer.Count;

        public IReferenceCounted Retain(int increment = 1) => this.buffer.Retain(increment);

        public virtual IReferenceCounted Touch(object hint = null) => this.buffer.Touch(hint);

        public virtual bool Release(int decrement = 1) => this.buffer.Release(decrement);

        public IArrayBufferAllocator<T> Allocator => this.buffer.Allocator;

        public virtual IArrayBuffer<T> Copy() => this.buffer.Copy();

        public virtual IArrayBuffer<T> Copy(int index, int length) => this.buffer.Copy(index, length);

        public IArrayBuffer<T> Unwrap() => this.buffer;
    }
}
