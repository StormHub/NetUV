// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Text;

    public struct WritableBuffer : IDisposable
    {
        readonly IByteBuffer buffer;

        internal WritableBuffer(IByteBuffer buffer)
        {
            Contract.Requires(buffer != null);

            this.buffer = buffer;
        }

        public static WritableBuffer From(byte[] array)
        {
            Contract.Requires(array != null);

            IByteBuffer buf = Unpooled.WrappedBuffer(array);
            return new WritableBuffer(buf);
        }

        internal IByteBuffer GetBuffer() => this.buffer;

        public void WriteBoolean(bool value) => this.buffer.WriteBoolean(value);

        public void WriteByte(byte value) => this.buffer.WriteByte(value);

        public void WriteSByte(sbyte value) => this.buffer.WriteByte((byte)value);

        public void WriteInt16(short value) => this.buffer.WriteShort(value);

        public void WriteInt16LE(short value) => this.buffer.WriteShortLE(value);

        public void WriteUInt16(ushort value) => this.buffer.WriteUnsignedShort(value);

        public void WriteUInt16LE(ushort value) => this.buffer.WriteUnsignedShortLE(value);

        public void WriteInt24(int value) => this.buffer.WriteMedium(value);

        public void WriteInt24LE(int value) => this.buffer.WriteMediumLE(value);

        public void WriteInt32(int value) => this.buffer.WriteInt(value);

        public void WriteInt32LE(int value) => this.buffer.WriteIntLE(value);

        public void WriteUInt32(uint value) => this.buffer.WriteInt((int)value);

        public void WriteUInt32LE(uint value) => this.buffer.WriteIntLE((int)value);

        public void WriteInt64(long value) => this.buffer.WriteLong(value);

        public void WriteInt64LE(long value) => this.buffer.WriteLongLE(value);

        public void WriteUInt64(ulong value) => this.buffer.WriteLong((long)value);

        public void WriteUInt64LE(ulong value) => this.buffer.WriteLongLE((long)value);

        public void WriteFloat(float value) => this.buffer.WriteFloat(value);

        public void WriteFloatLE(float value) => this.buffer.WriteFloatLE(value);

        public void WriteDouble(double value) => this.buffer.WriteDouble(value);

        public void WriteDoubleLE(double value) => this.buffer.WriteDoubleLE(value);

        public void WriteString(string value, Encoding encoding)
        {
            Contract.Requires(!string.IsNullOrEmpty(value));
            Contract.Requires(encoding != null);

            byte[] bytes = encoding.GetBytes(value);
            this.buffer.WriteBytes(bytes);
        }

        public void Dispose() => this.buffer.Release();
    }
}
