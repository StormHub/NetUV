// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System.Diagnostics.Contracts;

    static class UnpooledByteBuffer
    {
        static readonly UnpooledArrayBufferAllocator<byte> Allocator = new UnpooledArrayBufferAllocator<byte>();

        internal static ByteBuffer From(byte[] array)
        {
            Contract.Requires(array != null);

            return From(array, 0, array.Length);
        }

        internal static ByteBuffer From(byte[] array, int offset, int count)
        {
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);

            return new ByteBuffer(new UnpooledArrayBuffer<byte>(Allocator, array, offset, array.Length));
        }
    }
}
