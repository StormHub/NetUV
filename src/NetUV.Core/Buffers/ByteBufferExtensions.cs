// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Text;

    static class ByteBufferExtensions
    {
        public static bool GetBoolean(this IArrayBuffer<byte> buffer)
        {
            Contract.Requires(buffer != null);

            return buffer.GetBoolean(buffer.ReaderIndex);
        }

        public static bool GetBoolean(this IArrayBuffer<byte> buffer, int start)
        {
            Contract.Requires(buffer != null);

            buffer.CheckIndex(start);
            int index = buffer.ArrayOffset + start;
            return buffer.Array[index] != 0;
        }

        public static void SetBoolean(this IArrayBuffer<byte> buffer, bool value)
        {
            Contract.Requires(buffer != null);

            buffer.SetBoolean(buffer.WriterIndex, value);
        }

        public static void SetBoolean(this IArrayBuffer<byte> buffer, int start, bool value)
        {
            Contract.Requires(buffer != null);

            buffer.CheckIndex(start);
            buffer.Array[buffer.WriterIndex] = (byte)(value ? 1 : 0);
        }

        public static short GetInt16(this IArrayBuffer<byte> buffer, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            return (short)buffer.GetUInt16(buffer.ReaderIndex, isLittleEndian);
        }

        public static ushort GetUInt16(this IArrayBuffer<byte> buffer, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            return buffer.GetUInt16(buffer.ReaderIndex, isLittleEndian);
        }

        public static short GetInt16(this IArrayBuffer<byte> buffer, int start, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            return (short)buffer.GetUInt16(start, isLittleEndian);
        }

        public static ushort GetUInt16(this IArrayBuffer<byte> buffer, int start, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.CheckIndex(start, sizeof(ushort));
            int index = buffer.ArrayOffset + start;
            if (isLittleEndian)
            {
                return (ushort)(buffer.Array[index]
                    | buffer.Array[index + 1] << 8);
            }

            return (ushort)(buffer.Array[index] << 8
                | buffer.Array[index + 1] & 0xFF);
        }

        public static void SetUInt16(this IArrayBuffer<byte> buffer, ushort value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.SetInt16(buffer.WriterIndex, (short)value, isLittleEndian);
        }

        public static void SetInt16(this IArrayBuffer<byte> buffer, short value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.SetInt16(buffer.WriterIndex, value, isLittleEndian);
        }

        public static void SetUInt16(this IArrayBuffer<byte> buffer, int start, ushort value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.SetInt16(start, (short)value, isLittleEndian);
        }

        public static void SetInt16(this IArrayBuffer<byte> buffer, int start, short value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.CheckIndex(start, sizeof(ushort));
            int index = buffer.ArrayOffset + start;
            if (isLittleEndian)
            {
                buffer.Array[index] = (byte)value;
                buffer.Array[index + 1] = ((byte)(value >> 8));
            }
            else
            {
                buffer.Array[index] = (byte)(value >> 8);
                buffer.Array[index + 1] = (byte)value;
            }
        }

        public static int GetInt32(this IArrayBuffer<byte> buffer, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            return buffer.GetInt32(buffer.ReaderIndex, isLittleEndian);
        }

        public static uint GetUInt32(this IArrayBuffer<byte> buffer, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            return (uint)buffer.GetInt32(buffer.ReaderIndex, isLittleEndian);
        }

        public static uint GetUInt32(this IArrayBuffer<byte> buffer, int start, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            return (uint)buffer.GetInt32(start, isLittleEndian);
        }

        public static int GetInt32(this IArrayBuffer<byte> buffer, int start, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.CheckIndex(start, sizeof(int));
            int index = buffer.ArrayOffset + start;
            if (isLittleEndian)
            {
                return (buffer.Array[index]
                    | buffer.Array[index + 1] << 8
                    | buffer.Array[index + 2] << 16
                    | buffer.Array[index + 3] << 24);
            }

            return (buffer.Array[index] & 0xFF) << 24
                | (buffer.Array[index + 1] & 0xFF) << 16
                | (buffer.Array[index + 2] & 0xFF) << 8
                | (buffer.Array[index + 3] & 0xFF);
        }

        public static void SetUInt32(this IArrayBuffer<byte> buffer, uint value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.SetInt32(buffer.WriterIndex, (int)value, isLittleEndian);
        }

        public static void SetInt32(this IArrayBuffer<byte> buffer, int value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.SetInt32(buffer.WriterIndex, value, isLittleEndian);
        }

        public static void SetUInt32(this IArrayBuffer<byte> buffer, int start, uint value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.SetInt32(start, (int)value, isLittleEndian);
        }

        public static void SetInt32(this IArrayBuffer<byte> buffer, int start, int value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.CheckIndex(start, sizeof(int));
            int index = buffer.ArrayOffset + start;
            if (isLittleEndian)
            {
                buffer.Array[index] = (byte)value;
                buffer.Array[index + 1] = (byte)(value >> 8);
                buffer.Array[index + 2] = (byte)(value >> 16);
                buffer.Array[index + 3] = (byte)(value >> 24);
            }
            else
            {
                buffer.Array[index] = (byte)(value >> 24);
                buffer.Array[index + 1] = (byte)(value >> 16);
                buffer.Array[index + 2] = (byte)(value >> 8);
                buffer.Array[index + 3] = (byte)value;
            }
        }

        public static ulong GetUInt64(this IArrayBuffer<byte> buffer, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            return (ulong)buffer.GetInt64(buffer.ReaderIndex, isLittleEndian);
        }

        public static long GetInt64(this IArrayBuffer<byte> buffer, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            return buffer.GetInt64(buffer.ReaderIndex, isLittleEndian);
        }

        public static ulong GetUInt64(this IArrayBuffer<byte> buffer, int start, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            return (ulong)buffer.GetInt64(start, isLittleEndian);
        }

        public static long GetInt64(this IArrayBuffer<byte> buffer, int start, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.CheckIndex(start, sizeof(long));
            int index = buffer.ArrayOffset + start;
            if (isLittleEndian)
            {
                // ReSharper disable once RedundantCast
                return ((long)buffer.Array[index]
                    | (long)buffer.Array[index + 1] << 8
                    | (long)buffer.Array[index + 2] << 16
                    | (long)buffer.Array[index + 3] << 24
                    | (long)buffer.Array[index + 4] << 32
                    | (long)buffer.Array[index + 5] << 40
                    | (long)buffer.Array[index + 6] << 48
                    | (long)buffer.Array[index + 7] << 56);
            }

            return ((long)buffer.Array[index] & 0xFF) << 56
                | ((long)buffer.Array[index + 1] & 0xFF) << 48
                | ((long)buffer.Array[index + 2] & 0xFF) << 40
                | ((long)buffer.Array[index + 3] & 0xFF) << 32
                | ((long)buffer.Array[index + 4] & 0xFF) << 24
                | ((long)buffer.Array[index + 5] & 0xFF) << 16
                | ((long)buffer.Array[index + 6] & 0xFF) << 8
                | ((long)buffer.Array[index + 7] & 0xFF);
        }

        public static void SetUInt64(this IArrayBuffer<byte> buffer, ulong value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.SetInt64(buffer.WriterIndex, (long)value, isLittleEndian);
        }

        public static void SetInt64(this IArrayBuffer<byte> buffer, long value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.SetInt64(buffer.WriterIndex, value, isLittleEndian);
        }

        public static void SetUInt64(this IArrayBuffer<byte> buffer, int start, ulong value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.SetInt64(start, (long)value, isLittleEndian);
        }

        public static void SetInt64(this IArrayBuffer<byte> buffer, int start, long value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.CheckIndex(start, sizeof(long));
            int index = buffer.ArrayOffset + start;
            if (isLittleEndian)
            {
                buffer.Array[index] = (byte)value;
                buffer.Array[index + 1] = (byte)(value >> 8);
                buffer.Array[index + 2] = (byte)(value >> 16);
                buffer.Array[index + 3] = (byte)(value >> 24);
                buffer.Array[index + 4] = (byte)(value >> 32);
                buffer.Array[index + 5] = (byte)(value >> 40);
                buffer.Array[index + 6] = (byte)(value >> 48);
                buffer.Array[index + 7] = (byte)(value >> 56);
            }
            else
            {
                buffer.Array[index] = (byte)(value >> 56);
                buffer.Array[index + 1] = (byte)(value >> 48);
                buffer.Array[index + 2] = (byte)(value >> 40);
                buffer.Array[index + 3] = (byte)(value >> 32);
                buffer.Array[index + 4] = (byte)(value >> 24);
                buffer.Array[index + 5] = (byte)(value >> 16);
                buffer.Array[index + 6] = (byte)(value >> 8);
                buffer.Array[index + 7] = (byte)value;
            }
        }

        public static float GetFloat(this IArrayBuffer<byte> buffer, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            return buffer.GetFloat(buffer.ReaderIndex, isLittleEndian);
        }

        public static float GetFloat(this IArrayBuffer<byte> buffer, int start, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);
            
            buffer.CheckIndex(start, sizeof(float));
            return ByteBufferUtil.Int32BitsToSingle(
                buffer.GetInt32(start, BitConverter.IsLittleEndian == isLittleEndian));
        }

        public static void SetFloat(this IArrayBuffer<byte> buffer, float value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.SetFloat(buffer.WriterIndex, value, isLittleEndian);
        }

        public static void SetFloat(this IArrayBuffer<byte> buffer, int start, float value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.CheckIndex(start, sizeof(float));
            buffer.SetInt32(start, 
                ByteBufferUtil.SingleToInt32Bits(value), 
                BitConverter.IsLittleEndian == isLittleEndian);
        }

        public static double GetDouble(this IArrayBuffer<byte> buffer, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            return buffer.GetDouble(buffer.ReaderIndex, isLittleEndian);
        }

        public static double GetDouble(this IArrayBuffer<byte> buffer, int start, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.CheckIndex(start, sizeof(double));
            return BitConverter.Int64BitsToDouble(
                buffer.GetInt64(start, BitConverter.IsLittleEndian == isLittleEndian));
        }

        public static void SetDouble(this IArrayBuffer<byte> buffer, double value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.SetDouble(buffer.WriterIndex, value, isLittleEndian);
        }

        public static void SetDouble(this IArrayBuffer<byte> buffer, int start, double value, bool isLittleEndian)
        {
            Contract.Requires(buffer != null);

            buffer.SetInt64(start, 
                BitConverter.DoubleToInt64Bits(value),
                BitConverter.IsLittleEndian == isLittleEndian);
        }

        public static string GetString(this IArrayBuffer<byte> buffer, Encoding encoding)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(encoding != null);

            if (buffer.ReadableCount == 0)
            {
                return string.Empty;
            }

            int index = buffer.ArrayOffset + buffer.ReaderIndex;
            return encoding.GetString(buffer.Array, index, buffer.ReadableCount);
        }

        public static string GetString(this IArrayBuffer<byte> buffer, int length, Encoding encoding)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(encoding != null);
            Contract.Requires(length >= 0);

            if (length == 0)
            {
                return string.Empty;
            }

            int index = buffer.ReaderIndex;
            buffer.CheckIndex(index, length);
            index += buffer.ArrayOffset;
            return encoding.GetString(buffer.Array, index, length);
        }

        public static string GetString(this IArrayBuffer<byte> buffer, int start, int length, Encoding encoding)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(encoding != null);
            Contract.Requires(length >= 0);

            if (length == 0)
            {
                return string.Empty;
            }

            buffer.CheckIndex(start, length);
            int index = buffer.ArrayOffset + start;
            return encoding.GetString(buffer.Array, index, length);
        }

        public static string GetString(this IArrayBuffer<byte> buffer, byte[] separator, Encoding encoding, out int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(encoding != null);
            Contract.Requires(separator != null && separator.Length > 0);

            if (buffer.ReadableCount == 0)
            {
                count = 0;
                return string.Empty;
            }

            return buffer.GetString(buffer.ReaderIndex, separator, encoding, out count);
        }

        public static string GetString(this IArrayBuffer<byte> buffer, int start, byte[] separator, Encoding encoding, out int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(encoding != null);
            Contract.Requires(separator != null && separator.Length > 0);

            buffer.CheckIndex(start);
            int index = buffer.ArrayOffset + start;

            if (buffer.ReadableCount < separator.Length)
            {
                count = buffer.ReadableCount;
                return encoding.GetString(buffer.Array, index, count);
            }

            int frameLength = IndexOf(buffer, separator);
            if (frameLength == 0) // Leading separator
            {
                frameLength = separator.Length;
            }
            else if (frameLength < 0) // Not found
            {
                frameLength = buffer.ReadableCount;
            }

            count = frameLength;
            return encoding.GetString(buffer.Array, index, count);
        }

        static int IndexOf(IArrayBuffer<byte> haystack, byte[] separator)
        {
            for (int i = haystack.ReaderIndex; i < haystack.WriterIndex; i++)
            {
                int haystackIndex = i;
                int needleIndex;
                for (needleIndex = 0; needleIndex < separator.Length; needleIndex++)
                {
                    if (haystack.Get(haystackIndex) != separator[needleIndex])
                    {
                        break;
                    }
                    else
                    {
                        haystackIndex++;
                        if (haystackIndex == haystack.WriterIndex && needleIndex != separator.Length - 1)
                        {
                            return -1;
                        }
                    }
                }

                if (needleIndex == separator.Length)
                {
                    // Found the needle from the haystack!
                    return i - haystack.ReaderIndex;
                }
            }

            return -1;
        }

        public static int SetString(this IArrayBuffer<byte> buffer, string value, Encoding encoding)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(encoding != null);

            return !string.IsNullOrEmpty(value) 
                ? buffer.SetString(buffer.WriterIndex, value, encoding) 
                : 0;
        }

        public static int SetString(this IArrayBuffer<byte> buffer, int start, string value, Encoding encoding)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(encoding != null);

            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            byte[] data = encoding.GetBytes(value);
            buffer.CheckIndex(start, data.Length);

            int index = buffer.ArrayOffset + start;
            buffer.Set(index, data);
            return data.Length;
        }
    }
}
