// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using NetUV.Core.Handles;
    using NetUV.Core.Requests;

    sealed class GetAddrInfo : IDisposable
    {
        const string Name = "localhost";
        const int TotalCalls = 10000;
        const int ConcurrentCalls = 10;

        Loop loop;
        List<AddressInfoRequest> requests;
        int callsInitiated;
        int callsCompleted;

        public GetAddrInfo()
        {
            this.callsInitiated = 0;
            this.callsCompleted = 0;
            this.requests = new List<AddressInfoRequest>();
            this.loop = new Loop();
        }

        public void Run()
        {
            this.loop.UpdateTime();
            long startTime = this.loop.Now;

            for (int i = 0; i < ConcurrentCalls; i++)
            {
                AddressInfoRequest request = this.CreateRequest();
                this.requests.Add(request);
            }

            this.loop.RunDefault();

            this.loop.UpdateTime();
            long endTime = this.loop.Now;

            if (this.callsInitiated != TotalCalls
                || this.callsCompleted != TotalCalls)
            {
                Console.WriteLine($"Getaddrinfo : failed Initiated = {this.callsInitiated}, completed = {this.callsCompleted} expecting {TotalCalls}.");
            }
            else
            {
                double value = (double)this.callsCompleted / (endTime - startTime) * 1000;
                Console.WriteLine($"Getaddrinfo : {TestHelper.Format(value)} req/s.");
            }
        }

        void OnAddressInfo(AddressInfoRequest request, AddressInfo info)
        {
            if (info.Error != null)
            {
                Console.WriteLine($"Getaddrinfo : failed {info.Error}");
            }
            this.callsCompleted++;

            request.Dispose();
            if (this.callsInitiated >= TotalCalls)
            {
                return;
            }

            int index = this.requests.IndexOf(request);
            this.requests[index] = this.CreateRequest();
        }

        AddressInfoRequest CreateRequest()
        {
            AddressInfoRequest request = this.loop
                .CreateAddressInfoRequest()
                .Start(Name, null, this.OnAddressInfo);

            this.callsInitiated++;

            return request;
        }

        public void Dispose()
        {
            this.requests?.ForEach(x => x.Dispose());
            this.requests?.Clear();
            this.requests = null;

            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
