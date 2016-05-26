/* Copyright 2013-2016 MongoDB Inc.
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
using System.Linq;
using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors
{
    public class RandomServerSelectorTests
    {
        private ClusterDescription _description;

        public RandomServerSelectorTests()
        {
            var clusterId = new ClusterId();
            _description = new ClusterDescription(
                clusterId,
                ClusterConnectionMode.Automatic,
                ClusterType.Unknown,
                new[]
                {
                    ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27017), averageRoundTripTime: TimeSpan.FromMilliseconds(10)),
                    ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27018), averageRoundTripTime: TimeSpan.FromMilliseconds(30)),
                    ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27019), averageRoundTripTime: TimeSpan.FromMilliseconds(20))
                });
        }

        [Fact]
        public void Should_select_a_random_server()
        {
            var subject = new RandomServerSelector();

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Count.Should().Be(1);
        }

        [Fact]
        public void Should_select_no_servers_when_none_exist()
        {
            var subject = new RandomServerSelector();

            var result = subject.SelectServers(_description, Enumerable.Empty<ServerDescription>()).ToList();

            result.Should().BeEmpty();
        }
    }
}