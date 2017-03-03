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

        public readonly long tv_sec;
        public readonly long tv_nsec;

        public static explicit operator DateTime(uv_timespec_t timespec)
        {
            if (timespec.tv_sec <= 0)
            {
                return StartDateTime;
            }


            try
            {
                return StartDateTime
                    .AddSeconds(timespec.tv_sec)
                    .AddTicks(timespec.tv_nsec / 100);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Invalid time values, sometimes happens on Window
                // for last change time.
                return StartDateTime;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_stat_t
    {
        public readonly long st_dev;
        public readonly long st_mode;
        public readonly long st_nlink;
        public readonly long st_uid;
        public readonly long st_gid;
        public readonly long st_rdev;
        public readonly long st_ino;
        public readonly long st_size;
        public readonly long st_blksize;
        public readonly long st_blocks;
        public readonly long st_flags;
        public readonly long st_gen;
        public readonly uv_timespec_t st_atim;
        public readonly uv_timespec_t st_mtim;
        public readonly uv_timespec_t st_ctim;
        public readonly uv_timespec_t st_birthtim;

        public static explicit operator FileStatus(uv_stat_t stat) => new FileStatus(stat);
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
                OperationException error = CheckError(result);
                if (error != null)
                {
                    throw error;
                }

                path = Marshal.PtrToStringAnsi(buf, length.ToInt32());
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

            int result = uv_fs_poll_start(handle, FSPoll.FSPollCallback, path, interval);
            OperationException error = CheckError(result);
            if (error != null)
            {
                throw error;
            }
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

            int result = uv_fs_event_start(handle, FSEvent.FSEventCallback, path, (int)mask);
            OperationException error = CheckError(result);
            if (error != null)
            {
                throw error;
            }
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
                OperationException error = CheckError(result);
                if (error != null)
                {
                    throw error;
                }

                path = Marshal.PtrToStringAnsi(buf, length.ToInt32());
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
