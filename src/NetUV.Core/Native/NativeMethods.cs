// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable InconsistentNaming
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace NetUV.Core.Native
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using NetUV.Core.Logging;

    #region uv_err_t

    enum uv_err_code
    {
        UV_OK = 0,
        UV_E2BIG,
        UV_EACCES = -4092,
        UV_EADDRINUSE = -4091,
        UV_EADDRNOTAVAIL,
        UV_EAFNOSUPPORT,
        UV_EAGAIN = -4088,
        UV_EAI_ADDRFAMILY,
        UV_EAI_AGAIN,
        UV_EAI_BADFLAGS,
        UV_EAI_BADHINTS,
        UV_EAI_CANCELED,
        UV_EAI_FAIL,
        UV_EAI_FAMILY,
        UV_EAI_MEMORY,
        UV_EAI_NODATA,
        UV_EAI_NONAME,
        UV_EAI_OVERFLOW,
        UV_EAI_PROTOCOL,
        UV_EAI_SERVICE,
        UV_EAI_SOCKTYPE,
        UV_EALREADY,
        UV_EBADF = -4083,
        UV_EBUSY = -4082,
        UV_ECANCELED = -4081,
        UV_ECHARSET,
        UV_ECONNABORTED,
        UV_ECONNREFUSED = -4078,
        UV_ECONNRESET,
        UV_EDESTADDRREQ,
        UV_EEXIST,
        UV_EFAULT,
        UV_EFBIG,
        UV_EHOSTUNREACH,
        UV_EINTR,
        UV_EINVAL = -4071,
        UV_EIO,
        UV_EISCONN,
        UV_EISDIR,
        UV_ELOOP,
        UV_EMFILE,
        UV_EMSGSIZE = -4065,
        UV_ENAMETOOLONG,
        UV_ENETDOWN,
        UV_ENETUNREACH = -4062,
        UV_ENFILE,
        UV_ENOBUFS,
        UV_ENODEV,
        UV_ENOENT = -4058,
        UV_ENOMEM,
        UV_ENONET,
        UV_ENOPROTOOPT,
        UV_ENOSPC,
        UV_ENOSYS = -4054,
        UV_ENOTCONN = -4053,
        UV_ENOTDIR,
        UV_ENOTEMPTY,
        UV_ENOTSOCK,
        UV_ENOTSUP,
        UV_EPERM,
        UV_EPIPE = -4047,
        UV_EPROTO,
        UV_EPROTONOSUPPORT,
        UV_EPROTOTYPE,
        UV_ERANGE,
        UV_EROFS,
        UV_ESHUTDOWN,
        UV_ESPIPE,
        UV_ESRCH,
        UV_ETIMEDOUT,
        UV_ETXTBSY,
        UV_EXDEV,
        UV_UNKNOWN,
        UV_EOF = -4095,
        UV_ENXIO,
        UV_EMLINK,
    }

    #endregion

    #region Native Callbacks

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void uv_close_cb(IntPtr conn);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void uv_work_cb(IntPtr watcher);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void uv_watcher_cb(IntPtr watcher, int status);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void uv_poll_cb(IntPtr handle, int status, int events);

    #endregion Native Callbacks

    static partial class NativeMethods
    {
        const string LibraryName = "libuv";
        static readonly ILog Log = LogFactory.ForContext(LibraryName);

        #region Common

        static void Invoke(Func<IntPtr, int> function, IntPtr handle)
        {
            Contract.Requires(function != null);
            Contract.Requires(handle != IntPtr.Zero);

            int result;
            try
            {
                result = function(handle);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to invoke native method on handle {handle}", exception);
                throw;
            }

            ThrowIfError(result);
        }

        static void Invoke<T1>(Func<IntPtr, T1, int> function, IntPtr handle, T1 arg1)
        {
            Contract.Requires(function != null);
            Contract.Requires(handle != IntPtr.Zero);

            int result;
            try
            {
                result = function(handle, arg1);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to invoke native method on handle {handle}", exception);
                throw;
            }

            ThrowIfError(result);
        }

        static void Invoke<T1, T2>(Func<IntPtr, T1, T2, int> function, IntPtr handle, T1 arg1, T2 arg2)
        {
            Contract.Requires(function != null);
            Contract.Requires(handle != IntPtr.Zero);

            int result;
            try
            {
                result = function(handle, arg1, arg2);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to invoke native method on handle {handle}", exception);
                throw;
            }

            ThrowIfError(result);
        }

        static void Invoke<T1, T2, T3>(Func<IntPtr, T1, T2, T3, int> function, IntPtr handle, T1 arg1, T2 arg2, T3 arg3)
        {
            Contract.Requires(function != null);
            Contract.Requires(handle != IntPtr.Zero);

            int result;
            try
            {
                result = function(handle, arg1, arg2, arg3);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to invoke native method on handle {handle}", exception);
                throw;
            }

            ThrowIfError(result);
        }

        static void Invoke<T1, T2, T3, T4>(Func<IntPtr, T1, T2, T3, T4, int> function, IntPtr handle, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            Contract.Requires(function != null);
            Contract.Requires(handle != IntPtr.Zero);

            int result;
            try
            {
                result = function(handle, arg1, arg2, arg3, arg4);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to invoke native method on handle {handle}", exception);
                throw;
            }

            ThrowIfError(result);
        }

        static T InvokeFunction<T>(Func<IntPtr, T> function, IntPtr handle)
        {
            Contract.Requires(function != null);
            Contract.Requires(handle != IntPtr.Zero);

            try
            {
                return function(handle);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to invoke native method on handle {handle}", exception);
                throw;
            }
        }

        static T InvokeFunction<T1, T>(Func<IntPtr, T1, T> function, IntPtr handle, T1 arg1)
        {
            Contract.Requires(function != null);
            Contract.Requires(handle != IntPtr.Zero);

            try
            {
                return function(handle, arg1);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to invoke native method on handle {handle}", exception);
                throw;
            }
        }

        static void InvokeAction(Action<IntPtr> action, IntPtr handle)
        {
            Contract.Requires(action != null);
            Contract.Requires(handle != IntPtr.Zero);

            try
            {
                action(handle);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to invoke native method on handle {handle}", exception);
                throw;
            }
        }

        static void InvokeAction<T1>(Action<IntPtr, T1> action, IntPtr handle, T1 arg1)
        {
            Contract.Requires(action != null);
            Contract.Requires(handle != IntPtr.Zero);

            try
            {
                action(handle, arg1);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to invoke native method on handle {handle}", exception);
                throw;
            }
        }

        static void InvokeAction<T1, T2>(Action<IntPtr, T1, T2> action, IntPtr handle, T1 arg1, T2 arg2)
        {
            Contract.Requires(action != null);
            Contract.Requires(handle != IntPtr.Zero);

            try
            {
                action(handle, arg1, arg2);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to invoke native method on handle {handle}", exception);
                throw;
            }
        }

        internal static bool IsHandleActive(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            int result = InvokeFunction(uv_is_active, handle);
            return result != 0;
        }

        internal static void AddReference(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            InvokeAction(uv_ref, handle);
        }

        internal static void ReleaseReference(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            InvokeAction(uv_unref, handle);
        }

        internal static bool HadReference(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            int result = InvokeFunction(uv_has_ref, handle);
            return result != 0;
        }

        internal static void CloseHandle(IntPtr handle, uv_close_cb callback)
        {
            if (handle == IntPtr.Zero 
                || callback == null)
            {
                return;
            }

            int result = InvokeFunction(uv_is_closing, handle);
            if (result == 0)
            {
                InvokeAction(uv_close, handle, callback);
            }
        }

        internal static bool IsHandleClosing(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            return uv_is_closing(handle) != 0;
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern void uv_close(IntPtr handle, uv_close_cb close_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_is_closing(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern void uv_ref(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern void uv_unref(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_has_ref(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_is_active(IntPtr handle);

        #endregion Common

        #region Error

        static void ThrowIfError(int code)
        {
            if (code >= 0) // OK
            {
                return;
            }

            throw CreateError((uv_err_code)code);
        }

        internal static OperationException CreateError(uv_err_code error)
        {
            string name = GetErrorName(error);
            string description = GetErrorDescription(error);
            return new OperationException((int)error, name, description);
        }

        static string GetErrorDescription(uv_err_code code)
        {
            IntPtr ptr = uv_strerror(code);
            return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
        }

        static string GetErrorName(uv_err_code code)
        {
            IntPtr ptr = uv_err_name(code);
            return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr uv_strerror(uv_err_code err);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr uv_err_name(uv_err_code err);

        #endregion Error

        #region Version

        internal static Version GetVersion()
        {
            uint version = uv_version();
            int major = (int)(version & 0xFF0000) >> 16;
            int minor = (int)(version & 0xFF00) >> 8;
            int patch = (int)(version & 0xFF);

            return new Version(major, minor, patch);
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint uv_version();

        #endregion Version
    }
}
