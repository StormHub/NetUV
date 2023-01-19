// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System;
    using NetUV.Core.Buffers;
    using NetUV.Core.Logging;

    static class ReferenceCountUtil
    {
        static readonly ILog Logger = LogFactory.ForContext(typeof(ReferenceCountUtil));

        public static T Retain<T>(T msg)
        {
            if (msg is IReferenceCounted counted)
            {
                return (T)counted.Retain();
            }
            return msg;
        }

        public static T Retain<T>(T msg, int increment)
        {
            if (msg is IReferenceCounted counted)
            {
                return (T)counted.Retain(increment);
            }
            return msg;
        }

        public static T Touch<T>(T msg)
        {
            if (msg is IReferenceCounted refCnt)
            {
                return (T)refCnt.Touch();
            }
            return msg;
        }

        public static T Touch<T>(T msg, object hint)
        {
            if (msg is IReferenceCounted refCnt)
            {
                return (T)refCnt.Touch(hint);
            }
            return msg;
        }

        public static bool Release(object msg)
        {
            if (msg is IReferenceCounted counted)
            {
                return counted.Release();
            }
            return false;
        }

        public static bool Release(object msg, int decrement)
        {
            if (msg is IReferenceCounted counted)
            {
                return counted.Release(decrement);
            }
            return false;
        }

        public static void SafeRelease(object msg)
        {
            try
            {
                Release(msg);
            }
            catch (Exception ex)
            {
                if (Logger.IsWarnEnabled)
                {
                    Logger.Warn(msg, ex);
                }
            }
        }

        public static void SafeRelease(object msg, int decrement)
        {
            try
            {
                Release(msg, decrement);
            }
            catch (Exception ex)
            {
                if (Logger.IsWarnEnabled)
                {
                    Logger.Warn(msg, ex);
                }
            }
        }

        public static void SafeRelease(this IReferenceCounted msg)
        {
            try
            {
                msg?.Release();
            }
            catch (Exception ex)
            {
                if (Logger.IsWarnEnabled)
                {
                    Logger.Warn(msg, ex);
                }
            }
        }

        public static void SafeRelease(this IReferenceCounted msg, int decrement)
        {
            try
            {
                msg?.Release(decrement);
            }
            catch (Exception ex)
            {
                if (Logger.IsWarnEnabled)
                {
                    Logger.Warn(msg, ex);
                }
            }
        }
    }
}
