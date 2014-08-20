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
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Servers
{
    [TestFixture]
    class ServerDescriptionTests
    {
        #region static
        // static fields
        private static readonly ClusterId __clusterId;
        private static readonly DnsEndPoint __endPoint;
        private static readonly ServerId __serverId;
        private static readonly ServerDescription __subject;

        // static constructor
        static ServerDescriptionTests()
        {
            __clusterId = new ClusterId();
            __endPoint = new DnsEndPoint("localhost", 27017);
            __serverId = new ServerId(__clusterId, __endPoint);
            __subject = new ServerDescription(
                __serverId,
                __endPoint,
                ServerState.Connected,
                ServerType.Standalone,
                TimeSpan.FromSeconds(1),
                null,
                null,
                new SemanticVersion(2, 6, 3),
                new Range<int>(0, 2));
        }
        #endregion

        [Test]
        public void Constructor_with_serverId_and_endPoint_only_should_return_disconnected_instance()
        {
            var subject = new ServerDescription(__serverId, __endPoint);
            subject.AverageRoundTripTime.Should().Be(TimeSpan.Zero);
            subject.EndPoint.Should().Be(__endPoint);
            subject.ReplicaSetConfig.Should().BeNull();
            subject.ServerId.Should().Be(__serverId);
            subject.State.Should().Be(ServerState.Disconnected);
            subject.Tags.Should().BeNull();
            subject.Type.Should().Be(ServerType.Unknown);
            subject.Version.Should().BeNull();
            subject.WireVersionRange.Should().BeNull();
        }

        [Test]
        public void Constructor_with_multiple_parameters_should_return_properly_initialized_instance()
        {
            var averageRoundTripTime = TimeSpan.FromSeconds(1);
            var replicaSetConfig = new ReplicaSetConfig(
                new [] { new DnsEndPoint("localhost", 27017), new DnsEndPoint("localhost", 27018) },
                "name",
                new DnsEndPoint("localhost", 27017),
                1);
            var state = ServerState.Connected;
            var tags = new TagSet(new [] { new Tag("x", "a") });
            var type = ServerType.ReplicaSetPrimary;
            var version = new SemanticVersion(2, 6, 3);
            var wireVersionRange = new Range<int>(2, 3);

            var subject = new ServerDescription(
                __serverId,
                __endPoint,
                state,
                type,
                averageRoundTripTime,
                replicaSetConfig,
                tags,
                version,
                wireVersionRange);

            subject.AverageRoundTripTime.Should().Be(TimeSpan.FromSeconds(1));
            subject.EndPoint.Should().Be(__endPoint);
            subject.ReplicaSetConfig.Should().Be(replicaSetConfig);
            subject.ServerId.Should().Be(__serverId);
            subject.State.Should().Be(state);
            subject.Tags.Should().Be(tags);
            subject.Type.Should().Be(type);
        }


        [TestCase("AverageRoundTripTime")]
        [TestCase("EndPoint")]
        [TestCase("ReplicaSetConfig")]
        [TestCase("ServerId")]
        [TestCase("State")]
        [TestCase("Tags")]
        [TestCase("Type")]
        [TestCase("Version")]
        [TestCase("WireVersionRange")]
        public void Equals_should_return_false_when_any_field_is_not_equal(string notEqualField)
        {
            var averageRoundTripTime = TimeSpan.FromSeconds(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var replicaSetConfig = new ReplicaSetConfig(
                new[] { new DnsEndPoint("localhost", 27017), new DnsEndPoint("localhost", 27018) },
                "name",
                new DnsEndPoint("localhost", 27017),
                1);
            var serverId = new ServerId(__clusterId, endPoint);
            var state = ServerState.Connected;
            var tags = new TagSet(new[] { new Tag("x", "a") });
            var type = ServerType.ReplicaSetPrimary;
            var version = new SemanticVersion(2, 6, 3);
            var wireVersionRange = new Range<int>(2, 3);

            var subject = new ServerDescription(
                serverId,
                endPoint,
                state,
                type,
                averageRoundTripTime,
                replicaSetConfig,
                tags,
                version,
                wireVersionRange);

            switch (notEqualField)
            {
                case "AverageRoundTripTime": averageRoundTripTime = averageRoundTripTime.Add(TimeSpan.FromSeconds(1)); break;
                case "EndPoint": endPoint = new DnsEndPoint(endPoint.Host, endPoint.Port + 1); serverId = new ServerId(__clusterId, endPoint); break;
                case "ReplicaSetConfig": replicaSetConfig = new ReplicaSetConfig(replicaSetConfig.Members, "newname", replicaSetConfig.Primary, replicaSetConfig.Version); break;
                case "State": state = ServerState.Disconnected; break;
                case "ServerId": serverId = new ServerId(new ClusterId(), endPoint); break;
                case "Tags": tags = new TagSet(new[] { new Tag("x", "b") }); break;
                case "Type": type = ServerType.ReplicaSetSecondary; break;
                case "Version": version = new SemanticVersion(version.Major, version.Minor, version.Patch + 1); break;
                case "WireVersionRange": wireVersionRange = new Range<int>(0, 0); break;
            }

            var serverDescription2 = new ServerDescription(
                serverId,
                endPoint,
                state,
                type,
                averageRoundTripTime,
                replicaSetConfig,
                tags,
                version,
                wireVersionRange);

            subject.Equals(serverDescription2).Should().BeFalse();
            subject.Equals((object)serverDescription2).Should().BeFalse();
            subject.GetHashCode().Should().NotBe(serverDescription2.GetHashCode());
        }

        [Test]
        public void Equals_should_return_true_when_all_fields_are_equal()
        {
            ServerDescription subject = new ServerDescription(__serverId, __endPoint);
            ServerDescription serverDescription2 = new ServerDescription(__serverId, __endPoint);
            subject.Equals(serverDescription2).Should().BeTrue();
            subject.Equals((object)serverDescription2).Should().BeTrue();
            subject.GetHashCode().Should().Be(serverDescription2.GetHashCode());
        }

        [TestCase("AverageRoundTripTime")]
        [TestCase("ReplicaSetConfig")]
        [TestCase("Tags")]
        [TestCase("Type")]
        [TestCase("Version")]
        [TestCase("WireVersionRange")]
        public void WithHeartbeat_should_return_new_instance_when_a_field_is_not_equal(string notEqualField)
        {
            var averageRoundTripTime = TimeSpan.FromSeconds(1);
            var replicaSetConfig = new ReplicaSetConfig(
                new[] { new DnsEndPoint("localhost", 27017), new DnsEndPoint("localhost", 27018) },
                "name",
                new DnsEndPoint("localhost", 27017),
                1);
            var state = ServerState.Connected;
            var tags = new TagSet(new[] { new Tag("x", "a") });
            var type = ServerType.ReplicaSetPrimary;
            var version = new SemanticVersion(2, 6, 3);
            var wireVersionRange = new Range<int>(2, 3);

            var subject = new ServerDescription(
                __serverId,
                __endPoint,
                state,
                type,
                averageRoundTripTime,
                replicaSetConfig,
                tags,
                version,
                wireVersionRange);

            switch (notEqualField)
            {
                case "AverageRoundTripTime": averageRoundTripTime = averageRoundTripTime.Add(TimeSpan.FromSeconds(1)); break;
                case "ReplicaSetConfig": replicaSetConfig = new ReplicaSetConfig(replicaSetConfig.Members, "newname", replicaSetConfig.Primary, replicaSetConfig.Version); break;
                case "Tags": tags = new TagSet(new[] { new Tag("x", "b") }); break;
                case "Type": type = ServerType.ReplicaSetSecondary; break;
                case "Version": version = new SemanticVersion(version.Major, version.Minor, version.Patch + 1); break;
                case "WireVersionRange": wireVersionRange = new Range<int>(0, 0); break;
            }

            var serverDescription2 = subject.WithHeartbeatInfo(averageRoundTripTime, replicaSetConfig, tags, type, version, wireVersionRange);

            subject.Equals(serverDescription2).Should().BeFalse();
            subject.Equals((object)serverDescription2).Should().BeFalse();
            subject.GetHashCode().Should().NotBe(serverDescription2.GetHashCode());
        }

        [Test]
        public void WithHeartbeat_should_return_same_instance_when_all_fields_are_equal()
        {
            var averageRoundTripTime = TimeSpan.FromSeconds(1);
            var replicaSetConfig = new ReplicaSetConfig(
                new[] { new DnsEndPoint("localhost", 27017), new DnsEndPoint("localhost", 27018) },
                "name",
                new DnsEndPoint("localhost", 27017),
                1);
            var state = ServerState.Connected;
            var tags = new TagSet(new[] { new Tag("x", "a") });
            var type = ServerType.ReplicaSetPrimary;
            var version = new SemanticVersion(2, 6, 3);
            var wireVersionRange = new Range<int>(0, 2);

            var subject = new ServerDescription(
                __serverId,
                __endPoint,
                state,
                type,
                averageRoundTripTime,
                replicaSetConfig,
                tags,
                version,
                wireVersionRange);

            var serverDescription2 = subject.WithHeartbeatInfo(averageRoundTripTime, replicaSetConfig, tags, type, version, wireVersionRange);
            serverDescription2.Should().BeSameAs(subject);
        }
    }
}
