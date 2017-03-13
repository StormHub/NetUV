// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Text;
    using NetUV.Core.Common;

    public struct ReadableBuffer : IDisposable
    {
        internal static readonly ReadableBuffer Empty = new ReadableBuffer(ByteBufferAllocator.EmptyByteBuffer, 0);

        ByteBuffer byteBuffer;

        internal ReadableBuffer(ByteBuffer byteBuffer, int count)
        {
            Contract.Requires(byteBuffer != null);

            this.byteBuffer = byteBuffer;
            this.Index = 0;
            this.Count = count;
        }

        public int Index { get; private set; }

        public int Count { get; private set; }

        internal ReadableBuffer Retain()
        {
            this.byteBuffer?.Retain();
            return this;
        }

        public string ReadString(int length, Encoding encoding)
        {
            Contract.Requires(encoding != null);

            int readIndex = this.Index;
            this.Validate(readIndex, length);
            this.Index += length;

            return this.byteBuffer.ReadString(readIndex, length, encoding);
        }

        public bool ReadBoolean()
        {
            this.Validate(this.Index, sizeof(bool));
            return this.ReadRawByte() != 0;
        }

        public void ReadBytes(byte[] destination, int length)
        {
            Contract.Requires(destination != null);
            Contract.Requires(length > 0);

            int readIndex = this.Index;
            this.Validate(readIndex, length);
            this.Index += length;

            this.byteBuffer.ReadBytes(destination, readIndex, length);
        }

        public byte ReadByte()
        {
            this.Validate(this.Index, sizeof(byte));
            return this.ReadRawByte();
        } 

        public sbyte ReadSByte() => (sbyte)this.ReadByte();

        byte ReadRawByte() => 
            this.byteBuffer.ReadByte(this.Index++);

        public ushort ReadUInt16() 
        {
            this.Validate(this.Index, sizeof(short));
            if (BitConverter.IsLittleEndian)
            {
                return (ushort)(this.ReadRawByte() | 
                    this.ReadRawByte() << 8);
            }

            return  (ushort)(this.ReadRawByte() << 8 | 
                this.ReadRawByte() & 0xFF);
        }

        public short ReadInt16() => (short)this.ReadUInt16();

        public int ReadInt32()
        {
            this.Validate(this.Index, sizeof(uint));

            if (BitConverter.IsLittleEndian)
            {
                return (this.ReadRawByte() 
                    | this.ReadRawByte() << 8 
                    | this.ReadRawByte() << 16 
                    | this.ReadRawByte() << 24);
            }

            return (this.ReadRawByte() & 0xFF) << 24 
                | (this.ReadRawByte() & 0xFF) << 16 
                | (this.ReadRawByte() & 0xFF) << 8 
                | this.ReadRawByte() & 0xFF;
        }

        public uint ReadUInt32() => (uint)this.ReadInt32();

        public long ReadInt64()
        {
            this.Validate(this.Index, sizeof(long));

            if (BitConverter.IsLittleEndian)
            {
                // ReSharper disable once RedundantCast
                return ((long)this.ReadRawByte() | 
                    (long)this.ReadRawByte() << 8 | 
                    (long)this.ReadRawByte() << 16 | 
                    (long)this.ReadRawByte() << 24 | 
                    (long)this.ReadRawByte() << 32 | 
                    (long)this.ReadRawByte() << 40 | 
                    (long)this.ReadRawByte() << 48 | 
                    (long)this.ReadRawByte() << 56);
            }

            return ((long)this.ReadRawByte() & 0xFF) << 56 |
                ((long)this.ReadRawByte() & 0xFF) << 48 |
                ((long)this.ReadRawByte() & 0xFF) << 40 |
                ((long)this.ReadRawByte() & 0xFF) << 32 |
                ((long)this.ReadRawByte() & 0xFF) << 24 |
                ((long)this.ReadRawByte() & 0xFF) << 16 |
                ((long)this.ReadRawByte() & 0xFF) << 8 |
                (long)this.ReadRawByte() & 0xFF;
        }

        public ulong ReadUInt64() => (ulong)this.ReadInt64();

        public float ReadFloat()
        {
            const int Size = sizeof(float);

            this.Validate(this.Index, Size);
            var bytes = new byte[Size];
            this.byteBuffer.ReadBytes(bytes, this.Index, Size);
            this.Index += Size;

            if (BitConverter.IsLittleEndian)
            {
                bytes.Reverse();
            }

            return BitConverter.ToSingle(bytes, 0);
        }

        public double ReadDouble()
        {
            long value = this.ReadInt64();
            return BitConverter.Int64BitsToDouble(value);
        }

        void Validate(int readIndex, int length)
        {
            if (this.byteBuffer == null)
            {
                throw new ObjectDisposedException($"{nameof(ReadableBuffer)} has already been disposed.");
            }
           
            this.byteBuffer.Validate(readIndex, length);
        }

        public void Dispose()
        {
            this.byteBuffer?.Dispose();
            this.byteBuffer = null;
            this.Count = 0;
            this.Index = 0;
        }
    }
}
