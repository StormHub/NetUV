// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Channels
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Handles;

    sealed class ReadStreamConsumer<T> : IStreamConsumer<T>
        where T : StreamHandle
    {
        readonly Action<T, IStreamReadCompletion> readAction;

        public ReadStreamConsumer(Action<T, IStreamReadCompletion> readAction)
        {
            Contract.Requires(readAction != null);

            this.readAction = readAction;
        }

        public void Consume(T stream, IStreamReadCompletion readCompletion) => 
            this.readAction(stream, readCompletion);
    }
}
