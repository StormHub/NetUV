// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable InconsistentNaming

namespace NetUV.Core.Native
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using NetUV.Core.Handles;
    using NetUV.Core.Requests;

    [StructLayout(LayoutKind.Sequential)]
    struct uv_buf_t
    {
        static readonly bool isWindows = Platform.IsWindows;
        /*
           Windows 
           public int length;
           public IntPtr data;

           Unix
           public IntPtr data;
           public IntPtr length;
        */

        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        readonly IntPtr first;
        readonly IntPtr second;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        internal uv_buf_t(IntPtr memory, int length)
        {
            Contract.Requires(length >= 0);

            if (isWindows)
            {
                this.first = (IntPtr)length;
                this.second = memory;
            }
            else
            {
                this.first = memory;
                this.second = (IntPtr)length;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_stream_t
    {
        /* handle fields */
        public IntPtr data;
        public IntPtr loop;
        public uv_handle_type type;
        public IntPtr close_cb;

        /* stream fields */
        public IntPtr write_queue_size; /* number of bytes queued for writing */
        public IntPtr alloc_cb;
        public IntPtr read_cb;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void uv_alloc_cb(IntPtr handle, IntPtr suggested_size, out uv_buf_t buf);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void uv_read_cb(IntPtr handle, IntPtr nread, ref uv_buf_t buf);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void uv_udp_recv_cb(IntPtr handle, IntPtr nread, ref uv_buf_t buf, ref sockaddr addr, int flags);

    static partial class NativeMethods
    {
        internal static void StreamReadStart(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_read_start(handle, StreamHandle.AllocateCallback, StreamHandle.ReadCallback);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void StreamReadStop(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_read_stop(handle);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static bool IsStreamReadable(IntPtr handle) => 
            handle != IntPtr.Zero && uv_is_readable(handle) == 1;

        internal static bool IsStreamWritable(IntPtr handle) => 
            handle != IntPtr.Zero && uv_is_writable(handle) == 1;

        internal static void TryWriteStream(IntPtr handle, ref uv_buf_t buf)
        {
            Contract.Requires(handle != IntPtr.Zero);

            var bufs = new [] { buf };
            int result = uv_try_write(handle , bufs, bufs.Length);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void WriteStream(IntPtr requestHandle, IntPtr streamHandle, ref uv_buf_t[] bufs, ref int size)
        {
            Contract.Requires(requestHandle != IntPtr.Zero);
            Contract.Requires(streamHandle != IntPtr.Zero);
            Contract.Requires(bufs != null && bufs.Length > 0);

            int result = uv_write(requestHandle, streamHandle, bufs, size, WriteBufferRequest.WriteCallback);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void WriteStream(IntPtr requestHandle, IntPtr streamHandle, ref uv_buf_t[] bufs, ref int size, IntPtr sendHandle)
        {
            Contract.Requires(requestHandle != IntPtr.Zero);
            Contract.Requires(streamHandle != IntPtr.Zero);
            Contract.Requires(sendHandle != IntPtr.Zero);
            Contract.Requires(bufs != null && bufs.Length > 0);

            int result = uv_write2(requestHandle, streamHandle, bufs, size, sendHandle, WriteBufferRequest.WriteCallback);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void StreamListen(IntPtr handle, int backlog)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(backlog > 0);

            int result = uv_listen(handle, backlog, ServerStream.ConnectionCallback);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void StreamAccept(IntPtr serverHandle, IntPtr clientHandle)
        {
            Contract.Requires(serverHandle != IntPtr.Zero);
            Contract.Requires(clientHandle != IntPtr.Zero);

            int result = uv_accept(serverHandle, clientHandle);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        // If *value == 0, it will return the current send buffer size, 
        // otherwise it will use *value to set the new send buffersize.
        // This function works for TCP, pipe and UDP handles on Unix and for TCP and UDP handles on Windows.
        internal static int SendBufferSize(IntPtr handle, int value)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(value >= 0);

            var size = (IntPtr)value;
            int result = uv_send_buffer_size(handle, ref size);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }

            return size.ToInt32();
        }

        // If *value == 0, it will return the current receive buffer size,
        // otherwise it will use *value to set the new receive buffer size.
        // This function works for TCP, pipe and UDP handles on Unix and for TCP and UDP handles on Windows.

        internal static int ReceiveBufferSize(IntPtr handle, int value)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(value >= 0);

            var size = (IntPtr)value;
            int result = uv_recv_buffer_size(handle, ref size);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }

            return size.ToInt32();
        }

        // Gets the platform dependent file descriptor equivalent.
        // The following handles are supported: TCP, pipes, TTY, UDP and poll. Passing any other handle 
        // type will fail with UV_EINVAL.
        // If a handle doesn’t have an attached file descriptor yet or the handle itself has been closed, 
        // this function will return UV_EBADF.
        // Warning: Be very careful when using this function. libuv assumes it’s in control of the 
        // file descriptor so any change to it may lead to malfunction.
        internal static IntPtr GetFileDescriptor(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_fileno(handle, out IntPtr value);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }

            return value;
        }

        #region Stream Status

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_listen(IntPtr handle, int backlog, uv_watcher_cb connection_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_accept(IntPtr server, IntPtr client);

        #endregion Stream Status

        #region Read/Write

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_fileno(IntPtr handle, out IntPtr value);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_send_buffer_size(IntPtr handle, ref IntPtr value);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_recv_buffer_size(IntPtr handle, ref IntPtr value);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_is_readable(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_is_writable(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_read_start(IntPtr handle, uv_alloc_cb alloc_cb, uv_read_cb read_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_read_stop(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_try_write(IntPtr handle, uv_buf_t[] bufs, int bufcnt);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_write(IntPtr req, IntPtr handle, uv_buf_t[] bufs, int nbufs, uv_watcher_cb cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_write2(IntPtr req, IntPtr handle, uv_buf_t[] bufs, int nbufs, IntPtr sendHandle, uv_watcher_cb cb);

        #endregion
    }
}
