// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Buffers
{
    using System;
    using NetUV.Core.Buffers;
    using Xunit;

    public class ByteBufferTests
    {
        [Fact]
        public void ValidateIndex()
        {
            // Empty
            ByteBuffer byteBuffer = UnpooledByteBuffer.From(new byte[0]);
            
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(0));
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(-1));
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(1));

            // Unpooled
            var bytes = new byte[10];
            byteBuffer = UnpooledByteBuffer.From(bytes, 0, 5);
            byteBuffer.Validate(0);
            byteBuffer.Validate(4);
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(-1));
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(5));

            byteBuffer = UnpooledByteBuffer.From(bytes, 4, 5);
            byteBuffer.Validate(0);
            byteBuffer.Validate(4);
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(-1));
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(5));

            // Pooled
            byteBuffer = ByteBufferAllocator.Default.Buffer(5, 5);
            byteBuffer.Validate(0);
            byteBuffer.Validate(4);
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(-1));
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(5));
        }

        [Fact]
        public void ValidateIndexAndLength()
        {
            // Empty
            ByteBuffer byteBuffer = UnpooledByteBuffer.From(new byte[0]);

            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(0, -1));
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(-1, -1));
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(1, 0));

            // Unpooled
            var bytes = new byte[10];
            byteBuffer = UnpooledByteBuffer.From(bytes, 0, 5);
            byteBuffer.Validate(0, 5);
            byteBuffer.Validate(4, 1);
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(-1));
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(5, 1));

            byteBuffer = UnpooledByteBuffer.From(bytes, 4, 5);
            byteBuffer.Validate(0, 5);
            byteBuffer.Validate(4, 1);
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(-1));
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(5, 1));

            // Pooled
            byteBuffer = ByteBufferAllocator.Default.Buffer(5, 5);
            byteBuffer.Validate(0, 5);
            byteBuffer.Validate(4, 1);
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(-1));
            Assert.Throws<IndexOutOfRangeException>(() => byteBuffer.Validate(5, 1));
        }
    }
}
