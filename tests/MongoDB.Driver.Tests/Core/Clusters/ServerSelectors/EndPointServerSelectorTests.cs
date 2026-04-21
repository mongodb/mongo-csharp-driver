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
using Xunit;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors;

public class EndPointServerSelectorTests
{
    private static readonly ClusterId __clusterId = new ClusterId();
    private static readonly ServerDescription __primary = ServerDescriptionHelper.Connected(__clusterId, new DnsEndPoint("localhost", 27017), ServerType.ReplicaSetPrimary, new TagSet(new[] { new Tag("a", "1") }));
    private static readonly ServerDescription __secondary1 = ServerDescriptionHelper.Connected(__clusterId, new DnsEndPoint("localhost", 27018), ServerType.ReplicaSetSecondary, new TagSet(new[] { new Tag("a", "1") }));
    private static readonly ServerDescription __secondary2 = ServerDescriptionHelper.Connected(__clusterId, new DnsEndPoint("localhost", 27019), ServerType.ReplicaSetSecondary, new TagSet(new[] { new Tag("a", "2") }));
    private static readonly ClusterDescription __description = new ClusterDescription(
        __clusterId,
        false,
        null,
        ClusterType.ReplicaSet,
        [__primary, __secondary1, __secondary2]);

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SelectServers_should_return_expected_results(ServerDescription server, IEnumerable<ServerDescription> servers, ServerDescription[] expected)
    {
        var subject = new EndPointServerSelector(server.EndPoint);

        var result = subject.SelectServers(__description, servers).ToList();

        result.ToArray().ShouldBeEquivalentTo(expected);
    }

    public static readonly object[][] TestCases =
    [
        [__primary, new[] { __primary, __secondary1, __secondary2 }, new[] { __primary } ],
        [__secondary2, new[] { __primary, __secondary1, __secondary2 }, new[] { __secondary2 } ],
        [__secondary2, new[] { __primary, __secondary1 }, Array.Empty<ServerDescription>() ],
        [__secondary2, Array.Empty<ServerDescription>(), Array.Empty<ServerDescription>() ],
    ];
}
