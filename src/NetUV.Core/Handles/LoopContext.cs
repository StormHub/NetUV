// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Runtime.InteropServices;
    using NetUV.Core.Native;

    sealed unsafe class LoopContext : NativeHandle
    {
        static readonly uv_walk_cb WalkCallback = OnWalkCallback;

        public LoopContext()
        {
            int size = NativeMethods.GetLoopSize();
            IntPtr handle = Marshal.AllocCoTaskMem(size);

            this.Handle = handle;
            try
            {
                NativeMethods.InitializeLoop(handle);
            }
            catch
            {
                Marshal.FreeCoTaskMem(handle);
                throw;
            }

            GCHandle gcHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            ((uv_loop_t*)handle)->data = GCHandle.ToIntPtr(gcHandle);

            Log.Info($"Loop {handle} allocated.");
        }

        public bool IsAlive =>
            this.IsValid
            && NativeMethods.IsLoopAlive(this.Handle);

        public long Now
        {
            get
            {
                this.Validate();
                return NativeMethods.LoopNow(this.Handle);
            }
        }

        public long NowInHighResolution
        {
            get
            {
                this.Validate();
                return NativeMethods.LoopNowInHighResolution(this.Handle);
            }
        }

        public int ActiveHandleCount() =>
            this.IsValid
                ? (int)((uv_loop_t*)this.Handle)->active_handles
                : 0;

        public void UpdateTime()
        {
            this.Validate();
            NativeMethods.LoopUpdateTime(this.Handle);
        }

        internal int GetBackendTimeout()
        {
            this.Validate();
            return NativeMethods.GetBackendTimeout(this.Handle);
        }

        internal int Run(uv_run_mode mode)
        {
            this.Validate();
            return NativeMethods.RunLoop(this.Handle, mode);
        }

        public void Stop()
        {
            this.Validate();
            NativeMethods.StopLoop(this.Handle);
        }

        protected override void CloseHandle()
        {
            IntPtr handle = this.Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            // Get gc handle before close loop
            IntPtr pHandle = ((uv_loop_t*)handle)->data;

            // Fully close the loop, similar to 
            //https://github.com/libuv/libuv/blob/v1.x/test/task.h#L190

            int count = 0;
            int result;
            while (true)
            {
                Log.Debug($"Loop {handle} walking handles, count = {count}.");
                NativeMethods.WalkLoop(handle, WalkCallback);

                Log.Debug($"Loop {handle} running default to call close callbacks, count = {count}.");
                NativeMethods.RunLoop(this.Handle, uv_run_mode.UV_RUN_DEFAULT);

                result = NativeMethods.CloseLoop(handle);
                Log.Debug($"Loop {handle} close result = {result}, count = {count}.");
                if (result == 0)
                {
                    break;
                }
                else
                {
                    if (Log.IsTraceEnabled)
                    {
                        OperationException error = NativeMethods.CreateError((uv_err_code)result);
                        Log.TraceFormat($"Loop {handle} close error {error}");
                    }
                }
                count++;
                if (count >= 20)
                {
                    Log.Warn($"Loop {handle} close all handles limit 20 times exceeded.");
                    break;
                }
            }

            Log.Info($"Loop {handle} closed.");

            // Free GCHandle
            if (pHandle != IntPtr.Zero)
            {
                GCHandle nativeHandle = GCHandle.FromIntPtr(pHandle);
                if (nativeHandle.IsAllocated)
                {
                    nativeHandle.Free();
                    ((uv_loop_t*)handle)->data = IntPtr.Zero;
                    Log.Info($"Loop {handle} GCHandle released.");
                }
            }

            // Release memory
            Marshal.FreeCoTaskMem(handle);
            this.Handle = IntPtr.Zero;
            Log.Info($"Loop {handle} memory released.");
        }

        static void OnWalkCallback(IntPtr handle, IntPtr loopHandle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                var target = HandleContext.GetTarget<IDisposable>(handle);
                target?.Dispose();
                Log.Info($"Loop {loopHandle} walk callback disposed {handle} {target?.GetType()}");
            }
            catch (Exception exception)
            {
                Log.Warn($"Loop {loopHandle} Walk callback attempt to close handle {handle} failed. {exception}");
            }
        }
    }
}
