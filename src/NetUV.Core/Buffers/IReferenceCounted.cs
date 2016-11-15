// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    interface IReferenceCounted
    {
        int ReferenceCount { get; }

        IReferenceCounted Retain(int increment = 1);

        IReferenceCounted Touch(object hint = null);

        bool Release(int decrement = 1);
    }
}
