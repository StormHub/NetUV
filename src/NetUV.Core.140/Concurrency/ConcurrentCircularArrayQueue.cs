// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Concurrency
{
    using NetUV.Core.Common;

    abstract class ConcurrentCircularArrayQueue<T> : ConcurrentCircularArrayQueueL0Pad<T>
        where T : class
    {
        protected long Mask;
        protected readonly T[] Buffer;

        protected ConcurrentCircularArrayQueue(int capacity)
        {
            int actualCapacity = IntegerExtensions.RoundUpToPowerOfTwo(capacity);
            this.Mask = actualCapacity - 1;
            // pad data on either end with some empty slots.
            this.Buffer = new T[actualCapacity + RefArrayAccessUtil.RefBufferPad * 2];
        }

        protected long CalcElementOffset(long index) => RefArrayAccessUtil.CalcElementOffset(index, this.Mask);

        protected void SpElement(long offset, T e) => RefArrayAccessUtil.SpElement(this.Buffer, offset, e);

        protected void SoElement(long offset, T e) => RefArrayAccessUtil.SoElement(this.Buffer, offset, e);

        protected T LpElement(long offset) => RefArrayAccessUtil.LpElement(this.Buffer, offset);

        protected T LvElement(long offset) => RefArrayAccessUtil.LvElement(this.Buffer, offset);

        public override void Clear()
        {
            while (this.TryDequeue(out T _) || !this.IsEmpty)
            {
                // looping
            }
        }

        public int Capacity() => (int)(this.Mask + 1);
    }

    abstract class ConcurrentCircularArrayQueueL0Pad<T> : AbstractQueue<T>
    {
#pragma warning disable 169 // padded reference
        long p00, p01, p02, p03, p04, p05, p06, p07;
        long p30, p31, p32, p33, p34, p35, p36, p37;
#pragma warning restore 169
    }
}
