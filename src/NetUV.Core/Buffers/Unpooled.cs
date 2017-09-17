// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Text;
    using NetUV.Core.Common;

    static class Unpooled
    {
        static readonly UnpooledByteBufferAllocator Allocator = UnpooledByteBufferAllocator.Default;

        public static readonly IByteBuffer Empty = Allocator.Buffer(0, 0);

        public static IByteBuffer Buffer() => Allocator.HeapBuffer();

        public static IByteBuffer Buffer(int initialCapacity) => Allocator.HeapBuffer(initialCapacity);

        public static IByteBuffer Buffer(int initialCapacity, int maxCapacity) => Allocator.HeapBuffer(initialCapacity, maxCapacity);

        public static IByteBuffer WrappedBuffer(byte[] array) => array.Length == 0 ? Empty : new UnpooledHeapByteBuffer(Allocator, array, array.Length);

        public static IByteBuffer WrappedBuffer(byte[] array, int offset, int length)
        {
            if (length == 0)
            {
                return Empty;
            }

            if (offset == 0 && length == array.Length)
            {
                return WrappedBuffer(array);
            }

            return WrappedBuffer(array).Slice(offset, length);
        }

        public static IByteBuffer WrappedBuffer(IByteBuffer buffer)
        {
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

        public static IByteBuffer WrappedBuffer(params byte[][] arrays) => WrappedBuffer(AbstractByteBufferAllocator.DefaultMaxComponents, arrays);

        public static IByteBuffer WrappedBuffer(params IByteBuffer[] buffers) => WrappedBuffer(AbstractByteBufferAllocator.DefaultMaxComponents, buffers);

        public static IByteBuffer WrappedBuffer(int maxNumComponents, params byte[][] arrays)
        {
            switch (arrays.Length)
            {
                case 0:
                    break;
                case 1:
                    if (arrays[0].Length != 0)
                    {
                        return WrappedBuffer(arrays[0]);
                    }
                    break;
                default:
                    // Get the list of the component, while guessing the byte order.
                    var components = new List<IByteBuffer>(arrays.Length);
                    foreach (byte[] array in arrays)
                    {
                        if (array == null)
                        {
                            break;
                        }
                        if (array.Length > 0)
                        {
                            components.Add(WrappedBuffer(array));
                        }
                    }

                    if (components.Count > 0)
                    {
                        return new CompositeByteBuffer(Allocator, maxNumComponents, components);
                    }
                    break;
            }

            return Empty;
        }

        public static IByteBuffer WrappedBuffer(int maxNumComponents, params IByteBuffer[] buffers)
        {
            switch (buffers.Length)
            {
                case 0:
                    break;
                case 1:
                    IByteBuffer buffer = buffers[0];
                    if (buffer.IsReadable())
                        return WrappedBuffer(buffer);
                    else
                        buffer.Release();
                    break;
                default:
                    for (int i = 0; i < buffers.Length; i++)
                    {
                        IByteBuffer buf = buffers[i];
                        if (buf.IsReadable())
                            return new CompositeByteBuffer(Allocator, maxNumComponents, buffers, i, buffers.Length);
                        else
                            buf.Release();
                    }
                    break;
            }

            return Empty;
        }

        public static CompositeByteBuffer CompositeBuffer() => CompositeBuffer(AbstractByteBufferAllocator.DefaultMaxComponents);

        public static CompositeByteBuffer CompositeBuffer(int maxNumComponents) => new CompositeByteBuffer(Allocator, maxNumComponents);

        public static IByteBuffer CopiedBuffer(byte[] array)
        {
            if (array.Length == 0)
            {
                return Empty;
            }

            var newArray = new byte[array.Length];
            PlatformDependent.CopyMemory(array, 0, newArray, 0, array.Length);

            return WrappedBuffer(newArray);
        }

        public static IByteBuffer CopiedBuffer(byte[] array, int offset, int length)
        {
            if (length == 0)
            {
                return Empty;
            }

            var copy = new byte[length];
            PlatformDependent.CopyMemory(array, offset, copy, 0, length);
            return WrappedBuffer(copy);
        }

        public static IByteBuffer CopiedBuffer(IByteBuffer buffer)
        {
            int readable = buffer.ReadableBytes;
            if (readable > 0)
            {
                IByteBuffer copy = Buffer(readable);
                copy.WriteBytes(buffer, buffer.ReaderIndex, readable);
                return copy;
            }
            else
            {
                return Empty;
            }
        }

        public static IByteBuffer CopiedBuffer(params byte[][] arrays)
        {
            switch (arrays.Length)
            {
                case 0:
                    return Empty;
                case 1:
                    return arrays[0].Length == 0 ? Empty : CopiedBuffer(arrays[0]);
            }

            // Merge the specified arrays into one array.
            int length = 0;
            foreach (byte[] a in arrays)
            {
                if (int.MaxValue - length < a.Length)
                {
                    throw new ArgumentException("The total length of the specified arrays is too big.");
                }
                length += a.Length;
            }

            if (length == 0)
            {
                return Empty;
            }

            var mergedArray = new byte[length];
            for (int i = 0, j = 0; i < arrays.Length; i++)
            {
                byte[] a = arrays[i];
                PlatformDependent.CopyMemory(a, 0, mergedArray, j, a.Length);
                j += a.Length;
            }

            return WrappedBuffer(mergedArray);
        }

        public static IByteBuffer CopiedBuffer(params IByteBuffer[] buffers)
        {
            switch (buffers.Length)
            {
                case 0:
                    return Empty;
                case 1:
                    return CopiedBuffer(buffers[0]);
            }

            // Merge the specified buffers into one buffer.
            int length = 0;
            foreach (IByteBuffer b in buffers)
            {
                int bLen = b.ReadableBytes;
                if (bLen <= 0)
                {
                    continue;
                }
                if (int.MaxValue - length < bLen)
                {
                    throw new ArgumentException("The total length of the specified buffers is too big.");
                }

                length += bLen;
            }

            if (length == 0)
            {
                return Empty;
            }

            var mergedArray = new byte[length];
            for (int i = 0, j = 0; i < buffers.Length; i++)
            {
                IByteBuffer b = buffers[i];
                int bLen = b.ReadableBytes;
                b.GetBytes(b.ReaderIndex, mergedArray, j, bLen);
                j += bLen;
            }

            return WrappedBuffer(mergedArray);
        }

        public static IByteBuffer CopiedBuffer(char[] array, int offset, int length, Encoding encoding)
        {
            Contract.Requires(array != null);
            return length == 0 ? Empty : CopiedBuffer(new string(array, offset, length), encoding);
        }

        static IByteBuffer CopiedBuffer(string value, Encoding encoding) => ByteBufferUtil.EncodeString0(Allocator, true, value, encoding, 0);

        public static IByteBuffer CopyInt(int value)
        {
            IByteBuffer buf = Buffer(4);
            buf.WriteInt(value);
            return buf;
        }

        public static IByteBuffer CopyInt(params int[] values)
        {
            if (values == null || values.Length == 0)
            {
                return Empty;
            }

            IByteBuffer buffer = Buffer(values.Length * 4);
            foreach (int v in values)
            {
                buffer.WriteInt(v);
            }

            return buffer;
        }

        public static IByteBuffer CopyShort(int value)
        {
            IByteBuffer buf = Buffer(2);
            buf.WriteShort(value);
            return buf;
        }

        public static IByteBuffer CopyShort(params short[] values)
        {
            if (values == null || values.Length == 0)
            {
                return Empty;
            }

            IByteBuffer buffer = Buffer(values.Length * 2);
            foreach (short v in values)
            {
                buffer.WriteShort(v);
            }

            return buffer;
        }

        public static IByteBuffer CopyShort(params int[] values)
        {
            if (values == null || values.Length == 0)
            {
                return Empty;
            }

            IByteBuffer buffer = Buffer(values.Length * 2);
            foreach (int v in values)
            {
                buffer.WriteShort(v);
            }
            return buffer;
        }

        public static IByteBuffer CopyMedium(int value)
        {
            IByteBuffer buf = Buffer(3);
            buf.WriteMedium(value);
            return buf;
        }

        public static IByteBuffer CopyMedium(params int[] values)
        {
            if (values == null || values.Length == 0)
            {
                return Empty;
            }

            IByteBuffer buffer = Buffer(values.Length * 3);
            foreach (int v in values)
            {
                buffer.WriteMedium(v);
            }

            return buffer;
        }

        public static IByteBuffer CopyLong(long value)
        {
            IByteBuffer buf = Buffer(8);
            buf.WriteLong(value);
            return buf;
        }

        public static IByteBuffer CopyLong(params long[] values)
        {
            if (values == null || values.Length == 0)
            {
                return Empty;
            }

            IByteBuffer buffer = Buffer(values.Length * 8);
            foreach (long v in values)
            {
                buffer.WriteLong(v);
            }

            return buffer;
        }

        public static IByteBuffer CopyBoolean(bool value)
        {
            IByteBuffer buf = Buffer(1);
            buf.WriteBoolean(value);
            return buf;
        }

        public static IByteBuffer CopyBoolean(params bool[] values)
        {
            if (values == null || values.Length == 0)
            {
                return Empty;
            }

            IByteBuffer buffer = Buffer(values.Length);
            foreach (bool v in values)
            {
                buffer.WriteBoolean(v);
            }

            return buffer;
        }

        public static IByteBuffer CopyFloat(float value)
        {
            IByteBuffer buf = Buffer(4);
            buf.WriteFloat(value);
            return buf;
        }

        public static IByteBuffer CopyFloat(params float[] values)
        {
            if (values == null || values.Length == 0)
            {
                return Empty;
            }

            IByteBuffer buffer = Buffer(values.Length * 4);
            foreach (float v in values)
            {
                buffer.WriteFloat(v);
            }

            return buffer;
        }

        public static IByteBuffer CopyDouble(double value)
        {
            IByteBuffer buf = Buffer(8);
            buf.WriteDouble(value);
            return buf;
        }

        public static IByteBuffer CopyDouble(params double[] values)
        {
            if (values == null || values.Length == 0)
            {
                return Empty;
            }

            IByteBuffer buffer = Buffer(values.Length * 8);
            foreach (double v in values)
            {
                buffer.WriteDouble(v);
            }

            return buffer;
        }

        public static IByteBuffer UnreleasableBuffer(IByteBuffer buffer) => new UnreleasableByteBuffer(buffer);
    }
}
