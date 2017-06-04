// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;

    // Forked from https://github.com/Azure/DotNetty
    static class ArrayExtensions
    {
        public static T[] Slice<T>(this T[] array, int length)
        {
            Contract.Requires(array != null);

            if (length > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), 
                    $"length({length}) cannot be longer than Array.length({array.Length})");
            }
            return Slice(array, 0, length);
        }

        public static T[] Slice<T>(this T[] array, int index, int length)
        {
            Contract.Requires(array != null);

            if (index + length > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), 
                    $"index: ({index}), length({length}) index + length cannot be longer than Array.length({array.Length})");
            }
            var result = new T[length];
            Array.Copy(array, index, result, 0, length);
            return result;
        }

        public static void SetRange<T>(this T[] array, int index, T[] src) => SetRange(array, index, src, 0, src.Length);

        public static void SetRange<T>(this T[] array, int index, T[] src, int srcIndex, int srcLength)
        {
            Contract.Requires(array != null);
            Contract.Requires(src != null);
            if (index + srcLength > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(srcLength), 
                    $"index: ({index}), srcLength({srcLength}) index + length cannot be longer than Array.length({array.Length})");
            }
            if (srcIndex + srcLength > src.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(srcLength), 
                    $"index: ({srcIndex}), srcLength({srcLength}) index + length cannot be longer than src.length({src.Length})");
            }

            Array.Copy(src, srcIndex, array, index, srcLength);
        }

        public static void Fill<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        public static void Fill<T>(this T[] array, int offset, int count, T value)
        {
            Contract.Requires(count + offset <= array.Length);

            for (int i = offset; i < count + offset; i++)
            {
                array[i] = value;
            }
        }

        public static byte[] CombineBytes(this byte[][] arrays)
        {
            long newLength = 0;
            foreach (byte[] array in arrays)
            {
                newLength += array.Length;
            }

            var mergedArray = new byte[newLength];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, mergedArray, offset, array.Length);
                offset += array.Length;
            }

            return mergedArray;
        }

        public static unsafe void CopyBlock<T>(this T[] source, T[] destination, int offset, int count)
        {
            Contract.Requires(source != null);
            Contract.Requires(destination != null);

            if (destination.Length < count)
            {
                throw new ArgumentOutOfRangeException(
                    $"{nameof(destination)} : {destination.Length} < {nameof(count)} : {count}");
            }

            Unsafe.CopyBlock(
                Unsafe.AsPointer(ref destination[0]), 
                Unsafe.AsPointer(ref source[offset]), 
                (uint)(Unsafe.SizeOf<T>() * count));
        }

        public static void Reverse(this byte[] bytes)
        {
            Contract.Requires(bytes != null);

            int index1 = 0;
            for (int index2 = bytes.Length - 1; index1 < index2; --index2)
            {
                byte num = bytes[index1];
                bytes[index1] = bytes[index2];
                bytes[index2] = num;
                ++index1;
            }
        }
    }
}
