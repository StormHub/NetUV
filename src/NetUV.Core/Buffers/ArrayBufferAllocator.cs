// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Common;

    abstract class ArrayBufferAllocator<T> : IArrayBufferAllocator<T>
    {
        const int DefaultInitialCapacity = 256;

        protected ArrayBufferAllocator()
        {
            this.EmptyBuffer = new EmptyArrayBuffer<T>(this);
        }

        protected static IArrayBuffer<T> ToLeakAwareBuffer(IArrayBuffer<T> buffer)
        {
            IResourceLeak leak = ReferenceCounted.LeakDetector.Open(buffer);

            return leak != null 
                ? new LeakAwareByteBuffer<T>(buffer, leak) 
                : buffer;
        }

        public IArrayBuffer<T> EmptyBuffer { get; }

        public IArrayBuffer<T> Buffer() => 
            this.Buffer(DefaultInitialCapacity, int.MaxValue);

        public IArrayBuffer<T> Buffer(int initialCapacity) => 
            this.Buffer(initialCapacity, int.MaxValue);

        public IArrayBuffer<T> Buffer(int initialCapacity, int capacity)
        {
            Contract.Ensures(Contract.Result<ArrayBuffer<T>>() != null);

            if (initialCapacity == 0 
                && capacity == 0)
            {
                return this.EmptyBuffer;
            }

            Validate(initialCapacity, capacity);
            return this.NewBuffer(initialCapacity, capacity);
        }

        static void Validate(int initialCapacity, int capacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), 
                    $"initialCapacity {initialCapacity} must be greater than zero");
            }

            if (initialCapacity > capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), 
                    $"{nameof(initialCapacity)} ({initialCapacity}) must be less than or equal to {nameof(capacity)} ({capacity})");
            }
        }

        protected abstract IArrayBuffer<T> NewBuffer(int initialCapacity, int capacity);
    }
}
