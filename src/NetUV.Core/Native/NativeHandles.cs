// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable InconsistentNaming

namespace NetUV.Core.Native
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using NetUV.Core.Handles;
    using NetUV.Core.Requests;

    enum uv_handle_type
    {
        UV_UNKNOWN_HANDLE = 0,
        UV_ASYNC,
        UV_CHECK,
        UV_FS_EVENT,
        UV_FS_POLL,
        UV_HANDLE,
        UV_IDLE,
        UV_NAMED_PIPE,
        UV_POLL,
        UV_PREPARE,
        UV_PROCESS,
        UV_STREAM,
        UV_TCP,
        UV_TIMER,
        UV_TTY,
        UV_UDP,
        UV_SIGNAL,
        UV_FILE,
        UV_HANDLE_TYPE_MAX
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_handle_t
    {
        public IntPtr data;
        public IntPtr loop;
        public uv_handle_type type;
        public IntPtr close_cb;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_prepare_t
    {
        /* uv_handle_t fields */
        public IntPtr data;
        public IntPtr loop;
        public uv_handle_type type;
        public IntPtr close_cb;

        /* prepare fields */
        public IntPtr prepare_prev;
        public IntPtr prepare_next;
        public IntPtr prepare_cb;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_check_t
    {
        /* uv_handle_t fields */
        public IntPtr data;
        public IntPtr loop;
        public uv_handle_type type;
        public IntPtr close_cb;

        /* prepare fields */
        public IntPtr check_prev;
        public IntPtr check_next;
        public IntPtr uv_check_cb;
    }

    /// <summary>
    /// https://github.com/aspnet/KestrelHttpServer/blob/dev/src/Microsoft.AspNetCore.Server.Kestrel/Internal/Networking/SockAddr.cs
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct sockaddr
    {
        // this type represents native memory occupied by sockaddr struct
        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms740496(v=vs.85).aspx
        // although the c/c++ header defines it as a 2-byte short followed by a 14-byte array,
        // the simplest way to reserve the same size in c# is with four nameless long values
        public long field0;
        public long field1;
        public long field2;
        public long field3;

        // ReSharper disable once UnusedParameter.Local
        internal sockaddr(long ignored)
        {
            this.field0 = 0;
            this.field1 = 0;
            this.field2 = 0;
            this.field3 = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe IPEndPoint GetIPEndPoint()
        {
            // The bytes are represented in network byte order.
            //
            // Example 1: [2001:4898:e0:391:b9ef:1124:9d3e:a354]:39179
            //
            // 0000 0000 0b99 0017  => The third and fourth bytes 990B is the actual port
            // 9103 e000 9848 0120  => IPv6 address is represented in the 128bit field1 and field2.
            // 54a3 3e9d 2411 efb9     Read these two 64-bit long from right to left byte by byte.
            // 0000 0000 0000 0000
            //
            // Example 2: 10.135.34.141:39178 when adopt dual-stack sockets, IPv4 is mapped to IPv6
            //
            // 0000 0000 0a99 0017  => The port representation are the same
            // 0000 0000 0000 0000
            // 8d22 870a ffff 0000  => IPv4 occupies the last 32 bit: 0A.87.22.8d is the actual address.
            // 0000 0000 0000 0000
            //
            // Example 3: 10.135.34.141:12804, not dual-stack sockets
            //
            // 8d22 870a fd31 0002  => sa_family == AF_INET (02)
            // 0000 0000 0000 0000
            // 0000 0000 0000 0000
            // 0000 0000 0000 0000
            //
            // Example 4: 127.0.0.1:52798, on a Mac OS
            //
            // 0100 007F 3ECE 0210  => sa_family == AF_INET (02) Note that struct sockaddr on mac use
            // 0000 0000 0000 0000     the second unint8 field for sa family type
            // 0000 0000 0000 0000     http://www.opensource.apple.com/source/xnu/xnu-1456.1.26/bsd/sys/socket.h
            // 0000 0000 0000 0000
            //
            // Reference:
            //  - Windows: https://msdn.microsoft.com/en-us/library/windows/desktop/ms740506(v=vs.85).aspx
            //  - Linux: https://github.com/torvalds/linux/blob/6a13feb9c82803e2b815eca72fa7a9f5561d7861/include/linux/socket.h
            //  - Apple: http://www.opensource.apple.com/source/xnu/xnu-1456.1.26/bsd/sys/socket.h

            // Quick calculate the port by mask the field and locate the byte 3 and byte 4
            // and then shift them to correct place to form a int.
            int port = ((int)(this.field0 & 0x00FF0000) >> 8) | (int)((this.field0 & 0xFF000000) >> 24);

            int family = (int)this.field0;
            if (Platform.IsMacOS)
            {
                // see explaination in example 4
                family = family >> 8;
            }
            family = family & 0xFF;

            if (family == 2)
            {
                // AF_INET => IPv4
                return new IPEndPoint(new IPAddress((this.field0 >> 32) & 0xFFFFFFFF), port);
            }
            else if (this.IsIPv4MappedToIPv6())
            {
                long ipv4bits = (this.field2 >> 32) & 0x00000000FFFFFFFF;
                return new IPEndPoint(new IPAddress(ipv4bits), port);
            }
            else
            {
                // otherwise IPv6
                var bytes = new byte[16];
                fixed (byte* b = bytes)
                {
                    *((long*)b) = this.field1;
                    *((long*)(b + 8)) = this.field2;
                }

                return new IPEndPoint(new IPAddress(bytes), port);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsIPv4MappedToIPv6()
        {
            // If the IPAddress is an IPv4 mapped to IPv6, return the IPv4 representation instead.
            // For example [::FFFF:127.0.0.1] will be transform to IPAddress of 127.0.0.1
            if (this.field1 != 0)
            {
                return false;
            }

            return (this.field2 & 0xFFFFFFFF) == 0xFFFF0000;
        }
    }

    enum uv_udp_flags
    {
        /* Disables dual stack mode. */
        UV_UDP_IPV6ONLY = 1,
        /*
         * Indicates message was truncated because read buffer was too small. The
         * remainder was discarded by the OS. Used in uv_udp_recv_cb.
         */
        UV_UDP_PARTIAL = 2,
        /*
         * Indicates if SO_REUSEADDR will be set when binding the handle in
         * uv_udp_bind.
         * This sets the SO_REUSEPORT socket flag on the BSDs and OS X. On other
         * Unix platforms, it sets the SO_REUSEADDR flag. What that means is that
         * multiple threads or processes can bind to the same address without error
         * (provided they all set the flag) but only the last one to bind will receive
         * any traffic, in effect "stealing" the port from the previous listener.
        */
        UV_UDP_REUSEADDR = 4
    };

    enum uv_membership
    {
        UV_LEAVE_GROUP = 0,
        UV_JOIN_GROUP = 1
    }

    enum uv_tty_mode_t
    {
        /* Initial/normal terminal mode */
        UV_TTY_MODE_NORMAL = 0,

        /* Raw input mode (On Windows, ENABLE_WINDOW_INPUT is also enabled) */
        UV_TTY_MODE_RAW = 1,

        /* Binary-safe I/O mode for IPC (Unix-only) */
        UV_TTY_MODE_IO
    }

    static partial class NativeMethods
    {
        const int NameBufferSize = 1024;

        #region Common

        internal static HandleContext Initialize(IntPtr loopHandle, uv_handle_type handleType, ScheduleHandle target, object[] args)
        {
            Contract.Requires(loopHandle != IntPtr.Zero);
            Contract.Requires(target != null);

            switch (handleType)
            {
                case uv_handle_type.UV_TIMER:
                    return new HandleContext(handleType, InitializeTimer, loopHandle, target, args);
                case uv_handle_type.UV_PREPARE:
                    return new HandleContext(handleType, InitializePrepare, loopHandle, target, args);
                case uv_handle_type.UV_CHECK:
                    return new HandleContext(handleType, InitializeCheck, loopHandle, target, args);
                case uv_handle_type.UV_IDLE:
                    return new HandleContext(handleType, InitializeIdle, loopHandle, target, args);
                case uv_handle_type.UV_ASYNC:
                    return new HandleContext(handleType, InitializeAsync, loopHandle, target, args);
                case uv_handle_type.UV_POLL:
                    return new HandleContext(handleType, InitializePoll, loopHandle, target, args);
                case uv_handle_type.UV_SIGNAL:
                    return new HandleContext(handleType, InitializeSignal, loopHandle, target, args);
                case uv_handle_type.UV_TCP:
                    return new HandleContext(handleType, InitializeTcp, loopHandle, target, args);
                case uv_handle_type.UV_NAMED_PIPE:
                    return new HandleContext(handleType, InitializePipe, loopHandle, target, args);
                case uv_handle_type.UV_TTY:
                    return new HandleContext(handleType, InitializeTty, loopHandle, target, args);
                case uv_handle_type.UV_UDP:
                    return new HandleContext(handleType, InitializeUdp, loopHandle, target, args);
                case uv_handle_type.UV_FS_EVENT:
                    return new HandleContext(handleType, InitializeFSEvent, loopHandle, target, args);
                case uv_handle_type.UV_FS_POLL:
                    return new HandleContext(handleType, InitializeFSPoll, loopHandle, target, args);
                default:
                    throw new NotSupportedException($"Handle type to initialize {handleType} not supported");
            }
        }

        static int InitializeTimer(IntPtr loopHandle, IntPtr handle, object[] args)
        {
            Contract.Requires(handle != IntPtr.Zero);

            return uv_timer_init(loopHandle, handle);
        }

        static int InitializePrepare(IntPtr loopHandle, IntPtr handle, object[] args)
        {
            Contract.Requires(handle != IntPtr.Zero);

            return uv_prepare_init(loopHandle, handle);
        }

        static int InitializeCheck(IntPtr loopHandle, IntPtr handle, object[] args)
        {
            Contract.Requires(handle != IntPtr.Zero);

            return uv_check_init(loopHandle, handle);
        }

        static int InitializeIdle(IntPtr loopHandle, IntPtr handle, object[] args)
        {
            Contract.Requires(handle != IntPtr.Zero);

            return uv_idle_init(loopHandle, handle);
        }

        static int InitializeAsync(IntPtr loopHandle, IntPtr handle, object[] args)
        {
            Contract.Requires(handle != IntPtr.Zero);

            return uv_async_init(loopHandle, handle, WorkHandle.WorkCallback);
        }

        static int InitializePoll(IntPtr loopHandle, IntPtr handle, object[] args)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(args != null && args.Length > 1);

            object arg = args[0];
            if (arg is IntPtr)
            {
                return uv_poll_init_socket(loopHandle, handle, (IntPtr)args[0]);
            }
            else if (arg is int)
            {
                return uv_poll_init(loopHandle, handle, (int)args[0]);
            }

            throw new NotSupportedException("Poll argument must be either IntPtr or int");
        }

        static int InitializeSignal(IntPtr loopHandle, IntPtr handle, object[] args)
        {
            Contract.Requires(handle != IntPtr.Zero);

            return uv_signal_init(loopHandle, handle);
        }

        static int InitializeTcp(IntPtr loopHandle, IntPtr handle, object[] args)
        {
            Contract.Requires(handle != IntPtr.Zero);

            return uv_tcp_init(loopHandle, handle);
        }

        static int InitializePipe(IntPtr loopHandle, IntPtr handle, object[] args)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(args != null && args.Length > 1);

            bool value = (bool)args[0];
            return uv_pipe_init(loopHandle, handle, value ? 1 : 0);
        }

        static int InitializeTty(IntPtr loopHandle, IntPtr handle, object[] args)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(args != null && args.Length > 1);

            var ttyType = (TtyType)args[0];
            return uv_tty_init(loopHandle, handle, (int)ttyType, ttyType == TtyType.In ? 1 : 0);
        }

        static int InitializeUdp(IntPtr loopHandle, IntPtr handle, object[] args)
        {
            Contract.Requires(handle != IntPtr.Zero);

            return uv_udp_init(loopHandle, handle);
        }

        static int InitializeFSEvent(IntPtr loopHandle, IntPtr handle, object[] args)
        {
            Contract.Requires(handle != IntPtr.Zero);

            return uv_fs_event_init(loopHandle, handle);
        }

        static int InitializeFSPoll(IntPtr loopHandle, IntPtr handle, object[] args)
        {
            Contract.Requires(handle != IntPtr.Zero);

            return uv_fs_poll_init(loopHandle, handle);
        }

        internal static void Start(uv_handle_type handleType, IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result;
            switch (handleType)
            {
                case uv_handle_type.UV_PREPARE:
                    result = uv_prepare_start(handle, WorkHandle.WorkCallback);
                    break;
                case uv_handle_type.UV_CHECK:
                    result = uv_check_start(handle, WorkHandle.WorkCallback);
                    break;
                case uv_handle_type.UV_IDLE:
                    result = uv_idle_start(handle, WorkHandle.WorkCallback);
                    break;
                default:
                    throw new NotSupportedException($"Handle type to start {handleType} not supported");
            }

            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void Stop(uv_handle_type handleType, IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            switch (handleType)
            {
                case uv_handle_type.UV_TIMER:
                    int result = uv_timer_stop(handle);
                    if (result < 0)
                    {
                        throw CreateError((uv_err_code)result);
                    }
                    break;
                case uv_handle_type.UV_PREPARE:
                    uv_prepare_stop(handle);
                    break;
                case uv_handle_type.UV_CHECK:
                    uv_check_stop(handle);
                    break;
                case uv_handle_type.UV_IDLE:
                    uv_idle_stop(handle);
                    break;
                case uv_handle_type.UV_POLL:
                    uv_poll_stop(handle);
                    break;
                case uv_handle_type.UV_SIGNAL:
                    uv_signal_stop(handle);
                    break;
                case uv_handle_type.UV_FS_EVENT:
                    uv_fs_event_stop(handle);
                    break;
                case uv_handle_type.UV_FS_POLL:
                    uv_fs_poll_stop(handle);
                    break;
                default:
                    throw new NotSupportedException($"Handle type to stop {handleType} not supported");
            }
        }

        static readonly int[] HandleSizeTable;
        static readonly int[] RequestSizeTable;

        static NativeMethods()
        {
            HandleSizeTable = new []
            {
                uv_handle_size(uv_handle_type.UV_ASYNC).ToInt32(),
                uv_handle_size(uv_handle_type.UV_CHECK).ToInt32(),
                uv_handle_size(uv_handle_type.UV_FS_EVENT).ToInt32(),
                uv_handle_size(uv_handle_type.UV_FS_POLL).ToInt32(),
                uv_handle_size(uv_handle_type.UV_HANDLE).ToInt32(),
                uv_handle_size(uv_handle_type.UV_IDLE).ToInt32(),
                uv_handle_size(uv_handle_type.UV_NAMED_PIPE).ToInt32(),
                uv_handle_size(uv_handle_type.UV_POLL).ToInt32(),
                uv_handle_size(uv_handle_type.UV_PREPARE).ToInt32(),
                uv_handle_size(uv_handle_type.UV_PROCESS).ToInt32(),
                uv_handle_size(uv_handle_type.UV_STREAM).ToInt32(),
                uv_handle_size(uv_handle_type.UV_TCP).ToInt32(),
                uv_handle_size(uv_handle_type.UV_TIMER).ToInt32(),
                uv_handle_size(uv_handle_type.UV_TTY).ToInt32(),
                uv_handle_size(uv_handle_type.UV_UDP).ToInt32(),
                uv_handle_size(uv_handle_type.UV_SIGNAL).ToInt32(),
                uv_handle_size(uv_handle_type.UV_FILE).ToInt32(),
            };

            RequestSizeTable = new []
            {
                uv_req_size(uv_req_type.UV_REQ).ToInt32(),
                uv_req_size(uv_req_type.UV_CONNECT).ToInt32(),
                uv_req_size(uv_req_type.UV_WRITE).ToInt32(),
                uv_req_size(uv_req_type.UV_SHUTDOWN).ToInt32(),
                uv_req_size(uv_req_type.UV_UDP_SEND).ToInt32(),
                uv_req_size(uv_req_type.UV_FS).ToInt32(),
                uv_req_size(uv_req_type.UV_WORK).ToInt32(),
                uv_req_size(uv_req_type.UV_GETADDRINFO).ToInt32(),
                uv_req_size(uv_req_type.UV_GETNAMEINFO).ToInt32()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetSize(uv_handle_type handleType) => 
            HandleSizeTable[unchecked((int)handleType - 1)];

        #endregion Common

        #region Udp

        internal static void UdpReceiveStart(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_udp_recv_start(handle, Udp.AllocateCallback, Udp.ReceiveCallback);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void UdpReceiveStop(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_udp_recv_stop(handle);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void UdpSend(IntPtr requestHandle, IntPtr handle, IPEndPoint remoteEndPoint, ref uv_buf_t[] bufs)
        {
            Contract.Requires(requestHandle != IntPtr.Zero);
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(remoteEndPoint != null);
            Contract.Requires(bufs != null && bufs.Length > 0);

            GetSocketAddress(remoteEndPoint, out sockaddr addr);

            int result = uv_udp_send(
                requestHandle, 
                handle, 
                bufs, bufs.Length, 
                ref addr, 
                WriteBufferRequest.WriteCallback);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void UdpTrySend(IntPtr handle, IPEndPoint remoteEndPoint, ref uv_buf_t buf)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(remoteEndPoint != null);

            GetSocketAddress(remoteEndPoint, out sockaddr addr);

            var bufs = new[] { buf };
            int result = uv_udp_try_send(handle, bufs, bufs.Length, ref addr);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void UdpSetMembership(IntPtr handle, IPAddress multicastAddress, IPAddress interfaceAddress, uv_membership membership)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(multicastAddress != null);

            string multicast_addr = multicastAddress.ToString();
            string interface_addr = interfaceAddress?.ToString();

            int result = uv_udp_set_membership(handle, multicast_addr, interface_addr, membership);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void UdpSetMulticastInterface(IntPtr handle, IPAddress interfaceAddress)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(interfaceAddress != null);

            string ip = interfaceAddress.ToString();
            int result = uv_udp_set_multicast_interface(handle, ip);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void UdpBind(IntPtr handle, IPEndPoint endPoint, bool reuseAddress, bool dualStack)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(endPoint != null);

            GetSocketAddress(endPoint, out sockaddr addr);

            uint flag = 0;
            if (reuseAddress)
            {
                flag = (uint)uv_udp_flags.UV_UDP_REUSEADDR;
            }
            else
            {
                if (!dualStack 
                    && endPoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    flag = (uint)uv_udp_flags.UV_UDP_IPV6ONLY;
                }
            }

            int result = uv_udp_bind(handle, ref addr, flag);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static IPEndPoint UdpGetSocketName(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int namelen = Marshal.SizeOf<sockaddr>();
            int result = uv_udp_getsockname(handle, out sockaddr sockaddr, ref namelen);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }

            return sockaddr.GetIPEndPoint();
        }

        internal static void UpdSetMulticastLoopback(IntPtr handle, bool value)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_udp_set_multicast_loop(handle, value ? 1 : 0);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void UdpSetMulticastTtl(IntPtr handle, int value)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_udp_set_multicast_ttl(handle, value);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void UdpSetTtl(IntPtr handle, int value)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_udp_set_ttl(handle, value);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void UdpSetBroadcast(IntPtr handle, bool value)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_udp_set_broadcast(handle, value ? 1 : 0);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_udp_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_udp_bind(IntPtr handle, ref sockaddr sockaddr, uint flags);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_udp_getsockname(IntPtr handle, out sockaddr sockaddr, ref int namelen);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_udp_set_multicast_loop(IntPtr handle, int on /* – 1 for on, 0 for off */);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_udp_set_multicast_ttl(IntPtr handle, int ttl /* – 1 through 255 */);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_udp_set_ttl(IntPtr handle, int ttl /* – 1 through 255 */);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_udp_set_broadcast(IntPtr handle, int on /* – 1 for on, 0 for off */);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_udp_set_multicast_interface(IntPtr handle, string interface_addr);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_udp_set_membership(IntPtr handle, string multicast_addr, string interface_addr, uv_membership membership);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_udp_try_send(IntPtr handle, uv_buf_t[] bufs, int nbufs, ref sockaddr addr);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_udp_recv_start(IntPtr handle, uv_alloc_cb alloc_cb, uv_udp_recv_cb recv_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_udp_recv_stop(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_udp_send(IntPtr req, IntPtr handle, uv_buf_t[] bufs, int nbufs, ref sockaddr addr, uv_watcher_cb cb);

        #endregion Udp

        #region Pipe

        internal static void PipeBind(IntPtr handle, string name)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(!string.IsNullOrEmpty(name));

            int result = uv_pipe_bind(handle, name);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void PipeConnect(IntPtr requestHandle, IntPtr handle, string remoteName)
        {
            Contract.Requires(requestHandle != IntPtr.Zero);
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(!string.IsNullOrEmpty(remoteName));

            uv_pipe_connect(requestHandle, handle, remoteName, WatcherRequest.WatcherCallback);
        }

        internal static string PipeGetSocketName(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            string socketName;
            IntPtr buf = IntPtr.Zero; 
            try
            {
                buf = Marshal.AllocHGlobal(NameBufferSize);
                var length = (IntPtr)NameBufferSize;

                int result = uv_pipe_getsockname(handle, buf, ref length);
                if (result < 0)
                {
                    throw CreateError((uv_err_code)result);
                }

                socketName = Marshal.PtrToStringAnsi(buf, length.ToInt32());
            }
            finally 
            {
                if (buf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buf);
                }
            }

            return socketName;
        }

        internal static string PipeGetPeerName(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            string peerName;
            IntPtr buf = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal(NameBufferSize);
                var length = (IntPtr)NameBufferSize;

                int result = uv_pipe_getpeername(handle, buf, ref length);
                if (result < 0)
                {
                    throw CreateError((uv_err_code)result);
                }

                peerName = Marshal.PtrToStringAnsi(buf, length.ToInt32());
            }
            finally
            {
                if (buf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buf);
                }
            }

            return peerName;
        }

        internal static void PipePendingInstances(IntPtr handle, int count)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(count > 0);

            uv_pipe_pending_instances(handle, count);
        }

        internal static int PipePendingCount(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            return uv_pipe_pending_count(handle);
        }

        internal static uv_handle_type PipePendingType(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            return (uv_handle_type)uv_pipe_pending_type(handle);
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int uv_pipe_init(IntPtr loop, IntPtr handle, int ipc);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int uv_pipe_bind(IntPtr handle, string name);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void uv_pipe_connect(IntPtr req, IntPtr handle, string name, uv_watcher_cb connect_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_pipe_getsockname(IntPtr handle, IntPtr buffer, ref IntPtr size);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_pipe_getpeername(IntPtr handle, IntPtr buffer, ref IntPtr size);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern void uv_pipe_pending_instances(IntPtr handle, int count);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_pipe_pending_count(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_pipe_pending_type(IntPtr handle);

        #endregion Pipe

        #region TCP

        internal static void TcpSetNoDelay(IntPtr handle, bool value)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_tcp_nodelay(handle, value ? 1 : 0);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void TcpSetKeepAlive(IntPtr handle, bool value, int delay)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(delay >= 0);

            int result = uv_tcp_keepalive(handle, value ? 1 : 0, delay);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void TcpSimultaneousAccepts(IntPtr handle, bool value)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_tcp_simultaneous_accepts(handle, value ? 1 : 0);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void TcpBind(IntPtr handle, IPEndPoint endPoint, bool dualStack /* Both IPv4 & IPv6 */)
        {
            Contract.Requires(endPoint != null);
            Contract.Requires(handle != IntPtr.Zero);

            GetSocketAddress(endPoint, out sockaddr addr);

            int result = uv_tcp_bind(handle, ref addr, (uint)(dualStack ? 1 : 0));
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void TcpConnect(IntPtr requestHandle, IntPtr handle, IPEndPoint endPoint)
        {
            Contract.Requires(endPoint != null);
            Contract.Requires(requestHandle != IntPtr.Zero);
            Contract.Requires(handle != IntPtr.Zero);

            GetSocketAddress(endPoint, out sockaddr addr);

            int result = uv_tcp_connect(requestHandle, handle, ref addr, WatcherRequest.WatcherCallback);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static IPEndPoint TcpGetSocketName(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int namelen = Marshal.SizeOf<sockaddr>();
            uv_tcp_getsockname(handle, out sockaddr sockaddr, ref namelen);

            return sockaddr.GetIPEndPoint();
        }

        internal static IPEndPoint TcpGetPeerName(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int namelen = Marshal.SizeOf<sockaddr>();
            int result = uv_tcp_getpeername(handle, out sockaddr sockaddr, ref namelen);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }

            return sockaddr.GetIPEndPoint();
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_connect(IntPtr req, IntPtr handle, ref sockaddr sockaddr, uv_watcher_cb connect_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_bind(IntPtr handle, ref sockaddr sockaddr, uint flags);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_getsockname(IntPtr handle, out sockaddr sockaddr, ref int namelen);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_getpeername(IntPtr handle, out sockaddr name, ref int namelen);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_nodelay(IntPtr handle, int enable);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_keepalive(IntPtr handle, int enable, int delay);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_simultaneous_accepts(IntPtr handle, int enable);

        #endregion TCP

        #region Tty

        internal static void TtySetMode(IntPtr handle, TtyMode ttyMode)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_tty_set_mode(handle, (uv_tty_mode_t)ttyMode);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void TtyResetMode()
        {
            // To be called when the program exits. 
            // Resets TTY settings to default values for the next process to take over.
            int result = uv_tty_reset_mode();
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void TtyWindowSize(IntPtr handle, out int width, out int height)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_tty_get_winsize(handle, out width, out height);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tty_init(IntPtr loopHandle, IntPtr handle, int fd, int readable);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tty_set_mode(IntPtr handle, uv_tty_mode_t mode);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tty_reset_mode();

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tty_get_winsize(IntPtr handle, out int width, out int height);

        #endregion Tty

        #region Timer

        internal static void Start(IntPtr handle, long timeout, long repeat)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(timeout >= 0);
            Contract.Requires(repeat >= 0);

            int result = uv_timer_start(handle, WorkHandle.WorkCallback, timeout, repeat);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void Again(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_timer_again(handle);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        internal static void SetTimerRepeat(IntPtr handle, long repeat)
        {
            Contract.Requires(handle != IntPtr.Zero);

            uv_timer_set_repeat(handle, repeat);
        }

        internal static long GetTimerRepeat(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            return uv_timer_get_repeat(handle);
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_timer_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_timer_start(IntPtr handle, uv_work_cb work_cb, long timeout, long repeat);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_timer_stop(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_timer_again(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern void uv_timer_set_repeat(IntPtr handle, long repeat);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern long uv_timer_get_repeat(IntPtr handle);

        #endregion Timer

        #region Prepare

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_prepare_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_prepare_start(IntPtr handle, uv_work_cb prepare_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern void uv_prepare_stop(IntPtr handle);

        #endregion Prepare

        #region Check

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_check_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_check_start(IntPtr handle, uv_work_cb check_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_check_stop(IntPtr handle);

        #endregion Check

        #region Idle

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_idle_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_idle_start(IntPtr handle, uv_work_cb check_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_idle_stop(IntPtr handle);

        #endregion Idle

        #region Async

        internal static void Send(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            int result = uv_async_send(handle);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_async_init(IntPtr loopHandle, IntPtr handle, uv_work_cb async_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_async_send(IntPtr handle);

        #endregion Async

        #region Poll

        // Calling uv_poll_start() on a handle that is already active is fine. 
        // Doing so will update the events mask that is being watched for.
        internal static void PollStart(IntPtr handle, PollMask mask)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_poll_start(handle, (int)mask, Poll.PollCallback);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_poll_init(IntPtr loop, IntPtr handle, int fd);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_poll_init_socket(IntPtr loop, IntPtr handle, IntPtr socket);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_poll_start(IntPtr handle, int events, uv_poll_cb cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_poll_stop(IntPtr handle);

        #endregion Poll

        #region Signal

        internal static void SignalStart(IntPtr handle, int signum)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = uv_signal_start(handle, Signal.SignalCallback, signum);
            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_signal_init(IntPtr loop, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_signal_start(IntPtr handle, uv_watcher_cb cb, int signum);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_signal_stop(IntPtr handle);

        #endregion Signal

        #region Common

        internal static void GetSocketAddress(IPEndPoint endPoint, out sockaddr addr)
        {
            Contract.Requires(endPoint != null);

            string ip = endPoint.Address.ToString();
            int result;
            switch (endPoint.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    result = uv_ip4_addr(ip, endPoint.Port, out addr);
                    break;
                case AddressFamily.InterNetworkV6:
                    result = uv_ip6_addr(ip, endPoint.Port, out addr);
                    break;
                default:
                    throw new NotSupportedException(
                        $"End point {endPoint} is not supported, expecting InterNetwork/InterNetworkV6.");
            }

            if (result < 0)
            {
                throw CreateError((uv_err_code)result);
            }
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_ip4_addr(string ip, int port, out sockaddr address);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_ip6_addr(string ip, int port, out sockaddr address);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr uv_handle_size(uv_handle_type handleType);

        #endregion Common
    }
}
