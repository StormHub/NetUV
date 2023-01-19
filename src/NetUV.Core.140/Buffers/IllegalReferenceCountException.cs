// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;

    public class IllegalReferenceCountException : InvalidOperationException
    {
        public IllegalReferenceCountException(int count)
            : base($"Illegal reference count of {count} for this object")
        {
        }

        public IllegalReferenceCountException(int refCnt, int increment)
            : base("refCnt: " + refCnt + ", " + (increment > 0 ? "increment: " + increment : "decrement: " + -increment))
        {
        }
    }
}
