// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    interface IArrayBuffer<T> : IReferenceCounted
    {
        int Capacity { get; }

        T[] Array { get; }

        int Offset { get; }

        int Count { get; }

        IArrayBufferAllocator<T> Allocator { get; }

        IArrayBuffer<T> Copy();

        IArrayBuffer<T> Copy(int index, int length);
    }
}
