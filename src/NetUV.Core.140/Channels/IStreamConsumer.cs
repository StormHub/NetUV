// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Channels
{
    using NetUV.Core.Handles;

    interface IStreamConsumer<in T> 
        where T : StreamHandle
    {
        void Consume(T stream, IStreamReadCompletion readCompletion);
    }
}
