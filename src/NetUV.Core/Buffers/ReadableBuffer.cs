// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Text;
    using NetUV.Core.Common;

    public struct ReadableBuffer : IDisposable
    {
        internal static ReadableBuffer Empty = new ReadableBuffer(Unpooled.Empty, 0);
        readonly IByteBuffer buffer;

        internal ReadableBuffer(IByteBuffer buffer, int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(count >= 0);

            this.buffer = buffer;
            this.buffer.SetWriterIndex(count);
        }

        ReadableBuffer(IByteBuffer buffer)
        {
            Contract.Requires(buffer != null);

            this.buffer = buffer;
        }

        public int Count => this.buffer.ReadableBytes;

        public ReadableBuffer Retain()
        {
            this.buffer.Retain();
            return this;
        }

        public static ReadableBuffer Composite(IEnumerable<ReadableBuffer> buffers)
        {
            Contract.Requires(buffers != null);

            CompositeByteBuffer composite = Unpooled.CompositeBuffer();
            foreach (ReadableBuffer buf in buffers)
            {
                IByteBuffer byteBuffer = buf.buffer;
                if (byteBuffer.ReadableBytes > 0)
                {
                    composite.AddComponent(byteBuffer);
                }
            }

            return new ReadableBuffer(composite);
        }

        public string ReadString(Encoding encoding, byte[] separator)
        {
            Contract.Requires(encoding != null);
            Contract.Requires(separator != null && separator.Length > 0);

            int readableBytes = this.buffer.ReadableBytes;
            if (readableBytes == 0)
            {
                return string.Empty;
            }

            IByteBuffer buf = Unpooled.WrappedBuffer(separator);
            return ByteBufferUtil.ReadString(this.buffer, buf, encoding);
        }

        public string ReadString(Encoding encoding) => this.ReadString(this.buffer.ReadableBytes, encoding);

        public string ReadString(int length, Encoding encoding) => this.buffer.ToString(this.buffer.ReaderIndex, length, encoding);

        public bool ReadBoolean() => this.buffer.ReadBoolean();

        public byte ReadByte() => this.buffer.ReadByte();

        public sbyte ReadSByte() => unchecked((sbyte)this.buffer.ReadByte());

        public short ReadInt16() => this.buffer.ReadShort();

        public short ReadInt16LE() => this.buffer.ReadShortLE();

        public ushort ReadUInt16() => this.buffer.ReadUnsignedShort();

        public ushort ReadUInt16LE() => this.buffer.ReadUnsignedShortLE();

        public int ReadInt24() => this.buffer.ReadMedium();

        public int ReadInt24LE() => this.buffer.ReadMediumLE();

        public uint ReadUInt24() => unchecked ((uint) this.buffer.ReadUnsignedMedium());

        public uint ReadUInt24LE() => unchecked((uint)this.buffer.ReadUnsignedMediumLE());

        public int ReadInt32() => this.buffer.ReadInt();

        public int ReadInt32LE() => this.buffer.ReadIntLE();

        public uint ReadUInt32() => this.buffer.ReadUnsignedInt();

        public uint ReadUInt32LE() => this.buffer.ReadUnsignedIntLE();

        public long ReadInt64() => this.buffer.ReadLong();

        public long ReadInt64LE() => this.buffer.ReadLongLE();

        public ulong ReadUInt64() => unchecked((ulong)this.buffer.ReadLong());

        public ulong ReadUInt64LE() => unchecked((ulong)this.buffer.ReadLongLE());

        public float ReadFloat() => this.buffer.ReadFloat();

        public float ReadFloatLE() => this.buffer.ReadFloatLE();

        public double ReadDouble() => this.buffer.ReadDouble();

        public double ReadDoubleLE() => this.buffer.ReadDoubleLE();

        public void ReadBytes(byte[] destination) => this.buffer.ReadBytes(destination);

        public void ReadBytes(byte[] destination, int length) => this.buffer.ReadBytes(destination, 0, length);

        public void Dispose() => this.buffer.SafeRelease();
    }
}
