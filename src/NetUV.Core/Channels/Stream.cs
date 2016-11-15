// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Channels
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;

    abstract class InternalStream : IObservable<ReadableBuffer>, IDisposable
    {
        readonly ConcurrentDictionary<SubscriptionHandle, IObserver<ReadableBuffer>> observers;

        struct SubscriptionHandle : IDisposable
        {
            readonly InternalStream stream;

            internal SubscriptionHandle(InternalStream stream)
            {
                Contract.Requires(stream != null);
                this.stream = stream;
            }

            public void Dispose()
            {
                IObserver<ReadableBuffer> ignore;
                this.stream.observers.TryRemove(this, out ignore);
            }
        }

        protected InternalStream(StreamHandle streamHandle)
        {
            Contract.Requires(streamHandle != null);

            this.InternalHandle = streamHandle;
            this.observers = new ConcurrentDictionary<SubscriptionHandle, IObserver<ReadableBuffer>>();
            this.InternalHandle.RegisterReadAction(this.OnStreamRead);
        }

        protected StreamHandle InternalHandle { get; }

        public WritableBuffer Allocate(int size)
        {
            Contract.Requires(size > 0);

            if (!this.InternalHandle.IsValid)
            {
                throw new ObjectDisposedException(
                    $"{this.InternalHandle.GetType().Name} stream has already been disposed.");
            }

            return this.InternalHandle.Allocate(size);
        }

        public IDisposable Subscribe(IObserver<ReadableBuffer> observer)
        {
            Contract.Requires(observer != null);

            if (!this.InternalHandle.IsValid)
            {
                throw new ObjectDisposedException(
                    $"{this.InternalHandle.GetType().Name} stream has already been disposed.");
            }

            if (this.observers.Values.Contains(observer))
            {
                throw new InvalidOperationException(
                    $"{nameof(observer)} has already subscribed to the stream.");
            }

            var token = new SubscriptionHandle(this);
            if (this.observers.TryAdd(token, observer))
            {
                return token;
            }

            token.Dispose();
            throw new InvalidOperationException(
                $"{nameof(observer)} failed to subscribe to {this.InternalHandle.GetType().Name} stream");
        }

        void OnStreamRead(StreamHandle handle, IStreamReadCompletion completion)
        {
            Contract.Assert(this.InternalHandle == handle);
            Contract.Requires(completion != null);

            foreach (IObserver<ReadableBuffer> observer in this.observers.Values)
            {
                NotifyReadCompletion(observer, completion);
            }
        }

        static void NotifyReadCompletion(IObserver<ReadableBuffer> observer, IStreamReadCompletion readCompletion)
        {
            try
            {
                if (readCompletion.Error != null)
                {
                    observer.OnError(readCompletion.Error);
                }
                else
                {
                    observer.OnNext(readCompletion.Data);
                }

                if (readCompletion.Completed)
                {
                    observer.OnCompleted();
                }
            }
            catch (Exception exception)
            {
                observer.OnError(exception);
            }
            finally
            {
                readCompletion.Dispose();
            }
        }

        protected void Write(WritableBuffer buffer, Action<StreamHandle, Exception> completion)
        {
            if (!this.InternalHandle.IsValid)
            {
                throw new ObjectDisposedException(
                    $"{this.InternalHandle.GetType().Name} stream has already been disposed.");
            }

            this.InternalHandle.QueueWriteStream(buffer, completion);
        }

        protected void Shutdown(Action<StreamHandle, Exception> completion = null) =>
            this.InternalHandle.ShutdownStream(completion);

        public void Dispose()
        {
            foreach (SubscriptionHandle subscription in this.observers.Keys)
            {
                IObserver<ReadableBuffer> observer = this.observers[subscription];
                subscription.Dispose();
                observer.OnCompleted();
            }

            this.observers.Clear();
            this.InternalHandle.Dispose();
        }
    }

    class Stream : InternalStream, IStream
    {
        internal Stream(StreamHandle streamHandle)
            : base(streamHandle)
        { }

        public StreamHandle Handle => this.InternalHandle;

        public void Write(WritableBuffer buffer, Action<IStream, Exception> completion) => 
            base.Write(buffer,
                (handle, exception) => completion?.Invoke(this, exception));

        public void Shutdown(Action<IStream, Exception> completion = null) =>
            base.Shutdown(
                (handle, exception) => completion?.Invoke(this, exception));
    }

    sealed class Stream<T> : InternalStream, IStream<T> 
        where T : StreamHandle
    {
        internal Stream(StreamHandle streamHandle)
            : base(streamHandle)
        { }

        public T Handle => (T)this.InternalHandle;

        public void Write(WritableBuffer buffer, Action<IStream<T>, Exception> completion) => 
            base.Write(buffer, 
                (handle, exception) => completion?.Invoke(this, exception));

        public void Shutdown(Action<IStream<T>, Exception> completion = null) => 
            base.Shutdown((handle, exception) => completion?.Invoke(this, exception));
    }
}
