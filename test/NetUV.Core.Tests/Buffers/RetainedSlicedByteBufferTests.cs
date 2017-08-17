// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Buffers
{
    using NetUV.Core.Buffers;
    using Xunit;

    public sealed class RetainedSlicedByteBufferTests : SlicedByteBufferTest
    {
        internal override IByteBuffer NewSlice(IByteBuffer buffer, int offset, int length)
        {
            IByteBuffer slice = buffer.RetainedSlice(offset, length);
            buffer.Release();
            Assert.Equal(buffer.ReferenceCount, slice.ReferenceCount);
            return slice;
        }
    }
}
