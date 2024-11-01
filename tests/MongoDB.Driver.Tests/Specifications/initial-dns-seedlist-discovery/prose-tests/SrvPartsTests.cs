/* Copyright 2010-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.initial_dns_seedlist_discovery.prose_tests
{
    public class SrvPartsTests
    {
        // https://github.com/mongodb/specifications/blob/master/source/initial-dns-seedlist-discovery/tests/README.md#1-allow-srvs-with-fewer-than-3--separated-parts
        [Theory]
        [InlineData("mongodb+srv://localhost")]
        [InlineData("mongodb+srv://mongo.local")]
        public void Allow_srv_with_fewer_than_3_parts(string uri)
        {
            var exception = Record.Exception(() => new ConnectionString(uri));

            exception.Should().BeNull();
        }

        // https://github.com/mongodb/specifications/blob/master/source/initial-dns-seedlist-discovery/tests/README.md#2-throw-when-return-address-does-not-end-with-srv-domain
        [Theory]
        [InlineData("mongodb+srv://localhost", "localhost.mongodb")]
        [InlineData("mongodb+srv://mongo.local", "test_1.evil.local")]
        [InlineData("mongodb+srv://blogs.mongodb.com", "blogs.evil.com")]
        public void Throw_when_return_address_does_not_end_with_srv_domain(string uri, string resolvedHost)
        {
            var connectionString = new ConnectionString(uri, true, new MockDnsResolver(resolvedHost));
            var exception = Record.Exception(() => connectionString.Resolve());

            exception.Should().BeOfType<MongoConfigurationException>().
                Which.Message.Should().Be("Hosts in the SRV record must have the same parent domain as the seed host.");
        }

        // https://github.com/mongodb/specifications/blob/master/source/initial-dns-seedlist-discovery/tests/README.md#3-throw-when-return-address-is-identical-to-srv-hostname
        [Theory]
        [InlineData("mongodb+srv://localhost", "localhost")]
        [InlineData("mongodb+srv://mongo.local", "mongo.local")]
        public void Throw_when_return_address_is_identical_to_srv_hostname(string uri, string resolvedHost)
        {
            var connectionString = new ConnectionString(uri, true, new MockDnsResolver(resolvedHost));
            var exception = Record.Exception(() => connectionString.Resolve());

            exception.Should().BeOfType<MongoConfigurationException>().
                Which.Message.Should().Be("Hosts in the SRV record must have the same parent domain as the seed host.");
        }

        // https://github.com/mongodb/specifications/blob/master/source/initial-dns-seedlist-discovery/tests/README.md#4-throw-when-return-address-does-not-contain--separating-shared-part-of-domain
        [Theory]
        [InlineData("mongodb+srv://localhost", "test_1.cluster_1localhost")]
        [InlineData("mongodb+srv://mongo.local", "test_1.my_hostmongo.local")]
        [InlineData("mongodb+srv://blogs.mongodb.com", "cluster.testmongodb.com")]
        public void Throw_when_return_address_does_not_contain_dot_separating_shared_part_of_domain(string uri, string resolvedHost)
        {
            var connectionString = new ConnectionString(uri, true, new MockDnsResolver(resolvedHost));
            var exception = Record.Exception(() => connectionString.Resolve());

            exception.Should().BeOfType<MongoConfigurationException>().
                Which.Message.Should().Be("Hosts in the SRV record must have the same parent domain as the seed host.");
        }

        private class MockDnsResolver(string dnsEndPointString) : IDnsResolver
        {
            public List<SrvRecord> ResolveSrvRecords(string service, CancellationToken cancellation)
            {
                return new List<SrvRecord>
                    { new SrvRecord(new DnsEndPoint(dnsEndPointString, 2090), TimeSpan.MaxValue) };
            }

            public List<TxtRecord> ResolveTxtRecords(string domainName, CancellationToken cancellation)
                => throw new NotImplementedException();

            public Task<List<SrvRecord>> ResolveSrvRecordsAsync(string service, CancellationToken cancellation)
                => throw new NotImplementedException();

            public Task<List<TxtRecord>> ResolveTxtRecordsAsync(string domainName, CancellationToken cancellation)
                => throw new NotImplementedException();

        }
    }
}