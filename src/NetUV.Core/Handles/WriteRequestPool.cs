// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Common;
    using NetUV.Core.Native;
    using NetUV.Core.Requests;

    sealed class WriteRequestPool : IDisposable
    {
        const int DefaultPoolSize = 128;
        readonly uv_req_type requestType;
        readonly MpscArrayQueue<WriteRequest> requestPool;

        internal WriteRequestPool(uv_req_type requestType, int poolSize = DefaultPoolSize)
        {
            Contract.Requires(poolSize > 0);

            this.requestType = requestType;
            this.requestPool = new MpscArrayQueue<WriteRequest>(poolSize);
        }

        public WriteRequest Take()
        {
            WriteRequest request;
            if (!this.requestPool.TryDequeue(out request))
            {
                request = new WriteRequest(this.requestType);
            }

            return request;
        }

        public void Return(WriteRequest request)
        {
            if (request == null)
            {
                return;
            }

            if (!this.requestPool.TryEnqueue(request))
            {
                request.Dispose();
            }
        }

        public void Dispose()
        {
            WriteRequest request;
            while (this.requestPool.TryDequeue(out request))
            {
                request.Dispose();
            }
        }
    }
}
