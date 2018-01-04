// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace NetUV.Core.Native
{
    using System;
    using System.Runtime.CompilerServices;
    using NetUV.Core.Buffers;
    using NetUV.Core.Logging;

    abstract class NativeHandle : IDisposable
    {
        protected static readonly ILog Log = LogFactory.ForContext<NativeHandle>();
        IntPtr handle;

        protected NativeHandle()
        {
            this.handle = IntPtr.Zero;
        }

        protected internal IntPtr Handle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.handle;
            protected set => this.handle = value;
        }

        internal bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.handle != IntPtr.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void Validate()
        {
            if (!this.IsValid)
            {
                ThrowHelper.ThrowObjectDisposedException($"{this.GetType()}");
            }
        }

        internal void SetHandleAsInvalid() => this.handle = IntPtr.Zero;

        protected abstract void CloseHandle();

        void Dispose(bool disposing)
        {
            try
            {
                if (!this.IsValid)
                {
                    return;
                }
                if (Log.IsDebugEnabled)
                {
                    Log.DebugFormat("Disposing {0} (Finalizer {1})", this.handle, !disposing);
                }
                this.CloseHandle();
            }
            catch (Exception exception) 
            {
                Log.Error($"{nameof(NativeHandle)} {this.handle} error whilst closing handle.", exception);

                // For finalizer, we cannot allow this to escape.
                if (disposing) throw;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~NativeHandle()
        {
            this.Dispose(false);
        }
    }
}
