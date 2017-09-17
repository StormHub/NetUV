// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Buffers
{
    using System;
    using NetUV.Core.Buffers;
    using NetUV.Core.Common;

    public sealed class AdvancedLeakAwareCompositeByteBufferTests : SimpleLeakAwareCompositeByteBufferTests
    {
        internal override IByteBuffer Wrap(CompositeByteBuffer buffer, IResourceLeakTracker tracker) => new AdvancedLeakAwareCompositeByteBuffer(buffer, tracker);

        protected override Type ByteBufferType => typeof(AdvancedLeakAwareByteBuffer);
    }
}
