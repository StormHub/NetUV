// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System.Diagnostics.Contracts;

    sealed class EmptyArrayBuffer<T> : IArrayBuffer<T>
    {
        internal static readonly T[] EmptyArray = new T[0];

        internal EmptyArrayBuffer(IArrayBufferAllocator<T> allocator)
        {
            Contract.Requires(allocator != null);

            this.Allocator = allocator;
        }

        public int Capacity => 0;

        public int MaxCapacity => 0;

        public T[] Array => EmptyArray;

        public int Offset => 0;

        public int Count => 0;

        public int ReferenceCount => 1;

        public IReferenceCounted Retain(int increment = 1) => this;

        public IReferenceCounted Touch(object hint = null) => this;

        public bool Release(int decrement = 1) => false;

        public int CompareTo(IArrayBuffer<T> other) => other.Count > 0  ? -1 : 0;

        public override int GetHashCode() => 0;

        public override bool Equals(object obj)
        {
            var buffer = obj as IArrayBuffer<T>;
            return this.Equals(buffer);
        }

        public bool Equals(IArrayBuffer<T> other) => other != null && other.Count == 0;

        public IArrayBufferAllocator<T> Allocator { get; }

        public IArrayBuffer<T> Copy() => this;

        public IArrayBuffer<T> Copy(int index, int length) => this;

        public IArrayBuffer<T> Slice() => this;

        public IArrayBuffer<T> Slice(int index, int length) => this;
    }
}
