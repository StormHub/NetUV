// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace NetUV.Core.Tests.Buffers
{
    using System;
    using System.Linq;
    using Xunit;
    using NetUV.Core.Buffers;

    // Forked from https://github.com/Azure/DotNetty
    public sealed class UnpooledBufferAllocatorTests
    {
        static readonly IArrayBuffer<byte>[] EmptyByteBuffer = new IArrayBuffer<byte>[0];
        static readonly byte[] EmptyBytes = { };

        [Fact]
        public void ShouldReturnEmptyBufferWhenLengthIsZero()
        {
            AssertSameAndRelease(Unpooled.Empty, Unpooled.WrappedBuffer(EmptyBytes));
            AssertSameAndRelease(Unpooled.Empty, Unpooled.WrappedBuffer(new byte[8], 0, 0));
            AssertSameAndRelease(Unpooled.Empty, Unpooled.WrappedBuffer(new byte[8], 8, 0));
            AssertSameAndRelease(Unpooled.Empty, Unpooled.WrappedBuffer(Unpooled.Empty));
            AssertSameAndRelease(Unpooled.Empty, Unpooled.CopiedBuffer(EmptyBytes));
            AssertSameAndRelease(Unpooled.Empty, Unpooled.CopiedBuffer(new byte[8], 0, 0));
            AssertSameAndRelease(Unpooled.Empty, Unpooled.CopiedBuffer(new byte[8], 8, 0));
            AssertSameAndRelease(Unpooled.Empty, Unpooled.CopiedBuffer(Unpooled.Empty));
            AssertSameAndRelease(Unpooled.Empty, Unpooled.CopiedBuffer(new[] { EmptyBytes }));
            AssertSameAndRelease(Unpooled.Empty, Unpooled.CopiedBuffer(EmptyByteBuffer));
            AssertSameAndRelease(Unpooled.Empty, Unpooled.CopiedBuffer(new[] { Unpooled.Buffer(0) }));
            AssertSameAndRelease(Unpooled.Empty, Unpooled.CopiedBuffer(Unpooled.Buffer(0), Unpooled.Buffer(0)));
        }

        static void AssertSameAndRelease(IArrayBuffer<byte> expected, IArrayBuffer<byte> actual)
        {
            Assert.Same(expected, actual);
            expected.Release();
            actual.Release();
        }

        static void AssertEqualAndRelease(IArrayBuffer<byte> expected, IArrayBuffer<byte> actual)
        {
            byte[] expectedData = expected.ToArray();
            byte[] actualData = actual.ToArray();
            Assert.True(expectedData.SequenceEqual(actualData));
            expected.Release();
            actual.Release();
        }

        [Fact]
        public void TestSingleWrappedByteBufReleased()
        {
            IArrayBuffer<byte> buf = Unpooled.Buffer(12).Write(new byte[]{ 0 });
            IArrayBuffer<byte> wrapped = Unpooled.WrappedBuffer(buf);
            Assert.True(wrapped.Release());
            Assert.Equal(0, buf.ReferenceCount);
        }

        [Fact]
        public void TestCopiedBuffer()
        {
            AssertEqualAndRelease(Unpooled.WrappedBuffer(new byte[] { 1, 2, 3 }),
                Unpooled.CopiedBuffer(new[] { new byte[] { 1, 2, 3 } }));

            AssertEqualAndRelease(Unpooled.WrappedBuffer(new byte[] { 1, 2, 3 }),
                Unpooled.CopiedBuffer(new byte[] { 1 }, new byte[] { 2 }, new byte[] { 3 }));

            AssertEqualAndRelease(Unpooled.WrappedBuffer(new byte[] { 1, 2, 3 }),
                Unpooled.CopiedBuffer(new[] { Unpooled.WrappedBuffer(new byte[] { 1, 2, 3 }) }));

            AssertEqualAndRelease(Unpooled.WrappedBuffer(new byte[] { 1, 2, 3 }),
                Unpooled.CopiedBuffer(Unpooled.WrappedBuffer(new byte[] { 1 }),
                    Unpooled.WrappedBuffer(new byte[] { 2 }), Unpooled.WrappedBuffer(new byte[] { 3 })));
        }

        [Fact]
        public void TestHexDump()
        {
            Assert.Equal("", ByteBufferUtil.HexDump(Unpooled.Empty));

            IArrayBuffer<byte> buffer = Unpooled.WrappedBuffer(new byte[] { 0x12, 0x34, 0x56 });
            Assert.Equal("123456", ByteBufferUtil.HexDump(buffer));
            buffer.Release();

            buffer = Unpooled.WrappedBuffer(new byte[]{
                0x12, 0x34, 0x56, 0x78,
                0x90, 0xAB, 0xCD, 0xEF
            });
            Assert.Equal("1234567890abcdef", ByteBufferUtil.HexDump(buffer));
            buffer.Release();
        }

        [Fact]
        public void SkipBytesNegativeLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                IArrayBuffer<byte> buf = Unpooled.Buffer(8);
                try
                {
                    buf.Skip(-1);
                }
                finally
                {
                    buf.Release();
                }
            });
        }
    }
}
