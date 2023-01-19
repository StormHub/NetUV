// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Concurrency
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading;

    sealed class Gate
    {
        const int Busy = 1;
        const int Free = 0;

        readonly Guard guard;
        long state;

        internal Gate()
        {
            this.state = Free;
            this.guard = new Guard(this);
        }

        internal IDisposable TryAquire() => 
            Interlocked.CompareExchange(ref this.state, Busy, Free) == Free
            ? this.guard 
            : default(IDisposable);

        internal IDisposable Aquire()
        {
            IDisposable disposable;
            while ((disposable = this.TryAquire()) == null) { /* Aquire */ }
            return disposable;
        }

        void Release()
        {
            long previousState = Interlocked.CompareExchange(ref this.state, Free, Busy);
            Contract.Assert(previousState == Busy);
        }

        struct Guard : IDisposable
        {
            readonly Gate gate;

            internal Guard(Gate gate)
            {
                this.gate = gate;
            }

            public void Dispose() => this.gate.Release();
        }
    }
}
