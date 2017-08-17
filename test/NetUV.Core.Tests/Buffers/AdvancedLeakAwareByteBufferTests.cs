// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Buffers
{
    using System;
    using NetUV.Core.Buffers;
    using NetUV.Core.Common;

    public sealed class AdvancedLeakAwareByteBufferTests : SimpleLeakAwareByteBufferTests
    {
        protected override Type ByteBufferType => typeof(AdvancedLeakAwareByteBuffer);

        internal override IByteBuffer Wrap(IByteBuffer buffer, IResourceLeakTracker tracker) => new AdvancedLeakAwareByteBuffer(buffer, tracker);
    }
}
