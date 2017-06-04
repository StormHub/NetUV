// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    // Forked from https://github.com/Azure/DotNetty
    abstract class AbstractDerivedArrayBuffer<T> : AbstractArrayBuffer<T>
    {
        protected AbstractDerivedArrayBuffer(int maxCapacity)
            : base(maxCapacity)
        { }

        public sealed override int ReferenceCount => this.Unwrap().ReferenceCount;

        public sealed override IReferenceCounted Retain(int increment = 1)
        {
            this.Unwrap().Retain(increment);
            return this;
        }

        public sealed override IReferenceCounted Touch(object hint = null)
        {
            this.Unwrap().Touch(hint);
            return this;
        }

        public sealed override bool Release(int decrement = 1) => this.Unwrap().Release(decrement);
    }
}
