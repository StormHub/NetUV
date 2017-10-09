// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System.Runtime.CompilerServices;

    static unsafe class UnsafeByteBufferUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte GetByte(byte[] array, int index) => array[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static short GetShort(byte[] array, int index)
        {
            fixed (byte* bytes = &array[index])
                unchecked
                {
                    return (short)(((*bytes) << 8) 
                        | (*(bytes + 1)));
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static short GetShortLE(byte[] array, int index)
        {
            fixed (byte* bytes = &array[index])
                unchecked
                {
                    return (short)((*bytes) | (*(bytes + 1) << 8));
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetUnsignedMedium(byte[] array, int index)
        {
            fixed (byte* bytes = &array[index])
                return *bytes << 16 | 
                    *(bytes + 1) << 8 | 
                    *(bytes + 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetUnsignedMediumLE(byte[] array, int index)
        {
            fixed (byte* bytes = &array[index])
                return *bytes | 
                    *(bytes + 1) << 8 | 
                    *(bytes + 2) << 16;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetInt(byte[] array, int index)
        {
            fixed (byte* bytes = &array[index])
                return (*bytes << 24) | 
                    (*(bytes + 1) << 16) | 
                    (*(bytes + 2) << 8) | 
                    (*(bytes + 3));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetIntLE(byte[] array, int index)
        {
            fixed (byte* bytes = &array[index])
                return *bytes | 
                    (*(bytes + 1) << 8) |
                    (*(bytes + 2) << 16) | 
                    (*(bytes + 3) << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long GetLong(byte[] array, int index)
        {
            fixed (byte* bytes = &array[index])
                unchecked
                {
                    int i1 = (*bytes << 24) | (*(bytes + 1) << 16) | (*(bytes + 2) << 8) | (*(bytes + 3));
                    int i2 = (*(bytes + 4) << 24) | (*(bytes + 5) << 16) | (*(bytes + 6) << 8) | *(bytes + 7);
                    return (uint)i2 | ((long)i1 << 32);
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long GetLongLE(byte[] array, int index)
        {
            fixed (byte* bytes = &array[index])
                unchecked
                {
                    int i1 = *bytes | (*(bytes + 1) << 8) | (*(bytes + 2) << 16) | (*(bytes + 3) << 24);
                    int i2 = *(bytes + 4) | (*(bytes + 5) << 8) | (*(bytes + 6) << 16) | (*(bytes + 7) << 24);
                    return (uint)i1 | ((long)i2 << 32);
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetByte(byte[] array, int index, int value)
        {
            unchecked
            {
                array[index] = (byte)value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetShort(byte[] array, int index, int value)
        {
            fixed (byte* bytes = &array[index])
                unchecked
                {
                    *bytes = (byte)((ushort)value >> 8);
                    *(bytes + 1) = (byte)value;
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetShortLE(byte[] array, int index, int value)
        {
            fixed (byte* bytes = &array[index])
                unchecked
                {
                    *bytes = (byte)value;
                    *(bytes + 1) = (byte)((ushort)value >> 8);
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetMedium(byte[] array, int index, int value)
        {
            fixed (byte* bytes = &array[index])
                unchecked
                {
                    uint unsignedValue = (uint)value;
                    *bytes = (byte)(unsignedValue >> 16);
                    *(bytes + 1) = (byte)(unsignedValue >> 8);
                    *(bytes + 2) = (byte)unsignedValue;
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetMediumLE(byte[] array, int index, int value)
        {
            fixed (byte* bytes = &array[index])
                unchecked
                {
                    uint unsignedValue = (uint)value;
                    *bytes = (byte)unsignedValue;
                    *(bytes + 1) = (byte)(unsignedValue >> 8);
                    *(bytes + 2) = (byte)(unsignedValue >> 16);
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetInt(byte[] array, int index, int value)
        {
            fixed (byte* bytes = &array[index])
                unchecked
                {
                    uint unsignedValue = (uint)value;
                    *bytes = (byte)(unsignedValue >> 24);
                    *(bytes + 1) = (byte)(unsignedValue >> 16);
                    *(bytes + 2) = (byte)(unsignedValue >> 8);
                    *(bytes + 3) = (byte)unsignedValue;
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetIntLE(byte[] array, int index, int value)
        {
            fixed (byte* bytes = &array[index])
                unchecked
                {
                    uint unsignedValue = (uint)value;
                    *bytes = (byte)unsignedValue;
                    *(bytes + 1) = (byte)(unsignedValue >> 8);
                    *(bytes + 2) = (byte)(unsignedValue >> 16);
                    *(bytes + 3) = (byte)(unsignedValue >> 24);
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetLong(byte[] array, int index, long value)
        {
            fixed (byte* bytes = &array[index])
                unchecked
                {
                    ulong unsignedValue = (ulong)value;
                    *bytes = (byte)(unsignedValue >> 56);
                    *(bytes + 1) = (byte)(unsignedValue >> 48);
                    *(bytes + 2) = (byte)(unsignedValue >> 40);
                    *(bytes + 3) = (byte)(unsignedValue >> 32);
                    *(bytes + 4) = (byte)(unsignedValue >> 24);
                    *(bytes + 5) = (byte)(unsignedValue >> 16);
                    *(bytes + 6) = (byte)(unsignedValue >> 8);
                    *(bytes + 7) = (byte)unsignedValue;
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetLongLE(byte[] array, int index, long value)
        {
            fixed (byte* bytes = &array[index])
                unchecked
                {
                    ulong unsignedValue = (ulong)value;
                    *bytes = (byte)unsignedValue;
                    *(bytes + 1) = (byte)(unsignedValue >> 8);
                    *(bytes + 2) = (byte)(unsignedValue >> 16);
                    *(bytes + 3) = (byte)(unsignedValue >> 24);
                    *(bytes + 4) = (byte)(unsignedValue >> 32);
                    *(bytes + 5) = (byte)(unsignedValue >> 40);
                    *(bytes + 6) = (byte)(unsignedValue >> 48);
                    *(bytes + 7) = (byte)(unsignedValue >> 56);
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetZero(byte[] array, int index, int length)
        {
            fixed (byte* bytes = &array[index])
                Unsafe.InitBlock(bytes, 0, (uint)length);
        }
    }
}
