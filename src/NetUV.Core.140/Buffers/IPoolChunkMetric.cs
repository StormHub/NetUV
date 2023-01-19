﻿// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    // Forked from https://github.com/Azure/DotNetty
    public interface IPoolChunkMetric
    {
        /// Return the percentage of the current usage of the chunk.
        int Usage { get; }

        /// Return the size of the chunk in bytes, this is the maximum of bytes that can be served out of the chunk.
        int ChunkSize { get; }

        /// Return the number of free bytes in the chunk.
        int FreeBytes { get; }
    }
}
