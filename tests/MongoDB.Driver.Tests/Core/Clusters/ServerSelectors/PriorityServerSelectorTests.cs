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

using System.Linq;
using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors;

public class PriorityServerSelectorTests
{
    private readonly ClusterDescription _description;
    private readonly ServerDescription _server1;
    private readonly ServerDescription _server2;
    private readonly ServerDescription _server3;

    public PriorityServerSelectorTests()
    {
        var clusterId = new ClusterId();

        _server1 = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27017));
        _server2 = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27018));
        _server3 = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27019));

        _description = new ClusterDescription(
            clusterId,
            false,
            null,
            ClusterType.Sharded,
            [_server1, _server2, _server3]);
    }

    [Fact]
    public void Should_select_all_the_servers_not_deprioritized()
    {
        var subject = new PriorityServerSelector(new[] { _server1, _server2 });

        var result = subject.SelectServers(_description, _description.Servers).ToList();

        result.Count.Should().Be(1);
        result.Should().BeEquivalentTo(_server3);
    }

    [Fact]
    public void Should_select_all_the_servers_if_all_servers_are_deprioritized()
    {
        var subject = new PriorityServerSelector(new[] { _server1, _server2, _server3});

        var result = subject.SelectServers(_description, _description.Servers).ToList();

        result.Count.Should().Be(3);
        result.Should().BeEquivalentTo(_description.Servers);
    }

    [Fact]
    public void Should_ignore_deprioritized_servers_if_not_in_sharded_mode()
    {
        var changedDescription = _description.WithType(ClusterType.Unknown);

        var subject = new PriorityServerSelector(new[] { _server2, _server3 });

        var result = subject.SelectServers(changedDescription, _description.Servers).ToList();

        result.Count.Should().Be(3);
        result.Should().BeEquivalentTo(_description.Servers);
    }
}
