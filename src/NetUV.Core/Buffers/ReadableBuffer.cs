// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Text;

    public struct ReadableBuffer : IDisposable
    {
        internal static ReadableBuffer Empty = new ReadableBuffer(Unpooled.Empty, 0);
        readonly bool isLittleEndian;

        internal ReadableBuffer(IArrayBuffer<byte> buffer, int count) 
            : this(buffer, count, BitConverter.IsLittleEndian)
        {
        }

        internal ReadableBuffer(IArrayBuffer<byte> buffer, int count, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(count >= 0);

            this.ArrayBuffer = buffer;
            this.ArrayBuffer.SetWriterIndex(count);
            this.isLittleEndian = isLittleEndian;
        }

        internal IArrayBuffer<byte> ArrayBuffer { get; private set; }

        public int Count => this.ArrayBuffer.ReadableCount;

        public string ReadString(Encoding encoding, byte[] separator)
        {
            Contract.Requires(encoding != null);
            Contract.Requires(separator != null && separator.Length > 0);

            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(ReadableBuffer)} has already been disposed.");
            }
            if (buffer.ReadableCount == 0)
            {
                return string.Empty;
            }
            string result = buffer.GetString(separator, encoding, out int count);
            buffer.Skip(count);

            return result;
        }

        public string ReadString(Encoding encoding)
        {
            Contract.Requires(encoding != null);

            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(ReadableBuffer)} has already been disposed.");
            }

            int length = buffer.ReadableCount;
            if (length == 0)
            {
                return string.Empty;
            }

            string result = buffer.GetString(length, encoding);
            buffer.Skip(length);

            return result;
        }

        public string ReadString(int length, Encoding encoding)
        {
            Contract.Requires(length > 0);
            Contract.Requires(encoding != null);

            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(ReadableBuffer)} has already been disposed.");
            }

            string result = buffer.GetString(length, encoding);
            buffer.Skip(length);

            return result;
        }

        public bool ReadBoolean()
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(ReadableBuffer)} has already been disposed.");
            }

            bool value = buffer.GetBoolean();
            buffer.Skip(1);
            return value;
        }

        public byte ReadByte()
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(ReadableBuffer)} has already been disposed.");
            }

            byte value = buffer.Get(buffer.ReaderIndex);
            buffer.Skip(1);
            return value;
        }

        public sbyte ReadSByte() => (sbyte)this.ReadByte();

        public ushort ReadUInt16()
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(ReadableBuffer)} has already been disposed.");
            }

            ushort value = buffer.GetUInt16(this.isLittleEndian);
            buffer.Skip(sizeof(ushort));
            return value;
        }

        public short ReadInt16() => (short)this.ReadUInt16();

        public void ReadBytes(byte[] destination, int length)
        {
            Contract.Requires(destination != null);
            Contract.Requires(length > 0);

            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(ReadableBuffer)} has already been disposed.");
            }
            if (!buffer.IsReadable(length))
            {
                throw new InvalidOperationException(
                    $"{nameof(ReadableBuffer)} expecting : {length} < {buffer.ReadableCount}");
            }

            buffer.Read(destination, length);
            //buffer.SetReaderIndex(buffer.ReaderIndex + length);
        }

        public int ReadInt32()
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(ReadableBuffer)} has already been disposed.");
            }

            int value = buffer.GetInt32(this.isLittleEndian);
            buffer.Skip(sizeof(int));
            return value;
        }

        public uint ReadUInt32() => (uint)this.ReadInt32();

        public long ReadInt64()
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(ReadableBuffer)} has already been disposed.");
            }

            long value = buffer.GetInt64(this.isLittleEndian);
            buffer.Skip(sizeof(long));
            return value;
        }

        public ulong ReadUInt64() => (ulong)this.ReadInt64();

        public float ReadFloat()
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(ReadableBuffer)} has already been disposed.");
            }

            float value = buffer.GetFloat(this.isLittleEndian);
            buffer.Skip(sizeof(float));
            return value;
        }

        public double ReadDouble()
        {
            IArrayBuffer<byte> buffer = this.ArrayBuffer;
            if (buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(ReadableBuffer)} has already been disposed.");
            }

            double value = buffer.GetDouble(this.isLittleEndian);
            buffer.Skip(sizeof(double));
            return value;
        }

        public void Dispose()
        {
            if (this.ArrayBuffer == null)
            {
                return;
            }

            // It is possible for the consumers to release
            // the buffer
            if (this.ArrayBuffer.ReferenceCount > 0)
            {
                this.ArrayBuffer.Release();
            }

            this.ArrayBuffer = null;
        }
    }
}
