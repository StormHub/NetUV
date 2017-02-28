// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Logging;
    using NetUV.Core.Native;

    public abstract class ScheduleHandle : IDisposable
    {
        internal static readonly ILog Log = LogFactory.ForContext<ScheduleHandle>();

        readonly HandleContext handle;
        Action<ScheduleHandle> closeCallback;

        internal ScheduleHandle(
            LoopContext loop, 
            uv_handle_type handleType, 
            params object[] args)
        {
            Contract.Requires(loop != null);

            HandleContext initialHandle = NativeMethods.Initialize(loop.Handle, handleType, this, args);
            if (initialHandle == null)
            {
                throw new InvalidOperationException(
                    $"Initialize {handleType} for loop {loop.Handle} failed.");
            }

            this.handle = initialHandle;
            this.HandleType = handleType;
        }

        public bool IsActive => this.handle.IsActive;

        public bool IsClosing => this.handle.IsClosing;

        public bool IsValid => this.handle.IsValid;

        public object UserToken { get; set; }

        internal IntPtr InternalHandle => this.handle.Handle;

        internal uv_handle_type HandleType { get; }

        internal void OnHandleClosed()
        {
            try
            {
                this.handle.SetHandleAsInvalid();
                this.closeCallback?.Invoke(this);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.HandleType} close handle callback error.", exception);
            }
            finally
            {
                this.closeCallback = null;
                this.UserToken = null;
            }
        } 

        internal void Validate() => this.handle.Validate();

        public unsafe bool TryGetLoop(out Loop loop)
        {
            loop = null;
            try
            {
                IntPtr nativeHandle = this.InternalHandle;
                if (nativeHandle == IntPtr.Zero)
                {
                    return false;
                }

                IntPtr loopHandle = ((uv_handle_t*)nativeHandle)->loop;
                if (loopHandle != IntPtr.Zero)
                {
                    loop = HandleContext.GetTarget<Loop>(loopHandle);
                }

                return loop != null;
            }
            catch (Exception exception)
            {
                Log.Warn($"{this.HandleType} Failed to get loop.", exception);
                return false;
            }
        }

        protected internal void CloseHandle(Action<ScheduleHandle> handler = null)
        {
            try
            {
                if (!this.IsValid)
                {
                    return;
                }

                this.closeCallback = handler;
                this.Close();
                this.handle.Dispose();
            }
            catch (Exception exception)
            {
                Log.Error($"{nameof(ScheduleHandle)} {this.HandleType} Failed to close handle.", exception);
                throw;
            }
        }

        protected abstract void Close();

        protected void StopHandle()
        {
            if (!this.IsValid)
            {
                return;
            }

            NativeMethods.Stop(this.HandleType, this.handle.Handle);
        }

        public void AddReference()
        {
            if (!this.IsValid)
            {
                return;
            }

            this.handle.AddReference();
        }

        public void RemoveReference()
        {
            if (!this.IsValid)
            {
                return;
            }

            this.handle.ReleaseReference();
        }

        public bool HasReference() => this.IsValid && this.handle.HasReference();

        public void Dispose()
        {
            try
            {
                this.CloseHandle();
            }
            catch (Exception exception)
            {
                Log.Warn($"{this.handle} Failed to close and releasing resources.", exception);
            }
        }
    }
}
