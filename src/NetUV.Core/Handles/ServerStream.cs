// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using NetUV.Core.Native;

    public abstract class ServerStream : StreamHandle
    {
        internal static readonly uv_watcher_cb ConnectionCallback = OnConnectionCallback;

        internal ServerStream(
            LoopContext loop, 
            uv_handle_type handleType, 
            params object[] args)
            : base(loop, handleType, args)
        { }

        internal Action<StreamHandle, Exception> ConnectionHandler { get; set; }

        protected abstract StreamHandle NewStream();

        static void OnConnectionCallback(IntPtr handle, int status)
        {
            var server = HandleContext.GetTarget<ServerStream>(handle);
            if (server == null)
            {
                return;
            }

            StreamHandle client = null;
            Exception error = null;
            try
            {
                if (status < 0)
                {
                    error = NativeMethods.CreateError((uv_err_code)status);
                }
                else
                {
                    client = server.NewStream();
                    if (client == null)
                    {
                        throw new InvalidOperationException(
                            $"{server.HandleType} {server.InternalHandle} failed to create new client stream.");
                    }

                    NativeMethods.StreamAccept(server.InternalHandle, client.InternalHandle);
                    client.ReadStart();

                    Log.DebugFormat("{0} {1} client {2} accepted", server.HandleType, handle, client.InternalHandle);
                }

                server.ConnectionHandler.Invoke(client, error);
            }
            catch
            {
                client?.Dispose();
                throw;
            }
        }
    }
}
