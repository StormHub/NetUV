// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Buffers
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using NetUV.Core.Buffers;
    using Xunit;

    public sealed class ByteBufferUtilTests
    {
        [Fact]
        public void EqualsBufferSubsections()
        {
            var b1 = new byte[128];
            var b2 = new byte[256];
            var rand = new Random();
            rand.NextBytes(b1);
            rand.NextBytes(b2);
            int iB1 = b1.Length / 2;
            int iB2 = iB1 + b1.Length;
            int length = b1.Length - iB1;
            Array.Copy(b1, iB1, b2, iB2, length);
            Assert.True(ByteBufferUtil.Equals(Unpooled.WrappedBuffer(b1), iB1, Unpooled.WrappedBuffer(b2), iB2, length));
        }

        static int GetRandom(Random r, int min, int max) => r.Next((max - min) + 1) + min;

        [Fact]
        public void NotEqualsBufferSubsections()
        {
            var b1 = new byte[50];
            var b2 = new byte[256];
            var rand = new Random();
            rand.NextBytes(b1);
            rand.NextBytes(b2);
            int iB1 = b1.Length / 2;
            int iB2 = iB1 + b1.Length;
            int length = b1.Length - iB1;

            Array.Copy(b1, iB1, b2, iB2, length);
            // Randomly pick an index in the range that will be compared and make the value at that index differ between
            // the 2 arrays.
            int diffIndex = GetRandom(rand, iB1, iB1 + length - 1);
            ++b1[diffIndex];
            Assert.False(ByteBufferUtil.Equals(Unpooled.WrappedBuffer(b1), iB1, Unpooled.WrappedBuffer(b2), iB2, length));
        }

        [Fact]
        public void NotEqualsBufferOverflow()
        {
            var b1 = new byte[8];
            var b2 = new byte[16];
            var rand = new Random();
            rand.NextBytes(b1);
            rand.NextBytes(b2);
            int iB1 = b1.Length / 2;
            int iB2 = iB1 + b1.Length;
            int length = b1.Length - iB1;
            Array.Copy(b1, iB1, b2, iB2, length - 1);
            Assert.False(ByteBufferUtil.Equals(Unpooled.WrappedBuffer(b1), iB1, Unpooled.WrappedBuffer(b2), iB2,
                Math.Max(b1.Length, b2.Length) * 2));
        }

        [Fact]
        public void NotEqualsBufferUnderflow()
        {
            var b1 = new byte[8];
            var b2 = new byte[16];
            var rand = new Random();
            rand.NextBytes(b1);
            rand.NextBytes(b2);
            int iB1 = b1.Length / 2;
            int iB2 = iB1 + b1.Length;
            int length = b1.Length - iB1;
            Array.Copy(b1, iB1, b2, iB2, length - 1);
            Assert.Throws<ArgumentException>(() => ByteBufferUtil.Equals(Unpooled.WrappedBuffer(b1), iB1, Unpooled.WrappedBuffer(b2), iB2, -1));
        }

        public static IEnumerable<object[]> ReadStringCases()
        {
            string value = "\r\nhello\r\nworld";
            string separator = "\r\n";

            yield return new object[]
            {
                value,
                separator,
                string.Empty,
                separator.Length
            };

            yield return new object[]
            {
                value,
                value,
                string.Empty,
                value.Length
            };

            yield return new object[]
            {
                separator,
                value,
                separator,
                separator.Length
            };

            value = "\rh\nello\r\nworld";
            yield return new object[]
            {
                value,
                separator,
                "\rh\nello",
                "\rh\nello".Length
            };

            value = "\n\r\r\nhello\r\nworld";
            yield return new object[]
            {
                value,
                separator,
                "\n\r",
                2
            };

            value = "\r\n\r\nhello\r\nworld";
            yield return new object[]
            {
                value,
                separator,
                string.Empty,
                2
            };

            separator = "NA";
            yield return new object[]
            {
                value,
                separator,
                value,
                value.Length
            };
        }

        [Theory]
        [MemberData(nameof(ReadStringCases))]
        public void ReadStringAscii(string value, string separator, string expected, int readerIndex) =>
            ReadString(Encoding.ASCII, value, separator, expected, readerIndex);

        [Theory]
        [MemberData(nameof(ReadStringCases))]
        public void ReadStringUtf8(string value, string separator, string expected, int readerIndex) =>
            ReadString(Encoding.UTF8, value, separator, expected, readerIndex);

        // ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
        static void ReadString(Encoding encoding, string value, string separator, string expected, int readerIndex)
        {
            byte[] bytes = encoding.GetBytes(value);
            IByteBuffer buffer = Unpooled.WrappedBuffer(bytes);

            bytes = encoding.GetBytes(separator);
            IByteBuffer buf = Unpooled.WrappedBuffer(bytes);

            string actual = ByteBufferUtil.ReadString(buffer, buf, encoding);
            Assert.Equal(readerIndex, buffer.ReaderIndex);
            Assert.Equal(expected, actual);
        }
        // ReSharper restore ParameterOnlyUsedForPreconditionCheck.Local
    }
}
