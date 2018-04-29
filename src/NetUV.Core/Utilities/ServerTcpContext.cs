// Copyright (c) egmkang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Utilities
{
    using System;
    using System.Text;
    using System.Threading;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;

    sealed class ServerTcpContext
    {
        static int SEED = 1;

        public ServerTcpContext(Tcp tcp, Action<Tcp, Tcp> onAccept, Action<Tcp, ReadableBuffer> onRead, Action<Tcp, Exception> onError)
        {
            this.ServerTcp = tcp;
            this.OnAccept = onAccept;
            this.OnRead = onRead;
            this.OnError = onError;
            this.ID = Interlocked.Add(ref SEED, 1);
            this.Hello = WritableBuffer.From(Encoding.UTF8.GetBytes(this.ID.ToString()));
        }


        public int ID { get; private set; }

        public Tcp ServerTcp { get; private set; }

        public WritableBuffer Hello { get; private set; }

        //Action(ServerTcp, NewTcp)
        public Action<Tcp, Tcp> OnAccept { get; private set; }

        public Action<Tcp, ReadableBuffer> OnRead { get; private set; }

        public Action<Tcp, Exception> OnError { get; private set; }
    }
}
