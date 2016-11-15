// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    interface IArrayBufferAllocator<T>
    {
        IArrayBuffer<T> EmptyBuffer { get; }

        IArrayBuffer<T> Buffer();

        IArrayBuffer<T> Buffer(int initialCapacity);

        IArrayBuffer<T> Buffer(int initialCapacity, int maxCapacity);
    }
}
