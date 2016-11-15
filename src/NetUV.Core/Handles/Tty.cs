// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Channels;
    using NetUV.Core.Native;

    public enum TtyType
    {
        In    = 0,  // stdin  - readable
        Out   = 1,  // stdout - not readable
        Error = 2   // stderr
    }

    public enum TtyMode
    {
        Normal = 0,

        /* Raw input mode (On Windows, ENABLE_WINDOW_INPUT is also enabled) */
        Raw = 1,

        /* Binary-safe I/O mode for IPC (Unix-only) */
        IO
    }

    public sealed class Tty : StreamHandle
    {
        readonly TtyType ttyType;

        internal Tty(LoopContext loop, TtyType ttyType)
            : base(loop, uv_handle_type.UV_TTY, ttyType)
        {
            this.ttyType = ttyType;
        }

        public IStream<Tty> TtyStream()
        {
            this.Validate();
            return this.CreateStream<Tty>();
        }

        public Tty RegisterRead(Action<Tty, IStreamReadCompletion> readAction)
        {
            Contract.Requires(readAction != null);

            if (this.ttyType != TtyType.In)
            {
                throw new InvalidOperationException(
                    $"{this.HandleType} {this.InternalHandle} mode {this.ttyType} is not readable");
            }

            this.RegisterReadAction(
                (stream, completion) => readAction.Invoke((Tty)stream, completion));

            this.ReadStart();

            return this;
        }

        public void Shutdown(Action<Tty, Exception> completedAction = null) =>
            this.ShutdownStream((state, error) => completedAction?.Invoke((Tty)state, error));

        public Tty Mode(TtyMode mode)
        {
            if (mode == TtyMode.IO 
                && !Platform.IsUnix)
            {
                throw new ArgumentException($"{mode} is Unix only.", nameof(mode));
            }

            this.Validate();
            NativeMethods.TtySetMode(this.InternalHandle, mode);

            return this;
        }

        public Tty WindowSize(out int width, out int height)
        {
            this.Validate();
            NativeMethods.TtyWindowSize(this.InternalHandle, out width, out height);

            return this;
        }

        public static void ResetMode() => NativeMethods.TtyResetMode();

        public void CloseHandle(Action<Tty> callback = null) =>
            base.CloseHandle(state => callback?.Invoke((Tty)state));
    }
}
