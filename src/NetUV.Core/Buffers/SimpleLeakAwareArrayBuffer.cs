// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using NetUV.Core.Common;

    // Forked from https://github.com/Azure/DotNetty
    sealed class SimpleLeakAwareArrayBuffer<T> : WrappedArrayBuffer<T>
    {
        readonly IResourceLeak leak;

        internal SimpleLeakAwareArrayBuffer(IArrayBuffer<T> buf, IResourceLeak leak)
            : base(buf)
        {
            this.leak = leak;
        }

        public override IReferenceCounted Touch(object hint = null) => this;

        public override bool Release(int decrement = 1)
        {
            bool deallocated = base.Release(decrement);
            if (deallocated)
            {
                this.leak.Close();
            }
            return deallocated;
        }

        public override IArrayBuffer<T> Slice() => 
            new SimpleLeakAwareArrayBuffer<T>(base.Slice(), this.leak);

        public override IArrayBuffer<T> Slice(int index, int length) => 
            new SimpleLeakAwareArrayBuffer<T>(base.Slice(index, length), this.leak);

        public override IArrayBuffer<T> Duplicate() => 
            new SimpleLeakAwareArrayBuffer<T>(base.Duplicate(), this.leak);

        public override IArrayBuffer<T> ReadSlice(int length) => 
            new SimpleLeakAwareArrayBuffer<T>(base.ReadSlice(length), this.leak);
    }
}
