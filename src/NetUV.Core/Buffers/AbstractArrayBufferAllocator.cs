// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using NetUV.Core.Common;

    // Forked from https://github.com/Azure/DotNetty
    abstract class AbstractArrayBufferAllocator<T> : IArrayBufferAllocator<T>
    {
        public const int DefaultInitialCapacity = 256;
        public const int DefaultMaxComponents = 16;
        public const int DefaultMaxCapacity = int.MaxValue;

        protected static IArrayBuffer<T> ToLeakAwareBuffer(IArrayBuffer<T> buf)
        {
            IResourceLeak leak;
            switch (ResourceLeakDetector.Level)
            {
                case ResourceLeakDetector.DetectionLevel.Simple:
                    leak = AbstractArrayBuffer<T>.LeakDetector.Open(buf);
                    if (leak != null)
                    {
                        buf = new SimpleLeakAwareArrayBuffer<T>(buf, leak);
                    }
                    break;
                case ResourceLeakDetector.DetectionLevel.Advanced:
                case ResourceLeakDetector.DetectionLevel.Paranoid:
                    leak = AbstractArrayBuffer<T>.LeakDetector.Open(buf);
                    if (leak != null)
                    {
                        buf = new AdvancedLeakAwareArrayBuffer<T>(buf, leak);
                    }
                    break;
                case ResourceLeakDetector.DetectionLevel.Disabled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown type {ResourceLeakDetector.Level}");
            }
            return buf;
        }

        protected AbstractArrayBufferAllocator()
        {
            this.EmptyBuffer = new EmptyArrayBuffer<T>(this);
        }

        public IArrayBuffer<T> EmptyBuffer { get; }

        public IArrayBuffer<T> Buffer() => this.Buffer(DefaultInitialCapacity, int.MaxValue);

        public IArrayBuffer<T> Buffer(int initialCapacity) => this.Buffer(initialCapacity, int.MaxValue);

        public IArrayBuffer<T> Buffer(int initialCapacity, int maxCapacity)
        {
            if (initialCapacity == 0 && maxCapacity == 0)
            {
                return this.EmptyBuffer;
            }

            Validate(initialCapacity, maxCapacity);

            return this.NewBuffer(initialCapacity, maxCapacity);
        }

        protected abstract IArrayBuffer<T> NewBuffer(int initialCapacity, int maxCapacity);

        static void Validate(int initialCapacity, int maxCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), 
                    "initialCapacity must be greater than zero");
            }

            if (initialCapacity > maxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), 
                    $"initialCapacity ({initialCapacity}) must be greater than maxCapacity ({maxCapacity})");
            }
        }
    }
}
