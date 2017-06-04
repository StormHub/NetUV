// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Text;

    public struct WritableBuffer : IDisposable
    {
        readonly bool isLittleEndian;

        internal WritableBuffer(IArrayBuffer<byte> buffer) 
            : this(buffer, BitConverter.IsLittleEndian)
        {
        }

        internal WritableBuffer(IArrayBuffer<byte> buffer, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            this.ArrayBuffer = buffer;
            this.isLittleEndian = isLittleEndian;
        }

        internal IArrayBuffer<byte> ArrayBuffer { get; private set; }

        public static WritableBuffer From(byte[] array)
        {
            Contract.Requires(array != null && array.Length > 0);

            IArrayBuffer<byte> buffer = Unpooled.WrappedBuffer(array);
            return new WritableBuffer(buffer);
        }

        public void WriteBoolean(bool value)
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(WritableBuffer)} has already been disposed.");
            }

            buffer.SetBoolean(value);
            buffer.SetWriterIndex(buffer.WriterIndex + sizeof(bool));
        }

        public void WriteByte(byte value)
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(WritableBuffer)} has already been disposed.");
            }

            buffer.EnsureWritable(1);
            buffer.Write(value);
            buffer.SetWriterIndex(buffer.WriterIndex + 1);
        }

        public void WriteSByte(sbyte value) => this.WriteByte((byte)value);

        public void WriteInt16(short value)
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(WritableBuffer)} has already been disposed.");
            }

            buffer.SetInt16(value, this.isLittleEndian);
            buffer.SetWriterIndex(buffer.WriterIndex + sizeof(short));
        }

        public void WriteUInt16(ushort value) => this.WriteInt16((short)value);

        public void WriteInt32(int value)
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(WritableBuffer)} has already been disposed.");
            }

            buffer.SetInt32(value, this.isLittleEndian);
            buffer.SetWriterIndex(buffer.WriterIndex + sizeof(int));
        }

        public void WriteUInt32(uint value) => this.WriteInt32((int)value);

        public void WriteInt64(long value)
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(WritableBuffer)} has already been disposed.");
            }

            buffer.SetInt64(value, this.isLittleEndian);
            buffer.SetWriterIndex(buffer.WriterIndex + sizeof(long));
        }

        public void WriteUInt64(ulong value) => this.WriteInt64((long)value);

        public void WriteFloat(float value)
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(WritableBuffer)} has already been disposed.");
            }

            buffer.SetFloat(value, this.isLittleEndian);
            buffer.SetWriterIndex(buffer.WriterIndex + sizeof(float));
        }

        public void WriteDouble(double value)
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(WritableBuffer)} has already been disposed.");
            }

            buffer.SetDouble(value, this.isLittleEndian);
            buffer.SetWriterIndex(buffer.WriterIndex + sizeof(double));
        }

        public void WriteString(string value, Encoding encoding)
        {
            Contract.Requires(!string.IsNullOrEmpty(value));
            Contract.Requires(encoding != null);

            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(WritableBuffer)} has already been disposed.");
            }

            byte[] bytes = encoding.GetBytes(value);
            if (!buffer.IsWritable(bytes.Length))
            {
                throw new InvalidOperationException(
                    $"{nameof(WritableBuffer)} expecting : {bytes.Length} >= {buffer.WritableCount} .");
            }

            this.ArrayBuffer.Write(bytes);
        }

        public void Dispose()
        {
            this.ArrayBuffer?.Release();
            this.ArrayBuffer = null;
        }
    }
}
