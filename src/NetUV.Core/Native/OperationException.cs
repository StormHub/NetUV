// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Native
{
    using System;

    public sealed class OperationException : Exception
    {
        public OperationException(
            int errorCode, 
            string errorName, 
            string description)
            : base(description)
        {
            this.ErrorCode = errorCode;
            this.ErrorName = errorName;
        }

        public int ErrorCode { get; }

        public string ErrorName { get; }
    }
}
