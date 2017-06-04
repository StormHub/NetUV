// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Buffers
{
    using NetUV.Core.Buffers;
    using Xunit;

    // Forked from https://github.com/Azure/DotNetty
    public sealed class PooledBufferAllocatorTests
    {
        [Theory]
        [InlineData(8000, 32000, new[] { 1024, 0, 10 * 1024 })]
        [InlineData(16 * 1024, 10, new[] { 16 * 1024 - 100, 8 * 1024 })]
        [InlineData(16 * 1024, 0, new[] { 16 * 1024 - 100, 8 * 1024 })]
        [InlineData(1024, 2 * 1024, new[] { 16 * 1024 - 100, 8 * 1024 })]
        [InlineData(1024, 0, new[] { 1024, 1 })]
        [InlineData(1024, 0, new[] { 1024, 0, 10 * 1024 })]
        public void PooledBufferGrowTest(int bufferSize, int startSize, int[] writeSizes)
        {
            var alloc = new PooledArrayBufferAllocator<byte>();
            IArrayBuffer<byte> buffer = alloc.Buffer(startSize);
            int wrote = 0;
            foreach (int size in writeSizes)
            {
                buffer.Write(Unpooled.WrappedBuffer(new byte[size]));
                wrote += size;
            }

            Assert.Equal(wrote, buffer.ReadableCount);
        }
    }
}
