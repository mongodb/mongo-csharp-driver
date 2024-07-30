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
using FluentAssertions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors
{
    public class WritableServerSelectorTests
    {
        [Theory]
        [MemberData(nameof(ReplicaSetTestCases))]
        public void WritableServerSelector_should_work(
            ReadPreference readPreference,
            ClusterDescription cluster,
            ReadPreference expectedReadPreference,
            IEnumerable<ServerDescription> expectedServers)
        {
            IMayUseSecondaryCriteria mayUseSecondary = null;
            if (readPreference != null)
            {
                mayUseSecondary = new AggregateToCollectionOperation.MayUseSecondary(readPreference);
            }

            var selector = new WritableServerSelector(mayUseSecondary);
            var results = selector.SelectServers(cluster, cluster.Servers);

            results.Should().BeEquivalentTo(expectedServers);
            if (readPreference != null)
            {
                selector.MayUseSecondary.EffectiveReadPreference.Should().Be(expectedReadPreference);
            }
        }

        public static IEnumerable<object[]> ReplicaSetTestCases()
        {
            var clusterId = new ClusterId();
            var primary = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27017), ServerType.ReplicaSetPrimary, wireVersionRange: new Range<int>(WireVersion.Server70, WireVersion.Server70));
            var secondary1Server70 = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27018), ServerType.ReplicaSetSecondary, wireVersionRange: new Range<int>(WireVersion.Server70, WireVersion.Server70));
            var secondary2Server70 = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27019), ServerType.ReplicaSetSecondary, wireVersionRange: new Range<int>(WireVersion.Server70, WireVersion.Server70));
            var secondary3Server42 = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27020), ServerType.ReplicaSetSecondary, wireVersionRange: new Range<int>(WireVersion.Server42, WireVersion.Server42));

            yield return new object[]
            {
                ReadPreference.SecondaryPreferred,
                new ClusterDescription(
                    clusterId,
                    false,
                    null,
                    ClusterType.ReplicaSet,
                    Array.Empty<ServerDescription>()),
                ReadPreference.SecondaryPreferred,
                Array.Empty<ServerDescription>()
            };

            yield return new object[]
            {
                ReadPreference.SecondaryPreferred,
                new ClusterDescription(
                    clusterId,
                    false,
                    null,
                    ClusterType.ReplicaSet,
                    new[] { primary }),
                ReadPreference.SecondaryPreferred,
                new[] { primary }
            };

            yield return new object[]
            {
                ReadPreference.SecondaryPreferred,
                new ClusterDescription(
                    clusterId,
                    false,
                    null,
                    ClusterType.ReplicaSet,
                    new[] { primary, secondary1Server70, secondary2Server70 }),
                ReadPreference.SecondaryPreferred,
                new[] { secondary1Server70, secondary2Server70 }
            };

            yield return new object[]
            {
                ReadPreference.SecondaryPreferred,
                new ClusterDescription(
                    clusterId,
                    false,
                    null,
                    ClusterType.ReplicaSet,
                    new[] { primary, secondary1Server70, secondary2Server70, secondary3Server42 }),
                ReadPreference.Primary,
                new[] { primary }
            };
        }
    }
}
