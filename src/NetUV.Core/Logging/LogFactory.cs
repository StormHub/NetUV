// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Logging
{
    using System;
    using Microsoft.Extensions.Logging;

    public static class LogFactory
    {
        static readonly ILoggerFactory DefaultFactory;

        static LogFactory()
        {
            DefaultFactory = new LoggerFactory();
        }

        public static void AddProvider(ILoggerProvider provider)
        {
            if (provider != null)
            {
                DefaultFactory.AddProvider(provider);
            }
        }

        public static ILog ForContext<T>() => ForContext(typeof(T).Name);

        public static ILog ForContext(Type type) => ForContext(type?.Name);

        public static ILog ForContext(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = $"Unknown {Guid.NewGuid()}";
            }

            ILogger logger = DefaultFactory.CreateLogger(name);
            return new DefaultLog(logger);
        }
    }
}
