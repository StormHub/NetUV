// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.IO;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;
    using Xunit;

    public sealed class PollTests : IDisposable
    {
        Loop loop;
        IDisposable fdHandle;

        public PollTests()
        {
            this.loop = new Loop();
        }

        [Fact]
        public void BadFileDescriptorType()
        {
            FileStream file = TestHelper.OpenTempFile();
            this.fdHandle = file;

            IntPtr handle = file.SafeFileHandle.DangerousGetHandle();
            var error = Assert.Throws<OperationException>(() => this.loop.CreatePoll(handle.ToInt32()));
            file.Dispose();

            Assert.Equal((int)uv_err_code.UV_ENOTSOCK, error.ErrorCode);
        }

        public void Dispose()
        {
            this.fdHandle?.Dispose();
            this.fdHandle = null;

            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
