// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System.Diagnostics.Contracts;

    sealed class ByteBufferAllocator : IByteBufferAllocator
    {
        internal static readonly ByteBufferAllocator Default;
        internal static readonly ByteBuffer EmptyByteBuffer;

        static ByteBufferAllocator()
        {
            Default = new ByteBufferAllocator();
            EmptyByteBuffer = new ByteBuffer(Default.ArrayAllocator.EmptyBuffer);
        }

        internal ByteBufferAllocator() 
            : this(new PooledArrayBufferAllocator<byte>())
        { }

        internal ByteBufferAllocator(IArrayBufferAllocator<byte> arrayAllocator)
        {
            Contract.Requires(arrayAllocator != null);

            this.ArrayAllocator = arrayAllocator;
        }

        internal IArrayBufferAllocator<byte> ArrayAllocator { get; }

        internal ByteBuffer Buffer()
        {
            IArrayBuffer<byte> buffer = this.ArrayAllocator.Buffer();
            var byteBuffer = new ByteBuffer(buffer);
            return byteBuffer;
        }

        internal ByteBuffer Buffer(int initialCapacity)
        {
            IArrayBuffer<byte> buffer = this.ArrayAllocator.Buffer(initialCapacity);
            var byteBuffer = new ByteBuffer(buffer);
            return byteBuffer;
        }

        internal ByteBuffer Buffer(int initialCapacity, int maxCapacity)
        {
            IArrayBuffer<byte> buffer = this.ArrayAllocator.Buffer(initialCapacity, maxCapacity);
            var byteBuffer = new ByteBuffer(buffer);
            return byteBuffer;
        }

        WritableBuffer IByteBufferAllocator.Buffer() => 
            new WritableBuffer(this.Buffer());

        WritableBuffer IByteBufferAllocator.Buffer(int initialCapacity) => 
            new WritableBuffer(this.Buffer(initialCapacity));

        WritableBuffer IByteBufferAllocator.Buffer(int initialCapacity, int maxCapacity) => 
            new WritableBuffer(this.Buffer(initialCapacity, maxCapacity));
    }
}
