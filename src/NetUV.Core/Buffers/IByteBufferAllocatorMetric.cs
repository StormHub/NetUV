﻿// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    interface IByteBufferAllocatorMetric
    {
        long UsedHeapMemory { get; }
    }
}
