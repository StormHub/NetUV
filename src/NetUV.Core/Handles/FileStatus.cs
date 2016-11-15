// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using NetUV.Core.Native;

    public sealed class FileStatus
    {
        internal FileStatus(uv_stat_t stat)
        {
            this.Device = (long)stat.st_dev;
            this.Mode = (long)stat.st_mode;
            this.LinkCount = (long)stat.st_nlink;

            this.UserIdentifier = (long)stat.st_uid;
            this.GroupIdentifier = (long)stat.st_gid;

            this.DeviceType = (long)stat.st_rdev;
            this.Inode = (long)stat.st_ino;

            this.Size = (long)stat.st_size;
            this.BlockSize = (long)stat.st_blksize;
            this.Blocks = (long)stat.st_blocks;

            this.Flags = (long)stat.st_flags;
            this.FileGeneration = (long)stat.st_gen;

            this.LastAccessTime = stat.st_atim;
            this.LastModifyTime = stat.st_mtim;
            this.LastChangeTime = stat.st_ctim;

            this.CreateTime = stat.st_birthtim;
        }

        public long Device { get; }

        public long Mode { get; }

        public long LinkCount { get; }

        public long UserIdentifier { get; }

        public long GroupIdentifier { get; }

        public long DeviceType { get; }

        public long Inode { get; }

        public long Size { get; }

        public long BlockSize { get; }

        public long Blocks { get; }

        public long Flags { get; }

        public long FileGeneration { get; }

        public DateTime LastAccessTime { get; }

        public DateTime LastModifyTime { get; }

        public DateTime LastChangeTime { get; }

        public DateTime CreateTime { get; }
    }
}
