// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    interface IByteBufferAllocator
    {
        IByteBuffer Buffer();

        IByteBuffer Buffer(int initialCapacity);

        IByteBuffer Buffer(int initialCapacity, int maxCapacity);

        IByteBuffer HeapBuffer();

        IByteBuffer HeapBuffer(int initialCapacity);

        IByteBuffer HeapBuffer(int initialCapacity, int maxCapacity);

        CompositeByteBuffer CompositeBuffer();

        CompositeByteBuffer CompositeBuffer(int maxComponents);

        CompositeByteBuffer CompositeHeapBuffer();

        CompositeByteBuffer CompositeHeapBuffer(int maxComponents);

        int CalculateNewCapacity(int minNewCapacity, int maxCapacity);
    }
}
