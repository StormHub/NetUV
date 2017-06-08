// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Common;

    static class Unpooled
    {
        internal static readonly UnpooledArrayBufferAllocator<byte> Allocator = 
            new UnpooledArrayBufferAllocator<byte>();

        public static readonly IArrayBuffer<byte> Empty = Allocator.Buffer(0, 0);

        public static IArrayBuffer<byte> Buffer() => Allocator.Buffer();

        public static IArrayBuffer<byte> Buffer(int initialCapacity)
        {
            Contract.Requires(initialCapacity >= 0);

            return Allocator.Buffer(initialCapacity);
        }

        public static IArrayBuffer<byte> Buffer(int initialCapacity, int maxCapacity)
        {
            Contract.Requires(initialCapacity >= 0 && initialCapacity <= maxCapacity);
            Contract.Requires(maxCapacity >= 0);

            return Allocator.Buffer(initialCapacity, maxCapacity);
        }

        public static IArrayBuffer<byte> WrappedBuffer(byte[] array)
        {
            Contract.Requires(array != null);

            return array.Length > 0 
                ? new UnpooledArrayBuffer<byte>(Allocator, array, array.Length)
                : Empty;
        }

        public static IArrayBuffer<byte> WrappedBuffer(IArrayBuffer<byte> buffer)
        {
            Contract.Requires(buffer != null);

            if (buffer.IsReadable())
            {
                return buffer.Slice();
            }
            else
            {
                buffer.Release();
                return Empty;
            }
        }

        public static IArrayBuffer<byte> WrappedBuffer(byte[] array, int offset, int length)
        {
            Contract.Requires(array != null);
            Contract.Requires(offset >= 0);
            Contract.Requires(length >= 0);

            if (length == 0)
            {
                return Empty;
            }

            if (offset == 0 
                && length == array.Length)
            {
                return WrappedBuffer(array);
            }

            return WrappedBuffer(array).Slice(offset, length);
        }

        public static IArrayBuffer<byte> CopiedBuffer(byte[] array)
        {
            Contract.Requires(array != null);

            if (array.Length == 0)
            {
                return Empty;
            }
            var newArray = new byte[array.Length];
            Array.Copy(array, newArray, array.Length);
            return WrappedBuffer(newArray);
        }

        public static IArrayBuffer<byte> CopiedBuffer(params byte[][] arrays)
        {
            Contract.Requires(arrays != null);

            if (arrays.Length == 0)
            {
                return Empty;
            }

            if (arrays.Length == 1)
            {
                return arrays[0].Length == 0 ? Empty : CopiedBuffer(arrays[0]);
            }

            byte[] mergedArray = arrays.CombineBytes();
            return WrappedBuffer(mergedArray);
        }

        public static IArrayBuffer<byte> CopiedBuffer(byte[] array, int offset, int length)
        {
            Contract.Requires(array != null);

            if (array.Length == 0)
            {
                return Empty;
            }

            var copy = new byte[length];
            Array.Copy(array, offset, copy, 0, length);
            return WrappedBuffer(copy);
        }

        public static IArrayBuffer<byte> CopiedBuffer(IArrayBuffer<byte> buffer)
        {
            Contract.Requires(buffer != null);

            int length = buffer.ReadableCount;
            if (length == 0)
            {
                return Empty;
            }
            var copy = new byte[length];

            // Duplicate the buffer so we do not adjust our position during our get operation
            IArrayBuffer<byte> duplicate = buffer.Duplicate();
            duplicate.Get(0, copy);
            return WrappedBuffer(copy);
        }

        public static IArrayBuffer<byte> CopiedBuffer(params IArrayBuffer<byte>[] buffers)
        {
            Contract.Requires(buffers != null);

            if (buffers.Length == 0)
            {
                return Empty;
            }

            if (buffers.Length == 1)
            {
                return CopiedBuffer(buffers[0]);
            }

            long newlength = 0;
            foreach (IArrayBuffer<byte> buffer in buffers)
            {
                newlength += buffer.ReadableCount;
            }

            var mergedArray = new byte[newlength];
            for (int i = 0, j = 0; i < buffers.Length; i++)
            {
                IArrayBuffer<byte> b = buffers[i];

                int bLen = b.ReadableCount;
                b.Get(b.ReaderIndex, mergedArray, j, bLen);
                j += bLen;
            }

            return WrappedBuffer(mergedArray);
        }
    }
}
