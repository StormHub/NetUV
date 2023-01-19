// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System.Collections.Generic;

    // Forked from https://github.com/Azure/DotNetty
    public interface IPoolChunkListMetric : IEnumerable<IPoolChunkMetric>
    {
        /// Return the minum usage of the chunk list before which chunks are promoted to the previous list.
        int MinUsage { get; }

        /// Return the minum usage of the chunk list after which chunks are promoted to the next list.
        int MaxUsage { get; }
    }
}
