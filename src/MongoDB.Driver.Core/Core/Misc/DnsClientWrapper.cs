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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;

namespace MongoDB.Driver.Core.Misc
{
    internal class DnsClientWrapper : IDnsResolver
    {
        #region static
        private static IDnsResolver __instance = new DnsClientWrapper();
        public static IDnsResolver Instance => __instance;
        #endregion

        // private fields
        private readonly LookupClient _lookupClient;

        // constructors
        private DnsClientWrapper()
        {
            _lookupClient = new LookupClient();
        }

        // public methods
        public List<SrvRecord> ResolveSrvRecords(string service, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(service, nameof(service));
            cancellationToken.ThrowIfCancellationRequested();
            var response = _lookupClient.Query(service, QueryType.SRV, QueryClass.IN);
            return GetSrvRecords(response);
        }

        public async Task<List<SrvRecord>> ResolveSrvRecordsAsync(string service, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(service, nameof(service));
            var response = await _lookupClient.QueryAsync(service, QueryType.SRV, QueryClass.IN, cancellationToken).ConfigureAwait(false);
            return GetSrvRecords(response);
        }

        public List<TxtRecord> ResolveTxtRecords(string domainName, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(domainName, nameof(domainName));
            cancellationToken.ThrowIfCancellationRequested();
            var response = _lookupClient.Query(domainName, QueryType.TXT, QueryClass.IN);
            return GetTxtRecords(response);
        }

        public async Task<List<TxtRecord>> ResolveTxtRecordsAsync(string domainName, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(domainName, nameof(domainName));
            var response = await _lookupClient.QueryAsync(domainName, QueryType.TXT, QueryClass.IN, cancellationToken).ConfigureAwait(false);
            return GetTxtRecords(response);
        }

        // private methods
        private List<SrvRecord> GetSrvRecords(IDnsQueryResponse response)
        {
            var wrappedSrvRecords = response.Answers.SrvRecords().ToList();
            var srvRecords = new List<SrvRecord>();
            
            foreach (var wrappedSrvRecord in wrappedSrvRecords)
            {
                var host = wrappedSrvRecord.Target.ToString();
                var port = wrappedSrvRecord.Port;
                var endPoint = new DnsEndPoint(host, port);
                var timeToLive = TimeSpan.FromSeconds(wrappedSrvRecord.InitialTimeToLive);
                var srvRecord = new SrvRecord(endPoint, timeToLive);

                srvRecords.Add(srvRecord);
            }

            return srvRecords;
        }

        private List<TxtRecord> GetTxtRecords(IDnsQueryResponse response)
        {
            var wrappedTxtRecords = response.Answers.TxtRecords().ToList();
            var txtRecords = new List<TxtRecord>();

            foreach (var wrappedTxtRecord in wrappedTxtRecords)
            {
                var strings = wrappedTxtRecord.Text.ToList();
                var txtRecord = new TxtRecord(strings);

                txtRecords.Add(txtRecord);
            }

            return txtRecords;
        }
    }
}
