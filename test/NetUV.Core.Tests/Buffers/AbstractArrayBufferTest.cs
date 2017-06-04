// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Buffers
{
    using System;
    using System.Threading;
    using NetUV.Core.Buffers;
    using NetUV.Core.Common;
    using Xunit;

    public abstract class AbstractArrayBufferTest : IDisposable
    {
        const int Capacity = 4096; // Must be even
        const int BlockSize = 128;

        readonly int seed;
        IArrayBufferAllocator<byte> allocator;
        Random random;
        IArrayBuffer<byte> buffer;

        protected virtual bool DiscardReadBytesDoesNotMoveWritableBytes() => true;

        protected AbstractArrayBufferTest(bool pooled)
        {
            if (pooled)
            {
                this.allocator = new PooledArrayBufferAllocator<byte>();
            }
            else
            {
                this.allocator = new UnpooledArrayBufferAllocator<byte>();
            }

            this.buffer = this.allocator.Buffer(Capacity);
            this.seed = Environment.TickCount;
            this.random = new Random(this.seed);
        }

        [Fact]
        public void InitialState()
        {
            Assert.Equal(Capacity, this.buffer.Capacity);
            Assert.Equal(0, this.buffer.ReaderIndex);
        }

        [Fact]
        public void ReaderIndexBoundaryCheck1()
        {
            this.buffer.SetWriterIndex(0);
            Assert.Throws<IndexOutOfRangeException>(() => this.buffer.SetReaderIndex(-1));
        }

        [Fact]
        public void ReaderIndexBoundaryCheck2()
        {
            this.buffer.SetWriterIndex(this.buffer.Capacity);
            Assert.Throws<IndexOutOfRangeException>(() => this.buffer.SetReaderIndex(this.buffer.Capacity + 1));
        }

        [Fact]
        public void ReaderIndexBoundaryCheck3()
        {
            this.buffer.SetWriterIndex(Capacity / 2);
            Assert.Throws<IndexOutOfRangeException>(() => this.buffer.SetReaderIndex(Capacity * 3 / 2));
        }

        [Fact]
        public void ReaderIndexBoundaryCheck4()
        {
            this.buffer.SetWriterIndex(0);
            this.buffer.SetReaderIndex(0);
            this.buffer.SetWriterIndex(this.buffer.Capacity);
            this.buffer.SetReaderIndex(this.buffer.Capacity);
        }

        [Fact]
        public void WriterIndexBoundaryCheck1() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.SetWriterIndex(-1));

        [Fact]
        public void WriterIndexBoundaryCheck2()
        {
            this.buffer.SetWriterIndex(Capacity);
            this.buffer.SetReaderIndex(Capacity);
            Assert.Throws<IndexOutOfRangeException>(() => this.buffer.SetWriterIndex(this.buffer.Capacity + 1));
        }

        [Fact]
        public void WriterIndexBoundaryCheck3()
        {
            this.buffer.SetWriterIndex(Capacity);
            this.buffer.SetReaderIndex(Capacity / 2);
            Assert.Throws<IndexOutOfRangeException>(() => this.buffer.SetWriterIndex(Capacity / 4));
        }

        [Fact]
        public void WriterIndexBoundaryCheck4()
        {
            this.buffer.SetWriterIndex(0);
            this.buffer.SetReaderIndex(0);
            this.buffer.SetWriterIndex(Capacity);

            this.buffer.Write(new byte[0]);
        }

        [Fact]
        public void GetBooleanBoundaryCheck1() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.GetBoolean(-1));

        [Fact]
        public void GetBooleanBoundaryCheck2() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.GetBoolean(this.buffer.Capacity));

        [Fact]
        public void GetByteBoundaryCheck1() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.Get(-1));

        [Fact]
        public void GetByteBoundaryCheck2() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.Get(this.buffer.Capacity));

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetShortBoundaryCheck1(bool isLittleEndian) => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.GetUInt16(-1, isLittleEndian));

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetShortBoundaryCheck2(bool isLittleEndian) => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.GetUInt16(this.buffer.Capacity - 1, isLittleEndian));

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetIntBoundaryCheck1(bool isLittleEndian) => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.GetInt32(-1, isLittleEndian));

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetIntBoundaryCheck2(bool isLittleEndian) => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.GetInt32(this.buffer.Capacity - 3, isLittleEndian));

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetLongBoundaryCheck1(bool isLittleEndian) => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.GetInt64(-1, isLittleEndian));

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetLongBoundaryCheck2(bool isLittleEndian) => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.GetInt64(this.buffer.Capacity - 7, isLittleEndian));

        [Fact]
        public void GetByteArrayBoundaryCheck1() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.Get(-1, new byte[0]));

        [Fact]
        public void GetByteArrayBoundaryCheck2() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.Get(-1, new byte[0], 0, 0));

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetByteArrayBoundaryCheck3(bool isLittleEndian)
        {
            var dst = new byte[4];
            this.buffer.SetInt32(0, 0x01020304, isLittleEndian);
            Assert.Throws<IndexOutOfRangeException>(() => this.buffer.Get(0, dst, -1, 4));

            // No partial copy is expected.
            Assert.Equal(0, dst[0]);
            Assert.Equal(0, dst[1]);
            Assert.Equal(0, dst[2]);
            Assert.Equal(0, dst[3]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetByteArrayBoundaryCheck4(bool isLittleEndian)
        {
            var dst = new byte[4];
            this.buffer.SetInt32(0, 0x01020304, isLittleEndian);
            Assert.Throws<IndexOutOfRangeException>(() => this.buffer.Get(0, dst, 1, 4));

            // No partial copy is expected.
            Assert.Equal(0, dst[0]);
            Assert.Equal(0, dst[1]);
            Assert.Equal(0, dst[2]);
            Assert.Equal(0, dst[3]);
        }

        [Fact]
        public void CopyBoundaryCheck1() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.Copy(-1, 0));

        [Fact]
        public void CopyBoundaryCheck2() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.Copy(0, this.buffer.Capacity + 1));

        [Fact]
        public void CopyBoundaryCheck3() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.Copy(this.buffer.Capacity + 1, 0));

        [Fact]
        public void CopyBoundaryCheck4() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.Copy(this.buffer.Capacity, 1));

        [Fact]
        public void SetIndexBoundaryCheck1() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.SetIndex(-1, Capacity));

        [Fact]
        public void SetIndexBoundaryCheck2() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.SetIndex(Capacity / 2, Capacity / 4));

        [Fact]
        public void SetIndexBoundaryCheck3() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.SetIndex(0, Capacity + 1));

        [Fact]
        public void GetByteBufferState()
        {
            var dst = new byte[4];

            this.buffer.Set(0, 1);
            this.buffer.Set(1, 2);
            this.buffer.Set(2, 3);
            this.buffer.Set(3, 4);
            this.buffer.Get(1, dst, 1, 2);

            Assert.Equal(0, dst[0]);
            Assert.Equal(2, dst[1]);
            Assert.Equal(3, dst[2]);
            Assert.Equal(0, dst[3]);
        }

        [Fact]
        public void GetDirectByteBufferBoundaryCheck() => Assert.Throws<IndexOutOfRangeException>(() => this.buffer.Get(-1, new byte[0]));

        [Fact]
        public void RandomByteAccess()
        {
            for (int i = 0; i < this.buffer.Capacity; i++)
            {
                byte value = (byte)this.random.Next();
                this.buffer.Set(i, value);
            }

            this.random = new Random(this.seed);
            for (int i = 0; i < this.buffer.Capacity; i++)
            {
                byte value = (byte)this.random.Next();
                Assert.Equal(value, this.buffer.Get(i));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RandomShortAccess(bool isLittleEndian)
        {
            for (int i = 0; i < this.buffer.Capacity - 1; i += 2)
            {
                short value = (short)this.random.Next();
                this.buffer.SetInt16(i, value, isLittleEndian);
            }

            this.random = new Random(this.seed);
            for (int i = 0; i < this.buffer.Capacity - 1; i += 2)
            {
                short value = (short)this.random.Next();
                Assert.Equal(value, this.buffer.GetInt16(i, isLittleEndian));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RandomUnsignedShortAccess(bool isLittleEndian)
        {
            for (int i = 0; i < this.buffer.Capacity - 1; i += 2)
            {
                ushort value = (ushort)(this.random.Next() & 0xFFFF);
                this.buffer.SetUInt16(i, value, isLittleEndian);
            }

            this.random = new Random(this.seed);
            for (int i = 0; i < this.buffer.Capacity - 1; i += 2)
            {
                int value = this.random.Next() & 0xFFFF;
                Assert.Equal(value, this.buffer.GetUInt16(i, isLittleEndian));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RandomIntAccess(bool isLittleEndian)
        {
            for (int i = 0; i < this.buffer.Capacity - 3; i += 4)
            {
                int value = this.random.Next();
                this.buffer.SetInt32(i, value, isLittleEndian);
            }

            this.random = new Random(this.seed);
            for (int i = 0; i < this.buffer.Capacity - 3; i += 4)
            {
                int value = this.random.Next();
                Assert.Equal(value, this.buffer.GetInt32(i, isLittleEndian));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RandomUnsignedIntAccess(bool isLittleEndian)
        {
            for (int i = 0; i < this.buffer.Capacity - 3; i += 4)
            {
                uint value = (uint)(this.random.Next() & 0xFFFFFFFFL);
                this.buffer.SetUInt32(i, value, isLittleEndian);
            }

            this.random = new Random(this.seed);
            for (int i = 0; i < this.buffer.Capacity - 3; i += 4)
            {
                long value = this.random.Next() & 0xFFFFFFFFL;
                Assert.Equal(value, this.buffer.GetUInt32(i, isLittleEndian));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RandomLongAccess(bool isLittleEndian)
        {
            for (int i = 0; i < this.buffer.Capacity - 7; i += 8)
            {
                long value = this.random.NextLong();
                this.buffer.SetInt64(i, value, isLittleEndian);
            }

            this.random = new Random(this.seed);
            for (int i = 0; i < this.buffer.Capacity - 7; i += 8)
            {
                long value = this.random.NextLong();
                Assert.Equal(value, this.buffer.GetInt64(i, isLittleEndian));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RandomFloatAccess(bool isLittleEndian)
        {
            for (int i = 0; i < this.buffer.Capacity - 3; i += 4)
            {
                float value = (float)this.random.NextDouble();
                this.buffer.SetFloat(i, value, isLittleEndian);
            }

            this.random = new Random(this.seed);
            for (int i = 0; i < this.buffer.Capacity - 3; i += 4)
            {
                float value = (float)this.random.NextDouble();
                Assert.Equal(value, this.buffer.GetFloat(i, isLittleEndian), 2);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RandomDoubleAccess(bool isLittleEndian)
        {
            for (int i = 0; i < this.buffer.Capacity - 7; i += 8)
            {
                double value = this.random.NextDouble();
                this.buffer.SetDouble(i, value, isLittleEndian);
            }

            this.random = new Random(this.seed);
            for (int i = 0; i < this.buffer.Capacity - 7; i += 8)
            {
                double value = this.random.NextDouble();
                Assert.Equal(value, this.buffer.GetDouble(i, isLittleEndian), 2);
            }
        }

        [Fact]
        public void ByteArrayTransfer()
        {
            var value = new byte[BlockSize * 2];
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(value);
                this.buffer.Set(i, value, this.random.Next(BlockSize), BlockSize);
            }

            this.random = new Random(this.seed);
            var expectedValue = new byte[BlockSize * 2];
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(expectedValue);
                int valueOffset = this.random.Next(BlockSize);
                this.buffer.Get(i, value, valueOffset, BlockSize);
                for (int j = valueOffset; j < valueOffset + BlockSize; j++)
                {
                    Assert.Equal(expectedValue[j], value[j]);
                }
            }
        }

        [Fact]
        public void RandomByteArrayTransfer1()
        {
            var value = new byte[BlockSize];
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(value);
                this.buffer.Set(i, value);
            }

            this.random = new Random(this.seed);
            var expectedValueContent = new byte[BlockSize];
            IArrayBuffer<byte> expectedValue = Unpooled.WrappedBuffer(expectedValueContent);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(expectedValueContent);
                this.buffer.Get(i, value);
                for (int j = 0; j < BlockSize; j++)
                {
                    Assert.Equal(expectedValue.Get(j), value[j]);
                }
            }
        }

        [Fact]
        public void RandomByteArrayTransfer2()
        {
            var value = new byte[BlockSize * 2];
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(value);
                this.buffer.Set(i, value, this.random.Next(BlockSize), BlockSize);
            }

            this.random = new Random(this.seed);
            var expectedValueContent = new byte[BlockSize * 2];
            IArrayBuffer<byte> expectedValue = Unpooled.WrappedBuffer(expectedValueContent);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(expectedValueContent);
                int valueOffset = this.random.Next(BlockSize);
                this.buffer.Get(i, value, valueOffset, BlockSize);
                for (int j = valueOffset; j < valueOffset + BlockSize; j++)
                {
                    Assert.Equal(expectedValue.Get(j), value[j]);
                }
            }
        }

        [Fact]
        public void RandomHeapBufferTransfer1()
        {
            var valueContent = new byte[BlockSize];
            IArrayBuffer<byte> value = Unpooled.WrappedBuffer(valueContent);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(valueContent);
                value.SetIndex(0, BlockSize);
                this.buffer.Set(i, value);
                Assert.Equal(BlockSize, value.ReaderIndex);
                Assert.Equal(BlockSize, value.WriterIndex);
            }

            this.random = new Random(this.seed);
            var expectedValueContent = new byte[BlockSize];
            IArrayBuffer<byte> expectedValue = Unpooled.WrappedBuffer(expectedValueContent);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(expectedValueContent);
                value.Clear();
                this.buffer.Get(i, value);
                Assert.Equal(0, value.ReaderIndex);
                Assert.Equal(BlockSize, value.WriterIndex);
                for (int j = 0; j < BlockSize; j++)
                {
                    Assert.Equal(expectedValue.Get(j), value.Get(j));
                }
            }
        }

        [Fact]
        public void RandomHeapBufferTransfer2()
        {
            var valueContent = new byte[BlockSize * 2];
            IArrayBuffer<byte> value = Unpooled.WrappedBuffer(valueContent);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(valueContent);
                this.buffer.Set(i, value, this.random.Next(BlockSize), BlockSize);
            }

            this.random = new Random(this.seed);
            var expectedValueContent = new byte[BlockSize * 2];
            IArrayBuffer<byte> expectedValue = Unpooled.WrappedBuffer(expectedValueContent);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(expectedValueContent);
                int valueOffset = this.random.Next(BlockSize);
                this.buffer.Get(i, value, valueOffset, BlockSize);
                for (int j = valueOffset; j < valueOffset + BlockSize; j++)
                {
                    Assert.Equal(expectedValue.Get(j), value.Get(j));
                }
            }
        }

        [Fact]
        public void RandomByteBufferTransfer()
        {
            var value = new byte[BlockSize * 2];
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(value);
                this.buffer.Set(i, value, this.random.Next(BlockSize), BlockSize);
            }

            this.random = new Random(this.seed);
            var expectedValue = new byte[BlockSize * 2];
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(expectedValue);
                int valueOffset = this.random.Next(BlockSize);
                this.buffer.Get(i, value, valueOffset, BlockSize);
                for (int j = valueOffset; j < valueOffset + BlockSize; j++)
                {
                    Assert.Equal(expectedValue[j], value[j]);
                }
            }
        }

        [Fact]
        public void SequentialByteArrayTransfer1()
        {
            var value = new byte[BlockSize];
            this.buffer.SetWriterIndex(0);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(value);
                Assert.Equal(0, this.buffer.ReaderIndex);
                Assert.Equal(i, this.buffer.WriterIndex);
                this.buffer.Write(value);
            }

            this.random = new Random(this.seed);
            var expectedValue = new byte[BlockSize];
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(expectedValue);
                Assert.Equal(i, this.buffer.ReaderIndex);
                Assert.Equal(Capacity, this.buffer.WriterIndex);
                this.buffer.Read(value);
                for (int j = 0; j < BlockSize; j++)
                {
                    Assert.Equal(expectedValue[j], value[j]);
                }
            }
        }

        [Fact]
        public void SequentialByteArrayTransfer2()
        {
            var value = new byte[BlockSize * 2];
            this.buffer.SetWriterIndex(0);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(value);
                Assert.Equal(0, this.buffer.ReaderIndex);
                Assert.Equal(i, this.buffer.WriterIndex);
                int readerIndex = this.random.Next(BlockSize);
                this.buffer.Write(value, readerIndex, BlockSize);
            }

            this.random = new Random(this.seed);
            var expectedValue = new byte[BlockSize * 2];
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(expectedValue);
                int valueOffset = this.random.Next(BlockSize);
                Assert.Equal(i, this.buffer.ReaderIndex);
                Assert.Equal(Capacity, this.buffer.WriterIndex);
                this.buffer.Read(value, valueOffset, BlockSize);
                for (int j = valueOffset; j < valueOffset + BlockSize; j++)
                {
                    Assert.Equal(expectedValue[j], value[j]);
                }
            }
        }

        [Fact]
        public void TestSequentialHeapBufferTransfer1()
        {
            var valueContent = new byte[BlockSize * 2];
            IArrayBuffer<byte> value = Unpooled.WrappedBuffer(valueContent);
            this.buffer.SetWriterIndex(0);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(valueContent);
                Assert.Equal(0, this.buffer.ReaderIndex);
                Assert.Equal(i, this.buffer.WriterIndex);
                this.buffer.Write(value, this.random.Next(BlockSize), BlockSize);
                Assert.Equal(0, value.ReaderIndex);
                Assert.Equal(valueContent.Length, value.WriterIndex);
            }

            this.random = new Random(this.seed);
            var expectedValueContent = new byte[BlockSize * 2];
            IArrayBuffer<byte> expectedValue = Unpooled.WrappedBuffer(expectedValueContent);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(expectedValueContent);
                int valueOffset = this.random.Next(BlockSize);
                Assert.Equal(i, this.buffer.ReaderIndex);
                Assert.Equal(Capacity, this.buffer.WriterIndex);
                this.buffer.Read(value, valueOffset, BlockSize);
                for (int j = valueOffset; j < valueOffset + BlockSize; j++)
                {
                    Assert.Equal(expectedValue.Get(j), value.Get(j));
                }
                Assert.Equal(0, value.ReaderIndex);
                Assert.Equal(valueContent.Length, value.WriterIndex);
            }
        }

        [Fact]
        public void SequentialHeapBufferTransfer2()
        {
            var valueContent = new byte[BlockSize * 2];
            IArrayBuffer<byte> value = Unpooled.WrappedBuffer(valueContent);
            this.buffer.SetWriterIndex(0);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(valueContent);
                Assert.Equal(0, this.buffer.ReaderIndex);
                Assert.Equal(i, this.buffer.WriterIndex);
                int readerIndex = this.random.Next(BlockSize);
                value.SetReaderIndex(readerIndex);
                value.SetWriterIndex(readerIndex + BlockSize);
                this.buffer.Write(value);
                Assert.Equal(readerIndex + BlockSize, value.WriterIndex);
                Assert.Equal(value.WriterIndex, value.ReaderIndex);
            }

            this.random = new Random(this.seed);
            var expectedValueContent = new byte[BlockSize * 2];
            IArrayBuffer<byte> expectedValue = Unpooled.WrappedBuffer(expectedValueContent);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(expectedValueContent);
                int valueOffset = this.random.Next(BlockSize);
                Assert.Equal(i, this.buffer.ReaderIndex);
                Assert.Equal(Capacity, this.buffer.WriterIndex);
                value.SetReaderIndex(valueOffset);
                value.SetWriterIndex(valueOffset);
                this.buffer.Read(value, BlockSize);
                for (int j = valueOffset; j < valueOffset + BlockSize; j++)
                {
                    Assert.Equal(expectedValue.Get(j), value.Get(j));
                }
                Assert.Equal(valueOffset, value.ReaderIndex);
                Assert.Equal(valueOffset + BlockSize, value.WriterIndex);
            }
        }

        [Fact]
        public void SequentialByteBufferBackedHeapBufferTransfer1()
        {
            var valueContent = new byte[BlockSize * 2];
            IArrayBuffer<byte> value = Unpooled.WrappedBuffer(new byte[BlockSize * 2]);
            value.SetWriterIndex(0);
            this.buffer.SetWriterIndex(0);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(valueContent);
                value.Set(0, valueContent);
                Assert.Equal(0, this.buffer.ReaderIndex);
                Assert.Equal(i, this.buffer.WriterIndex);
                this.buffer.Write(value, this.random.Next(BlockSize), BlockSize);
                Assert.Equal(0, value.ReaderIndex);
                Assert.Equal(0, value.WriterIndex);
            }

            this.random = new Random(this.seed);
            var expectedValueContent = new byte[BlockSize * 2];
            IArrayBuffer<byte> expectedValue = Unpooled.WrappedBuffer(expectedValueContent);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(expectedValueContent);
                int valueOffset = this.random.Next(BlockSize);
                value.Set(0, valueContent);
                Assert.Equal(i, this.buffer.ReaderIndex);
                Assert.Equal(Capacity, this.buffer.WriterIndex);
                this.buffer.Read(value, valueOffset, BlockSize);
                for (int j = valueOffset; j < valueOffset + BlockSize; j++)
                {
                    Assert.Equal(expectedValue.Get(j), value.Get(j));
                }
                Assert.Equal(0, value.ReaderIndex);
                Assert.Equal(0, value.WriterIndex);
            }
        }

        [Fact]
        public void SequentialByteBufferBackedHeapBufferTransfer2()
        {
            var valueContent = new byte[BlockSize * 2];
            IArrayBuffer<byte> value = Unpooled.WrappedBuffer(new byte[BlockSize * 2]);
            value.SetWriterIndex(0);
            this.buffer.SetWriterIndex(0);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(valueContent);
                value.Set(0, valueContent);
                Assert.Equal(0, this.buffer.ReaderIndex);
                Assert.Equal(i, this.buffer.WriterIndex);
                int readerIndex = this.random.Next(BlockSize);
                value.SetReaderIndex(0);
                value.SetWriterIndex(readerIndex + BlockSize);
                value.SetReaderIndex(readerIndex);
                this.buffer.Write(value);
                Assert.Equal(readerIndex + BlockSize, value.WriterIndex);
                Assert.Equal(value.WriterIndex, value.ReaderIndex);
            }

            this.random = new Random(this.seed);
            var expectedValueContent = new byte[BlockSize * 2];
            IArrayBuffer<byte> expectedValue = Unpooled.WrappedBuffer(expectedValueContent);
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(expectedValueContent);
                value.Set(0, valueContent);
                int valueOffset = this.random.Next(BlockSize);
                Assert.Equal(i, this.buffer.ReaderIndex);
                Assert.Equal(Capacity, this.buffer.WriterIndex);
                value.SetReaderIndex(valueOffset);
                value.SetWriterIndex(valueOffset);
                this.buffer.Read(value, BlockSize);
                for (int j = valueOffset; j < valueOffset + BlockSize; j++)
                {
                    Assert.Equal(expectedValue.Get(j), value.Get(j));
                }
                Assert.Equal(valueOffset, value.ReaderIndex);
                Assert.Equal(valueOffset + BlockSize, value.WriterIndex);
            }
        }

        [Fact]
        public void SequentialByteBufferTransfer()
        {
            this.buffer.SetWriterIndex(0);
            var value = new byte[BlockSize * 2];
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(value);
                this.buffer.Write(value, this.random.Next(BlockSize), BlockSize);
            }

            this.random = new Random(this.seed);
            var expectedValue = new byte[BlockSize * 2];
            for (int i = 0; i < this.buffer.Capacity - BlockSize + 1; i += BlockSize)
            {
                this.random.NextBytes(expectedValue);
                int valueOffset = this.random.Next(BlockSize);
                this.buffer.Read(value, valueOffset, BlockSize);

                for (int j = valueOffset; j < valueOffset + BlockSize; j++)
                {
                    Assert.Equal(expectedValue[j], value[j]);
                }
            }
        }

        [Fact]
        public void DiscardReadBytes()
        {
            this.buffer.SetWriterIndex(0);
            for (int i = 0; i < this.buffer.Capacity; i += 4)
            {
                this.buffer.SetInt32(i, i, BitConverter.IsLittleEndian);
                this.buffer.SetWriterIndex(i + 4);
            }
            IArrayBuffer<byte> copy = Unpooled.CopiedBuffer(this.buffer);

            // Make sure there's no effect if called when readerIndex is 0.
            this.buffer.SetReaderIndex(Capacity / 4);
            this.buffer.MarkReaderIndex();
            this.buffer.SetWriterIndex(Capacity / 3);
            this.buffer.MarkWriterIndex();
            this.buffer.SetReaderIndex(0);
            this.buffer.SetWriterIndex(Capacity / 2);
            this.buffer.DiscardReadCount();

            Assert.Equal(0, this.buffer.ReaderIndex);
            Assert.Equal(Capacity / 2, this.buffer.WriterIndex);
            Assert.True(ByteBufferUtil.Equals(copy.Slice(0, Capacity / 2), this.buffer.Slice(0, Capacity / 2)));
            this.buffer.ResetReaderIndex();
            Assert.Equal(Capacity / 4, this.buffer.ReaderIndex);
            this.buffer.ResetWriterIndex();
            Assert.Equal(Capacity / 3, this.buffer.WriterIndex);

            // Make sure bytes after writerIndex is not copied.
            this.buffer.SetReaderIndex(1);
            this.buffer.SetWriterIndex(Capacity / 2);
            this.buffer.DiscardReadCount();

            Assert.Equal(0, this.buffer.ReaderIndex);
            Assert.Equal(Capacity / 2 - 1, this.buffer.WriterIndex);
            Assert.True(ByteBufferUtil.Equals(copy.Slice(1, Capacity / 2 - 1), this.buffer.Slice(0, Capacity / 2 - 1)));

            if (this.DiscardReadBytesDoesNotMoveWritableBytes())
            {
                // If writable bytes were copied, the test should fail to avoid unnecessary memory bandwidth consumption.
                Assert.False(ByteBufferUtil.Equals(copy.Slice(Capacity / 2, Capacity / 2), this.buffer.Slice(Capacity / 2 - 1, Capacity / 2)));
            }
            else
            {
                Assert.True(ByteBufferUtil.Equals(copy.Slice(Capacity / 2, Capacity / 2), this.buffer.Slice(Capacity / 2 - 1, Capacity / 2)));
            }

            // Marks also should be relocated.
            this.buffer.ResetReaderIndex();
            Assert.Equal(Capacity / 4 - 1, this.buffer.ReaderIndex);
            this.buffer.ResetWriterIndex();
            Assert.Equal(Capacity / 3 - 1, this.buffer.WriterIndex);

            copy.Release();
        }

        [Fact]
        public void Duplicate()
        {
            for (int i = 0; i < this.buffer.Capacity; i++)
            {
                byte value = (byte)this.random.Next();
                this.buffer.Set(i, value);
            }

            int readerIndex = Capacity / 3;
            int writerIndex = Capacity * 2 / 3;
            this.buffer.SetIndex(readerIndex, writerIndex);

            // Make sure all properties are copied.
            IArrayBuffer<byte> duplicate = this.buffer.Duplicate();
            Assert.Equal(this.buffer.ReaderIndex, duplicate.ReaderIndex);
            Assert.Equal(this.buffer.WriterIndex, duplicate.WriterIndex);
            Assert.Equal(this.buffer.Capacity, duplicate.Capacity);
            for (int i = 0; i < duplicate.Capacity; i++)
            {
                Assert.Equal(this.buffer.Get(i), duplicate.Get(i));
            }

            // Make sure the this.buffer content is shared.
            this.buffer.Set(readerIndex, (byte)(this.buffer.Get(readerIndex) + 1));
            Assert.Equal(this.buffer.Get(readerIndex), duplicate.Get(readerIndex));
            duplicate.Set(1, (byte)(duplicate.Get(1) + 1));
            Assert.Equal(this.buffer.Get(1), duplicate.Get(1));
        }

        [Fact]
        public void SliceIndex()
        {
            Assert.Equal(0, this.buffer.Slice(0, this.buffer.Capacity).ReaderIndex);
            Assert.Equal(0, this.buffer.Slice(0, this.buffer.Capacity - 1).ReaderIndex);
            Assert.Equal(0, this.buffer.Slice(1, this.buffer.Capacity - 1).ReaderIndex);
            Assert.Equal(0, this.buffer.Slice(1, this.buffer.Capacity - 2).ReaderIndex);

            Assert.Equal(this.buffer.Capacity, this.buffer.Slice(0, this.buffer.Capacity).WriterIndex);
            Assert.Equal(this.buffer.Capacity - 1, this.buffer.Slice(0, this.buffer.Capacity - 1).WriterIndex);
            Assert.Equal(this.buffer.Capacity - 1, this.buffer.Slice(1, this.buffer.Capacity - 1).WriterIndex);
            Assert.Equal(this.buffer.Capacity - 2, this.buffer.Slice(1, this.buffer.Capacity - 2).WriterIndex);
        }

        [Fact]
        public void BufferEquals()
        {
            Assert.False(this.buffer.Equals(null));
            Assert.False(this.buffer.Equals(new object()));

            var value = new byte[32];
            this.buffer.SetIndex(0, value.Length);
            this.random.NextBytes(value);
            this.buffer.Set(0, value);

            Assert.True(ByteBufferUtil.Equals(this.buffer, Unpooled.WrappedBuffer(value)));

            value[0]++;
            Assert.False(ByteBufferUtil.Equals(this.buffer, Unpooled.WrappedBuffer(value)));
            Assert.False(this.buffer.Equals(Unpooled.WrappedBuffer(value)));
        }

        [Fact]
        public void BufferCompareTo()
        {
            // Fill the this.random stuff
            var value = new byte[32];
            this.random.NextBytes(value);
            // Prevent overflow / underflow
            if (value[0] == 0)
            {
                value[0]++;
            }
            else if (value[0] == 0xFF)
            {
                value[0]--;
            }

            this.buffer.SetIndex(0, value.Length);
            this.buffer.Set(0, value);

            Assert.Equal(0, ByteBufferUtil.Compare(this.buffer, Unpooled.WrappedBuffer(value)));

            value[0]++;
            Assert.True(ByteBufferUtil.Compare(this.buffer, Unpooled.WrappedBuffer(value)) < 0);
            value[0] -= 2;
            Assert.True(ByteBufferUtil.Compare(this.buffer, Unpooled.WrappedBuffer(value)) > 0);
            value[0]++;

            Assert.True(ByteBufferUtil.Compare(this.buffer, Unpooled.WrappedBuffer(value, 0, 31)) > 0);
            Assert.True(ByteBufferUtil.Compare(this.buffer.Slice(0, 31), Unpooled.WrappedBuffer(value)) < 0);
        }

        [Fact]
        public void SkipBytes1()
        {
            this.buffer.SetIndex(Capacity / 4, Capacity / 2);

            this.buffer.Skip(Capacity / 4);
            Assert.Equal(Capacity / 4 * 2, this.buffer.ReaderIndex);

            Assert.Throws<IndexOutOfRangeException>(() => this.buffer.Skip(Capacity / 4 + 1));

            // Should remain unchanged.
            Assert.Equal(Capacity / 4 * 2, this.buffer.ReaderIndex);
        }

        [Fact]
        public void DiscardAllReadBytes()
        {
            this.buffer.SetWriterIndex(this.buffer.Capacity);
            this.buffer.SetReaderIndex(this.buffer.WriterIndex);
            this.buffer.DiscardReadCount();
        }

        IArrayBuffer<byte> ReleasedBuffer()
        {
            IArrayBuffer<byte> newBuffer = this.allocator.Buffer(8);
            Assert.True(newBuffer.Release());
            return newBuffer;
        }

        [Fact]
        public void DiscardReadBytesAfterRelease() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().DiscardReadCount());

        [Fact]
        public void DiscardSomeReadBytesAfterRelease() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().DiscardSomeReadCount());

        [Fact]
        public void EnsureWritableAfterRelease() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().EnsureWritable(16));

        [Fact]
        public void GetByteAfterRelease() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().Get(0));

        [Fact]
        public void GetBytesAfterRelease4() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().Get(0, new byte[8]));

        [Fact]
        public void GetBytesAfterRelease5() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().Get(0, new byte[8], 0, 1));

        [Fact]
        public void SetByteAfterRelease() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().Set(0, 1));

        [Fact]
        public void ReadBytesAfterRelease() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().Read(1));

        [Fact]
        public void ReadBytesAfterRelease5() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().Read(new byte[8]));

        [Fact]
        public void ReadBytesAfterRelease6() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().Read(new byte[8], 0, 1));

        [Fact]
        public void WriteByteAfterRelease() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().Write(1));

        [Fact]
        public void WriteBytesAfterRelease4() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().Write(new byte[8]));

        [Fact]
        public void WriteBytesAfterRelease5() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().Write(new byte[8], 0, 1));

        [Fact]
        public void CopyAfterRelease() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().Copy());

        [Fact]
        public void CopyAfterRelease1() => Assert.Throws<InvalidOperationException>(() => this.ReleasedBuffer().Copy());

        [Fact]
        public void ArrayAfterRelease()
        {
            IArrayBuffer<byte> buf = this.ReleasedBuffer();
            if (buf.HasArray)
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    byte[] a = buf.Array;
                });
            }
        }

        [Fact]
        public void SliceRelease()
        {
            IArrayBuffer<byte> buf = this.allocator.Buffer(8);
            Assert.Equal(1, buf.ReferenceCount);
            Assert.True(buf.Slice().Release());
            Assert.Equal(0, buf.ReferenceCount);
        }

        [Fact]
        public void DuplicateRelease()
        {
            IArrayBuffer<byte> buf = this.allocator.Buffer(8);
            Assert.Equal(1, buf.ReferenceCount);
            Assert.True(buf.Duplicate().Release());
            Assert.Equal(0, buf.ReferenceCount);
        }

        [Fact]
        public void ReadBytes()
        {
            IArrayBuffer<byte> buf = this.allocator.Buffer(8);
            //IByteBuffer buffer = this.NewBuffer(8);
            var bytes = new byte[8];
            buf.Write(bytes);

            IArrayBuffer<byte> buffer2 = buf.Read(4);
            Assert.Same(buf.Allocator, buffer2.Allocator);
            Assert.Equal(4, buf.ReaderIndex);
            Assert.True(buf.Release());
            Assert.Equal(0, buf.ReferenceCount);
            Assert.True(buffer2.Release());
            Assert.Equal(0, buffer2.ReferenceCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RefCnt0(bool parameter)
        {
            for (int i = 0; i < 10; i++)
            {
                var latch = new ManualResetEventSlim();
                var innerLatch = new ManualResetEventSlim();

                IArrayBuffer<byte> buf = this.allocator.Buffer(4);
                Assert.Equal(1, buf.ReferenceCount);
                int cnt = int.MaxValue;
                var t1 = new Thread(s =>
                {
                    bool released;
                    if (parameter)
                    {
                        released = buf.Release(buf.ReferenceCount);
                    }
                    else
                    {
                        released = buf.Release();
                    }
                    Assert.True(released);
                    var t2 = new Thread(s2 =>
                    {
                        Volatile.Write(ref cnt, buf.ReferenceCount);
                        latch.Set();
                    });
                    t2.Start();
                    // Keep Thread alive a bit so the ThreadLocal caches are not freed
                    innerLatch.Wait();
                });
                t1.Start();

                latch.Wait();
                Assert.Equal(0, Volatile.Read(ref cnt));
                innerLatch.Set();
            }
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
