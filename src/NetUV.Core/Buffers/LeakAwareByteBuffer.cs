// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using NetUV.Core.Common;

    // Forked from https://github.com/Azure/DotNetty
    sealed class LeakAwareByteBuffer<T> : WrappedArrayBuffer<T>
    {
        readonly IResourceLeak leak;

        internal LeakAwareByteBuffer(IArrayBuffer<T> buffer, IResourceLeak leak) 
            : base(buffer)
        {
            this.leak = leak;
            this.leak.Record();
        }

        public override IReferenceCounted Touch(object hint = null) => this;

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

        public override IArrayBuffer<T> Copy() => 
            new LeakAwareByteBuffer<T>(base.Copy(), this.leak);

        public override IArrayBuffer<T> Copy(int index, int length) => 
            new LeakAwareByteBuffer<T>(base.Copy(index, length), this.leak);
    }
}
