// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Native;

    [Flags]
    public enum FSEventType
    {
        Rename = 1,
        Change = 2
    }

    [Flags]
    public enum FSEventMask
    {
        Default = 0,

        /*
        * By default, if the fs event watcher is given a directory name, we will
        * watch for all events in that directory. This flags overrides this behavior
        * and makes fs_event report only changes to the directory entry itself. This
        * flag does not affect individual files watched.
        * This flag is currently not implemented yet on any backend.
        */
        Watchentry = 1,

        /*
        * By default uv_fs_event will try to use a kernel interface such as inotify
        * or kqueue to detect events. This may not work on remote filesystems such
        * as NFS mounts. This flag makes fs_event fall back to calling stat() on a
        * regular interval.
        * This flag is currently not implemented yet on any backend.
        */
        Status = 2,

        /*
        * By default, event watcher, when watching directory, is not registering
        * (is ignoring) changes in it's subdirectories.
        * This flag will override this behaviour on platforms that support it.
        */
        Recursive = 4
    };

    public struct FileSystemEvent
    {
        internal FileSystemEvent(string fileName, FSEventType eventType, Exception error)
        {
            this.FileName = fileName;
            this.EventType = eventType;
            this.Error = error;
        }

        public string FileName { get; }

        public FSEventType EventType { get; }

        public Exception Error { get; }
    }

    public sealed class FSEvent : ScheduleHandle
    {
        internal static readonly uv_fs_event_cb FSEventCallback = OnFSEventCallback;
        Action<FSEvent, FileSystemEvent> eventCallback;

        internal FSEvent(LoopContext loop)
            : base(loop, uv_handle_type.UV_FS_EVENT)
        { }

        public FSEvent Start(string path, 
            Action<FSEvent, FileSystemEvent> callback, 
            FSEventMask mask = FSEventMask.Default)
        {
            Contract.Requires(!string.IsNullOrEmpty(path));
            Contract.Requires(callback != null);

            this.Validate();
            this.eventCallback = callback;
            NativeMethods.FSEventStart(this.InternalHandle, path, mask);

            return this;
        }

        public string GetPath()
        {
            this.Validate();
            return NativeMethods.FSEventGetPath(this.InternalHandle);
        }

        void OnFSEventCallback(string fileName, int events, int status)
        {
            Log.TraceFormat("{0} {1} callback", this.HandleType, this.InternalHandle);
            try
            {
                OperationException error = null;
                if (status < 0)
                {
                    error = NativeMethods.CreateError((uv_err_code)status);
                }

                var fileSystemEvent = new FileSystemEvent(fileName, (FSEventType)events, error);
                this.eventCallback?.Invoke(this, fileSystemEvent);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.HandleType} {this.InternalHandle} callback error.", exception);
                throw;
            }
        }

        static void OnFSEventCallback(IntPtr handle, string fileName, int events, int status)
        {
            var fsEvent = HandleContext.GetTarget<FSEvent>(handle);
            fsEvent?.OnFSEventCallback(fileName, events, status);
        }

        public void Stop() => this.StopHandle();

        protected override void Close() => this.eventCallback = null;

        public void CloseHandle(Action<FSEvent> callback = null) =>
            base.CloseHandle(state => callback?.Invoke((FSEvent)state));
    }
}
