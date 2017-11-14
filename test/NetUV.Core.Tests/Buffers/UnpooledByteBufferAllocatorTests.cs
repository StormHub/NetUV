// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Buffers
{
    using NetUV.Core.Buffers;

    public class UnpooledByteBufferAllocatorTests : AbstractByteBufferAllocatorTests
    {
        internal override IByteBufferAllocator NewAllocator(bool preferDirect) => new UnpooledByteBufferAllocator(preferDirect);

        internal override IByteBufferAllocator NewUnpooledAllocator() => new UnpooledByteBufferAllocator(false);
    }
}
