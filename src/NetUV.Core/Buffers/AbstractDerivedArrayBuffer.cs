// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;

    abstract class AbstractDerivedByteBuffer : AbstractByteBuffer
    {
        protected AbstractDerivedByteBuffer(int maxCapacity)
            : base(maxCapacity)
        {
        }

        public sealed override int ReferenceCount => this.ReferenceCount0();

        protected virtual int ReferenceCount0() => this.Unwrap().ReferenceCount;

        public sealed override IReferenceCounted Retain(int increment = 1) => this.Retain0(increment);

        protected virtual IByteBuffer Retain0(int increment = 1)
        {
            this.Unwrap().Retain(increment);
            return this;
        }

        public sealed override IReferenceCounted Touch(object hint = null) => this.Touch0(hint);

        protected virtual IByteBuffer Touch0(object hint = null)
        {
            this.Unwrap().Touch(hint);
            return this;
        }

        public sealed override bool Release(int decrement = 1) => this.Release0(decrement);

        protected virtual bool Release0(int decrement = 1) => this.Unwrap().Release(decrement);

        public override ArraySegment<byte> GetIoBuffer(int index, int length) => this.Unwrap().GetIoBuffer(index, length);

        public override ArraySegment<byte>[] GetIoBuffers(int index, int length) => this.Unwrap().GetIoBuffers(index, length);
    }
}
