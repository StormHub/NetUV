// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Concurrency
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;

    static class TaskHelper
    {
        public static void Ignore(this Task task)
        {
            Contract.Requires(task != null);

            // ReSharper disable UnusedVariable
            if (task.IsCompleted)
            {
                AggregateException ignored = task.Exception;
            }
            else
            {
                task.ContinueWith(
                    t => { AggregateException ignored = t.Exception; },
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
            // ReSharper restore UnusedVariable
        }
    }
}
