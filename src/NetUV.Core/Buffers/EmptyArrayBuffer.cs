// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;

    // Forked from https://github.com/Azure/DotNetty
    sealed class EmptyArrayBuffer<T> : IArrayBuffer<T>
    {
        internal static readonly T[] EmptyArray = new T[0];

        internal EmptyArrayBuffer(IArrayBufferAllocator<T> allocator)
        {
            Contract.Requires(allocator != null);

            this.Allocator = allocator;
        }

        public int Capacity => 0;

        public IArrayBuffer<T> AdjustCapacity(int newCapacity)
        {
            throw new NotSupportedException();
        }

        public int MaxCapacity => 0;

        public IArrayBufferAllocator<T> Allocator { get; }

        public int ReaderIndex => 0;

        public int WriterIndex => 0;

        public IArrayBuffer<T> SetWriterIndex(int writerIndex)
        {
            this.CheckIndex(writerIndex);
            return this;
        }

        public IArrayBuffer<T> SetReaderIndex(int readerIndex)
        {
            this.CheckIndex(readerIndex);
            return this;
        }

        public IArrayBuffer<T> SetIndex(int readerIndex, int writerIndex)
        {
            this.CheckIndex(readerIndex);
            this.CheckIndex(writerIndex);
            return this;
        }

        public int ReadableCount => 0;

        public int WritableCount => 0;

        public int MaxWritableCount => 0;

        public bool IsReadable() => false;

        public bool IsReadable(int size) => false;

        public bool IsWritable() => false;

        public bool IsWritable(int size) => false;

        public IArrayBuffer<T> Clear() => this;

        public IArrayBuffer<T> MarkReaderIndex() => this;

        public IArrayBuffer<T> ResetReaderIndex() => this;

        public IArrayBuffer<T> MarkWriterIndex() => this;

        public IArrayBuffer<T> ResetWriterIndex() => this;

        public IArrayBuffer<T> DiscardReadCount() => this;

        public IArrayBuffer<T> DiscardSomeReadCount() => this;

        public IArrayBuffer<T> EnsureWritable(int minWritableBytes)
        {
            Contract.Requires(minWritableBytes >= 0);

            if (minWritableBytes != 0)
            {
                throw new IndexOutOfRangeException();
            }
            return this;
        }

        public int EnsureWritable(int minWritableBytes, bool force)
        {
            Contract.Requires(minWritableBytes >= 0);

            return minWritableBytes == 0 ? 0 : 1;
        }

        public T Get(int index)
        {
            throw new IndexOutOfRangeException();
        }

        public IArrayBuffer<T> Get(int index, IArrayBuffer<T> destination)
        {
            this.CheckIndex(index, destination.WritableCount);
            return this;
        }

        public IArrayBuffer<T> Get(int index, IArrayBuffer<T> destination, int length)
        {
            this.CheckIndex(index, length);
            return this;
        }

        public IArrayBuffer<T> Get(int index, IArrayBuffer<T> destination, int dstIndex, int length)
        {
            this.CheckIndex(index, length);
            return this;
        }

        public IArrayBuffer<T> Get(int index, T[] destination)
        {
            this.CheckIndex(index, destination.Length);
            return this;
        }

        public IArrayBuffer<T> Get(int index, T[] destination, int dstIndex, int length)
        {
            this.CheckIndex(index, length);
            return this;
        }

        public IArrayBuffer<T> Set(int index, T value)
        {
            throw new IndexOutOfRangeException();
        }

        public IArrayBuffer<T> Set(int index, IArrayBuffer<T> src)
        {
            throw new IndexOutOfRangeException();
        }

        public IArrayBuffer<T> Set(int index, IArrayBuffer<T> src, int length)
        {
            this.CheckIndex(index, length);
            return this;
        }

        public IArrayBuffer<T> Set(int index, IArrayBuffer<T> src, int srcIndex, int length)
        {
            this.CheckIndex(index, length);
            return this;
        }

        public IArrayBuffer<T> Set(int index, T[] src)
        {
            this.CheckIndex(index, src.Length);
            return this;
        }

        public IArrayBuffer<T> Set(int index, T[] src, int srcIndex, int length)
        {
            this.CheckIndex(index, length);
            return this;
        }

        public IArrayBuffer<T> Read(int length) => this.CheckLength(length);

        public IArrayBuffer<T> Read(IArrayBuffer<T> destination) => this.CheckLength(destination.WritableCount);

        public IArrayBuffer<T> Read(IArrayBuffer<T> destination, int length) => this.CheckLength(length);

        public IArrayBuffer<T> Read(IArrayBuffer<T> destination, int dstIndex, int length) => this.CheckLength(length);

        public IArrayBuffer<T> Read(T[] destination) => this.CheckLength(destination.Length);

        public IArrayBuffer<T> Read(T[] destination, int length) => this.CheckLength(length);

        public IArrayBuffer<T> Read(T[] destination, int dstIndex, int length) => this.CheckLength(length);

        public IArrayBuffer<T> Skip(int length) => this.CheckLength(length);

        public IArrayBuffer<T> Write(IArrayBuffer<T> src)
        {
            throw new IndexOutOfRangeException();
        }

        public IArrayBuffer<T> Write(T src)
        {
            throw new IndexOutOfRangeException();
        }

        public IArrayBuffer<T> Write(IArrayBuffer<T> src, int length) => this.CheckLength(length);

        public IArrayBuffer<T> Write(IArrayBuffer<T> src, int srcIndex, int length) => this.CheckLength(length);

        public IArrayBuffer<T> Write(T[] src) => this.CheckLength(src.Length);

        public IArrayBuffer<T> Write(T[] src, int srcIndex, int length) => this.CheckLength(length);

        public bool HasArray => true;

        public T[] Array => EmptyArray;

        public T[] ToArray() => EmptyArray;

        public IArrayBuffer<T> Duplicate() => this;

        public IArrayBuffer<T> Copy() => this;

        public IArrayBuffer<T> Copy(int index, int length)
        {
            this.CheckIndex(index, length);
            return this;
        }

        public IArrayBuffer<T> Slice() => this;

        public IArrayBuffer<T> Slice(int index, int length)
        {
            this.CheckIndex(index, length);
            return this;
        }

        public int ArrayOffset => 0;

        public IArrayBuffer<T> ReadSlice(int length) => this.CheckLength(length);

        public IArrayBuffer<T> Unwrap() => null;

        public int ReferenceCount => 1;

        public IReferenceCounted Retain(int increment = 1) => this;

        public IReferenceCounted Touch(object hint = null) => this;

        public bool Release(int decrement = 1) => false;

        public override int GetHashCode() => 0;

        public override bool Equals(object obj)
        {
            var buffer = obj as IArrayBuffer<T>;
            return this.Equals(buffer);
        }

        public bool Equals(IArrayBuffer<T> buffer) => buffer != null && !buffer.IsReadable();

        public int CompareTo(IArrayBuffer<T> buffer) => buffer.IsReadable() ? -1 : 0;

        public void CheckIndex(int index)
        {
            if (index != 0)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }
        }

        public void CheckIndex(int index, int length)
        {
            if (length < 0)
            {
                throw new ArgumentException($"length:{length}");
            }
            if (index != 0 || length != 0)
            {
                throw new IndexOutOfRangeException($"{nameof(index)} : {index} {nameof(length)} {length}");
            }
        }

        IArrayBuffer<T> CheckLength(int length)
        {
            if (length < 0)
            {
                throw new ArgumentException($"length: {length} (expected: >= 0)");
            }
            if (length != 0)
            {
                throw new IndexOutOfRangeException($"length:{length}");
            }

            return this;
        }
    }
}
