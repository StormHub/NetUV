// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Common;

    // Forked from https://github.com/Azure/DotNetty
    abstract class AbstractArrayBuffer<T> : IArrayBuffer<T>
    {
        static readonly T[] EmptyArray = new T[0];

        internal static readonly ResourceLeakDetector LeakDetector = 
            ResourceLeakDetector.Create<AbstractArrayBuffer<T>>();

        int markedReaderIndex;
        int markedWriterIndex;

        protected AbstractArrayBuffer(int maxCapacity)
        {
            this.MaxCapacity = maxCapacity;
        }

        public abstract int Capacity { get; }

        public abstract IArrayBuffer<T> AdjustCapacity(int newCapacity);

        public int MaxCapacity { get; protected set; }

        public abstract IArrayBufferAllocator<T> Allocator { get; }

        public virtual int ReaderIndex { get; protected set; }

        public virtual int WriterIndex { get; protected set; }

        public IArrayBuffer<T> SetWriterIndex(int writerIndex)
        {
            if (writerIndex < this.ReaderIndex 
                || writerIndex > this.Capacity)
            {
                throw new IndexOutOfRangeException(
                    $"WriterIndex: {writerIndex} (expected: 0 <= readerIndex({this.ReaderIndex}) <= writerIndex <= capacity ({this.Capacity})");
            }

            this.WriterIndex = writerIndex;
            return this;
        }

        public IArrayBuffer<T> SetReaderIndex(int readerIndex)
        {
            if (readerIndex < 0 
                || readerIndex > this.WriterIndex)
            {
                throw new IndexOutOfRangeException(
                    $"ReaderIndex: {readerIndex} (expected: 0 <= readerIndex <= writerIndex({this.WriterIndex})");
            }

            this.ReaderIndex = readerIndex;
            return this;
        }

        public IArrayBuffer<T> SetIndex(int readerIndex, int writerIndex)
        {
            if (readerIndex < 0 
                || readerIndex > writerIndex 
                || writerIndex > this.Capacity)
            {
                throw new IndexOutOfRangeException(
                    $"ReaderIndex: {readerIndex}, WriterIndex: {writerIndex} (expected: 0 <= readerIndex <= writerIndex <= capacity ({this.Capacity})");
            }

            this.ReaderIndex = readerIndex;
            this.WriterIndex = writerIndex;
            return this;
        }

        public virtual int ReadableCount => this.WriterIndex - this.ReaderIndex;

        public virtual int WritableCount => this.Capacity - this.WriterIndex;

        public virtual int MaxWritableCount => this.MaxCapacity - this.WriterIndex;

        public bool IsReadable(int size = 1) => this.ReadableCount >= size;

        public bool IsWritable(int size = 1) => this.WritableCount >= size;

        public IArrayBuffer<T> Clear()
        {
            this.ReaderIndex = this.WriterIndex = 0;
            return this;
        }

        public IArrayBuffer<T> MarkReaderIndex()
        {
            this.markedReaderIndex = this.ReaderIndex;
            return this;
        }

        public IArrayBuffer<T> ResetReaderIndex()
        {
            this.SetReaderIndex(this.markedReaderIndex);
            return this;
        }

        public IArrayBuffer<T> MarkWriterIndex()
        {
            this.markedWriterIndex = this.WriterIndex;
            return this;
        }

        public IArrayBuffer<T> ResetWriterIndex()
        {
            this.SetWriterIndex(this.markedWriterIndex);
            return this;
        }

        public virtual IArrayBuffer<T> DiscardReadCount()
        {
            this.EnsureAccessible();
            if (this.ReaderIndex == 0)
            {
                return this;
            }

            if (this.ReaderIndex != this.WriterIndex)
            {
                this.Set(0, this, this.ReaderIndex, this.WriterIndex - this.ReaderIndex);
                this.WriterIndex -= this.ReaderIndex;
                this.AdjustMarkers(this.ReaderIndex);
                this.ReaderIndex = 0;
            }
            else
            {
                this.AdjustMarkers(this.ReaderIndex);
                this.WriterIndex = this.ReaderIndex = 0;
            }

            return this;
        }

        public virtual IArrayBuffer<T> DiscardSomeReadCount()
        {
            this.EnsureAccessible();
            if (this.ReaderIndex == 0)
            {
                return this;
            }

            if (this.ReaderIndex == this.WriterIndex)
            {
                this.AdjustMarkers(this.ReaderIndex);
                this.WriterIndex = this.ReaderIndex = 0;
                return this;
            }

            if (this.ReaderIndex >= this.Capacity.RightUShift(1))
            {
                this.Set(0, this, this.ReaderIndex, this.WriterIndex - this.ReaderIndex);
                this.WriterIndex -= this.ReaderIndex;
                this.AdjustMarkers(this.ReaderIndex);
                this.ReaderIndex = 0;
            }

            return this;
        }

        public IArrayBuffer<T> EnsureWritable(int minWritableCount)
        {
            if (minWritableCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minWritableCount),
                    "expected minWritableBytes to be greater than zero");
            }

            if (minWritableCount <= this.WritableCount)
            {
                return this;
            }

            if (minWritableCount > this.MaxCapacity - this.WriterIndex)
            {
                throw new IndexOutOfRangeException(
                    $"writerIndex({this.WriterIndex}) + minWritableBytes({minWritableCount}) exceeds maxCapacity({this.MaxCapacity}): {this}");
            }

            //Normalize the current capacity to the power of 2
            int newCapacity = this.CalculateNewCapacity(this.WriterIndex + minWritableCount);

            //Adjust to the new capacity
            this.AdjustCapacity(newCapacity);
            return this;
        }

        public int EnsureWritable(int minWritableBytes, bool force)
        {
            Contract.Ensures(minWritableBytes >= 0);

            if (minWritableBytes <= this.WritableCount)
            {
                return 0;
            }

            if (minWritableBytes > this.MaxCapacity - this.WriterIndex)
            {
                if (force)
                {
                    if (this.Capacity == this.MaxCapacity)
                    {
                        return 1;
                    }

                    this.AdjustCapacity(this.MaxCapacity);
                    return 3;
                }
            }

            // Normalize the current capacity to the power of 2.
            int newCapacity = this.CalculateNewCapacity(this.WriterIndex + minWritableBytes);

            // Adjust to the new capacity.
            this.AdjustCapacity(newCapacity);
            return 2;
        }

        int CalculateNewCapacity(int minNewCapacity)
        {
            int maxCapacity = this.MaxCapacity;
            const int Threshold = 4 * 1024 * 1024; // 4 MiB page
            int newCapacity;
            if (minNewCapacity == Threshold)
            {
                return Threshold;
            }

            // If over threshold, do not double but just increase by threshold.
            if (minNewCapacity > Threshold)
            {
                newCapacity = minNewCapacity - (minNewCapacity % Threshold);
                return Math.Min(maxCapacity, newCapacity + Threshold);
            }

            // Not over threshold. Double up to 4 MiB, starting from 64.
            newCapacity = 64;
            while (newCapacity < minNewCapacity)
            {
                newCapacity <<= 1;
            }

            return Math.Min(newCapacity, maxCapacity);
        }

        public abstract T Get(int index);

        public virtual IArrayBuffer<T> Get(int index, IArrayBuffer<T> destination)
        {
            this.Get(index, destination, destination.WritableCount);
            return this;
        }

        public virtual IArrayBuffer<T> Get(int index, IArrayBuffer<T> destination, int length)
        {
            this.Get(index, destination, destination.WriterIndex, length);
            destination.SetWriterIndex(destination.WriterIndex + length);
            return this;
        }

        public abstract IArrayBuffer<T> Get(int index, IArrayBuffer<T> destination, int dstIndex, int length);

        public IArrayBuffer<T> Get(int index, T[] destination)
        {
            this.Get(index, destination, 0, destination.Length);
            return this;
        }

        public abstract IArrayBuffer<T> Get(int index, T[] destination, int dstIndex, int length);

        public abstract IArrayBuffer<T> Set(int index, T value);

        public IArrayBuffer<T> Set(int index, IArrayBuffer<T> src)
        {
            this.Set(index, src, src.ReadableCount);
            return this;
        }

        public IArrayBuffer<T> Set(int index, IArrayBuffer<T> src, int length)
        {
            Contract.Requires(src != null);

            this.CheckIndex(index, length);
            if (length > src.ReadableCount)
            {
                throw new IndexOutOfRangeException(
                    $"length({length}) exceeds src.readableBytes({src.ReadableCount}) where src is: {src}");
            }
            this.Set(index, src, src.ReaderIndex, length);
            src.SetReaderIndex(src.ReaderIndex + length);
            return this;
        }

        public abstract IArrayBuffer<T> Set(int index, IArrayBuffer<T> src, int srcIndex, int length);

        public IArrayBuffer<T> Set(int index, T[] src)
        {
            this.Set(index, src, 0, src.Length);
            return this;
        }

        public abstract IArrayBuffer<T> Set(int index, T[] src, int srcIndex, int length);

        public IArrayBuffer<T> Read(int length)
        {
            this.CheckReadableCount(length);
            if (length == 0)
            {
                return this.Allocator.EmptyBuffer;
            }

            IArrayBuffer<T> buf = this.Allocator.Buffer(length, this.MaxCapacity);
            buf.Write(this, this.ReaderIndex, length);
            this.ReaderIndex += length;
            return buf;
        }

        public IArrayBuffer<T> Read(IArrayBuffer<T> destination)
        {
            this.Read(destination, destination.WritableCount);
            return this;
        }

        public IArrayBuffer<T> Read(IArrayBuffer<T> destination, int length)
        {
            if (length > destination.WritableCount)
            {
                throw new IndexOutOfRangeException(
                    $"length({length}) exceeds destination.WritableBytes({destination.WritableCount}) where destination is: {destination}");
            }

            this.Read(destination, destination.WriterIndex, length);
            destination.SetWriterIndex(destination.WriterIndex + length);
            return this;
        }

        public IArrayBuffer<T> Read(IArrayBuffer<T> destination, int dstIndex, int length)
        {
            this.CheckReadableCount(length);
            this.Get(this.ReaderIndex, destination, dstIndex, length);
            this.ReaderIndex += length;
            return this;
        }

        public IArrayBuffer<T> Read(T[] destination)
        {
            this.Read(destination, 0, destination.Length);
            return this;
        }

        public IArrayBuffer<T> Read(T[] destination, int length)
        {
            this.Read(destination, 0, destination.Length);
            return this;
        }

        public IArrayBuffer<T> Read(T[] destination, int dstIndex, int length)
        {
            this.CheckReadableCount(length);
            this.Get(this.ReaderIndex, destination, dstIndex, length);
            this.ReaderIndex += length;
            return this;
        }

        public IArrayBuffer<T> Skip(int length)
        {
            this.CheckReadableCount(length);
            this.ReaderIndex += length;
            return this;
        }

        public IArrayBuffer<T> Write(T value)
        {
            this.EnsureAccessible();
            this.EnsureWritable(1);
            this.Set(this.WriterIndex, value);
            this.WriterIndex += 1;
            return this;
        }

        public IArrayBuffer<T> Write(IArrayBuffer<T> src)
        {
            this.Write(src, src.ReadableCount);
            return this;
        }

        public IArrayBuffer<T> Write(IArrayBuffer<T> src, int length)
        {
            if (length > src.ReadableCount)
            {
                throw new IndexOutOfRangeException(
                    $"length({length}) exceeds src.readableBytes({src.ReadableCount}) where src is: {src}");
            }

            this.Write(src, src.ReaderIndex, length);
            src.SetReaderIndex(src.ReaderIndex + length);
            return this;
        }

        public IArrayBuffer<T> Write(IArrayBuffer<T> src, int srcIndex, int length)
        {
            this.EnsureAccessible();
            this.EnsureWritable(length);
            this.Set(this.WriterIndex, src, srcIndex, length);
            this.WriterIndex += length;

            return this;
        }

        public IArrayBuffer<T> Write(T[] src)
        {
            this.Write(src, 0, src.Length);

            return this;
        }

        public IArrayBuffer<T> Write(T[] src, int srcIndex, int length)
        {
            this.EnsureAccessible();
            this.EnsureWritable(length);
            this.Set(this.WriterIndex, src, srcIndex, length);
            this.WriterIndex += length;

            return this;
        }

        public abstract bool HasArray { get; }

        public abstract T[] Array { get; }

        public abstract int ArrayOffset { get; }

        public T[] ToArray()
        {
            int readableBytes = this.ReadableCount;
            if (readableBytes == 0)
            {
                return EmptyArray;
            }

            if (this.HasArray)
            {
                return this.Array.Slice(this.ArrayOffset + this.ReaderIndex, readableBytes);
            }

            var bytes = new T[readableBytes];
            this.Get(this.ReaderIndex, bytes);

            return bytes;
        }

        public virtual IArrayBuffer<T> Duplicate() => new DuplicatedArrayBuffer<T>(this);

        public abstract IArrayBuffer<T> Unwrap();

        protected void AdjustMarkers(int decrement)
        {
            int markedReaderIdx = this.markedReaderIndex;
            if (markedReaderIdx <= decrement)
            {
                this.markedReaderIndex = 0;
                int markedWriterIdx = this.markedWriterIndex;
                if (markedWriterIdx <= decrement)
                {
                    this.markedWriterIndex = 0;
                }
                else
                {
                    this.markedWriterIndex = markedWriterIdx - decrement;
                }
            }
            else
            {
                this.markedReaderIndex = markedReaderIdx - decrement;
                this.markedWriterIndex -= decrement;
            }
        }

        public void CheckIndex(int index)
        {
            this.EnsureAccessible();
            if (index < 0 
                || index >= this.Capacity)
            {
                throw new IndexOutOfRangeException(
                    $"index: {index} (expected: range(0, {this.Capacity})");
            }
        }

        public void CheckIndex(int index, int fieldLength)
        {
            this.EnsureAccessible();
            if (fieldLength < 0)
            {
                throw new IndexOutOfRangeException(
                    $"length: {fieldLength} (expected: >= 0)");
            }

            if (index < 0 
                || index > this.Capacity - fieldLength)
            {
                throw new IndexOutOfRangeException(
                    $"index: {index}, length: {fieldLength} (expected: range(0, {this.Capacity})");
            }
        }

        protected void CheckSrcIndex(int index, int length, int srcIndex, int srcCapacity)
        {
            this.CheckIndex(index, length);
            if (srcIndex < 0 
                || srcIndex > srcCapacity - length)
            {
                throw new IndexOutOfRangeException(
                    $"srcIndex: {srcIndex}, length: {length} (expected: range(0, {srcCapacity}))");
            }
        }

        protected void CheckDstIndex(int index, int length, int dstIndex, int dstCapacity)
        {
            this.CheckIndex(index, length);
            if (dstIndex < 0 
                || dstIndex > dstCapacity - length)
            {
                throw new IndexOutOfRangeException(
                    $"dstIndex: {dstIndex}, length: {length} (expected: range(0, {dstCapacity}))");
            }
        }

        protected void CheckReadableCount(int minimumReadableBytes)
        {
            this.EnsureAccessible();
            if (minimumReadableBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumReadableBytes), 
                    $"minimumReadableBytes: {minimumReadableBytes} (expected: >= 0)");
            }

            if (this.ReaderIndex > this.WriterIndex - minimumReadableBytes)
            {
                throw new IndexOutOfRangeException(
                    $"readerIndex({this.ReaderIndex}) + length({minimumReadableBytes}) exceeds writerIndex({this.WriterIndex}): {this}");
            }
        }

        protected void CheckNewCapacity(int newCapacity)
        {
            this.EnsureAccessible();
            if (newCapacity < 0 
                || newCapacity > this.MaxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(newCapacity), 
                    $"newCapacity: {newCapacity} (expected: 0-{this.MaxCapacity})");
            }
        }

        protected void EnsureAccessible()
        {
            if (this.ReferenceCount == 0)
            {
                throw new InvalidOperationException(
                    $"{this.GetType().Name} has reference count of zero and should be de allocated.");
            }
        }

        public IArrayBuffer<T> Copy() => this.Copy(this.ReaderIndex, this.ReadableCount);

        public abstract IArrayBuffer<T> Copy(int index, int length);

        public IArrayBuffer<T> Slice() => this.Slice(this.ReaderIndex, this.ReadableCount);

        public virtual IArrayBuffer<T> Slice(int index, int length) => new SlicedArrayBuffer<T>(this, index, length);

        public IArrayBuffer<T> ReadSlice(int length)
        {
            IArrayBuffer<T> slice = this.Slice(this.ReaderIndex, length);
            this.ReaderIndex += length;
            return slice;
        }

        public abstract int ReferenceCount { get; }

        public abstract IReferenceCounted Retain(int increment = 1);

        public abstract IReferenceCounted Touch(object hint = null);

        public abstract bool Release(int decrement = 1);

        protected void DiscardMarkers()
        {
            this.markedReaderIndex = this.markedWriterIndex = 0;
        }
    }
}
