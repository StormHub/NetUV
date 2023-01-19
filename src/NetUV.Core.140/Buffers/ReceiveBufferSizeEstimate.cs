// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Common;
    using NetUV.Core.Logging;

    sealed class ReceiveBufferSizeEstimate
    {
        static readonly ILog Log = LogFactory.ForContext<ReceiveBufferSizeEstimate>();

        const int DefaultMinimum = 64;
        const int DefaultInitial = 1024;
        const int DefaultMaximum = 65536;

        const int IndexIncrement = 4;
        const int IndexDecrement = 1;

        static readonly int[] SizeTable;

        static ReceiveBufferSizeEstimate()
        {
            var sizeTable = new List<int>();
            for (int i = 16; i < 512; i += 16)
            {
                sizeTable.Add(i);
            }

            for (int i = 512; i > 0; i <<= 1)
            {
                sizeTable.Add(i);
            }

            SizeTable = sizeTable.ToArray();
        }

        static int GetSizeTableIndex(int size)
        {
            for (int low = 0, high = SizeTable.Length - 1; ;)
            {
                if (high < low)
                {
                    return low;
                }
                if (high == low)
                {
                    return high;
                }

                int mid = (low + high).RightUShift(1);
                int a = SizeTable[mid];
                int b = SizeTable[mid + 1];
                if (size > b)
                {
                    low = mid + 1;
                }
                else if (size < a)
                {
                    high = mid - 1;
                }
                else if (size == a)
                {
                    return mid;
                }
                else
                {
                    return mid + 1;
                }
            }
        }

        readonly int minIndex;
        readonly int maxIndex;
        int index;
        bool decreaseNow;
        int receiveBufferSize;

        public ReceiveBufferSizeEstimate(int minimum = DefaultMinimum, int initial = DefaultInitial, int maximum = DefaultMaximum)
        {
            Contract.Requires(minimum > 0);
            Contract.Requires(initial >= minimum);
            Contract.Requires(maximum >= initial);

            int min = GetSizeTableIndex(minimum);
            if (SizeTable[min] < minimum)
            {
                this.minIndex = min + 1;
            }
            else
            {
                this.minIndex = min;
            }

            int max = GetSizeTableIndex(maximum);
            if (SizeTable[max] > maximum)
            {
                this.maxIndex = max - 1;
            }
            else
            {
                this.maxIndex = max;
            }

            this.index = GetSizeTableIndex(initial);
            this.receiveBufferSize = SizeTable[this.index];
        }

        internal IByteBuffer Allocate(PooledByteBufferAllocator allocator)
        {
            Debug.Assert(allocator != null);

            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0} allocate, estimated size = {1}", nameof(ReceiveBufferSizeEstimate), this.receiveBufferSize);
            }

            return allocator.Buffer(this.receiveBufferSize);
        }

        internal void Record(int actualReadBytes)
        {
            if (actualReadBytes <= SizeTable[Math.Max(0, this.index - IndexDecrement - 1)])
            {
                if (this.decreaseNow)
                {
                    this.index = Math.Max(this.index - IndexDecrement, this.minIndex);
                    this.receiveBufferSize = SizeTable[this.index];
                    this.decreaseNow = false;
                }
                else
                {
                    this.decreaseNow = true;
                }
            }
            else if (actualReadBytes >= this.receiveBufferSize)
            {
                this.index = Math.Min(this.index + IndexIncrement, this.maxIndex);
                this.receiveBufferSize = SizeTable[this.index];
                this.decreaseNow = false;
            }

            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0} record actual size = {1}, next size = {2}", nameof(ReceiveBufferSizeEstimate), actualReadBytes, this.receiveBufferSize);
            }
        }
    }
}
