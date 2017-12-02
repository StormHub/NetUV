// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    public interface IReferenceCounted
    {
        int ReferenceCount { get; }

        IReferenceCounted Retain();

        IReferenceCounted Retain(int increment);

        IReferenceCounted Touch();

        IReferenceCounted Touch(object hint);

        bool Release();

        bool Release(int decrement);
    }
}
