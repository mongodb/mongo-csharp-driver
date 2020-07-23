/* Copyright 2013-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Servers
{
    public class ServerDescriptionTests
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
            subject.LogicalSessionTimeout.Should().NotHaveValue();
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
            var logicalSessionTimeout = TimeSpan.FromMinutes(1);
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
                logicalSessionTimeout: logicalSessionTimeout,
                replicaSetConfig: replicaSetConfig,
                tags: tags,
                version: version,
                wireVersionRange: wireVersionRange);

            subject.AverageRoundTripTime.Should().Be(TimeSpan.FromSeconds(1));
            subject.CanonicalEndPoint.Should().Be(canonicalEndPoint);
            subject.ElectionId.Should().Be(electionId);
            subject.EndPoint.Should().Be(__endPoint);
            subject.LogicalSessionTimeout.Should().Be(logicalSessionTimeout);
            subject.ReplicaSetConfig.Should().Be(replicaSetConfig);
            subject.ServerId.Should().Be(__serverId);
            subject.State.Should().Be(state);
            subject.Tags.Should().Be(tags);
            subject.Type.Should().Be(type);
        }

        [Theory]
        [MemberData(nameof(Exception_equals_test_cases))]
        public void Equals_for_exceptions_should_return_expected_result(Exception x, Exception y, bool expectedResult)
        {
            var result = ServerDescriptionReflector.Equals(x, y);
            result.Should().Be(expectedResult);
        }

        public static IEnumerable<object[]> Exception_equals_test_cases()
        {
            yield return new object[] { null, null, true };

            var exception = new Exception();
            yield return new object[] { exception, exception, true };

            yield return new object[] { null, exception, false };
            yield return new object[] { exception, null, false };

            yield return new object[] { exception, new ArgumentException(), false };
            yield return new object[] { new ArgumentException(), exception, false };

            var exceptionWithInnerException = new Exception("WithInnerException", exception);
            yield return new object[] { exceptionWithInnerException, exception, false };
            yield return new object[] { exception, exceptionWithInnerException, false };

            var exceptionWithDifferentInnerException = new Exception("WithInnerException", new ArgumentException());
            yield return new object[] { exceptionWithInnerException, exceptionWithDifferentInnerException, false };
            yield return new object[] { exceptionWithDifferentInnerException, exceptionWithInnerException, false };

            var exceptionWithALotInnerException = new Exception("main", new Exception("inner1", new Exception("inner3", new Exception())));
            var exceptionWithALotInnerExceptionAndDifferentLastMessage = new Exception("main", new Exception("inner1", new Exception("differentInner3", new Exception())));
            yield return new object[] { exceptionWithALotInnerException, exceptionWithALotInnerExceptionAndDifferentLastMessage, false };
            yield return new object[] { exceptionWithALotInnerExceptionAndDifferentLastMessage, exceptionWithALotInnerException, false };

            var exceptionWithStackTrace = new TestException("ex", "stack");
            var exceptionWithDifferentStackTrace = new TestException("ex", "stackDiff");
            yield return new object[] { exceptionWithStackTrace, exceptionWithDifferentStackTrace, false };
            yield return new object[] { exceptionWithDifferentStackTrace, exceptionWithStackTrace, false };
        }

        [Theory]
        [InlineData("AverageRoundTripTime")]
        [InlineData("CanonicalEndPoint")]
        [InlineData("ElectionId")]
        [InlineData("EndPoint")]
        [InlineData("LogicalSessionTimeout")]
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
            var logicalSessionTimeout = TimeSpan.FromMinutes(1);
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
                logicalSessionTimeout: logicalSessionTimeout,
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
                case "LogicalSessionTimeout": logicalSessionTimeout = TimeSpan.FromMinutes(2); break;
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
                logicalSessionTimeout: logicalSessionTimeout,
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
            var lastUpdateTimestamp = DateTime.UtcNow;
            ServerDescription subject = new ServerDescription(
                __serverId,
                __endPoint,
                type: ServerType.Standalone,
                lastUpdateTimestamp: lastUpdateTimestamp);
            ServerDescription serverDescription2 = new ServerDescription(
                __serverId,
                __endPoint,
                type: ServerType.Standalone,
                lastUpdateTimestamp: lastUpdateTimestamp);
            subject.Equals(serverDescription2).Should().BeTrue();
            subject.Equals((object)serverDescription2).Should().BeTrue();
            subject.GetHashCode().Should().Be(serverDescription2.GetHashCode());
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(new[] { 0, 0 }, false)]
        [InlineData(new[] { 0, 1 }, false)]
        [InlineData(new[] { 0, 2 }, true)]
        [InlineData(new[] { 0, 6 }, true)]
        [InlineData(new[] { 0, 7 }, true)]
        [InlineData(new[] { 2, 2 }, true)]
        [InlineData(new[] { 2, 6 }, true)]
        [InlineData(new[] { 2, 7 }, true)]
        [InlineData(new[] { 6, 6 }, true)]
        [InlineData(new[] { 6, 7 }, true)]
        [InlineData(new[] { 7, 7 }, true)]
        [InlineData(new[] { 7, 8 }, true)]
        public void IsCompatibleWithDriver_should_return_expected_result(int[] minMaxWireVersions, bool expectedResult)
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            var wireVersionRange = minMaxWireVersions == null ? null : new Range<int>(minMaxWireVersions[0], minMaxWireVersions[1]);
            var subject = new ServerDescription(serverId, endPoint, wireVersionRange: wireVersionRange, type: ServerType.Standalone);

            var result = subject.IsCompatibleWithDriver;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("AverageRoundTripTime")]
        [InlineData("CanonicalEndPoint")]
        [InlineData("ElectionId")]
        [InlineData("HeartbeatException")]
        [InlineData("HeartbeatInterval")]
        [InlineData("LastUpdateTimestamp")]
        [InlineData("LastWriteTimestamp")]
        [InlineData("LogicalSessionTimeout")]
        [InlineData("MaxBatchCount")]
        [InlineData("MaxDocumentSize")]
        [InlineData("MaxMessageSize")]
        [InlineData("MaxWireDocumentSize")]
        [InlineData("ReplicaSetConfig")]
        [InlineData("State")]
        [InlineData("Tags")]
        [InlineData("Type")]
        [InlineData("Version")]
        [InlineData("WireVersionRange")]
        public void With_should_return_new_instance_when_a_field_is_not_equal(string notEqualField)
        {
            var averageRoundTripTime = TimeSpan.FromSeconds(1);
            var canonicalEndPoint = new DnsEndPoint("localhost", 27017);
            var electionId = new ElectionId(ObjectId.GenerateNewId());
            var heartbeatException = new Exception();
            var heartbeatInterval = TimeSpan.FromSeconds(10);
            var lastUpdateTimestamp = DateTime.UtcNow;
            var lastWriteTimestamp = DateTime.UtcNow;
            var logicalSessionTimeout = TimeSpan.FromMinutes(1);
            var maxBatchCount = 1000;
            var maxDocumentSize = 16000000;
            var maxMessageSize = 48000000;
            var maxWireDocumentSize = 16000000;
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
                averageRoundTripTime: averageRoundTripTime,
                canonicalEndPoint: canonicalEndPoint,
                electionId: electionId,
                heartbeatException: heartbeatException,
                heartbeatInterval: heartbeatInterval,
                lastUpdateTimestamp: lastUpdateTimestamp,
                lastWriteTimestamp: lastWriteTimestamp,
                logicalSessionTimeout: logicalSessionTimeout,
                maxBatchCount: maxBatchCount,
                maxDocumentSize: maxDocumentSize,
                maxMessageSize: maxMessageSize,
                maxWireDocumentSize: maxWireDocumentSize,
                replicaSetConfig: replicaSetConfig,
                state: state,
                tags: tags,
                type: type,
                version: version,
                wireVersionRange: wireVersionRange);

            switch (notEqualField)
            {
                case "AverageRoundTripTime": averageRoundTripTime = averageRoundTripTime.Add(TimeSpan.FromSeconds(1)); break;
                case "CanonicalEndPoint": canonicalEndPoint = new DnsEndPoint("localhost", 27018); break;
                case "ElectionId": electionId = new ElectionId(ObjectId.Empty); break;
                case "HeartbeatException": heartbeatException = new Exception("NewMessage"); break;
                case "HeartbeatInterval": heartbeatInterval = TimeSpan.FromSeconds(11); break;
                case "LastUpdateTimestamp": lastUpdateTimestamp = lastUpdateTimestamp.Add(TimeSpan.FromSeconds(1)); break;
                case "LastWriteTimestamp": lastWriteTimestamp = lastWriteTimestamp.Add(TimeSpan.FromSeconds(1)); break;
                case "LogicalSessionTimeout": logicalSessionTimeout = TimeSpan.FromMinutes(2); break;
                case "MaxBatchCount": maxBatchCount += 1; break;
                case "MaxDocumentSize": maxDocumentSize += 1; break;
                case "MaxMessageSize": maxMessageSize += 1; break;
                case "MaxWireDocumentSize": maxWireDocumentSize += 1; break;
                case "ReplicaSetConfig": replicaSetConfig = new ReplicaSetConfig(replicaSetConfig.Members, "newname", replicaSetConfig.Primary, replicaSetConfig.Version); break;
                case "State": state = ServerState.Disconnected; break;
                case "Tags": tags = new TagSet(new[] { new Tag("x", "b") }); break;
                case "Type": type = ServerType.ReplicaSetSecondary; break;
                case "Version": version = new SemanticVersion(version.Major, version.Minor, version.Patch + 1); break;
                case "WireVersionRange": wireVersionRange = new Range<int>(0, 0); break;
            }

            var result = subject.With(
                averageRoundTripTime: averageRoundTripTime,
                canonicalEndPoint: canonicalEndPoint,
                electionId: electionId,
                heartbeatException: heartbeatException,
                heartbeatInterval: heartbeatInterval,
                lastUpdateTimestamp: lastUpdateTimestamp,
                lastWriteTimestamp: lastWriteTimestamp,
                logicalSessionTimeout: logicalSessionTimeout,
                maxBatchCount: maxBatchCount,
                maxDocumentSize: maxDocumentSize,
                maxMessageSize: maxMessageSize,
                maxWireDocumentSize: maxWireDocumentSize,
                replicaSetConfig: replicaSetConfig,
                state: state,
                tags: tags,
                type: type,
                version: version,
                wireVersionRange: wireVersionRange);

            result.Should().NotBeSameAs(subject);
            result.Equals(subject).Should().BeFalse();
            result.Equals((object)subject).Should().BeFalse();
            result.GetHashCode().Should().NotBe(subject.GetHashCode());
        }

        [Fact]
        public void With_should_return_same_instance_when_all_fields_are_equal()
        {
            var averageRoundTripTime = TimeSpan.FromSeconds(1);
            var lastUpdateTimestamp = DateTime.UtcNow;
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
                averageRoundTripTime: averageRoundTripTime,
                lastUpdateTimestamp: lastUpdateTimestamp,
                replicaSetConfig: replicaSetConfig,
                state: state,
                tags: tags,
                type: type,
                version: version,
                wireVersionRange: wireVersionRange);

            var result = subject.With(
                averageRoundTripTime: averageRoundTripTime,
                lastUpdateTimestamp: lastUpdateTimestamp,
                replicaSetConfig: replicaSetConfig,
                state: ServerState.Connected,
                tags: tags,
                type: type,
                version: version,
                wireVersionRange: wireVersionRange);

            result.ShouldBeEquivalentTo(subject);
        }

        // nested types
#pragma warning disable CA1064 // Exceptions should be public
        private class TestException : Exception
#pragma warning restore CA1064 // Exceptions should be public
        {
            private string _emulatedStackTrace;

            public TestException(string message, string stackTrace) : base(message)
            {
                _emulatedStackTrace = stackTrace;
            }

            public override string StackTrace
            {
                get
                {
                    return _emulatedStackTrace;
                }
            }
        }
    }

    internal static class ServerDescriptionReflector
    {
        public static bool Equals(this ServerDescription serverDescription, Exception x, Exception y)
        {
            return (bool)Reflector.InvokeStatic(typeof(ServerDescription), nameof(Equals), x, y);
        }
    }
}
