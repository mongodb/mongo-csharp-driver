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
using System.Net;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Core.Servers
{
    class ServerDescriptionTests
    {
        #region static
        // static fields
        private static readonly ClusterId __clusterId;
        private static readonly DnsEndPoint __endPoint;
        private static readonly ServerId __serverId;

        // static constructor
        static ServerDescriptionTests()
        {
            __clusterId = new ClusterId();
            __endPoint = new DnsEndPoint("localhost", 27017);
            __serverId = new ServerId(__clusterId, __endPoint);
        }
        #endregion

        [Fact]
        public void Constructor_with_serverId_and_endPoint_only_should_return_disconnected_instance()
        {
            var subject = new ServerDescription(__serverId, __endPoint);
            subject.AverageRoundTripTime.Should().Be(TimeSpan.Zero);
            subject.CanonicalEndPoint.Should().BeNull();
            subject.ElectionId.Should().BeNull();
            subject.EndPoint.Should().Be(__endPoint);
            subject.ReplicaSetConfig.Should().BeNull();
            subject.ServerId.Should().Be(__serverId);
            subject.State.Should().Be(ServerState.Disconnected);
            subject.Tags.Should().BeNull();
            subject.Type.Should().Be(ServerType.Unknown);
            subject.Version.Should().BeNull();
            subject.WireVersionRange.Should().BeNull();
        }

        [Fact]
        public void Constructor_with_multiple_parameters_should_return_properly_initialized_instance()
        {
            var averageRoundTripTime = TimeSpan.FromSeconds(1);
            var canonicalEndPoint = new DnsEndPoint("localhost", 27017);
            var electionId = new ElectionId(ObjectId.GenerateNewId());
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
                state: state,
                type: type,
                averageRoundTripTime: averageRoundTripTime,
                canonicalEndPoint: canonicalEndPoint,
                electionId: electionId,
                replicaSetConfig: replicaSetConfig,
                tags: tags,
                version: version,
                wireVersionRange: wireVersionRange);

            subject.AverageRoundTripTime.Should().Be(TimeSpan.FromSeconds(1));
            subject.CanonicalEndPoint.Should().Be(canonicalEndPoint);
            subject.ElectionId.Should().Be(electionId);
            subject.EndPoint.Should().Be(__endPoint);
            subject.ReplicaSetConfig.Should().Be(replicaSetConfig);
            subject.ServerId.Should().Be(__serverId);
            subject.State.Should().Be(state);
            subject.Tags.Should().Be(tags);
            subject.Type.Should().Be(type);
        }

        [Theory]
        [InlineData("AverageRoundTripTime")]
        [InlineData("CanonicalEndPoint")]
        [InlineData("ElectionId")]
        [InlineData("EndPoint")]
        [InlineData("ReplicaSetConfig")]
        [InlineData("ServerId")]
        [InlineData("State")]
        [InlineData("Tags")]
        [InlineData("Type")]
        [InlineData("Version")]
        [InlineData("WireVersionRange")]
        public void Equals_should_return_false_when_any_field_is_not_equal(string notEqualField)
        {
            var averageRoundTripTime = TimeSpan.FromSeconds(1);
            var canonicalEndPoint = new DnsEndPoint("localhost", 27017);
            var electionId = new ElectionId(ObjectId.GenerateNewId());
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
                state: state,
                type: type,
                averageRoundTripTime: averageRoundTripTime,
                canonicalEndPoint: canonicalEndPoint,
                replicaSetConfig: replicaSetConfig,
                tags: tags,
                version: version,
                wireVersionRange: wireVersionRange);

            switch (notEqualField)
            {
                case "AverageRoundTripTime": averageRoundTripTime = averageRoundTripTime.Add(TimeSpan.FromSeconds(1)); break;
                case "CanonicalEndPoint": canonicalEndPoint = new DnsEndPoint("localhost", 27018); break;
                case "ElectionId": electionId = new ElectionId(ObjectId.Empty); break;
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
                state: state,
                type: type,
                averageRoundTripTime: averageRoundTripTime,
                canonicalEndPoint: canonicalEndPoint,
                electionId: electionId,
                replicaSetConfig: replicaSetConfig,
                tags: tags,
                version: version,
                wireVersionRange: wireVersionRange);

            subject.Equals(serverDescription2).Should().BeFalse();
            subject.Equals((object)serverDescription2).Should().BeFalse();
            subject.GetHashCode().Should().NotBe(serverDescription2.GetHashCode());
        }

        [Fact]
        public void Equals_should_return_true_when_all_fields_are_equal()
        {
            ServerDescription subject = new ServerDescription(__serverId, __endPoint);
            ServerDescription serverDescription2 = new ServerDescription(__serverId, __endPoint);
            subject.Equals(serverDescription2).Should().BeTrue();
            subject.Equals((object)serverDescription2).Should().BeTrue();
            subject.GetHashCode().Should().Be(serverDescription2.GetHashCode());
        }

        [Theory]
        [InlineData("AverageRoundTripTime")]
        [InlineData("CanonicalEndPoint")]
        [InlineData("ElectionId")]
        [InlineData("ReplicaSetConfig")]
        [InlineData("Tags")]
        [InlineData("Type")]
        [InlineData("Version")]
        [InlineData("WireVersionRange")]
        public void WithHeartbeat_should_return_new_instance_when_a_field_is_not_equal(string notEqualField)
        {
            var averageRoundTripTime = TimeSpan.FromSeconds(1);
            var canonicalEndPoint = new DnsEndPoint("localhost", 27017);
            var electionId = new ElectionId(ObjectId.GenerateNewId());
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
                state: state,
                type: type,
                averageRoundTripTime: averageRoundTripTime,
                electionId: electionId,
                replicaSetConfig: replicaSetConfig,
                tags: tags,
                version: version,
                wireVersionRange: wireVersionRange);

            switch (notEqualField)
            {
                case "AverageRoundTripTime": averageRoundTripTime = averageRoundTripTime.Add(TimeSpan.FromSeconds(1)); break;
                case "CanonicalEndPoint": canonicalEndPoint = new DnsEndPoint("localhost", 27018); break;
                case "ElectionId": electionId = new ElectionId(ObjectId.Empty); break;
                case "ReplicaSetConfig": replicaSetConfig = new ReplicaSetConfig(replicaSetConfig.Members, "newname", replicaSetConfig.Primary, replicaSetConfig.Version); break;
                case "Tags": tags = new TagSet(new[] { new Tag("x", "b") }); break;
                case "Type": type = ServerType.ReplicaSetSecondary; break;
                case "Version": version = new SemanticVersion(version.Major, version.Minor, version.Patch + 1); break;
                case "WireVersionRange": wireVersionRange = new Range<int>(0, 0); break;
            }

            var serverDescription2 = subject.With(
                averageRoundTripTime: averageRoundTripTime,
                canonicalEndPoint: canonicalEndPoint,
                replicaSetConfig: replicaSetConfig,
                state: ServerState.Connected,
                electionId: electionId,
                tags: tags,
                type: type,
                version: version,
                wireVersionRange: wireVersionRange);

            subject.Equals(serverDescription2).Should().BeFalse();
            subject.Equals((object)serverDescription2).Should().BeFalse();
            subject.GetHashCode().Should().NotBe(serverDescription2.GetHashCode());
        }

        [Fact]
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
                state: state,
                type: type,
                averageRoundTripTime: averageRoundTripTime,
                replicaSetConfig: replicaSetConfig,
                tags: tags,
                version: version,
                wireVersionRange: wireVersionRange);

            var serverDescription2 = subject.With(
                averageRoundTripTime: averageRoundTripTime,
                replicaSetConfig: replicaSetConfig,
                state: ServerState.Connected,
                tags: tags,
                type: type,
                version: version,
                wireVersionRange: wireVersionRange);

            serverDescription2.Should().BeSameAs(subject);
        }
    }
}
