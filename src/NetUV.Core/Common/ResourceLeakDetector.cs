// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using NetUV.Core.Logging;

    /// <summary>
    /// Forked from https://github.com/Azure/DotNetty
    /// </summary>
    sealed class ResourceLeakDetector
    {
        const string ResourceLeakDetectorEnabled = nameof(ResourceLeakDetectorEnabled);
        const int DefaultSamplingInterval = 113;
        const int DefaultMaxRecords = 4;

        static readonly ILog Log = LogFactory.ForContext<ResourceLeakDetector>();

        static readonly int MaxRecords;

        static ResourceLeakDetector()
        {
            if (!Configuration.TryGetValue(ResourceLeakDetectorEnabled, out bool value))
            {
                value = false;
            }

            Enabled = value;
            MaxRecords = DefaultMaxRecords;

            Log.DebugFormat("{0} : Enabled = {1}", nameof(ResourceLeakDetector), Enabled);
            Log.DebugFormat("{0} : MaxRecords = {1}", nameof(ResourceLeakDetector), MaxRecords);
        }

        public static readonly bool Enabled;

        readonly ConditionalWeakTable<object, GCNotice> gcNotificationMap = new ConditionalWeakTable<object, GCNotice>();
        readonly ConcurrentDictionary<string, bool> reportedLeaks = new ConcurrentDictionary<string, bool>();

        readonly string resourceType;
        readonly int samplingInterval;
        readonly long maxActive;
        long active;
        int loggedTooManyActive;

        public ResourceLeakDetector(string resourceType)
            : this(resourceType, DefaultSamplingInterval, long.MaxValue)
        { }

        public ResourceLeakDetector(string resourceType, int samplingInterval, long maxActive)
        {
            Contract.Requires(resourceType != null);
            Contract.Requires(samplingInterval > 0);
            Contract.Requires(maxActive > 0);

            this.resourceType = resourceType;
            this.samplingInterval = samplingInterval;
            this.maxActive = maxActive;
        }

        public static ResourceLeakDetector Create<T>() => new ResourceLeakDetector(typeof(T).Name);

        public static ResourceLeakDetector Create<T>(int samplingInterval, long maxActive) => 
            new ResourceLeakDetector(typeof(T).Name, samplingInterval, maxActive);

        /// <summary>
        ///     Creates a new <see cref="IResourceLeak" /> which is expected to be closed via <see cref="IResourceLeak.Close()" />
        ///     when the
        ///     related resource is deallocated.
        /// </summary>
        /// <returns>the <see cref="IResourceLeak" /> or <c>null</c></returns>
        public IResourceLeak Open(object obj)
        {
            this.CheckForCountLeak();
            return Enabled ? new DefaultResourceLeak(this, obj) : null;
        }

        internal void CheckForCountLeak()
        {
            // Report too many instances.
            int interval = Enabled ? 1 : this.samplingInterval;
            if (Volatile.Read(ref this.active) * interval > this.maxActive
                && Interlocked.CompareExchange(ref this.loggedTooManyActive, 0, 1) == 0)
            {
                Log.Error($"LEAK: You are creating too many {this.resourceType} instances."
                    + $" {this.resourceType} is a shared resource that must be reused across the AppDomain," 
                    + " so that only a few instances are created.");
            }
        }

        internal void Report(IResourceLeak resourceLeak)
        {
            string records = resourceLeak.ToString();
            if (!this.reportedLeaks.TryAdd(records, true))
            {
                return;
            }

            Log.Error(records.Length == 0 
                ? $"LEAK: {this.resourceType}.Release() was not called before it's garbage-collected." 
                : $"LEAK: {this.resourceType}.Release() was not called before it's garbage-collected. {records}");
        }

        sealed class DefaultResourceLeak : IResourceLeak
        {
            readonly ResourceLeakDetector owner;
            readonly string creationRecord;
            readonly Deque<string> lastRecords = new Deque<string>();
            int freed;

            public DefaultResourceLeak(ResourceLeakDetector owner, object referent)
            {
                this.owner = owner;
                if (owner.gcNotificationMap.TryGetValue(referent, out GCNotice existingNotice))
                {
                    existingNotice.Rearm(this);
                }
                else
                {
                    owner.gcNotificationMap.Add(referent, new GCNotice(this));
                }

                if (referent != null)
                {
                    this.creationRecord = Enabled ? NewRecord(null) : null;

                    Interlocked.Increment(ref this.owner.active);
                }
                else
                {
                    this.creationRecord = null;
                    this.freed = 1;
                }
            }

            public void Record() => this.RecordInternal(null);

            public void Record(object hint) => this.RecordInternal(hint);

            void RecordInternal(object hint)
            {
                if (this.creationRecord == null)
                {
                    return;
                }

                string value = NewRecord(hint);
                lock (this.lastRecords)
                {
                    int size = this.lastRecords.Count;
                    if (size == 0 || this.lastRecords[size - 1].Equals(value))
                    {
                        this.lastRecords.AddToBack(value);
                    }
                    if (size > MaxRecords)
                    {
                        this.lastRecords.RemoveFromFront();
                    }
                }
            }

            public bool Close()
            {
                if (Interlocked.CompareExchange(ref this.freed, 1, 0) == 0)
                {
                    Interlocked.Decrement(ref this.owner.active);
                    return true;
                }

                return false;
            }

            internal void CloseFinal()
            {
                if (this.Close())
                {
                    this.owner.Report(this);
                }
            }

            public override string ToString()
            {
                if (this.creationRecord == null)
                {
                    return "";
                }

                string[] array;
                lock (this.lastRecords)
                {
                    array = new string[this.lastRecords.Count];
                    ((ICollection<string>)this.lastRecords).CopyTo(array, 0);
                }

                StringBuilder buf = new StringBuilder(16384)
                    .Append(Environment.NewLine)
                    .Append("Recent access records: ")
                    .Append(array.Length)
                    .Append(Environment.NewLine);

                if (array.Length > 0)
                {
                    for (int i = array.Length - 1; i >= 0; i--)
                    {
                        buf.Append('#')
                            .Append(i + 1)
                            .Append(':')
                            .Append(Environment.NewLine)
                            .Append(array[i]);
                    }
                    buf.Append(Environment.NewLine);
                }

                buf.Append("Created at:")
                    .Append(Environment.NewLine)
                    .Append(this.creationRecord);

                return buf.ToString();
            }
        }

        static string NewRecord(object hint)
        {
            Contract.Ensures(Contract.Result<string>() != null);

            var buf = new StringBuilder(4096);

            // Append the hint first if available.
            if (hint != null)
            {
                buf.Append("\tHint: ");
                // Prefer a hint string to a simple string form.
                var leakHint = hint as IResourceLeakHint;
                if (leakHint != null)
                {
                    buf.Append(leakHint.ToHintString());
                }
                else
                {
                    buf.Append(hint);
                }
                buf.Append(Environment.NewLine);
            }

            // Append the stack trace.
            buf.Append(Environment.StackTrace);
            return buf.ToString();
        }

        class GCNotice
        {
            DefaultResourceLeak leak;

            public GCNotice(DefaultResourceLeak leak)
            {
                this.leak = leak;
            }

            ~GCNotice()
            {
                this.leak.CloseFinal();
            }

            public void Rearm(DefaultResourceLeak newLeak)
            {
                DefaultResourceLeak oldLeak = Interlocked.Exchange(ref this.leak, newLeak);
                oldLeak.CloseFinal();
            }
        }
    }

    public interface IResourceLeak
    {
        /// <summary>
        /// Records the caller's current stack trace so that the <see cref="ResourceLeakDetector" /> 
        /// can tell where the leaked resource was accessed lastly. This method is a shortcut to <see cref="Record(object)" /> 
        /// with <c>null</c> as an argument.
        /// </summary>
        void Record();

        /// <summary>
        /// Records the caller's current stack trace and the specified additional arbitrary information
        /// so that the <see cref="ResourceLeakDetector" /> can tell where the leaked resource was accessed lastly.
        /// </summary>
        /// <param name="hint"></param>
        void Record(object hint);

        /// <summary>
        /// Close the leak so that <see cref="ResourceLeakDetector" /> does not warn about leaked resources.
        /// </summary>
        /// <returns><c>true</c> if called first time, <c>false</c> if called already</returns>
        bool Close();
    }

    /// <summary>
    /// A hint object that provides human-readable message for easier resource leak tracking.
    /// </summary>
    public interface IResourceLeakHint
    {
        /// <summary>
        /// Returns a human-readable message that potentially enables easier resource leak tracking.
        /// </summary>
        string ToHintString();
    }
}
