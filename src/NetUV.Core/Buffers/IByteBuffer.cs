// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Text;

    interface IByteBuffer : IReferenceCounted, IComparable<IByteBuffer>, IEquatable<IByteBuffer>
    {
        int Capacity { get; }

        IByteBuffer AdjustCapacity(int newCapacity);

        int MaxCapacity { get; }

        IByteBufferAllocator Allocator { get; }

        int ReaderIndex { get; }

        int WriterIndex { get; }

        IByteBuffer SetWriterIndex(int writerIndex);

        IByteBuffer SetReaderIndex(int readerIndex);

        IByteBuffer SetIndex(int readerIndex, int writerIndex);

        int ReadableBytes { get; }

        int WritableBytes { get; }

        int MaxWritableBytes { get; }

        bool IsReadable();

        bool IsReadable(int size);

        bool IsWritable();

        bool IsWritable(int size);

        IByteBuffer Clear();

        IByteBuffer MarkReaderIndex();

        IByteBuffer ResetReaderIndex();

        IByteBuffer MarkWriterIndex();

        IByteBuffer ResetWriterIndex();

        IByteBuffer DiscardReadBytes();

        IByteBuffer DiscardSomeReadBytes();

        IByteBuffer EnsureWritable(int minWritableBytes);

        int EnsureWritable(int minWritableBytes, bool force);

        bool GetBoolean(int index);

        byte GetByte(int index);

        short GetShort(int index);

        short GetShortLE(int index);

        ushort GetUnsignedShort(int index);

        ushort GetUnsignedShortLE(int index);

        int GetInt(int index);

        int GetIntLE(int index);

        uint GetUnsignedInt(int index);

        uint GetUnsignedIntLE(int index);

        long GetLong(int index);

        long GetLongLE(int index);

        int GetMedium(int index);

        int GetMediumLE(int index);

        int GetUnsignedMedium(int index);

        int GetUnsignedMediumLE(int index);

        char GetChar(int index);

        float GetFloat(int index);

        float GetFloatLE(int index);

        double GetDouble(int index);

        double GetDoubleLE(int index);

        IByteBuffer GetBytes(int index, IByteBuffer destination);

        IByteBuffer GetBytes(int index, IByteBuffer destination, int length);

        IByteBuffer GetBytes(int index, IByteBuffer destination, int dstIndex, int length);

        IByteBuffer GetBytes(int index, byte[] destination);

        IByteBuffer GetBytes(int index, byte[] destination, int dstIndex, int length);

        IByteBuffer SetBoolean(int index, bool value);

        IByteBuffer SetByte(int index, int value);

        IByteBuffer SetShort(int index, int value);

        IByteBuffer SetShortLE(int index, int value);

        IByteBuffer SetUnsignedShort(int index, ushort value);

        IByteBuffer SetUnsignedShortLE(int index, ushort value);

        IByteBuffer SetInt(int index, int value);

        IByteBuffer SetIntLE(int index, int value);

        IByteBuffer SetUnsignedInt(int index, uint value);

        IByteBuffer SetUnsignedIntLE(int index, uint value);

        IByteBuffer SetMedium(int index, int value);

        IByteBuffer SetMediumLE(int index, int value);

        IByteBuffer SetLong(int index, long value);

        IByteBuffer SetLongLE(int index, long value);

        IByteBuffer SetChar(int index, char value);

        IByteBuffer SetDouble(int index, double value);

        IByteBuffer SetFloat(int index, float value);

        IByteBuffer SetDoubleLE(int index, double value);

        IByteBuffer SetFloatLE(int index, float value);

        IByteBuffer SetBytes(int index, IByteBuffer src);

        IByteBuffer SetBytes(int index, IByteBuffer src, int length);

        IByteBuffer SetBytes(int index, IByteBuffer src, int srcIndex, int length);

        IByteBuffer SetBytes(int index, byte[] src);

        IByteBuffer SetBytes(int index, byte[] src, int srcIndex, int length);

        IByteBuffer SetZero(int index, int length);

        bool ReadBoolean();

        byte ReadByte();

        short ReadShort();

        short ReadShortLE();

        int ReadMedium();

        int ReadMediumLE();

        int ReadUnsignedMedium();

        int ReadUnsignedMediumLE();

        ushort ReadUnsignedShort();

        ushort ReadUnsignedShortLE();

        int ReadInt();

        int ReadIntLE();

        uint ReadUnsignedInt();

        uint ReadUnsignedIntLE();

        long ReadLong();

        long ReadLongLE();

        char ReadChar();

        double ReadDouble();

        double ReadDoubleLE();

        float ReadFloat();

        float ReadFloatLE();

        IByteBuffer ReadBytes(int length);

        IByteBuffer ReadBytes(IByteBuffer destination);

        IByteBuffer ReadBytes(IByteBuffer destination, int length);

        IByteBuffer ReadBytes(IByteBuffer destination, int dstIndex, int length);

        IByteBuffer ReadBytes(byte[] destination);

        IByteBuffer ReadBytes(byte[] destination, int dstIndex, int length);

        IByteBuffer SkipBytes(int length);

        IByteBuffer WriteBoolean(bool value);

        IByteBuffer WriteByte(int value);

        IByteBuffer WriteShort(int value);

        IByteBuffer WriteShortLE(int value);

        IByteBuffer WriteUnsignedShort(ushort value);

        IByteBuffer WriteUnsignedShortLE(ushort value);

        IByteBuffer WriteMedium(int value);

        IByteBuffer WriteMediumLE(int value);

        IByteBuffer WriteInt(int value);

        IByteBuffer WriteIntLE(int value);

        IByteBuffer WriteLong(long value);

        IByteBuffer WriteLongLE(long value);

        IByteBuffer WriteChar(char value);

        IByteBuffer WriteDouble(double value);

        IByteBuffer WriteDoubleLE(double value);

        IByteBuffer WriteFloat(float value);

        IByteBuffer WriteFloatLE(float value);

        IByteBuffer WriteBytes(IByteBuffer src);

        IByteBuffer WriteBytes(IByteBuffer src, int length);

        IByteBuffer WriteBytes(IByteBuffer src, int srcIndex, int length);

        IByteBuffer WriteBytes(byte[] src);

        IByteBuffer WriteBytes(byte[] src, int srcIndex, int length);

        int IoBufferCount { get; }

        ArraySegment<byte> GetIoBuffer();

        ArraySegment<byte> GetIoBuffer(int index, int length);

        ArraySegment<byte>[] GetIoBuffers();

        ArraySegment<byte>[] GetIoBuffers(int index, int length);

        bool HasArray { get; }

        byte[] Array { get; }

        int ArrayOffset { get; }

        IByteBuffer Duplicate();

        IByteBuffer RetainedDuplicate();

        IByteBuffer Unwrap();

        IByteBuffer Copy();

        IByteBuffer Copy(int index, int length);

        IByteBuffer Slice();

        IByteBuffer RetainedSlice();

        IByteBuffer Slice(int index, int length);

        IByteBuffer RetainedSlice(int index, int length);

        IByteBuffer ReadSlice(int length);

        IByteBuffer ReadRetainedSlice(int length);

        IByteBuffer WriteZero(int length);

        int IndexOf(int fromIndex, int toIndex, byte value);

        int BytesBefore(byte value);

        int BytesBefore(int length, byte value);

        int BytesBefore(int index, int length, byte value);

        string ToString();

        string ToString(Encoding encoding);

        string ToString(int index, int length, Encoding encoding);

        int ForEachByte(ByteProcessor processor);

        int ForEachByte(int index, int length, ByteProcessor processor);

        int ForEachByteDesc(ByteProcessor processor);

        int ForEachByteDesc(int index, int length, ByteProcessor processor);
    }
}
