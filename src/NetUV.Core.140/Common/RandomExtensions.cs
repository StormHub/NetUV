// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System;

    // Forked from https://github.com/Azure/DotNetty
    static class RandomExtensions
    {
        public static long NextLong(this Random random) => random.Next() << 32 & unchecked((uint)random.Next());
    }
}
