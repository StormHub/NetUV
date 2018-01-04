// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;

    sealed class UdpPummel : IDisposable
    {
        const int Port = 8980;
        const string ExpectedMessage = "RANG TANG DING DONG I AM THE JAPANESE SANDMAN";
        const int PacketCount = 1000 * 1000;
        const long NanoSeconds = 1000000000;

        readonly int numberOfSenders;
        readonly int numberOfReceivers;
        readonly int timeout;

        Loop loop;
        Timer timer;

        Udp[] receivers;
        Dictionary<Udp, IPEndPoint> senders;

        /* not used in timed mode */
        int packetCounter;
        readonly byte[] content;

        int sendCount;
        int receiveCount;
        int closeCount;
        bool exiting;

        public UdpPummel(int numberOfSenders, int numberOfReceivers, int timeout)
        {
            this.numberOfSenders = numberOfSenders;
            this.numberOfReceivers = numberOfReceivers;
            this.timeout = timeout;

            this.senders = new Dictionary<Udp, IPEndPoint>();
            this.receivers = new Udp[this.numberOfReceivers];
            this.packetCounter = PacketCount;
            this.content = Encoding.UTF8.GetBytes(ExpectedMessage);

            this.loop = new Loop();
        }

        public void Run()
        {
            this.sendCount = 0;
            this.closeCount = 0;
            this.exiting = false;
            this.receiveCount = 0;

            if (this.timeout > 0)
            {
                this.timer = this.loop
                    .CreateTimer()
                    .Start(this.OnTimer, this.timeout, 0);

                /* Timer should not keep loop alive. */
                this.timer.RemoveReference();
            }

            for (int i = 0; i < this.numberOfReceivers; i++)
            {
                var endPoint = new IPEndPoint(IPAddress.Any, Port + i);
                this.receivers[i] = this.loop
                    .CreateUdp()
                    .ReceiveStart(endPoint, this.OnReceive);
                this.receivers[i].RemoveReference();
            }

            for (int i = 0; i < this.numberOfSenders; i++)
            {
                var endPoint = new IPEndPoint(IPAddress.Loopback, Port + i);
                Udp udp = this.loop.CreateUdp();
                this.senders.Add(udp, endPoint);
                udp.QueueSend(this.content, endPoint, this.OnSendCompleted);
            }

            long duration = this.loop.NowInHighResolution;
            this.loop.RunDefault();
            duration = this.loop.NowInHighResolution - duration;
            double seconds = (double)duration / NanoSeconds;
            double received = this.receiveCount / seconds;
            double sent = this.sendCount / seconds;
            Console.WriteLine(
                $"Udp pummel {this.numberOfSenders}v{this.numberOfReceivers} " 
                + $" {TestHelper.Format(received)}/s received, {TestHelper.Format(sent)}/s sent." 
                + $" {TestHelper.Format(this.receiveCount)} received, {TestHelper.Format(this.sendCount)} sent in {TestHelper.Format(seconds)} seconds. ({TestHelper.Format(this.closeCount)} closed)");
        }

        void OnSendCompleted(Udp udp, Exception exception)
        {
            if (exception != null)
            {
                if (exception is OperationException error 
                    && error.ErrorCode == ErrorCode.ECANCELED)
                {
                    return;
                }

                Console.WriteLine($"Udp pummel {this.numberOfSenders}v{this.numberOfReceivers} failed, {exception}");
            }

            if (this.exiting)
            {
                return;
            }

            if (this.timeout > 0 
                || this.packetCounter > 0)
            {
                IPEndPoint endPoint = this.senders[udp];
                udp.QueueSend(this.content, endPoint, this.OnSendCompleted);
                this.sendCount++;
            }

            if (this.timeout != 0)
            {
                return;
            }

            this.packetCounter--;
            if (this.packetCounter == 0)
            {
                udp.CloseHandle(this.OnClose);
            }
        }

        void OnReceive(Udp udp, IDatagramReadCompletion completion)
        {
            if (completion.Error is OperationException error
                && error.ErrorCode == ErrorCode.ECANCELED) // UV_ECANCELED
            {
                return;
            }

            ReadableBuffer data = completion.Data;
            string message = data.ReadString(Encoding.UTF8);
            if (!string.IsNullOrEmpty(message) 
                && message != ExpectedMessage)
            {
                Console.WriteLine($"Udp pummel {this.numberOfSenders}v{this.numberOfReceivers} failed, wrong message '{message}' received.");
            }

            this.receiveCount++;
        }

        void OnTimer(Timer handle)
        {
            this.exiting = true;

            foreach (Udp udp in this.senders.Keys)
            {
                udp.CloseHandle(this.OnClose);
            }
            this.senders.Clear();

            foreach (Udp udp in this.receivers)
            {
                udp.CloseHandle(this.OnClose);
            }
            this.receivers = null;
        }

        void OnClose(Udp udp)
        {
            udp.Dispose();
            this.closeCount++;
        }

        public void Dispose()
        {
            Dictionary<Udp, IPEndPoint> dict = this.senders;
            this.senders = null;
            if (dict != null)
            {
                foreach (Udp handle in dict.Keys)
                {
                    handle.Dispose();
                }

                dict.Clear();
            }

            Udp[] handles = this.receivers;
            this.receivers = null;
            if (handles != null)
            {
                foreach (Udp handle in handles)
                {
                    handle.Dispose();
                }
            }

            this.timer?.Dispose();
            this.timer = null;

            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
