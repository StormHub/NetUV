// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    public interface IResourceLeakTracker
    {
        void Record();

        void Record(object hint);

        bool Close(object trackedObject);
    }
}
