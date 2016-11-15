// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Logging
{
    using System;
    using System.Globalization;
    using Microsoft.Extensions.Logging;

    public sealed class DefaultLog : ILog
    {
        static readonly Func<object, Exception, string> MessageFormatter = Format;
        readonly ILogger logger;

        public DefaultLog(ILogger logger)
        {
            this.logger = logger;
        }

        static string Format(object target, Exception exception)
        {
            string message = target?.ToString() ?? string.Empty;
            return exception == null ? message : $"{message} {exception}";
        }

        public bool IsTraceEnabled =>
            this.logger?.IsEnabled(LogLevel.Trace) ?? false;

        public bool IsDebugEnabled => 
            this.logger?.IsEnabled(LogLevel.Debug) ?? false;

        public bool IsInfoEnabled =>
            this.logger?.IsEnabled(LogLevel.Information) ?? false;

        public bool IsWarnEnabled =>
            this.logger?.IsEnabled(LogLevel.Warning) ?? false;

        public bool IsErrorEnabled =>
            this.logger?.IsEnabled(LogLevel.Error) ?? false;

        public bool IsCriticalEnabled =>
            this.logger?.IsEnabled(LogLevel.Critical) ?? false;

        public void Trace(object obj)
        {
            if (!this.IsTraceEnabled)
            {
                return;
            }

            this.Trace(obj, null);
        }

        public void Trace(object obj, Exception exception)
        {
            if (!this.IsTraceEnabled)
            {
                return;
            }

            this.logger?.Log(LogLevel.Trace, 0, obj, exception, MessageFormatter);
        }

        public void TraceFormat(string format, params object[] args)
        {
            if (!this.IsTraceEnabled)
            {
                return;
            }

            this.TraceFormat(CultureInfo.CurrentCulture, format, args);
        }

        public void TraceFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (!this.IsTraceEnabled
                || formatProvider == null
                || string.IsNullOrEmpty(format))
            {
                return;
            }

            string message = string.Format(formatProvider, format, args);
            this.logger.Log(LogLevel.Trace, 0, message, null, MessageFormatter);
        }

        public void Debug(object obj)
        {
            if (!this.IsDebugEnabled)
            {
                return;
            }

            this.Debug(obj, null);
        } 

        public void Debug(object obj, Exception exception)
        {
            if (!this.IsDebugEnabled)
            {
                return;
            }

            this.logger?.Log(LogLevel.Debug, 0, obj, exception, MessageFormatter);
        }

        public void DebugFormat(string format, params object[] args)
        {
            if (!this.IsDebugEnabled)
            {
                return;
            }

            this.DebugFormat(CultureInfo.CurrentCulture, format, args);
        }

        public void DebugFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (!this.IsDebugEnabled 
                || formatProvider == null 
                || string.IsNullOrEmpty(format))
            {
                return;
            }

            string message = string.Format(formatProvider, format, args);
            this.logger.Log(LogLevel.Debug, 0, message, null, MessageFormatter);
        }

        public void Info(object obj)
        {
            if (!this.IsInfoEnabled)
            {
                return;
            }

            this.Info(obj, null);
        }

        public void Info(object obj, Exception exception)
        {
            if (!this.IsInfoEnabled)
            {
                return;
            }

            this.logger?.Log(LogLevel.Information, 0, obj, exception, MessageFormatter);
        }

        public void InfoFormat(string format, params object[] args)
        {
            if (!this.IsInfoEnabled)
            {
                return;
            }

            this.InfoFormat(CultureInfo.CurrentCulture, format, args);
        }

        public void InfoFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (!this.IsInfoEnabled
                || formatProvider == null
                || string.IsNullOrEmpty(format))
            {
                return;
            }

            string message = string.Format(formatProvider, format, args);
            this.logger.Log(LogLevel.Debug, 0, message, null, MessageFormatter);
        }

        public void Warn(object obj)
        {
            if (!this.IsWarnEnabled)
            {
                return;
            }

            this.Warn(obj, null);
        } 

        public void Warn(object obj, Exception exception)
        {
            if (!this.IsWarnEnabled)
            {
                return;
            }

            this.logger?.Log(LogLevel.Warning, 0, obj, exception, MessageFormatter);
        }

        public void WarnFormat(string format, params object[] args)
        {
            if (!this.IsWarnEnabled)
            {
                return;
            }

            this.WarnFormat(CultureInfo.CurrentCulture, format, args);
        }

        public void WarnFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (!this.IsWarnEnabled
                || formatProvider == null
                || string.IsNullOrEmpty(format))
            {
                return;
            }

            string message = string.Format(formatProvider, format, args);
            this.logger.Log(LogLevel.Warning, 0, message, null, MessageFormatter);
        }

        public void Error(object obj)
        {
            if (!this.IsErrorEnabled)
            {
                return;
            }

            this.Error(obj, null);
        }

        public void Error(object obj, Exception exception)
        {
            if (!this.IsErrorEnabled)
            {
                return;
            }

            this.logger?.Log(LogLevel.Error, 0, obj, exception, MessageFormatter);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            if (!this.IsErrorEnabled)
            {
                return;
            }

            this.ErrorFormat(CultureInfo.CurrentCulture, format, args);
        }

        public void ErrorFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (!this.IsErrorEnabled
                || formatProvider == null
                || string.IsNullOrEmpty(format))
            {
                return;
            }

            string message = string.Format(formatProvider, format, args);
            this.logger.Log(LogLevel.Error, 0, message, null, MessageFormatter);
        }

        public void Critical(object obj)
        {
            if (!this.IsCriticalEnabled)
            {
                return;
            }

            this.Critical(obj, null);
        }

        public void Critical(object obj, Exception exception)
        {
            if (!this.IsCriticalEnabled)
            {
                return;
            }

            this.logger?.Log(LogLevel.Critical, 0, obj, exception, MessageFormatter);
        }

        public void CriticalFormat(string format, params object[] args)
        {
            if (!this.IsCriticalEnabled)
            {
                return;
            }

            this.CriticalFormat(CultureInfo.CurrentCulture, format, args);
        }

        public void CriticalFormat(IFormatProvider formatProvider, string format, params object[] args)
        {
            if (!this.IsCriticalEnabled
                || formatProvider == null
                || string.IsNullOrEmpty(format))
            {
                return;
            }

            string message = string.Format(formatProvider, format, args);
            this.logger.Log(LogLevel.Critical, 0, message, null, MessageFormatter);
        }
    }
}
