﻿// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable InconsistentNaming
// ReSharper disable RedundantAssignment
// ReSharper disable NotAccessedVariable
#pragma warning disable 414
#pragma warning disable 169
namespace LoopThread
{
    using System;
    using System.Runtime.InteropServices;
    using NetUV.Core.Handles;

    //https://github.com/aspnet/KestrelHttpServer/blob/dev/src/Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv/Internal/ListenerPrimary.cs
    sealed class WindowsApi : IDisposable
    {
        IntPtr fileCompletionInfoPtr;
        bool tryDetachFromIOCP = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public WindowsApi()
        {
            var fileCompletionInfo = new FILE_COMPLETION_INFORMATION { Key = IntPtr.Zero, Port = IntPtr.Zero };

            this.fileCompletionInfoPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(fileCompletionInfo));
            Marshal.StructureToPtr(fileCompletionInfo, this.fileCompletionInfoPtr, false);
        }

        public void DetachFromIOCP(Tcp handle)
        {
            if (!this.tryDetachFromIOCP)
            {
                return;
            }

            // https://msdn.microsoft.com/en-us/library/windows/hardware/ff728840(v=vs.85).aspx
            const int FileReplaceCompletionInformation = 61;
            // https://msdn.microsoft.com/en-us/library/cc704588.aspx
            const uint STATUS_INVALID_INFO_CLASS = 0xC0000003;

            var statusBlock = new IO_STATUS_BLOCK();
            IntPtr socket = IntPtr.Zero;
            handle.GetFileDescriptor(ref socket);
            uint len = (uint)Marshal.SizeOf<FILE_COMPLETION_INFORMATION>();

            if (NtSetInformationFile(socket,
                out statusBlock, this.fileCompletionInfoPtr, len,
                FileReplaceCompletionInformation) == STATUS_INVALID_INFO_CLASS)
            {
                // Replacing IOCP information is only supported on Windows 8.1 or newer
                this.tryDetachFromIOCP = false;
            }
        }

        struct IO_STATUS_BLOCK
        {
            uint status;
            ulong information;
        }

        struct FILE_COMPLETION_INFORMATION
        {
            public IntPtr Port;
            public IntPtr Key;
        }

        [DllImport("NtDll.dll")]
        static extern uint NtSetInformationFile(IntPtr FileHandle, out IO_STATUS_BLOCK IoStatusBlock, IntPtr FileInformation, uint Length, int FileInformationClass);

        public void Dispose()
        {
            IntPtr handle = this.fileCompletionInfoPtr;
            if (handle != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(handle);
            }
            this.fileCompletionInfoPtr = IntPtr.Zero;
        }
    }
}
