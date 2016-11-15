// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using NetUV.Core.Native;

    sealed unsafe class HandleContext : NativeHandle
    {
        static readonly uv_close_cb CloseCallback = OnCloseHandle;
        readonly uv_handle_type handleType;

        internal HandleContext(
            uv_handle_type handleType, 
            Action<IntPtr> initializer, 
            ScheduleHandle target)
        {
            Contract.Requires(initializer != null);
            Contract.Requires(target != null);

            int size = NativeMethods.GetSize(handleType);
            IntPtr handle = Marshal.AllocHGlobal(size);

            initializer(handle);

            GCHandle gcHandle = GCHandle.Alloc(target, GCHandleType.Normal);
            ((uv_handle_t*)handle)->data = GCHandle.ToIntPtr(gcHandle);

            this.Handle = handle;
            this.handleType = handleType;

            Log.InfoFormat("{0} {1} allocated.", handleType, handle);
        }

        internal bool IsActive => this.IsValid 
            && NativeMethods.IsHandleActive(this.Handle);

        internal bool IsClosing => this.IsValid 
            && NativeMethods.IsHandleClosing(this.Handle);

        internal void AddReference()
        {
            this.Validate();
            NativeMethods.AddReference(this.Handle);
        }

        internal void ReleaseReference()
        {
            this.Validate();
            NativeMethods.ReleaseReference(this.Handle);
        }

        internal bool HasReference()
        {
            this.Validate();
            return NativeMethods.HadReference(this.Handle);
        }

        protected override void CloseHandle()
        {
            IntPtr handle = this.Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.CloseHandle(handle, CloseCallback);
            Log.InfoFormat("{0} {1} closed, releasing resources pending.", this.handleType, handle);
        }

        internal static T GetTarget<T>(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"{nameof(handle)} cannot be empty.");
            }

            IntPtr inernalHandle = ((uv_handle_t*)handle)->data;
            GCHandle gcHandle = GCHandle.FromIntPtr(inernalHandle);

            if (!gcHandle.IsAllocated)
            {
                return default(T);
            }

            return (T)gcHandle.Target;
        }

        static void OnCloseHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            ScheduleHandle scheduleHandle = null;

            // Get gc handle first
            IntPtr pHandle = ((uv_handle_t*)handle)->data;
            if (pHandle != IntPtr.Zero)
            {
                GCHandle nativeHandle = GCHandle.FromIntPtr(pHandle);
                if (nativeHandle.IsAllocated)
                {
                    scheduleHandle = nativeHandle.Target as ScheduleHandle;
                    nativeHandle.Free();

                    ((uv_handle_t*)handle)->data = IntPtr.Zero;
                    Log.TraceFormat("{0} {1} GCHandle released.", scheduleHandle?.HandleType, handle);
                }
            }

            // Release memory
            Marshal.FreeHGlobal(handle);
            scheduleHandle?.OnHandleClosed();
            Log.InfoFormat("{0} {1} memory and GCHandle released.", scheduleHandle?.HandleType, handle);
        }
    }
}
