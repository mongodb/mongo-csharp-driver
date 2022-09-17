﻿/* Copyright 2018-present MongoDB Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class CoreSessionTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var cluster = Mock.Of<ICluster>();
            var serverSession = Mock.Of<ICoreServerSession>();
            var options = new CoreSessionOptions();

#pragma warning disable CS0618 // Type or member is obsolete
            var result = new CoreSession(cluster, serverSession, options);
#pragma warning restore CS0618 // Type or member is obsolete

            result.Cluster.Should().BeSameAs(cluster);
            result.CurrentTransaction.Should().BeNull();
            result.IsInTransaction.Should().BeFalse();
            result.Options.Should().BeSameAs(options);
            result.ServerSession.Should().BeSameAs(serverSession);
            result._disposed().Should().BeFalse();
            result._isCommitTransactionInProgress().Should().BeFalse();
        }

        [Fact]
        public void Cluster_should_return_expected_result()
        {
            var cluster = Mock.Of<ICluster>();
            var subject = CreateSubject(cluster: cluster);

            var result = subject.Cluster;

            result.Should().BeSameAs(cluster);
        }

        [Fact]
        public void ClusterTime_should_return_expected_result()
        {
            var subject = CreateSubject();
            var clusterTime = new BsonDocument();
            subject.AdvanceClusterTime(clusterTime);

            var result = subject.ClusterTime;

            result.Should().BeSameAs(clusterTime);
        }

        [Fact]
        public void Id_should_should_call_serverSession()
        {
            var subject = CreateSubject();
            var id = new BsonDocument();
            Mock.Get(subject.ServerSession).SetupGet(m => m.Id).Returns(id);

            var result = subject.Id;

            result.Should().Be(id);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsCausallyConsistent_should_return_expected_result(
            [Values(false, true)] bool value)
        {
            var options = new CoreSessionOptions(isCausallyConsistent: value);
            var subject = CreateSubject(options: options);

            var result = subject.IsCausallyConsistent;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsImplicit_should_return_expected_result(
            [Values(false, true)] bool value)
        {
            var options = new CoreSessionOptions(isImplicit: value);
            var subject = CreateSubject(options: options);

            var result = subject.IsImplicit;

            result.Should().Be(value);
        }

        [Theory]
        [InlineData(false, -1, false, false)]
        [InlineData(true, CoreTransactionState.Aborted, false, false)]
        [InlineData(true, CoreTransactionState.Committed, false, false)]
        [InlineData(true, CoreTransactionState.Committed, true, true)]
        [InlineData(true, CoreTransactionState.InProgress, false, true)]
        [InlineData(true, CoreTransactionState.Starting, false, true)]
        public void IsInTransaction_should_return_expected_result(bool hasCurrentTransaction, CoreTransactionState transactionState, bool isCommitTransactionInProgress, bool expectedResult)
        {
            var subject = CreateSubject();
            if (hasCurrentTransaction)
            {
                subject.StartTransaction();
                subject.CurrentTransaction.SetState(transactionState);
                subject._isCommitTransactionInProgress(isCommitTransactionInProgress);
            }

            var result = subject.IsInTransaction;

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void OperationTime_should_return_expected_result()
        {
            var subject = CreateSubject();
            var operationTime = new BsonTimestamp(0);
            subject.AdvanceOperationTime(operationTime);

            var result = subject.OperationTime;

            result.Should().BeSameAs(operationTime);
        }

        [Fact]
        public void ServerSession_should_return_expected_result()
        {
            var serverSession = Mock.Of<ICoreServerSession>();
            var subject = CreateSubject(serverSession: serverSession);

            var result = subject.ServerSession;

            result.Should().BeSameAs(serverSession);
        }

        [Theory]
        [InlineData(false, -1, false, false)]
        [InlineData(true, CoreTransactionState.Aborted, false, false)]
        [InlineData(true, CoreTransactionState.Committed, false, false)]
        [InlineData(true, CoreTransactionState.Committed, true, true)]
        [InlineData(true, CoreTransactionState.InProgress, false, true)]
        [InlineData(true, CoreTransactionState.Starting, false, true)]
        public void AboutToSendCommand_should_have_expected_result(bool hasCurrentTransaction, CoreTransactionState transactionState, bool isCommitTransactionInProgress, bool expectedHasCurrentTransaction)
        {
            var subject = CreateSubject();
            if (hasCurrentTransaction)
            {
                subject.StartTransaction();
                subject.CurrentTransaction.SetState(transactionState);
                subject._isCommitTransactionInProgress(isCommitTransactionInProgress);
            }

            subject.AboutToSendCommand();

            if (expectedHasCurrentTransaction)
            {
                subject.CurrentTransaction.Should().NotBeNull();
            }
            else
            {
                subject.CurrentTransaction.Should().BeNull();
            }
        }

        [Fact]
        public void AdvanceClusterTime_should_have_expected_result()
        {
            var subject = CreateSubject();
            var newClusterTime = new BsonDocument();

            subject.AdvanceClusterTime(newClusterTime);

            subject.ClusterTime.Should().BeSameAs(newClusterTime);
        }

        [Fact]
        public void AdvanceOperationTime_should_have_expected_result()
        {
            var subject = CreateSubject();
            var newOperationTime = new BsonTimestamp(0);

            subject.AdvanceOperationTime(newOperationTime);

            subject.OperationTime.Should().BeSameAs(newOperationTime);
        }

        [Theory]
        [ParameterAttributeData]
        public void Dispose_should_have_expected_result(
            [Values(1, 2)] int timesCalled)
        {
            var subject = CreateSubject();

            for (var i = 0; i < timesCalled; i++)
            {
                subject.Dispose();
            }

            subject._disposed().Should().BeTrue();
            Mock.Get(subject.ServerSession).Verify(m => m.Dispose(), Times.Once);
        }

        [SkippableFact]
        public void StartTransaction_should_throw_when_write_concern_is_unacknowledged()
        {
            RequireServer.Check().ClusterType(ClusterType.ReplicaSet).Supports(Feature.Transactions);
            var cluster = CoreTestConfiguration.Cluster;
            var session = cluster.StartSession();
            var transactionOptions = new TransactionOptions(writeConcern: WriteConcern.Unacknowledged);

            var exception = Record.Exception(() => session.StartTransaction(transactionOptions));

            var e = exception.Should().BeOfType<InvalidOperationException>().Subject;
            e.Message.ToLower().Should().Contain("transactions do not support unacknowledged write concerns");
        }

        [Fact]
        public void WasUsed_should_call_serverSession()
        {
            var subject = CreateSubject();

            subject.WasUsed();

            Mock.Get(subject.ServerSession).Verify(m => m.WasUsed(), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void EnsureTransactionsAreSupported_should_throw_when_there_are_no_connected_servers(
            [Values(0, 1, 2, 3)] int numberOfDisconnectedServers)
        {
            var clusterDescription = CreateClusterDescriptionWithDisconnectedServers(numberOfDisconnectedServers);
            var subject = CreateSubject(clusterDescription);

            var exception = Record.Exception(() => subject.EnsureTransactionsAreSupported());

            var e = exception.Should().BeOfType<NotSupportedException>().Subject;
            e.Message.Should().Be("StartTransaction cannot determine if transactions are supported because there are no connected servers.");
        }

        [Theory]
        [ParameterAttributeData]
        public void EnsureTransactionsAreSupported_should_not_throw_when_there_are_no_connected_servers_with_LB(
            [Values(0, 1, 2, 3)] int numberOfDisconnectedServers) // 0 - no servers at all
        {
            var clusterDescription = CreateClusterDescriptionWithDisconnectedServers(numberOfDisconnectedServers);
            clusterDescription = clusterDescription.WithType(ClusterType.LoadBalanced);
            var subject = CreateSubject(clusterDescription);

            subject.EnsureTransactionsAreSupported();       
        }

        // EnsureTransactionsAreSupported scenario codes
        // C = Connected, D = Disconnected
        // P = Primary, S = Secondary, A = Arbiter, R = ShardRouter, U = Unknown
        // T = transactions are supported, N = transactions are not supported

        [Theory]
        [InlineData("DU,CP")]
        [InlineData("CP,DU")]
        [InlineData("DU,CR")]
        [InlineData("CR,DU")]
        [InlineData("DU,CL")]
        [InlineData("CL,DU")]
        public void EnsureTransactionsAreSupported_should_ignore_disconnected_servers(string scenarios)
        {
            var clusterId = new ClusterId(1);
            var servers =
                SplitScenarios(scenarios)
                .Select((scenario, i) =>
                {
                    var endPoint = new DnsEndPoint("localhost", 27017 + i);
                    var serverId = new ServerId(clusterId, endPoint);
                    var state = MapServerStateCode(scenario[0]);
                    var type = MapServerTypeCode(scenario[1]);
                    var maxWireVersion = type switch
                    {
                        ServerType.ShardRouter => Feature.ShardedTransactions.FirstSupportedWireVersion,
                        ServerType.LoadBalanced => Feature.LoadBalancedMode.FirstSupportedWireVersion,
                        _ => Feature.Transactions.FirstSupportedWireVersion,
                    };
                    return CreateServerDescription(serverId, endPoint, state, type, maxWireVersion);
                })
                .ToList();
            var cluster = CreateClusterDescription(clusterId, servers: servers);
            var subject = CreateSubject(cluster);

            subject.EnsureTransactionsAreSupported();
        }

        [Theory]
        [InlineData("")]
        [InlineData("DU")]
        [InlineData("DL")]
        [InlineData("CA")]
        [InlineData("DU,DU")]
        [InlineData("DL,DL")]
        [InlineData("DU,CA")]
        [InlineData("CA,DU")]
        [InlineData("CA,CA")]
        [InlineData("CA,DL")]
        public void EnsureTransactionsAreSupported_should_throw_when_there_are_no_connected_data_bearing_servers(string scenarios)
        {
            var clusterId = new ClusterId(1);
            var servers =
                SplitScenarios(scenarios)
                .Select((scenario, i) =>
                {
                    var endPoint = new DnsEndPoint("localhost", 27017 + i);
                    var serverId = new ServerId(clusterId, endPoint);
                    var state = MapServerStateCode(scenario[0]);
                    var type = MapServerTypeCode(scenario[1]);
                    return CreateServerDescription(serverId, endPoint, state, type);
                })
                .ToList();
            var cluster = CreateClusterDescription(clusterId, servers: servers);
            var subject = CreateSubject(cluster);

            var exception = Record.Exception(() => subject.EnsureTransactionsAreSupported());

            var e = exception.Should().BeOfType<NotSupportedException>().Subject;
            e.Message.Should().Be("StartTransaction cannot determine if transactions are supported because there are no connected servers.");
        }

        [Theory]
        [InlineData("NT", "Standalone servers do not support transactions.")]
        [InlineData("PN", "Server version 3.6 does not support the Transactions feature.")]
        [InlineData("PN,ST", "Server version 3.6 does not support the Transactions feature.")]
        [InlineData("PT,SN", "Server version 3.6 does not support the Transactions feature.")]
        [InlineData("RN", "Server version 4.0 does not support the ShardedTransactions feature.")]
        [InlineData("RN,RT", "Server version 4.0 does not support the ShardedTransactions feature.")]
        [InlineData("RT,RN", "Server version 4.0 does not support the ShardedTransactions feature.")]
        public void EnsureTransactionsAreSupported_should_throw_when_any_connected_data_bearing_server_does_not_support_transactions(string scenarios, string expectedMesage)
        {
            var clusterId = new ClusterId(1);
            string unsupportedFeatureName = null;
            var servers =
                SplitScenarios(scenarios)
                .Select((scenario, i) =>
                {
                    var endPoint = new DnsEndPoint("localhost", 27017 + i);
                    var serverId = new ServerId(clusterId, endPoint);
                    var type = MapServerTypeCode(scenario[0]);
                    var supportsTransactions = MapSupportsTransactionsCode(scenario[1]);
                    var feature = type == ServerType.ShardRouter ? Feature.ShardedTransactions : Feature.Transactions;
                    if (!supportsTransactions)
                    {
                        unsupportedFeatureName = feature.Name;
                    }
                    var maxWireVersion = supportsTransactions ? feature.FirstSupportedWireVersion: feature.LastNotSupportedWireVersion;
                    return CreateServerDescription(serverId, endPoint, ServerState.Connected, type, maxWireVersion);
                })
                .ToList();
            var cluster = CreateClusterDescription(clusterId, servers: servers);
            var subject = CreateSubject(cluster);

            var exception = Record.Exception(() => subject.EnsureTransactionsAreSupported());

            var e = exception.Should().BeOfType<NotSupportedException>().Subject;
            e.Message.Should().Be(expectedMesage);
        }

        // private methods
        private ClusterDescription CreateClusterDescription(
            ClusterId clusterId = null,
#pragma warning disable CS0618 // Type or member is obsolete
            ClusterConnectionMode connectionMode = ClusterConnectionMode.Automatic,
#pragma warning restore CS0618 // Type or member is obsolete
            ClusterType type = ClusterType.Unknown,
            IEnumerable<ServerDescription> servers = null)
        {
            clusterId = clusterId ?? new ClusterId(1);
            servers = servers ?? new ServerDescription[0];
#pragma warning disable CS0618 // Type or member is obsolete
            return new ClusterDescription(clusterId, connectionMode, type, servers);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private ClusterDescription CreateClusterDescriptionWithDisconnectedServers(int numberOfDisconnectedServers)
        {
            var clusterId = new ClusterId(1);
            var servers = Enumerable.Range(27017, numberOfDisconnectedServers).Select(port => CreateDisconnectedServerDescription(clusterId, port)).ToList();
            return CreateClusterDescription(servers: servers);
        }

        private ServerDescription CreateDisconnectedServerDescription(ClusterId clusterId, int port)
        {
            var endPoint = new DnsEndPoint("localhost", port);
            var serverId = new ServerId(clusterId, endPoint);
            return new ServerDescription(serverId, endPoint, state: ServerState.Disconnected, type: ServerType.Unknown);
        }

        private ICluster CreateMockReplicaSetCluster()
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            var maxWireVersion = Feature.Transactions.FirstSupportedWireVersion;
            var servers = new[] { new ServerDescription(serverId, endPoint, state: ServerState.Connected, type: ServerType.ReplicaSetPrimary, version: WireVersion.ToServerVersion(maxWireVersion), wireVersionRange: new Range<int>(0, maxWireVersion)) };
#pragma warning disable CS0618 // Type or member is obsolete
            var clusterDescription = new ClusterDescription(clusterId, ClusterConnectionMode.Automatic, ClusterType.ReplicaSet, servers);
#pragma warning restore CS0618 // Type or member is obsolete
            var mockCluster = new Mock<ICluster>();
            mockCluster.SetupGet(m => m.Description).Returns(clusterDescription);
            return mockCluster.Object;
        }

        private ServerDescription CreateServerDescription(
            ServerId serverId = null,
            EndPoint endPoint = null,
            ServerState state = ServerState.Disconnected,
            ServerType type = ServerType.Unknown,
            int? maxWireVersion = null)
        {
            endPoint = endPoint ?? new DnsEndPoint("localhost", 27017);
            serverId = serverId ?? new ServerId(new ClusterId(1), endPoint);

            maxWireVersion = maxWireVersion ?? WireVersion.Server40;
            var approximateServerVersion = WireVersion.ToServerVersion(maxWireVersion.Value);
            return new ServerDescription(serverId, endPoint, state: state, type: type, version: approximateServerVersion, wireVersionRange: new Optional<Range<int>>(new Range<int>(0, maxWireVersion.Value)));
        }

        private CoreSession CreateSubject(
            ICluster cluster = null,
            ICoreServerSession serverSession = null,
            CoreSessionOptions options = null)
        {
            cluster = cluster ?? CreateMockReplicaSetCluster();
            serverSession = serverSession ?? Mock.Of<ICoreServerSession>();
            options = options ?? new CoreSessionOptions();
#pragma warning disable CS0618 // Type or member is obsolete
            return new CoreSession(cluster, serverSession, options);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private CoreSession CreateSubject(ClusterDescription clusterDescription)
        {
            var mockCluster = new Mock<ICluster>();
            mockCluster.SetupGet(m => m.Description).Returns(clusterDescription);
            return CreateSubject(cluster: mockCluster.Object);
        }

        private ServerState MapServerStateCode(char code)
        {
            switch (code)
            {
                case 'C': return ServerState.Connected;
                case 'D': return ServerState.Disconnected;
                default: throw new ArgumentException($"Invalid ServerState code: \"{code}\".", nameof(code));
            }
        }

        private ServerType MapServerTypeCode(char code)
        {
            switch (code)
            {
                case 'A': return ServerType.ReplicaSetArbiter;
                case 'N': return ServerType.Standalone;
                case 'P': return ServerType.ReplicaSetPrimary;
                case 'R': return ServerType.ShardRouter;
                case 'S': return ServerType.ReplicaSetSecondary;
                case 'U': return ServerType.Unknown;
                case 'L': return ServerType.LoadBalanced;
                default: throw new ArgumentException($"Invalid ServerType code: \"{code}\".", nameof(code));
            }
        }

        private bool MapSupportsTransactionsCode(char code)
        {
            switch (code)
            {
                case 'N': return false;
                case 'T': return true;
                default: throw new ArgumentException($"Invalid SupportsTransactions code: \"{code}\".", nameof(code));
            }
        }

        private IEnumerable<string> SplitScenarios(string scenarios)
        {
            if (scenarios == "")
            {
                return Enumerable.Empty<string>();
            }
            else
            {
                return scenarios.Split(',');
            }
        }
    }

    public static class CoreSessionReflector
    {
        public static bool _disposed(this CoreSession obj) => (bool)Reflector.GetFieldValue(obj, nameof(_disposed));
        public static bool _isCommitTransactionInProgress(this CoreSession obj) => (bool)Reflector.GetFieldValue(obj, nameof(_isCommitTransactionInProgress));
        public static void _isCommitTransactionInProgress(this CoreSession obj, bool value)
        {
            Reflector.SetFieldValue(obj, nameof(_isCommitTransactionInProgress), value);
        }

        public static void EnsureTransactionsAreSupported(this CoreSession obj) => Reflector.Invoke(obj, nameof(EnsureTransactionsAreSupported));
    }
}
