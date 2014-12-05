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
using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Clusters
{
    [TestFixture]
    public class SingleServerClusterTests
    {
        private IClusterListener _clusterListener;
        private IClusterableServerFactory _serverFactory;
        private ClusterSettings _settings;

        [SetUp]
        public void Setup()
        {
            _settings = new ClusterSettings();
            _serverFactory = Substitute.For<IClusterableServerFactory>();
            _clusterListener = Substitute.For<IClusterListener>();
        }

        [Test]
        public void Constructor_should_throw_if_more_than_one_endpoint_is_specified()
        {
            _settings = _settings.With(endPoints: new[] { new DnsEndPoint("localhost", 27017), new DnsEndPoint("localhost", 27018) });
            Action act = () => new SingleServerCluster(_settings, _serverFactory, _clusterListener);

            act.ShouldThrow<ArgumentException>();
        }
        
        [Test]
        public void Initialize_should_throw_if_disposed()
        {
            var subject = CreateSubject();
            subject.Dispose();
            
            Action act = () => subject.Initialize();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void Initialize_should_create_and_initialize_the_server()
        {
            var server = Substitute.For<IClusterableServer>();
            _serverFactory.CreateServer(null, null).ReturnsForAnyArgs(server);

            var subject = CreateSubject();
            subject.Initialize();

            server.Received().Initialize();
        }

        [Test]
        [TestCase(ClusterConnectionMode.ReplicaSet, ServerType.ShardRouter)]
        [TestCase(ClusterConnectionMode.ReplicaSet, ServerType.Standalone)]
        [TestCase(ClusterConnectionMode.Standalone, ServerType.ReplicaSetArbiter)]
        [TestCase(ClusterConnectionMode.Standalone, ServerType.ReplicaSetGhost)]
        [TestCase(ClusterConnectionMode.Standalone, ServerType.ReplicaSetOther)]
        [TestCase(ClusterConnectionMode.Standalone, ServerType.ReplicaSetPassive)]
        [TestCase(ClusterConnectionMode.Standalone, ServerType.ReplicaSetPrimary)]
        [TestCase(ClusterConnectionMode.Standalone, ServerType.ReplicaSetSecondary)]
        [TestCase(ClusterConnectionMode.Standalone, ServerType.ShardRouter)]
        [TestCase(ClusterConnectionMode.Sharded, ServerType.ReplicaSetArbiter)]
        [TestCase(ClusterConnectionMode.Sharded, ServerType.ReplicaSetGhost)]
        [TestCase(ClusterConnectionMode.Sharded, ServerType.ReplicaSetOther)]
        [TestCase(ClusterConnectionMode.Sharded, ServerType.ReplicaSetPassive)]
        [TestCase(ClusterConnectionMode.Sharded, ServerType.ReplicaSetPrimary)]
        [TestCase(ClusterConnectionMode.Sharded, ServerType.ReplicaSetSecondary)]
        [TestCase(ClusterConnectionMode.Sharded, ServerType.Standalone)]
        public void Description_should_not_contain_any_servers_if_the_provided_server_is_not_of_the_required_type(ClusterConnectionMode connectionMode, ServerType serverType)
        {
            var server = Substitute.For<IClusterableServer>();
            _serverFactory.CreateServer(null, null).ReturnsForAnyArgs(server);

            _settings = _settings.With(connectionMode: connectionMode);

            var subject = CreateSubject();
            subject.Initialize();

            var connected = ServerDescriptionHelper.Connected(subject.Description.ClusterId, serverType: serverType);

            server.DescriptionChanged += Raise.EventWith(new object(), new ServerDescriptionChangedEventArgs(connected, connected));

            subject.Description.Servers.Should().BeEmpty();
        }

        [Test]
        public void Description_should_regain_a_server_if_the_provided_server_is_rebooted_to_its_expected_type()
        {
            var server = Substitute.For<IClusterableServer>();
            _serverFactory.CreateServer(null, null).ReturnsForAnyArgs(server);

            _settings = _settings.With(connectionMode: ClusterConnectionMode.Standalone);

            var subject = CreateSubject();
            subject.Initialize();

            var stateA = ServerDescriptionHelper.Connected(subject.Description.ClusterId, serverType: ServerType.Standalone);
            var stateB = ServerDescriptionHelper.Connected(subject.Description.ClusterId, serverType: ServerType.ReplicaSetGhost);

            server.DescriptionChanged += Raise.EventWith(new object(), new ServerDescriptionChangedEventArgs(stateA, stateB));

            subject.Description.Servers.Should().BeEmpty();

            server.DescriptionChanged += Raise.EventWith(new object(), new ServerDescriptionChangedEventArgs(stateB, stateA));

            subject.Description.Servers.Count.Should().Be(1);
        }

        private SingleServerCluster CreateSubject()
        {
            return new SingleServerCluster(_settings, _serverFactory, _clusterListener);
        }
    }
}