/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Clusters;
using FluentAssertions;
using NUnit.Framework;
using MongoDB.Driver.Core.Clusters.Monitoring;
using MongoDB.Driver.Core.Servers;
using System.Net;
using System.Linq;

namespace MongoDB.Driver.Core.Tests.Clusters.Monitoring
{
    [TestFixture]
    public class ClusterMonitorLogicTests
    {
        #region static
        // static fields
        private readonly static ServerDescription __port27017Disconnected = ServerDescription.CreateDisconnectedServerDescription(new DnsEndPoint("localhost", 27017));
        private readonly static ServerDescription __port27018Disconnected = ServerDescription.CreateDisconnectedServerDescription(new DnsEndPoint("localhost", 27018));
        private readonly static ClusterDescription __emptyClusterDescription = new ClusterDescription(ClusterType.Unknown, ClusterState.Disconnected, Enumerable.Empty<ServerDescription>(), null, 0);
        #endregion

        [Test]
        public void Constructor_should_throw_if_newServerDescription_is_null()
        {
            Action action = () => new ClusterMonitorLogic(__emptyClusterDescription, null);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_if_newServerDescription_is_not_member_of_cluster()
        {
            var oldClusterDescription = new ClusterDescription(ClusterType.ReplicaSet, ClusterState.Disconnected, new[] { __port27017Disconnected }, null, 0);
            Action action = () => new ClusterMonitorLogic(oldClusterDescription, __port27018Disconnected);
            action.ShouldThrow<ArgumentException>();
        }

        [TestCase(ClusterType.Direct)]
        [TestCase(ClusterType.Standalone)]
        public void Constructor_should_throw_if_oldClusterDescription_type_is_not_valid(ClusterType clusterType)
        {
            var oldClusterDescription = new ClusterDescription(clusterType, ClusterState.Disconnected, Enumerable.Empty<ServerDescription>(), null, 0);
            Action action = () => new ClusterMonitorLogic(oldClusterDescription, __port27017Disconnected);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Constructor_should_throw_if_oldClusterDescription_is_null()
        {
            Action action = () => new ClusterMonitorLogic(null, __port27017Disconnected);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Transition_should_set_clusterType_if_it_is_unknown()
        {
            var oldClusterDescription = new ClusterDescription(ClusterType.Unknown, ClusterState.Disconnected, new[] { __port27017Disconnected }, null, 0);
            var newServerDescription = __port27017Disconnected.WithState(ServerState.Connected).WithType(ServerType.ReplicaSetPrimary);
            var subject = new ClusterMonitorLogic(oldClusterDescription, newServerDescription);
            var actions = subject.Transition().ToArray();
            var newClusterDescription = oldClusterDescription
                .WithType(ClusterType.ReplicaSet)
                .WithServerDescription(newServerDescription);
            var expectedUpdateClusterDescriptionAction = new UpdateClusterDescriptionAction(newClusterDescription);
            actions.Should().ContainInOrder(new [] { expectedUpdateClusterDescriptionAction });
        }
    }
}
