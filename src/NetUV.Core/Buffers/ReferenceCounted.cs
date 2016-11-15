// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using NetUV.Core.Common;

    abstract class ReferenceCounted : IReferenceCounted
    {
        internal static readonly ResourceLeakDetector LeakDetector = ResourceLeakDetector.Create<ReferenceCounted>();

        int referenceCount = 1;

        protected ReferenceCounted(int capacity)
        {
            Contract.Requires(capacity > 0);

            this.Capacity = capacity;
        }

        public int ReferenceCount => Volatile.Read(ref this.referenceCount);

        protected void Recycle(int capacity)
        {
            Volatile.Write(ref this.referenceCount, 1);
            this.Capacity = capacity;
        }

        public int Capacity { get; internal set; }

        public IReferenceCounted Retain(int increment = 1)
        {
            if (increment <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(increment), 
                    $"increment {increment} must be greater than 0)");
            }

            while (true)
            {
                int count = this.ReferenceCount;
                if (count == 0)
                {
                    throw new InvalidOperationException(
                        $"{this.GetType().Name} has reference count of zero and should be de allocated.");
                }
                if (count == int.MaxValue)
                {
                    throw new InvalidOperationException(
                        $"{this.GetType().Name} has reached the maximum number of references ({int.MaxValue}).");
                }

                if (Interlocked.CompareExchange(ref this.referenceCount, count + increment, count) == count)
                {
                    break;
                }
            }

            return this;
        }

        public IReferenceCounted Touch(object hint = null) => this;

        public bool Release(int decrement = 1)
        {
            if (decrement <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(decrement), 
                    $"decrement: {decrement} must be greater than zero.");
            }

            while (true)
            {
                int count = this.ReferenceCount;
                if (count < decrement)
                {
                    throw new InvalidOperationException(
                        $"{this.GetType().Name} has reference count of {count}, cannot decrement reference count by {decrement}.");
                }

                if (Interlocked.CompareExchange(ref this.referenceCount, count - decrement, count) == count)
                {
                    if (count == decrement)
                    {
                        this.Deallocate();
                        return true;
                    }

                    return false;
                }
            }
        }

        protected abstract void Deallocate();
    }
}
