// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using NetUV.Core.Channels;
    using NetUV.Core.Handles;
    using NetUV.Core.Logging;

    sealed class Worker : IDisposable
    {
        static readonly ILog Logger = LogFactory.ForContext<EventLoop>();

        readonly EventLoop eventLoop;
        readonly string pipeName;
        readonly List<ServerTcpContext> callbacks;

        public Worker(string pipeName, List<ServerTcpContext> callbacks)
        {
            this.pipeName = pipeName;
            this.callbacks = callbacks;
            this.eventLoop = new EventLoop();
        }

        public Task StartAsync()
        {
            Task task = this.eventLoop.ExecuteAsync(state => ((Loop)state)
                    .CreatePipe(true) // use IPC 
                    .ConnectTo(this.pipeName, this.OnConnected),
                this.eventLoop.Loop);
            return task;
        }

        public Task TerminationCompletion => this.eventLoop.TerminationCompletion;

        void OnConnected(Pipe pipe, Exception exception)
        {
            if (exception != null)
            {
                Logger.Error($"{nameof(Worker)} failed to connect to {this.pipeName}");
                pipe.CloseHandle(OnClosed);
            }
            else
            {
                Logger.Info($"{nameof(Worker)} connected to {this.pipeName}");
                pipe.OnRead(this.GetPendingHandle);
            }
        }

        void GetPendingHandle(Pipe pipe, IStreamReadCompletion completion)
        {
            if (completion.Error != null || completion.Completed)
            {
                if (completion.Error != null)
                {
                    Logger.Error($"{nameof(Worker)} read pending handle error {completion.Error}");
                }
                pipe.CloseHandle(OnClosed);
                return;
            }

            var tcp = (Tcp)pipe.CreatePendingType();

            var cb = FindTcpCallbackByID(completion.Data.ReadString(Encoding.UTF8));
            if (cb != null)
            {
                cb.OnAccept(cb.ServerTcp, tcp);
                tcp.OnRead(cb.OnRead, cb.OnError);
            }
            else
            {
                Logger.Error("Do not find TcpServeContext");
                tcp.CloseHandle(OnClosed);
            }
        }

        public Task ShutdownAsync() => this.eventLoop.ShutdownGracefullyAsync();

        static void OnClosed(StreamHandle handle) => handle.Dispose();

        public void Dispose()
        {
             this.ShutdownAsync().Wait();
        }

        ServerTcpContext FindTcpCallbackByID(string idString)
        {
            try
            {
                var id = Convert.ToInt32(idString);
                return this.callbacks.Find((item) => item.ID == id);
            }
            catch (System.Exception ex)
            {
                Logger.Error($"FindTcpCallbackByID, Exception:{ex}");
            }
            return null;
        }
    }
}
