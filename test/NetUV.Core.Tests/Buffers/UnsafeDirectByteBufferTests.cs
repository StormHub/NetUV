// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Buffers
{
    using NetUV.Core.Buffers;
    using Xunit;

    public class UnsafeDirectByteBufferTest : AbstractByteBufferTests
    {
        internal override IByteBuffer NewBuffer(int length, int maxCapacity)
        {
            IByteBuffer buffer = this.NewDirectBuffer(length, maxCapacity);
            Assert.Equal(0, buffer.WriterIndex);
            return buffer;
        }

        internal IByteBuffer NewDirectBuffer(int length, int maxCapacity) =>
            new UnpooledUnsafeDirectByteBuffer(UnpooledByteBufferAllocator.Default, length, maxCapacity);
    }
}
