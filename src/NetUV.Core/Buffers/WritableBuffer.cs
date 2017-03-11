// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Text;
    using NetUV.Core.Common;

    public struct WritableBuffer : IDisposable
    {
        ByteBuffer byteBuffer;

        internal WritableBuffer(ByteBuffer byteBuffer)
        {
            Contract.Requires(byteBuffer != null);

            this.byteBuffer = byteBuffer;
            this.Index = 0;
            this.Count = byteBuffer.Count;
        }

        public int Index { get; private set; }

        public int Count { get; private set; }

        internal ByteBuffer InternalBuffer
        {
            get
            {
                if (this.byteBuffer == null)
                {
                    throw new ObjectDisposedException($"{nameof(WritableBuffer)} has already been disposed.");
                }

                return this.byteBuffer;
            }
        }

        public static WritableBuffer From(byte[] array)
        {
            Contract.Requires(array != null && array.Length > 0);

            ByteBuffer byteBuffer = UnpooledByteBuffer.From(array);
            return new WritableBuffer(byteBuffer) { Index = array.Length };
        }

        public void WriteBytes(byte[] source)
        {
            Contract.Requires(source != null && source.Length > 0);

            int writeIndex = this.Index;
            this.Validate(writeIndex, source.Length);
            this.Index += source.Length;
            this.byteBuffer.WriteBytes(source, writeIndex, source.Length);
        }

        public void WriteBytes(byte[] source, int offset, int length)
        {
            Contract.Requires(source != null && source.Length > 0);
            Contract.Requires(length >= 0);
            Contract.Requires(offset >= 0 && offset <= source.Length - length);

            int writeIndex = this.Index;
            this.Validate(writeIndex, length);
            this.Index += length;

            this.byteBuffer.WriteBytes(source, offset, writeIndex, length);
        }

        public void WriteString(string value, Encoding encoding)
        {
            Contract.Requires(!string.IsNullOrEmpty(value));
            Contract.Requires(encoding != null);
           
            byte[] bytes = encoding.GetBytes(value);
            this.Validate(this.Index, bytes.Length);
            this.WriteBytes(bytes);
        }

        public void WriteBoolean(bool value) => this.WriteByte((byte)(value ? 1 : 0));

        public void WriteSByte(sbyte value) => this.WriteByte((byte)value);

        public void WriteByte(byte value)
        {
            this.Validate(this.Index, 1);
            this.WriteRawByte(value);
        }

        void WriteRawByte(byte value) => 
            this.byteBuffer.WriteByte(this.Index++, value);

        public void WriteInt16(short value)
        {
            const int Size = sizeof(short);
            this.Validate(this.Index, Size);

            if (BitConverter.IsLittleEndian)
            {
                this.WriteRawByte((byte)value);
                this.WriteRawByte((byte)(value >> 8));
            }
            else
            {
                this.WriteRawByte((byte)(value >> 8));
                this.WriteRawByte((byte)value);
            }
        }

        public void WriteUInt16(ushort value) => this.WriteInt16((short)value);

        public void WriteInt32(int value)
        {
            const int Size = sizeof(int);
            this.Validate(this.Index, Size);

            if (BitConverter.IsLittleEndian)
            {
                this.WriteRawByte((byte)value);
                this.WriteRawByte((byte)(value >> 8));
                this.WriteRawByte((byte)(value >> 16));
                this.WriteRawByte((byte)(value >> 24));
            }
            else
            {
                this.WriteRawByte((byte)(value >> 24));
                this.WriteRawByte((byte)(value >> 16));
                this.WriteRawByte((byte)(value >> 8));
                this.WriteRawByte((byte)value);
            }
        }

        public void WriteUInt32(uint value) => this.WriteInt32((int)value);

        public void WriteInt64(long value)
        {
            const int Size = sizeof(long);
            this.Validate(this.Index, Size);

            if (BitConverter.IsLittleEndian)
            {
                this.WriteRawByte((byte)value);
                this.WriteRawByte((byte)(value >> 8));
                this.WriteRawByte((byte)(value >> 16));
                this.WriteRawByte((byte)(value >> 24));
                this.WriteRawByte((byte)(value >> 32));
                this.WriteRawByte((byte)(value >> 40));
                this.WriteRawByte((byte)(value >> 48));
                this.WriteRawByte((byte)(value >> 56));
            }
            else
            {
                this.WriteRawByte((byte)(value >> 56));
                this.WriteRawByte((byte)(value >> 48));
                this.WriteRawByte((byte)(value >> 40));
                this.WriteRawByte((byte)(value >> 32));
                this.WriteRawByte((byte)(value >> 24));
                this.WriteRawByte((byte)(value >> 16));
                this.WriteRawByte((byte)(value >> 8));
                this.WriteRawByte((byte)value);
            }
        }

        public void WriteUInt64(ulong value) => this.WriteInt64((long)value);

        public void WriteDouble(double value)
        {
            const int Size = sizeof(double);
            this.Validate(this.Index, Size);

            long bits = BitConverter.DoubleToInt64Bits(value);
            this.WriteInt64(bits);
        }

        public void WriteFloat(float value)
        {
            const int Size = sizeof(float);
            this.Validate(this.Index, Size);

            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                bytes.Reverse();
            }

            this.WriteBytes(bytes, 0, Size);
        }

        void Validate(int index, int length)
        {
            if (this.byteBuffer == null)
            {
                throw new ObjectDisposedException($"{nameof(WritableBuffer)} has already been disposed.");
            }

            this.byteBuffer.Validate(index, length);
        }

        public void Dispose()
        {
            this.byteBuffer?.Dispose();
            this.byteBuffer = null;
            this.Index = 0;
            this.Count = 0;
        }
    }
}
