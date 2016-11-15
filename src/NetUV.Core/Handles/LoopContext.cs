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
            IntPtr handle = Marshal.AllocHGlobal(size);

            this.Handle = handle;
            NativeMethods.InitializeLoop(handle);

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

            // Close loop
            int retry = 0;
            while (retry < 10)
            {
                try
                {
                    // Force close all active handles before close the loop
                    NativeMethods.WalkLoop(handle, WalkCallback);
                    Log.Info($"Loop {handle} walk all handles completed.");

                    // Loop.Run here actually blocks in some intensive situitions 
                    // and it is highly unpredictable. For now, we rely on the users 
                    // to do the right things before disposing the loop, 
                    // e.g. close all handles before calling this.
                    // NativeMethods.RunLoop(handle, uv_run_mode.UV_RUN_DEFAULT);
                    if (NativeMethods.CloseLoop(handle))
                    {
                        break;
                    }
                }
                catch (Exception exception)
                {
                    Log.Warn($"Loop {handle} error attempt to run loop once before closing. {exception}");
                }

                retry++;
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
            Marshal.FreeHGlobal(handle);
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
