﻿// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Requests
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using NetUV.Core.Native;

    sealed unsafe class RequestContext : NativeHandle
    {
        readonly uv_req_type requestType;

        internal RequestContext(
            uv_req_type requestType,
            int size,
            ScheduleRequest target)
        {
            Contract.Requires(size >= 0);
            Contract.Requires(target != null);

            int totalSize = NativeMethods.GetSize(requestType);
            totalSize += size;
            IntPtr handle = Marshal.AllocCoTaskMem(totalSize);

            GCHandle gcHandle = GCHandle.Alloc(target, GCHandleType.Normal);
            *(IntPtr*)handle = GCHandle.ToIntPtr(gcHandle);

            this.Handle = handle;
            this.requestType = requestType;
            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0} {1} allocated.", requestType, handle);
            }
        }

        internal RequestContext(
            uv_req_type requestType,
            Action<IntPtr> initializer,
            ScheduleRequest target)
        {
            Contract.Requires(initializer != null);
            Contract.Requires(target != null);

            int size = NativeMethods.GetSize(requestType);
            IntPtr handle = Marshal.AllocCoTaskMem(size);

            try
            {
                initializer(handle);
            }
            catch
            {
                Marshal.FreeCoTaskMem(handle);
                throw;
            }

            GCHandle gcHandle = GCHandle.Alloc(target, GCHandleType.Normal);
            *(IntPtr*)handle = GCHandle.ToIntPtr(gcHandle);

            this.Handle = handle;
            this.requestType = requestType;
            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0} {1} allocated.", requestType, handle);
            }
        }

        internal static T GetTarget<T>(IntPtr handle)
        {
            Contract.Requires(handle != IntPtr.Zero);

            IntPtr internalHandle = ((uv_req_t*)handle)->data;
            if (internalHandle != IntPtr.Zero)
            {
                GCHandle gcHandle = GCHandle.FromIntPtr(internalHandle);
                if (gcHandle.IsAllocated)
                {
                    return (T)gcHandle.Target;
                }
            }

            return default(T);
        }

        protected override void CloseHandle()
        {
            IntPtr handle = this.Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            IntPtr pHandle = ((uv_req_t*)handle)->data;

            // Free GCHandle
            if (pHandle != IntPtr.Zero)
            {
                GCHandle nativeHandle = GCHandle.FromIntPtr(pHandle);
                if (nativeHandle.IsAllocated)
                {
                    nativeHandle.Free();
                    ((uv_req_t*)handle)->data = IntPtr.Zero;
                    if (Log.IsDebugEnabled)
                    {
                        Log.DebugFormat("{0} {1} GCHandle released.", this.requestType, handle);
                    }
                }
            }

            // Release memory
            Marshal.FreeCoTaskMem(handle);
            this.Handle = IntPtr.Zero;

            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0} {1} memory released.", this.requestType, handle);
            }
        }
    }
}
