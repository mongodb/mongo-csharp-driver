/* Copyright 2019-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Misc
{
    internal class SrvRecord
    {
        public SrvRecord(DnsEndPoint endPoint, TimeSpan timeToLive)
        {
            EndPoint = Ensure.IsNotNull(endPoint, nameof(endPoint));
            TimeToLive = timeToLive;
        }

        public DnsEndPoint EndPoint { get; }
        public TimeSpan TimeToLive { get; }
    }

    internal class TxtRecord
    {
        public TxtRecord(List<string> strings)
        {
            Strings = Ensure.IsNotNull(strings, nameof(strings));
        }

        public List<string> Strings { get; }
    }

    internal interface IDnsResolver
    {
        List<SrvRecord> ResolveSrvRecords(string service, CancellationToken cancellation);
        Task<List<SrvRecord>> ResolveSrvRecordsAsync(string service, CancellationToken cancellation);
        List<TxtRecord> ResolveTxtRecords(string domainName, CancellationToken cancellation);
        Task<List<TxtRecord>> ResolveTxtRecordsAsync(string domainName, CancellationToken cancellation);
    }
}
