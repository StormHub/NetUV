// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using NetUV.Core.Logging;
    using NetUV.Core.Common;

    // Forked from https://github.com/Azure/DotNetty
    sealed class AdvancedLeakAwareArrayBuffer<T> : WrappedArrayBuffer<T>
    {
        readonly IResourceLeak leak;

        internal AdvancedLeakAwareArrayBuffer(IArrayBuffer<T> buf, IResourceLeak leak) 
            : base(buf)
        {
            this.leak = leak;
        }

        void RecordLeakNonRefCountingOperation()
        {
            if (!LeakDetectionOption.AcquireAndReleaseOnly)
            {
                this.leak.Record();
            }
        }

        public override IArrayBuffer<T> Slice()
        {
            this.RecordLeakNonRefCountingOperation();
            return new AdvancedLeakAwareArrayBuffer<T>(base.Slice(), this.leak);
        }

        public override IArrayBuffer<T> Slice(int index, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return new AdvancedLeakAwareArrayBuffer<T>(base.Slice(index, length), this.leak);
        }

        public override IArrayBuffer<T> Duplicate()
        {
            this.RecordLeakNonRefCountingOperation();
            return new AdvancedLeakAwareArrayBuffer<T>(base.Duplicate(), this.leak);
        }

        public override IArrayBuffer<T> ReadSlice(int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return new AdvancedLeakAwareArrayBuffer<T>(base.ReadSlice(length), this.leak);
        }

        public override IArrayBuffer<T> DiscardReadCount()
        {
            this.RecordLeakNonRefCountingOperation();
            return base.DiscardReadCount();
        }

        public override IArrayBuffer<T> DiscardSomeReadCount()
        {
            this.RecordLeakNonRefCountingOperation();
            return base.DiscardSomeReadCount();
        }

        public override IArrayBuffer<T> EnsureWritable(int minWritableBytes)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.EnsureWritable(minWritableBytes);
        }

        public override int EnsureWritable(int minWritableBytes, bool force)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.EnsureWritable(minWritableBytes, force);
        }

        public override IArrayBuffer<T> Get(int index, IArrayBuffer<T> dst)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Get(index, dst);
        }

        public override IArrayBuffer<T> Get(int index, IArrayBuffer<T> dst, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Get(index, dst, length);
        }

        public override IArrayBuffer<T> Get(int index, IArrayBuffer<T> dst, int dstIndex, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Get(index, dst, dstIndex, length);
        }

        public override IArrayBuffer<T> Get(int index, T[] dst)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Get(index, dst);
        }

        public override IArrayBuffer<T> Get(int index, T[] dst, int dstIndex, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Get(index, dst, dstIndex, length);
        }

        public override IArrayBuffer<T> Set(int index, IArrayBuffer<T> src)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Set(index, src);
        }

        public override IArrayBuffer<T> Set(int index, IArrayBuffer<T> src, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Set(index, src, length);
        }

        public override IArrayBuffer<T> Set(int index, IArrayBuffer<T> src, int srcIndex, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Set(index, src, srcIndex, length);
        }

        public override IArrayBuffer<T> Set(int index, T[] src)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Set(index, src);
        }

        public override IArrayBuffer<T> Set(int index, T[] src, int srcIndex, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Set(index, src, srcIndex, length);
        }

        public override IArrayBuffer<T> Read(int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Read(length);
        }

        public override IArrayBuffer<T> Read(IArrayBuffer<T> dst)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Read(dst);
        }

        public override IArrayBuffer<T> Read(IArrayBuffer<T> dst, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Read(dst, length);
        }

        public override IArrayBuffer<T> Read(IArrayBuffer<T> dst, int dstIndex, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Read(dst, dstIndex, length);
        }

        public override IArrayBuffer<T> Read(T[] dst)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Read(dst);
        }

        public override IArrayBuffer<T> Read(T[] dst, int dstIndex, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Read(dst, dstIndex, length);
        }

        public override IArrayBuffer<T> Skip(int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Skip(length);
        }

        public override IArrayBuffer<T> Write(IArrayBuffer<T> src)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Write(src);
        }

        public override IArrayBuffer<T> Write(IArrayBuffer<T> src, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Write(src, length);
        }

        public override IArrayBuffer<T> Write(IArrayBuffer<T> src, int srcIndex, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Write(src, srcIndex, length);
        }

        public override IArrayBuffer<T> Write(T[] src)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Write(src);
        }

        public override IArrayBuffer<T> Write(T[] src, int srcIndex, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Write(src, srcIndex, length);
        }

        public override IArrayBuffer<T> Copy()
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Copy();
        }

        public override IArrayBuffer<T> Copy(int index, int length)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.Copy(index, length);
        }

        public override IArrayBuffer<T> AdjustCapacity(int newCapacity)
        {
            this.RecordLeakNonRefCountingOperation();
            return base.AdjustCapacity(newCapacity);
        }

        public override IReferenceCounted Retain(int increment = 1)
        {
            this.leak.Record();
            return base.Retain(increment);
        }

        public override IReferenceCounted Touch(object hint = null)
        {
            this.leak.Record(hint);
            return this;
        }

        public override bool Release(int decrement = 1)
        {
            bool deallocated = base.Release(decrement);
            if (deallocated)
            {
                this.leak.Close();
            }
            else
            {
                this.leak.Record();
            }

            return deallocated;
        }
    }

    static class LeakDetectionOption
    {
        internal static readonly bool AcquireAndReleaseOnly;

        static readonly ILog Log = LogFactory.ForContext(nameof(LeakDetectionOption));

        const string PropAcquireAndReleaseOnly = "leakDetection.acquireAndReleaseOnly";

        static LeakDetectionOption()
        {
            AcquireAndReleaseOnly = Configuration.TryGetValue(PropAcquireAndReleaseOnly, false);

            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0}: {1}", PropAcquireAndReleaseOnly, AcquireAndReleaseOnly);
            }
        }
    }
}
