// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System.Diagnostics.Contracts;
    using NetUV.Core.Common;

    /// <summary>
    /// Forked and adapted from https://github.com/Azure/DotNetty
    /// </summary>
    sealed class PoolSubpage<T> : IPoolSubpageMetric
    {
        internal readonly PoolChunk<T> Chunk;
        readonly int memoryMapIdx;
        readonly int runOffset;
        readonly int pageSize;
        readonly long[] bitmap;

        internal PoolSubpage<T> Prev;
        internal PoolSubpage<T> Next;

        internal bool DoNotDestroy;
        internal int ElemSize;
        int bitmapLength;
        int nextAvail;

        /** Special constructor that creates a linked list head */
        public PoolSubpage(int pageSize)
        {
            this.Chunk = null;
            this.memoryMapIdx = -1;
            this.runOffset = -1;
            this.ElemSize = -1;
            this.pageSize = pageSize;
            this.bitmap = null;
        }

        public PoolSubpage(PoolSubpage<T> head, PoolChunk<T> chunk, int memoryMapIdx, int runOffset, int pageSize, int elemSize)
        {
            this.Chunk = chunk;
            this.memoryMapIdx = memoryMapIdx;
            this.runOffset = runOffset;
            this.pageSize = pageSize;
            this.bitmap = new long[pageSize.RightUShift(10)]; // pageSize / 16 / 64
            this.Init(head, elemSize);
        }

        public void Init(PoolSubpage<T> head, int elemSize)
        {
            this.DoNotDestroy = true;
            this.ElemSize = elemSize;
            if (elemSize != 0)
            {
                this.MaxNumElements = this.NumAvailable = this.pageSize / elemSize;
                this.nextAvail = 0;
                this.bitmapLength = this.MaxNumElements.RightUShift(6);
                if ((this.MaxNumElements & 63) != 0)
                {
                    this.bitmapLength++;
                }

                for (int i = 0; i < this.bitmapLength; i++)
                {
                    this.bitmap[i] = 0;
                }
            }

            this.AddToPool(head);
        }

        /**
         * Returns the bitmap index of the subpage allocation.
         */
        internal long Allocate()
        {
            if (this.ElemSize == 0)
            {
                return this.ToHandle(0);
            }

            /**
             * Synchronize on the head of the SubpagePool stored in the {@link PoolArena. This is needed as we synchronize
             * on it when calling {@link PoolArena#allocate(PoolThreadCache, int, int)} und try to allocate out of the
             * {@link PoolSubpage} pool for a given size.
             */
            PoolSubpage<T> head = this.Chunk.Arena.FindSubpagePoolHead(this.ElemSize);
            lock (head)
            {
                if (this.NumAvailable == 0 || !this.DoNotDestroy)
                {
                    return -1;
                }

                int bitmapIdx = this.GetNextAvail();
                int q = bitmapIdx.RightUShift(6);
                int r = bitmapIdx & 63;
                Contract.Assert((this.bitmap[q].RightUShift(r) & 1) == 0);
                this.bitmap[q] |= 1L << r;

                if (--this.NumAvailable == 0)
                {
                    this.RemoveFromPool();
                }

                return this.ToHandle(bitmapIdx);
            }
        }

        /**
         * @return {@code true} if this subpage is in use.
         *         {@code false} if this subpage is not used by its chunk and thus it's OK to be released.
         */

        internal bool Free(PoolSubpage<T> head, int bitmapIdx)
        {
            if (this.ElemSize == 0)
            {
                return true;
            }

            int q = bitmapIdx.RightUShift(6);
            int r = bitmapIdx & 63;
            Contract.Assert((this.bitmap[q].RightUShift(r) & 1) != 0);
            this.bitmap[q] ^= 1L << r;

            this.SetNextAvail(bitmapIdx);

            if (this.NumAvailable++ == 0)
            {
                this.AddToPool(head);
                return true;
            }

            if (this.NumAvailable != this.MaxNumElements)
            {
                return true;
            }
            else
            {
                // Subpage not in use (numAvail == maxNumElems)
                if (this.Prev == this.Next)
                {
                    // Do not remove if this subpage is the only one left in the pool.
                    return true;
                }

                // Remove this subpage from the pool if there are other subpages left in the pool.
                this.DoNotDestroy = false;
                this.RemoveFromPool();
                return false;
            }
        }

        void AddToPool(PoolSubpage<T> head)
        {
            Contract.Assert(this.Prev == null && this.Next == null);

            this.Prev = head;
            this.Next = head.Next;
            this.Next.Prev = this;
            head.Next = this;
        }

        void RemoveFromPool()
        {
            Contract.Assert(this.Prev != null && this.Next != null);

            // ReSharper disable PossibleNullReferenceException
            this.Prev.Next = this.Next;
            this.Next.Prev = this.Prev;
            // ReSharper restore PossibleNullReferenceException
            this.Next = null;
            this.Prev = null;
        }

        void SetNextAvail(int bitmapIdx) => this.nextAvail = bitmapIdx;

        int GetNextAvail()
        {
            int next = this.nextAvail;
            if (next >= 0)
            {
                this.nextAvail = -1;
                return next;
            }
            return this.FindNextAvail();
        }

        int FindNextAvail()
        {
            long[] map = this.bitmap;
            int length = this.bitmapLength;
            for (int i = 0; i < length; i++)
            {
                long bits = map[i];
                if (~bits != 0)
                {
                    return this.FindNextAvail0(i, bits);
                }
            }
            return -1;
        }

        int FindNextAvail0(int i, long bits)
        {
            int maximum = this.MaxNumElements;
            int baseVal = i << 6;

            for (int j = 0; j < 64; j++)
            {
                if ((bits & 1) == 0)
                {
                    int val = baseVal | j;
                    if (val < maximum)
                    {
                        return val;
                    }
                    else
                    {
                        break;
                    }
                }
                bits = bits.RightUShift(1);
            }
            return -1;
        }

        public void Destroy() => this.Chunk?.Destroy();

#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
        long ToHandle(int bitmapIdx) => 0x4000000000000000L | (long)bitmapIdx << 32 | this.memoryMapIdx;
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand

        public override string ToString() => !this.DoNotDestroy 
            ? $"({this.memoryMapIdx}: not in use)" 
            : $"({this.memoryMapIdx}: {(this.MaxNumElements - this.NumAvailable)} / {this.MaxNumElements}, offset: {this.runOffset}, length: {this.pageSize}, elemSize: {this.ElemSize})";

        public int MaxNumElements { get; private set; }

        public int NumAvailable { get; private set; }

        public int ElementSize => this.ElemSize;

        public int PageSize => this.pageSize;
    }
}
