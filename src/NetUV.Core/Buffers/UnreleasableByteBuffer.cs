// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    sealed class UnreleasableByteBuffer : WrappedByteBuffer
    {
        internal UnreleasableByteBuffer(IByteBuffer buf) : base(buf)
        {
        }

        public override IByteBuffer ReadSlice(int length) => new UnreleasableByteBuffer(this.Buf.ReadSlice(length));

        public override IByteBuffer ReadRetainedSlice(int length) => this.ReadSlice(length);

        public override IByteBuffer Slice() => new UnreleasableByteBuffer(this.Buf.Slice());

        public override IByteBuffer RetainedSlice() => this.Slice();

        public override IByteBuffer Slice(int index, int length) => new UnreleasableByteBuffer(this.Buf.Slice(index, length));

        public override IByteBuffer RetainedSlice(int index, int length) => this.Slice(index, length);

        public override IByteBuffer Duplicate() => new UnreleasableByteBuffer(this.Buf.Duplicate());

        public override IByteBuffer RetainedDuplicate() => this.Duplicate();

        public override IReferenceCounted Retain(int increment = 1) => this;

        public override IReferenceCounted Touch(object hint = null) => this;

        public override bool Release(int decrement = 1) => false;
    }
}
