// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable InconsistentNaming
namespace NetUV.Core.Native
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using NetUV.Core.Handles;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void uv_fs_event_cb(IntPtr handle, string filename, int events, int status);

    [StructLayout(LayoutKind.Sequential)]
    struct uv_timespec_t
    {
        static readonly DateTime StartDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public IntPtr tv_sec;
        public IntPtr tv_nsec;

        public static implicit operator DateTime(uv_timespec_t timespec) => 
            StartDateTime.AddTicks(
                TimeSpan.TicksPerSecond * (long)timespec.tv_sec 
                + (long)timespec.tv_nsec / 100);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_stat_t
    {
        public ulong st_dev;
        public ulong st_mode;
        public ulong st_nlink;
        public ulong st_uid;
        public ulong st_gid;
        public ulong st_rdev;
        public ulong st_ino;
        public ulong st_size;
        public ulong st_blksize;
        public ulong st_blocks;
        public ulong st_flags;
        public ulong st_gen;
        public uv_timespec_t st_atim;
        public uv_timespec_t st_mtim;
        public uv_timespec_t st_ctim;
        public uv_timespec_t st_birthtim;


        public static implicit operator FileStatus(uv_stat_t stat) => new FileStatus(stat);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void uv_fs_poll_cb(IntPtr handle, int status, ref uv_stat_t prev, ref uv_stat_t curr);

    static partial class NativeMethods
    {
        const int FileNameBufferSize = 2048;

        #region FSPoll

        internal static string FSPollGetPath(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            string path;
            IntPtr buf = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal(FileNameBufferSize);
                var length = (IntPtr)FileNameBufferSize;

                int result = uv_fs_poll_getpath(handle, buf, ref length);
                ThrowIfError(result);

                path = Marshal.PtrToStringUni(buf, length.ToInt32());
            }
            finally
            {
                if (buf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buf);
                }
            }

            return path;
        }

        internal static void FSPollStart(IntPtr handle, string path, int interval)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(!string.IsNullOrEmpty(path));
            Contract.Requires(interval > 0);

            Invoke(uv_fs_poll_start, handle, FSPoll.FSPollCallback, path, interval);
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_fs_poll_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_fs_poll_stop(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_fs_poll_start(IntPtr handle, uv_fs_poll_cb cb, string path, int interval);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_fs_poll_getpath(IntPtr handle, IntPtr buffer, ref IntPtr size);

        #endregion FSPoll

        #region FSEvent

        internal static void FSEventStart(IntPtr handle, string path, FSEventMask mask)
        {
            Contract.Requires(handle != IntPtr.Zero);
            Contract.Requires(!string.IsNullOrEmpty(path));

            Invoke(uv_fs_event_start, handle, FSEvent.FSEventCallback, path, (int)mask);
        }

        internal static string FSEventGetPath(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            string path;
            IntPtr buf = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal(FileNameBufferSize);
                var length = (IntPtr)FileNameBufferSize;

                int result = uv_fs_event_getpath(handle, buf, ref length);
                ThrowIfError(result);

                path = Marshal.PtrToStringUni(buf, length.ToInt32());
            }
            finally
            {
                if (buf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buf);
                }
            }

            return path;
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_fs_event_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_fs_event_start(IntPtr handle, uv_fs_event_cb cb, string path, int flags);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_fs_event_stop(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_fs_event_getpath(IntPtr handle, IntPtr buffer, ref IntPtr size);

        #endregion FSEvent
    }
}
