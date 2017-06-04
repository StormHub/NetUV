// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    interface IArrayBuffer<T> : IReferenceCounted
    {
        int Capacity { get; }

        IArrayBuffer<T> AdjustCapacity(int newCapacity);

        int MaxCapacity { get; }

        IArrayBufferAllocator<T> Allocator { get; }

        int ReaderIndex { get; }

        int WriterIndex { get; }

        IArrayBuffer<T> SetWriterIndex(int writerIndex);

        IArrayBuffer<T> SetReaderIndex(int readerIndex);

        IArrayBuffer<T> SetIndex(int readerIndex, int writerIndex);

        int ReadableCount { get; }

        int WritableCount { get; }

        int MaxWritableCount { get; }

        bool IsReadable(int size = 1);

        bool IsWritable(int size = 1);

        IArrayBuffer<T> Clear();

        IArrayBuffer<T> MarkReaderIndex();

        IArrayBuffer<T> ResetReaderIndex();

        IArrayBuffer<T> MarkWriterIndex();

        IArrayBuffer<T> ResetWriterIndex();

        IArrayBuffer<T> DiscardReadCount();

        IArrayBuffer<T> DiscardSomeReadCount();

        IArrayBuffer<T> EnsureWritable(int minWritableCount);

        int EnsureWritable(int minWritableCount, bool force);

        T Get(int index);

        IArrayBuffer<T> Get(int index, T[] destination);

        IArrayBuffer<T> Get(int index, IArrayBuffer<T> destination);

        IArrayBuffer<T> Get(int index, IArrayBuffer<T> destination, int length);

        IArrayBuffer<T> Get(int index, IArrayBuffer<T> destination, int dstIndex, int length);

        IArrayBuffer<T> Get(int index, T[] destination, int dstIndex, int length);

        IArrayBuffer<T> Set(int index, T value);

        IArrayBuffer<T> Set(int index, IArrayBuffer<T> src);

        IArrayBuffer<T> Set(int index, IArrayBuffer<T> src, int length);

        IArrayBuffer<T> Set(int index, IArrayBuffer<T> src, int srcIndex, int length);

        IArrayBuffer<T> Set(int index, T[] src);

        IArrayBuffer<T> Set(int index, T[] src, int srcIndex, int length);

        IArrayBuffer<T> Read(int length);

        IArrayBuffer<T> Read(IArrayBuffer<T> destination);

        IArrayBuffer<T> Read(IArrayBuffer<T> destination, int length);

        IArrayBuffer<T> Read(IArrayBuffer<T> destination, int dstIndex, int length);

        IArrayBuffer<T> Read(T[] destination);

        IArrayBuffer<T> Read(T[] destination, int length);

        IArrayBuffer<T> Read(T[] destination, int dstIndex, int length);

        IArrayBuffer<T> Skip(int length);

        IArrayBuffer<T> Write(T value);

        IArrayBuffer<T> Write(IArrayBuffer<T> src);

        IArrayBuffer<T> Write(IArrayBuffer<T> src, int length);

        IArrayBuffer<T> Write(IArrayBuffer<T> src, int srcIndex, int length);

        IArrayBuffer<T> Write(T[] src);

        IArrayBuffer<T> Write(T[] src, int srcIndex, int length);

        bool HasArray { get; }

        T[] Array { get; }

        T[] ToArray();

        IArrayBuffer<T> Duplicate();

        IArrayBuffer<T> Unwrap();

        IArrayBuffer<T> Copy();

        IArrayBuffer<T> Copy(int index, int length);

        IArrayBuffer<T> Slice();

        IArrayBuffer<T> Slice(int index, int length);

        int ArrayOffset { get; }

        IArrayBuffer<T> ReadSlice(int length);

        void CheckIndex(int index);

        void CheckIndex(int index, int fieldLength);
    }
}
