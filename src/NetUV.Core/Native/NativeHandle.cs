// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Native
{
    using System;
    using System.Runtime.CompilerServices;
    using NetUV.Core.Logging;

    abstract class NativeHandle : IDisposable
    {
        protected static readonly ILog Log = LogFactory.ForContext<NativeHandle>();

        protected NativeHandle()
        {
            this.Handle = IntPtr.Zero;
        }

        protected internal IntPtr Handle
        {
            get;
            protected set;
        }

        internal bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return this.Handle != IntPtr.Zero;
            }
        } 

        protected internal void Validate()
        {
            if (this.IsValid)
            {
                return;
            }

            throw new ObjectDisposedException($"{nameof(NativeHandle)} has already been disposed");
        }

        internal void SetHandleAsInvalid() => this.Handle = IntPtr.Zero;

        protected abstract void CloseHandle();

        void Dispose(bool disposing)
        {
            try
            {
                if (!this.IsValid)
                {
                    return;
                }

                Log.DebugFormat("Disposing {0} (Finalizer {1})", this.Handle, !disposing);
                this.CloseHandle();
            }
            catch (Exception exception) 
            {
                Log.Error($"{nameof(NativeHandle)} {this.Handle} error whilst closing handle.", exception);

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
