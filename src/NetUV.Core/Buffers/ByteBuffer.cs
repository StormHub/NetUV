// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;
    using System.Text;
    using NetUV.Core.Common;

    sealed class ByteBuffer : IDisposable
    {
        internal ByteBuffer(IArrayBuffer<byte> buffer)
        {
            Contract.Requires(buffer != null);

            this.ArrayBuffer = buffer;
        }

        internal IArrayBuffer<byte> ArrayBuffer { get; private set; }

        internal ByteBuffer Retain()
        {
            if (this.ArrayBuffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(ByteBuffer)} has already been disposed.");
            }

            this.ArrayBuffer?.Retain();
            return this;
        } 

        internal int Count => this.ArrayBuffer?.Count ?? 0;

        internal ReadableBuffer ToReadableBuffer(int count) => 
            new ReadableBuffer(this, count);

        internal byte ReadByte(int index)
        {
            this.Validate(index);

            int idx = this.IndexOf(index);
            return this.ArrayBuffer.Array[idx];
        }

        internal void ReadBytes(byte[] destination, int index, int length)
        {
            Contract.Requires(destination != null);
            Contract.Requires(destination.Length >= length);

            this.Validate(index, length);
            this.ArrayBuffer.Array.CopyBlock(destination, this.IndexOf(index), length);
        }

        internal unsafe string ReadString(int index, int length, Encoding encoding)
        {
            Contract.Requires(encoding != null);

            this.Validate(index, length);
            void* source = Unsafe.AsPointer(ref this.ArrayBuffer.Array[this.IndexOf(index)]);

            return encoding.GetString((byte*)source, length);
        }

        internal void WriteByte(int index, byte value)
        {
            this.Validate(index);
            this.ArrayBuffer.Array[this.IndexOf(index)] = value;
        }

        internal unsafe void WriteBytes(byte[] source, int index)
        {
            Contract.Requires(source != null);
            Contract.Requires(source.Length >= 0);

            this.Validate(index, source.Length);
            void* destination = Unsafe.AsPointer(ref this.ArrayBuffer.Array[this.IndexOf(index)]);
            Unsafe.Write(destination, source);
        }

        internal void WriteBytes(byte[] source, int index, int length)
        {
            Contract.Requires(source != null);
            Contract.Requires(source.Length >= length);

            this.WriteBytes(source, 0, index, length);
        }

        internal unsafe void WriteBytes(byte[] source, int offset, int index, int length)
        {
            Contract.Requires(source != null);
            Contract.Requires(offset >= 0 && offset <= source.Length - length);

            this.Validate(index, length);

            void* from = Unsafe.AsPointer(ref source[offset]);
            void* destination = Unsafe.AsPointer(ref this.ArrayBuffer.Array[this.IndexOf(index)]);
            Unsafe.CopyBlock(destination, from, (uint)length);
        }

        int IndexOf(int index) => this.ArrayBuffer.Offset + index;

        internal void Validate(int index)
        {
            if (this.ArrayBuffer?.Array == null
                || this.ArrayBuffer.Count == 0)
            {
                throw new ObjectDisposedException($"{nameof(ByteBuffer)} has already been disposed.");
            }

            if (index < 0
                || index >= this.ArrayBuffer.Count - this.ArrayBuffer.Offset)
            {
                throw new IndexOutOfRangeException(
                    $"{nameof(index)}: {index} expected range >= 0 and < {this.ArrayBuffer.Count - this.ArrayBuffer.Offset}.");
            }
        }

        internal void Validate(int index, int length)
        {
            if (this.ArrayBuffer?.Array == null)
            {
                throw new ObjectDisposedException($"{nameof(ByteBuffer)} has already been disposed.");
            }
            if (this.ArrayBuffer.Array.Length == 0)
            {
                throw new InvalidOperationException($"{nameof(ByteBuffer)} is empty.");
            }

            if (length <= 0 
                || length > this.ArrayBuffer.Count)
            {
                throw new IndexOutOfRangeException(
                    $"{nameof(length)}:{length} must be between zero and {this.ArrayBuffer.Count}");
            }

            if (index < 0
                || index >= this.ArrayBuffer.Count - length)
            {
                throw new IndexOutOfRangeException(
                    $"{nameof(index)}:{index}, {nameof(length)}:{length} expected range >= 0 and < {this.ArrayBuffer.Count - length}.");
            }
        }

        public void Dispose()
        {
            this.ArrayBuffer?.Release();
            this.ArrayBuffer = null;
        }
    }
}
