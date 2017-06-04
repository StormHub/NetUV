// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System.Diagnostics.Contracts;

    sealed class ByteBufferAllocator : IByteBufferAllocator
    {
        internal static readonly ByteBufferAllocator Pooled;
        
        readonly IArrayBufferAllocator<byte> allocator;

        static ByteBufferAllocator()
        {
            Pooled = new ByteBufferAllocator(new PooledArrayBufferAllocator<byte>());
        }

        ByteBufferAllocator(IArrayBufferAllocator<byte> arrayBufferAllocator)
        {
            Contract.Requires(arrayBufferAllocator != null);

            this.allocator = arrayBufferAllocator;
        }

        public IArrayBuffer<byte> Buffer()
        {
            return this.allocator.Buffer();
        }

        public IArrayBuffer<byte> Buffer(int initialCapacity)
        {
            return this.allocator.Buffer(initialCapacity);
        }

        public IArrayBuffer<byte> Buffer(int initialCapacity, int maxCapacity)
        {
            return this.allocator.Buffer(initialCapacity, maxCapacity);
        }
    }
}
