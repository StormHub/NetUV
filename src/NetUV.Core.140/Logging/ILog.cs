// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Logging
{
    using System;

    public interface ILog
    {
        bool IsTraceEnabled { get; }

        bool IsDebugEnabled { get; }

        bool IsInfoEnabled { get; }

        bool IsWarnEnabled { get; }

        bool IsErrorEnabled { get; }

        bool IsCriticalEnabled { get; }

        void Trace(object obj);

        void Trace(object obj, Exception exception);

        void TraceFormat(IFormatProvider formatProvider, string format, params object[] args);

        void TraceFormat(string format, params object[] args);

        void Debug(object obj);

        void Debug(object obj, Exception exception);

        void DebugFormat(IFormatProvider formatProvider, string format, params object[] args);

        void DebugFormat(string format, params object[] args);

        void Info(object obj);

        void Info(object obj, Exception exception);

        void InfoFormat(IFormatProvider formatProvider, string format, params object[] args);

        void InfoFormat(string format, params object[] args);

        void Warn(object obj);

        void Warn(object obj, Exception exception);

        void WarnFormat(IFormatProvider formatProvider, string format, params object[] args);

        void WarnFormat(string format, params object[] args);

        void Error(object obj);

        void Error(object obj, Exception exception);

        void ErrorFormat(IFormatProvider formatProvider, string format, params object[] args);

        void ErrorFormat(string format, params object[] args);

        void Critical(object obj);

        void Critical(object obj, Exception exception);

        void CriticalFormat(IFormatProvider formatProvider, string format, params object[] args);

        void CriticalFormat(string format, params object[] args);
    }
}
