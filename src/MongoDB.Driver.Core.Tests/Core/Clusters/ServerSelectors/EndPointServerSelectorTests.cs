/* Copyright 2013-2015 MongoDB Inc.
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

using System.Linq;
using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors
{
    [TestFixture]
    public class EndPointServerSelectorTests
    {
        private ClusterDescription _description;

        [SetUp]
        public void Setup()
        {
            var clusterId = new ClusterId();
            _description = new ClusterDescription(
                clusterId,
                ClusterConnectionMode.Automatic,
                ClusterType.Unknown,
                new[]
                {
                    ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27017)),
                    ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27018)),
                    ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27019)),
                });
        }

        [Test]
        public void Should_select_the_server_if_it_exists()
        {
            var subject = new EndPointServerSelector(new DnsEndPoint("localhost", 27017));

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Count.Should().Be(1);
            result.Should().BeEquivalentTo(_description.Servers[0]);
        }

        [Test]
        public void Should_return_empty_if_the_server_does_not_exist()
        {
            var subject = new EndPointServerSelector(new DnsEndPoint("blargh", 27017));

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Should().BeEmpty();
        }

        [Test]
        public void Should_select_no_servers_when_none_exist()
        {
            var subject = new EndPointServerSelector(new DnsEndPoint("blargh", 27017));

            var result = subject.SelectServers(_description, Enumerable.Empty<ServerDescription>()).ToList();

            result.Should().BeEmpty();
        }
    }
}