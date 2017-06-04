// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetUV.Core.Buffers;

namespace NetUV.Core.Tests.Buffers
{
    using System;
    using System.Linq;
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
