// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System.Diagnostics.Contracts;

    class WrappedArrayBuffer<T> : IArrayBuffer<T>
    {
        protected readonly IArrayBuffer<T> Buf;

        protected WrappedArrayBuffer(IArrayBuffer<T> buf)
        {
            Contract.Requires(buf != null);

            this.Buf = buf;
        }

        public int Capacity => this.Buf.Capacity;

        public virtual IArrayBuffer<T> AdjustCapacity(int newCapacity)
        {
            this.Buf.AdjustCapacity(newCapacity);
            return this;
        }

        public int MaxCapacity => this.Buf.MaxCapacity;

        public IArrayBufferAllocator<T> Allocator => this.Buf.Allocator;

        public IArrayBuffer<T> Unwrap() => this.Buf;

        public int ReaderIndex => this.Buf.ReaderIndex;

        public IArrayBuffer<T> SetReaderIndex(int readerIndex)
        {
            this.Buf.SetReaderIndex(readerIndex);
            return this;
        }

        public int WriterIndex => this.Buf.WriterIndex;

        public IArrayBuffer<T> SetWriterIndex(int writerIndex)
        {
            this.Buf.SetWriterIndex(writerIndex);
            return this;
        }

        public virtual IArrayBuffer<T> SetIndex(int readerIndex, int writerIndex)
        {
            this.Buf.SetIndex(readerIndex, writerIndex);
            return this;
        }

        public int ReadableCount => this.Buf.ReadableCount;

        public int WritableCount => this.Buf.WritableCount;

        public int MaxWritableCount => this.Buf.MaxWritableCount;

        public bool IsReadable() => this.Buf.IsReadable();

        public bool IsWritable() => this.Buf.IsWritable();

        public IArrayBuffer<T> Clear()
        {
            this.Buf.Clear();
            return this;
        }

        public IArrayBuffer<T> MarkReaderIndex()
        {
            this.Buf.MarkReaderIndex();
            return this;
        }

        public IArrayBuffer<T> ResetReaderIndex()
        {
            this.Buf.ResetReaderIndex();
            return this;
        }

        public IArrayBuffer<T> MarkWriterIndex()
        {
            this.Buf.MarkWriterIndex();
            return this;
        }

        public IArrayBuffer<T> ResetWriterIndex()
        {
            this.Buf.ResetWriterIndex();
            return this;
        }

        public virtual IArrayBuffer<T> DiscardReadCount()
        {
            this.Buf.DiscardReadCount();
            return this;
        }

        public virtual IArrayBuffer<T> DiscardSomeReadCount()
        {
            this.Buf.DiscardSomeReadCount();
            return this;
        }

        public virtual IArrayBuffer<T> EnsureWritable(int minWritableBytes)
        {
            this.Buf.EnsureWritable(minWritableBytes);
            return this;
        }

        public virtual int EnsureWritable(int minWritableBytes, bool force) => this.Buf.EnsureWritable(minWritableBytes, force);


        public virtual T Get(int index) => this.Buf.Get(index);

        public virtual IArrayBuffer<T> Get(int index, IArrayBuffer<T> dst)
        {
            this.Buf.Get(index, dst);
            return this;
        }

        public virtual IArrayBuffer<T> Get(int index, IArrayBuffer<T> dst, int length)
        {
            this.Buf.Get(index, dst, length);
            return this;
        }

        public virtual IArrayBuffer<T> Get(int index, IArrayBuffer<T> dst, int dstIndex, int length)
        {
            this.Buf.Get(index, dst, dstIndex, length);
            return this;
        }

        public virtual IArrayBuffer<T> Get(int index, T[] dst)
        {
            this.Buf.Get(index, dst);
            return this;
        }

        public virtual IArrayBuffer<T> Get(int index, T[] dst, int dstIndex, int length)
        {
            this.Buf.Get(index, dst, dstIndex, length);
            return this;
        }

        public virtual IArrayBuffer<T> Set(int index, T value)
        {
            this.Buf.Set(index, value);
            return this;
        }


        public virtual IArrayBuffer<T> Set(int index, IArrayBuffer<T> src)
        {
            this.Buf.Set(index, src);
            return this;
        }

        public virtual IArrayBuffer<T> Set(int index, IArrayBuffer<T> src, int length)
        {
            this.Buf.Set(index, src, length);
            return this;
        }

        public virtual IArrayBuffer<T> Set(int index, IArrayBuffer<T> src, int srcIndex, int length)
        {
            this.Buf.Set(index, src, srcIndex, length);
            return this;
        }

        public virtual IArrayBuffer<T> Set(int index, T[] src)
        {
            this.Buf.Set(index, src);
            return this;
        }

        public virtual IArrayBuffer<T> Set(int index, T[] src, int srcIndex, int length)
        {
            this.Buf.Set(index, src, srcIndex, length);
            return this;
        }

        public virtual IArrayBuffer<T> Read(int length) => this.Buf.Read(length);

        public virtual IArrayBuffer<T> ReadSlice(int length) => this.Buf.ReadSlice(length);

        public virtual IArrayBuffer<T> Read(IArrayBuffer<T> dst)
        {
            this.Buf.Read(dst);
            return this;
        }

        public virtual IArrayBuffer<T> Read(IArrayBuffer<T> dst, int length)
        {
            this.Buf.Read(dst, length);
            return this;
        }

        public virtual IArrayBuffer<T> Read(IArrayBuffer<T> dst, int dstIndex, int length)
        {
            this.Buf.Read(dst, dstIndex, length);
            return this;
        }

        public virtual IArrayBuffer<T> Read(T[] dst)
        {
            this.Buf.Read(dst);
            return this;
        }

        public virtual IArrayBuffer<T> Read(T[] dst, int length)
        {
            this.Buf.Read(dst, length);
            return this;
        }

        public virtual IArrayBuffer<T> Read(T[] dst, int dstIndex, int length)
        {
            this.Buf.Read(dst, dstIndex, length);
            return this;
        }

        public virtual IArrayBuffer<T> Skip(int length)
        {
            this.Buf.Skip(length);
            return this;
        }

        public virtual IArrayBuffer<T> Write(T value)
        {
            this.Buf.Write(value);
            return this;
        }


        public virtual IArrayBuffer<T> Write(IArrayBuffer<T> src)
        {
            this.Buf.Write(src);
            return this;
        }

        public virtual IArrayBuffer<T> Write(IArrayBuffer<T> src, int length)
        {
            this.Buf.Write(src, length);
            return this;
        }

        public virtual IArrayBuffer<T> Write(IArrayBuffer<T> src, int srcIndex, int length)
        {
            this.Buf.Write(src, srcIndex, length);
            return this;
        }

        public virtual IArrayBuffer<T> Write(T[] src)
        {
            this.Buf.Write(src);
            return this;
        }

        public virtual IArrayBuffer<T> Write(T[] src, int srcIndex, int length)
        {
            this.Buf.Write(src, srcIndex, length);
            return this;
        }

        public virtual IArrayBuffer<T> Copy() => this.Buf.Copy();

        public virtual IArrayBuffer<T> Copy(int index, int length) => this.Buf.Copy(index, length);

        public virtual IArrayBuffer<T> Slice() => this.Buf.Slice();

        public virtual IArrayBuffer<T> Slice(int index, int length) => this.Buf.Slice(index, length);

        public virtual T[] ToArray() => this.Buf.ToArray();

        public virtual IArrayBuffer<T> Duplicate() => this.Buf.Duplicate();

        public virtual bool HasArray => this.Buf.HasArray;

        public virtual T[] Array => this.Buf.Array;

        public virtual int ArrayOffset => this.Buf.ArrayOffset;


        public override string ToString() => $"{this.GetType().Name} ({this.Buf})";

        public virtual IReferenceCounted Retain(int increment = 1)
        {
            this.Buf.Retain(increment);
            return this;
        }

        public virtual IReferenceCounted Touch(object hint = null)
        {
            this.Buf.Touch(hint);
            return this;
        }

        public bool IsReadable(int size) => this.Buf.IsReadable(size);

        public bool IsWritable(int size) => this.Buf.IsWritable(size);
        
        public int ReferenceCount => this.Buf.ReferenceCount;

        public virtual bool Release(int decrement = 1) => this.Buf.Release(decrement);

        public void CheckIndex(int index) => this.Buf.CheckIndex(index);

        public void CheckIndex(int index, int fieldLength) => this.Buf.CheckIndex(index, fieldLength);
    }
}
