// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;

    static class ByteBufferUtil
    {
        static readonly char[] HexdumpTable = new char[256 * 4];

        static ByteBufferUtil()
        {
            char[] digits = "0123456789abcdef".ToCharArray();
            for (int i = 0; i < 256; i++)
            {
                HexdumpTable[i << 1] = digits[(int)((uint)i >> 4 & 0x0F)];
                HexdumpTable[(i << 1) + 1] = digits[i & 0x0F];
            }
        }
        public static unsafe int SingleToInt32Bits(float value)
        {
            return *(int*)(&value);
        }

        public static unsafe float Int32BitsToSingle(int value)
        {
            return *(float*)(&value);
        }

        public static bool Equals(IArrayBuffer<byte> bufferA, IArrayBuffer<byte> bufferB)
        {
            int aLen = bufferA.ReadableCount;
            return aLen == bufferB.ReadableCount 
                && Equals(bufferA, bufferA.ReaderIndex, bufferB, bufferB.ReaderIndex, aLen);
        }

        public static bool Equals(IArrayBuffer<byte> a, int aStartIndex, IArrayBuffer<byte> b, int bStartIndex, int length, bool isLittleEndian)
        {
            Contract.Requires(aStartIndex >= 0 && bStartIndex >= 0 && length >= 0);

            if (a.WriterIndex - length < aStartIndex 
                || b.WriterIndex - length < bStartIndex)
            {
                return false;
            }

            int longCount = unchecked((int)((uint)length >> 3));
            int byteCount = length & 7;

            for (int i = longCount; i > 0; i--)
            {
                if (a.GetInt64(aStartIndex, BitConverter.IsLittleEndian) != b.GetInt64(bStartIndex, BitConverter.IsLittleEndian))
                {
                    return false;
                }

                aStartIndex += 8;
                bStartIndex += 8;
            }

            for (int i = byteCount; i > 0; i--)
            {
                if (a.Get(aStartIndex) != b.Get(bStartIndex))
                {
                    return false;
                }
                aStartIndex++;
                bStartIndex++;
            }

            return true;
        }

        public static bool Equals(IArrayBuffer<byte> a, int aStartIndex, IArrayBuffer<byte> b, int bStartIndex, int length) =>
            Equals(a, aStartIndex, b, bStartIndex, length, BitConverter.IsLittleEndian);

        public static int Compare(IArrayBuffer<byte> bufferA, IArrayBuffer<byte> bufferB, bool isLittleEndian)
        {
            int aLen = bufferA.ReadableCount;
            int bLen = bufferB.ReadableCount;
            int minLength = Math.Min(aLen, bLen);
            int uintCount = (int)((uint)minLength >> 2);
            int byteCount = minLength & 3;

            int aIndex = bufferA.ReaderIndex;
            int bIndex = bufferB.ReaderIndex;

            for (int i = uintCount; i > 0; i--)
            {
                long va = bufferA.GetUInt32(aIndex, isLittleEndian);
                long vb = bufferB.GetUInt32(bIndex, isLittleEndian);
                if (va > vb)
                {
                    return 1;
                }
                if (va < vb)
                {
                    return -1;
                }
                aIndex += 4;
                bIndex += 4;
            }

            for (int i = byteCount; i > 0; i--)
            {
                short va = bufferA.Get(aIndex);
                short vb = bufferB.Get(bIndex);
                if (va > vb)
                {
                    return 1;
                }
                if (va < vb)
                {
                    return -1;
                }
                aIndex++;
                bIndex++;
            }

            return aLen - bLen;
        }

        public static int Compare(IArrayBuffer<byte> bufferA, IArrayBuffer<byte> bufferB) =>
            Compare(bufferA, bufferB, BitConverter.IsLittleEndian);


        public static string HexDump(IArrayBuffer<byte> buffer) => 
            HexDump(buffer, buffer.ReaderIndex, buffer.ReadableCount);

        /// <summary>
        /// Returns a <a href="http://en.wikipedia.org/wiki/Hex_dump">hex dump</a>
        /// of the specified buffer's sub-region.
        /// </summary>
        public static string HexDump(IArrayBuffer<byte> buffer, int fromIndex, int length)
        {
            Contract.Requires(length >= 0);
            if (length == 0)
            {
                return "";
            }
            int endIndex = fromIndex + length;
            var buf = new char[length << 1];

            int srcIdx = fromIndex;
            int dstIdx = 0;
            for (; srcIdx < endIndex; srcIdx++, dstIdx += 2)
            {
                Array.Copy(
                    HexdumpTable, buffer.Get(srcIdx) << 1,
                    buf, dstIdx, 2);
            }

            return new string(buf);
        }
    }
}
