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
using MongoDB.Driver.Core.Connections;
using MongoDB.Bson;

namespace MongoDB.Driver.Core.Tests.Clusters.Monitoring
{
    [TestFixture]
    public class ClusterMonitorLogicTests
    {
        #region static
        // static fields
        private static readonly ClusterId __clusterId;
        private static readonly ServerDescription __port27017Disconnected;
        private static readonly ServerDescription __port27018Disconnected;
        private static readonly ClusterDescription __emptyClusterDescription = new ClusterDescription(ClusterType.Unknown, ClusterState.Disconnected, Enumerable.Empty<ServerDescription>(), null, 0);

        // static constructor
        static ClusterMonitorLogicTests()
        {
            __clusterId = new ClusterId();
            __emptyClusterDescription = new ClusterDescription(ClusterType.Unknown, ClusterState.Disconnected, Enumerable.Empty<ServerDescription>(), null, 0);
            var endPoint27017 = new DnsEndPoint("localhost", 27017);
            var endPoint27018 = new DnsEndPoint("localhost", 27018);
            var serverId27017 = new ServerId(__clusterId, endPoint27017);
            var serverId27018 = new ServerId(__clusterId, endPoint27018);
            __port27017Disconnected = new ServerDescription(serverId27017, endPoint27017);
            __port27018Disconnected = new ServerDescription(serverId27018, endPoint27018);
        }
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
    }
}
