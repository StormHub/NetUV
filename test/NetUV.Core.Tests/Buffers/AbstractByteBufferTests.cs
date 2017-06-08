// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetUV.Core.Buffers;

namespace NetUV.Core.Tests.Buffers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using NetUV.Core.Common;
    using Xunit;

    public abstract class AbstractByteBufferTests : IDisposable
    {
        readonly Random random;

        IArrayBufferAllocator<byte> allocator;
        IArrayBuffer<byte> buffer;

        protected AbstractByteBufferTests(bool pooled)
        {
            if (pooled)
            {
                this.allocator = new PooledArrayBufferAllocator<byte>();
            }
            else
            {
                this.allocator = new UnpooledArrayBufferAllocator<byte>();
            }

            this.random = new Random(Environment.TickCount);
        }

        [Fact]
        public void Boolean()
        {
            this.buffer = this.allocator.Buffer(1);
            this.buffer.SetBoolean(0, true);
            Assert.True(this.buffer.GetBoolean(0));

            this.buffer.SetBoolean(false);
            Assert.False(this.buffer.GetBoolean());
        }

        [Fact]
        public void Int16()
        {
            this.buffer = this.allocator.Buffer(sizeof(short));
            short value = (short)this.random.Next();

            this.buffer.SetInt16(0, value, BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetInt16(0, BitConverter.IsLittleEndian));

            byte[] expectedData = BitConverter.GetBytes(value);
            var data = new byte[sizeof(short)];
            this.buffer.Get(0, data);
            Assert.True(expectedData.SequenceEqual(data));

            this.buffer.SetInt16(value, !BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetInt16(!BitConverter.IsLittleEndian));
        }

        [Fact]
        public void UInt16()
        {
            this.buffer = this.allocator.Buffer(sizeof(ushort));
            ushort value = (ushort)this.random.Next();

            this.buffer.SetUInt16(0, value, BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetUInt16(0, BitConverter.IsLittleEndian));

            byte[] expectedData = BitConverter.GetBytes(value);
            var data = new byte[sizeof(ushort)];
            this.buffer.Get(0, data);
            Assert.True(expectedData.SequenceEqual(data));

            this.buffer.SetUInt16(value, !BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetUInt16(!BitConverter.IsLittleEndian));
        }

        [Fact]
        public void Int32()
        {
            this.buffer = this.allocator.Buffer(sizeof(int));
            int value = this.random.Next();

            this.buffer.SetInt32(0, value, BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetInt32(0, BitConverter.IsLittleEndian));

            byte[] expectedData = BitConverter.GetBytes(value);
            var data = new byte[sizeof(int)];
            this.buffer.Get(0, data);
            Assert.True(expectedData.SequenceEqual(data));

            this.buffer.SetInt32(value, !BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetInt32(!BitConverter.IsLittleEndian));
        }

        [Fact]
        public void UInt32()
        {
            this.buffer = this.allocator.Buffer(sizeof(uint));
            uint value = (uint)this.random.Next();

            this.buffer.SetUInt32(0, value, BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetUInt32(0, BitConverter.IsLittleEndian));

            byte[] expectedData = BitConverter.GetBytes(value);
            var data = new byte[sizeof(uint)];
            this.buffer.Get(0, data);
            Assert.True(expectedData.SequenceEqual(data));

            this.buffer.SetUInt32(value, !BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetUInt32(!BitConverter.IsLittleEndian));
        }

        [Fact]
        public void Int64()
        {
            this.buffer = this.allocator.Buffer(sizeof(long));
            long value = this.random.NextLong();

            this.buffer.SetInt64(0, value, BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetInt64(0, BitConverter.IsLittleEndian));

            byte[] expectedData = BitConverter.GetBytes(value);
            var data = new byte[sizeof(long)];
            this.buffer.Get(0, data);
            Assert.True(expectedData.SequenceEqual(data));

            this.buffer.SetInt64(value, !BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetInt64(!BitConverter.IsLittleEndian));
        }

        [Fact]
        public void UInt64()
        {
            this.buffer = this.allocator.Buffer(sizeof(ulong));
            ulong value = (ulong)this.random.NextLong();

            this.buffer.SetUInt64(0, value, BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetUInt64(0, BitConverter.IsLittleEndian));

            byte[] expectedData = BitConverter.GetBytes(value);
            var data = new byte[sizeof(ulong)];
            this.buffer.Get(0, data);
            Assert.True(expectedData.SequenceEqual(data));

            this.buffer.SetUInt64(value, !BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetUInt64(!BitConverter.IsLittleEndian));
        }

        [Fact]
        public void Float()
        {
            this.buffer = this.allocator.Buffer(sizeof(float));
            float value = (float)this.random.NextDouble();

            this.buffer.SetFloat(0, value, BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetFloat(0, BitConverter.IsLittleEndian));

            byte[] expectedData = BitConverter.GetBytes(value);
            var data = new byte[sizeof(float)];
            this.buffer.Get(0, data);
            Assert.True(expectedData.SequenceEqual(data));

            this.buffer.SetFloat(value, !BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetFloat(!BitConverter.IsLittleEndian), 2);
        }

        [Fact]
        public void Double()
        {
            this.buffer = this.allocator.Buffer(sizeof(double));
            double value = this.random.NextDouble();

            this.buffer.SetDouble(0, value, BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetDouble(0, BitConverter.IsLittleEndian));

            byte[] expectedData = BitConverter.GetBytes(value);
            var data = new byte[sizeof(double)];
            this.buffer.Get(0, data);
            Assert.True(expectedData.SequenceEqual(data));

            this.buffer.SetDouble(value, !BitConverter.IsLittleEndian);
            Assert.Equal(value, this.buffer.GetDouble(!BitConverter.IsLittleEndian), 2);
        }

        static IEnumerable<object[]> GetEncodingCases()
        {
            yield return new object[] { Encoding.ASCII };
            yield return new object[] { Encoding.UTF8 };
        }

        [Theory]
        [MemberData(nameof(GetEncodingCases))]
        public void StringEmpty(Encoding encoding)
        {
            this.buffer = this.allocator.Buffer(0);

            Assert.Equal(string.Empty, this.buffer.GetString(encoding));
            Assert.Equal(string.Empty, this.buffer.GetString(0, encoding));
            Assert.Equal(string.Empty, this.buffer.GetString(0, 0, encoding));

            byte[] separator = Encoding.ASCII.GetBytes("\r\n");
            Assert.Equal(string.Empty, this.buffer.GetString(separator, encoding, out int count));
            Assert.Equal(0, count);

            this.buffer.SetString(null, encoding);
            Assert.Equal(0, this.buffer.ReadableCount);
            this.buffer.SetString(string.Empty, encoding);
            Assert.Equal(0, this.buffer.ReadableCount);

            this.buffer.SetString(0, string.Empty, encoding);
            Assert.Equal(0, this.buffer.ReadableCount);
        }

        static IEnumerable<object[]> GetStringCases()
        {
            yield return new object[]
            {
                "Hello World!", Encoding.ASCII
            };
            yield return new object[]
            {
                "你 好 hello 世 界 World", Encoding.UTF8
            };
        }

        [Theory]
        [MemberData(nameof(GetStringCases))]
        public void String(string message, Encoding encoding)
        {
            byte[] expectedBytes = encoding.GetBytes(message);
            this.buffer = this.allocator.Buffer(expectedBytes.Length);

            int count = this.buffer.SetString(message, encoding);
            Assert.Equal(expectedBytes.Length, count);

            // ReadableCount = 0
            Assert.Equal(string.Empty, this.buffer.GetString(encoding));
            this.buffer.SetWriterIndex(count);

            var actualBytes = new byte[expectedBytes.Length];
            this.buffer.Get(0, actualBytes);
            Assert.True(expectedBytes.SequenceEqual(actualBytes));

            Assert.Equal(message, this.buffer.GetString(encoding));
            Assert.Equal(message, this.buffer.GetString(count, encoding));
            Assert.Equal(message, this.buffer.GetString(0, count, encoding));
        }

        static IEnumerable<object[]> GetStringSeparatorCases()
        {
            string message = "\r \nHello\n\rWorld\n \r";
            yield return new object[]
            {
                message,
                Encoding.ASCII.GetBytes(message),
                Encoding.ASCII,
                message,
                Encoding.ASCII.GetByteCount(message)
            };

            string separator = "\r\n";
            yield return new object[]
            {
                separator,
                Encoding.ASCII.GetBytes(separator),
                Encoding.ASCII,
                separator,
                Encoding.ASCII.GetByteCount(separator)
            };

            yield return new object[]
            {
                message,
                Encoding.ASCII.GetBytes(separator),
                Encoding.ASCII,
                message,
                Encoding.ASCII.GetByteCount(message)
            };

            yield return new object[]
            {
                separator,
                Encoding.ASCII.GetBytes(message),
                Encoding.ASCII,
                separator,
                Encoding.ASCII.GetByteCount(separator)
            };

            message = "\n\rHello\r\n\r\nWorld";
            yield return new object[]
            {
                message,
                Encoding.ASCII.GetBytes(separator),
                Encoding.ASCII,
                "\n\rHello",
                Encoding.ASCII.GetByteCount("\n\rHello")
            };

            message = "h\re\nllo\r你好\r\n世界";
            yield return new object[]
            {
                message,
                Encoding.UTF8.GetBytes(message),
                Encoding.UTF8,
                message,
                Encoding.UTF8.GetByteCount(message)
            };

            yield return new object[]
            {
                separator,
                Encoding.UTF8.GetBytes(separator),
                Encoding.UTF8,
                separator,
                Encoding.UTF8.GetByteCount(separator)
            };

            yield return new object[]
            {
                message,
                Encoding.UTF8.GetBytes(separator),
                Encoding.UTF8,
                "h\re\nllo\r你好",
                Encoding.UTF8.GetByteCount("h\re\nllo\r你好")
            };

            separator = "好\r";
            yield return new object[]
            {
                message,
                Encoding.UTF8.GetBytes(separator),
                Encoding.UTF8,
                "h\re\nllo\r你",
                Encoding.UTF8.GetByteCount("h\re\nllo\r你")
            };
        }

        [Theory]
        [MemberData(nameof(GetStringSeparatorCases))]
        public void StringSeparator(string message, byte[] separator, Encoding encoding, 
            string expected, int expectedCount)
        {
            byte[] expectedBytes = encoding.GetBytes(message);
            this.buffer = this.allocator.Buffer(expectedBytes.Length);

            int count = this.buffer.SetString(message, encoding);
            Assert.Equal(expectedBytes.Length, count);

            // ReadableCount = 0
            Assert.Equal(string.Empty, this.buffer.GetString(encoding));
            this.buffer.SetWriterIndex(count);

            string actual = this.buffer.GetString(separator, encoding, out count);
            Assert.Equal(expected, actual);
            Assert.Equal(expectedCount, count);

            actual = this.buffer.GetString(0, separator, encoding, out count);
            Assert.Equal(expected, actual);
            Assert.Equal(expectedCount, count);
        }

        public void Dispose()
        {
            if (this.buffer != null)
            {
                Assert.True(this.buffer.Release());
                Assert.Equal(0, this.buffer.ReferenceCount);

                try
                {
                    this.buffer.Release();
                }
                catch (Exception)
                {
                    // Ignore.
                }
                this.buffer = null;
            }

            this.allocator = null;
        }
    }
}
