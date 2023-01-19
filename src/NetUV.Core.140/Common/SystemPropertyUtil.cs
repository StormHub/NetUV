// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Logging;

    static class SystemPropertyUtil
    {
        static readonly ILog Logger = LogFactory.ForContext(typeof(SystemPropertyUtil));
        static bool loggedException;

        public static bool Contains(string key) => Get(key) != null;

        public static string Get(string key) => Get(key, null);

        public static string Get(string key, string def)
        {
            Contract.Requires(!string.IsNullOrEmpty(key));

            try
            {
                return Environment.GetEnvironmentVariable(key) ?? def;
            }
            catch (Exception e)
            {
                if (!loggedException)
                {
                    Log($"Unable to retrieve a system property '{key}'; default values will be used.", e);
                    loggedException = true;
                }
                return def;
            }
        }

        public static bool GetBoolean(string key, bool def)
        {
            string value = Get(key);
            if (value == null)
            {
                return def;
            }

            value = value.Trim().ToLowerInvariant();
            if (value.Length == 0)
            {
                return true;
            }

            if ("true".Equals(value, StringComparison.OrdinalIgnoreCase)
                || "yes".Equals(value, StringComparison.OrdinalIgnoreCase)
                || "1".Equals(value, StringComparison.Ordinal))
            {
                return true;
            }

            if ("false".Equals(value, StringComparison.OrdinalIgnoreCase)
                || "no".Equals(value, StringComparison.OrdinalIgnoreCase)
                || "0".Equals(value, StringComparison.Ordinal))
            {
                return false;
            }

            Log($"Unable to parse the boolean system property '{key}':{value} - using the default value: {def}");

            return def;
        }

        public static int GetInt(string key, int def)
        {
            string value = Get(key);
            if (value == null)
            {
                return def;
            }

            value = value.Trim().ToLowerInvariant();
            if (!int.TryParse(value, out int result))
            {
                result = def;

                Log($"Unable to parse the integer system property '{key}':{value} - using the default value: {def}");
            }

            return result;
        }

        public static long GetLong(string key, long def)
        {
            string value = Get(key);
            if (value == null)
            {
                return def;
            }

            if (!long.TryParse(value, out long result))
            {
                result = def;
                Log($"Unable to parse the long integer system property '{key}':{value} - using the default value: {def}");
            }

            return result;
        }

        static void Log(string msg) => Logger.Warn(msg);

        static void Log(string msg, Exception e) => Logger.Warn(msg, e);
    }
}
