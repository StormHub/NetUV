// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Buffers
{
    using NetUV.Core.Buffers;

    public class WrappedCompositeByteBufferTests : CompositeByteBufferTests
    {
        internal sealed override IByteBuffer NewBuffer(int length, int maxCapacity) => this.Wrap((CompositeByteBuffer)base.NewBuffer(length, maxCapacity));

        internal virtual IByteBuffer Wrap(CompositeByteBuffer buffer) => new WrappedCompositeByteBuffer(buffer);
    }
}
