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
            this.Device = stat.st_dev;
            this.Mode = stat.st_mode;
            this.LinkCount = stat.st_nlink;

            this.UserIdentifier = stat.st_uid;
            this.GroupIdentifier = stat.st_gid;

            this.DeviceType = stat.st_rdev;
            this.Inode = stat.st_ino;

            this.Size = stat.st_size;
            this.BlockSize = stat.st_blksize;
            this.Blocks = stat.st_blocks;

            this.Flags = stat.st_flags;
            this.FileGeneration = stat.st_gen;

            this.LastAccessTime = (DateTime)stat.st_atim;
            this.LastModifyTime = (DateTime)stat.st_mtim;
            this.LastChangeTime = (DateTime)stat.st_ctim;
            this.CreateTime = (DateTime)stat.st_birthtim;
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
