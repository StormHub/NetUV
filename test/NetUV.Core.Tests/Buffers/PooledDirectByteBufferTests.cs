// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Buffers
{
    using NetUV.Core.Buffers;

    public sealed class PooledDirectByteBufferTests : AbstractPooledByteBufferTests
    {
        internal override IByteBuffer Alloc(int length, int maxCapacity) => PooledByteBufferAllocator.Default.DirectBuffer(length, maxCapacity);
    }
}
