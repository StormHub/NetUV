// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;

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
            buffer.SetInt32(start, ByteBufferUtil.SingleToInt32Bits(value), 
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

            buffer.SetInt64(start, BitConverter.DoubleToInt64Bits(value),
                BitConverter.IsLittleEndian == isLittleEndian);
        }
    }
}
