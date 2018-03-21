// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace NetUV.Core.Requests
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using NetUV.Core.Native;

    sealed unsafe class RequestContext : NativeHandle
    {
        readonly uv_req_type requestType;
        readonly int handleSize;

        internal RequestContext(
            uv_req_type requestType,
            int size,
            ScheduleRequest target)
        {
            Debug.Assert(size >= 0);
            Debug.Assert(target != null);

            this.handleSize = NativeMethods.GetSize(requestType);
            int totalSize = this.handleSize + size;
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
            Debug.Assert(initializer != null);
            Debug.Assert(target != null);

            this.handleSize = NativeMethods.GetSize(requestType);
            IntPtr handle = Marshal.AllocCoTaskMem(this.handleSize);

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

        internal int HandleSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.handleSize;
        }

        internal static T GetTarget<T>(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero);

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
