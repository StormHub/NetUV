// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    sealed class UnpooledArrayBufferAllocator<T> : ArrayBufferAllocator<T>
    {
        protected override IArrayBuffer<T> NewBuffer(int initialCapacity, int capacity) => 
            new UnpooledArrayBuffer<T>(this, initialCapacity, capacity);
    }
}
