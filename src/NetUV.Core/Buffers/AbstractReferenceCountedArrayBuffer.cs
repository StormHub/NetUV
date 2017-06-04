// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Threading;

    // Forked from https://github.com/Azure/DotNetty
    abstract class AbstractReferenceCountedArrayBuffer<T> : AbstractArrayBuffer<T>
    {
        int referenceCount = 1;

        protected AbstractReferenceCountedArrayBuffer(int maxCapacity)
            : base(maxCapacity)
        { }

        public override int ReferenceCount => 
            Volatile.Read(ref this.referenceCount);

        protected void SetReferenceCount(int value) => 
            Volatile.Write(ref this.referenceCount, value);

        public override IReferenceCounted Retain(int increment = 1)
        {
            if (increment <= 0)
            {
                throw new ArgumentOutOfRangeException($"increment: {increment} (expected: > 0)");
            }

            while (true)
            {
                int refCnt = this.ReferenceCount;
                if (refCnt == 0)
                {
                    throw new InvalidOperationException(
                        $"{this.GetType().Name} has reference count of zero and should be de allocated.");
                }
                if (refCnt > int.MaxValue - increment)
                {
                    throw new InvalidOperationException(
                        $"{this.GetType().Name} has reached the maximum number of references ({int.MaxValue}).");
                }

                if (Interlocked.CompareExchange(ref this.referenceCount, refCnt + increment, refCnt) == refCnt)
                {
                    break;
                }
            }

            return this;
        }

        public override bool Release(int decrement = 1)
        {
            if (decrement <= 0)
            {
                throw new ArgumentOutOfRangeException($"decrement: {decrement} (expected: > 0)");
            }

            while (true)
            {
                int refCnt = this.ReferenceCount;
                if (refCnt < decrement)
                {
                    throw new InvalidOperationException(
                        $"{this.GetType().Name} has reference count of {refCnt}, cannot decrement reference count by {decrement}.");
                }

                if (Interlocked.CompareExchange(ref this.referenceCount, refCnt - decrement, refCnt) == refCnt)
                {
                    if (refCnt == decrement)
                    {
                        this.Deallocate();
                        return true;
                    }

                    return false;
                }
            }
        }

        public override IReferenceCounted Touch(object hint = null) => this;

        protected abstract void Deallocate();
    }
}
