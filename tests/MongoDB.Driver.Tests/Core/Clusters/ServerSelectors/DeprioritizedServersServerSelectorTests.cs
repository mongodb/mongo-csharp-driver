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
using System.Linq;
using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors;

public class DeprioritizedServersServerSelectorTests
{
    private readonly ClusterDescription _cluster;
    private readonly ServerDescription _server1;
    private readonly ServerDescription _server2;
    private readonly ServerDescription _server3;

    public DeprioritizedServersServerSelectorTests()
    {
        var clusterId = new ClusterId();

        _server1 = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27017), ServerType.ReplicaSetPrimary);
        _server2 = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27018), ServerType.ReplicaSetSecondary);
        _server3 = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27019), ServerType.ReplicaSetSecondary);

        _cluster = new ClusterDescription(
            clusterId,
            false,
            null,
            ClusterType.ReplicaSet,
            [_server1, _server2, _server3]);
    }

    [Fact]
    public void Ctor_throw_on_null_deprioritizedServers()
    {
        var selector = Mock.Of<IServerSelector>();
        var ex = Record.Exception(() => new DeprioritizedServersServerSelector(null, selector));

        ex.Should().BeOfType<ArgumentNullException>().Subject
            .ParamName.Should().Be("deprioritizedServers");
    }

    [Fact]
    public void Ctor_throws_on_null_wrappedServerSelector()
    {
        var ex = Record.Exception(() => new DeprioritizedServersServerSelector([_server1], null));

        ex.Should().BeOfType<ArgumentNullException>().Subject
            .ParamName.Should().Be("wrappedServerSelector");
    }

    [Fact]
    public void SelectServers_should_exclude_deprioritized_servers()
    {
        var selectorMock = new Mock<IServerSelector>();
        selectorMock.Setup(s => s.SelectServers(It.IsAny<ClusterDescription>(), It.IsAny<IEnumerable<ServerDescription>>()))
            .Returns((ClusterDescription _, IEnumerable<ServerDescription> servers) => servers);
        var subject = new DeprioritizedServersServerSelector([_server1], selectorMock.Object);

        var result = subject.SelectServers(_cluster, [_server1, _server2, _server3]);

        selectorMock.Verify(s => s.SelectServers(It.IsAny<ClusterDescription>(), It.IsAny<IEnumerable<ServerDescription>>()), Times.Once);
        result.Should().Match(servers => servers.SequenceEqual(new[] { _server2, _server3 }, ServerDescriptionComparerByEndPoint.Instance));
    }

    [Fact]
    public void SelectServers_should_ignore_deprioritized_servers_if_wrapped_selector_returns_empty_list()
    {
        var selectorMock = new Mock<IServerSelector>();
        var deprioritizedServers = new[] { _server1 };
        var allServers = new[] { _server1, _server2, _server3 };
        selectorMock.Setup(s => s.SelectServers(It.IsAny<ClusterDescription>(), It.IsAny<IEnumerable<ServerDescription>>()))
            .Returns((ClusterDescription _, IEnumerable<ServerDescription> servers) => servers.Intersect(deprioritizedServers, ServerDescriptionComparerByEndPoint.Instance));
        var subject = new DeprioritizedServersServerSelector(deprioritizedServers, selectorMock.Object);

        var result = subject.SelectServers(_cluster, [_server1, _server2, _server3]);

        var filteredServers = new[] { _server2, _server3 };
        selectorMock.Verify(s => s.SelectServers(It.IsAny<ClusterDescription>(), It.IsAny<IEnumerable<ServerDescription>>()), Times.Exactly(2));
        selectorMock.Verify(s => s.SelectServers(
                It.IsAny<ClusterDescription>(),
                It.Is<IEnumerable<ServerDescription>>(servers => servers.SequenceEqual(filteredServers, new ServerDescriptionComparerByEndPoint()))),
            Times.Once);
        selectorMock.Verify(s => s.SelectServers(
                It.IsAny<ClusterDescription>(),
                It.Is<IEnumerable<ServerDescription>>(servers => servers.SequenceEqual(allServers, new ServerDescriptionComparerByEndPoint()))),
            Times.Once);

        result.Should().Match(servers => servers.SequenceEqual(deprioritizedServers, new ServerDescriptionComparerByEndPoint()));
    }

    private class ServerDescriptionComparerByEndPoint : IEqualityComparer<ServerDescription>
    {
        public static ServerDescriptionComparerByEndPoint Instance = new();

        public bool Equals(ServerDescription x, ServerDescription y) => x?.EndPoint == y?.EndPoint;

        public int GetHashCode(ServerDescription obj) => obj.EndPoint.GetHashCode();
    }
}

