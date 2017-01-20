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
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class ConnectionDescriptionTests
    {
        private static readonly BuildInfoResult __buildInfoResult = new BuildInfoResult(BsonDocument.Parse(
            "{ ok: 1, version: \"2.6.3\" }"
        ));

        private static readonly ConnectionId __connectionId = new ConnectionId(
            new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));

        private static readonly IsMasterResult __isMasterResult = new IsMasterResult(BsonDocument.Parse(
            "{ ok: 1, maxWriteBatchSize: 10, maxBsonObjectSize: 20, maxMessageSizeBytes: 30 }"
        ));

        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_connectionId_is_null()
        {
            Action act = () => new ConnectionDescription(null, __isMasterResult, __buildInfoResult);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_isMasterResult_is_null()
        {
            Action act = () => new ConnectionDescription(__connectionId, null, __buildInfoResult);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_buildInfoResult_is_null()
        {
            Action act = () => new ConnectionDescription(__connectionId, __isMasterResult, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Equals_should_return_correct_results()
        {
            var connectionId1 = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27018)), 10);
            var connectionId2 = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27018)), 10);
            var isMasterResult1 = new IsMasterResult(new BsonDocument("x", 1));
            var isMasterResult2 = new IsMasterResult(new BsonDocument("x", 2));
            var buildInfoResult1 = new BuildInfoResult(new BsonDocument("version", "2.6.3"));
            var buildInfoResult2 = new BuildInfoResult(new BsonDocument("version", "2.4.10"));

            var subject1 = new ConnectionDescription(connectionId1, isMasterResult1, buildInfoResult1);
            var subject2 = new ConnectionDescription(connectionId1, isMasterResult1, buildInfoResult1);
            var subject3 = new ConnectionDescription(connectionId1, isMasterResult1, buildInfoResult2);
            var subject4 = new ConnectionDescription(connectionId1, isMasterResult2, buildInfoResult1);
            var subject5 = new ConnectionDescription(connectionId2, isMasterResult1, buildInfoResult1);

            subject1.Equals(subject2).Should().BeTrue();
            subject1.Equals(subject3).Should().BeFalse();
            subject1.Equals(subject4).Should().BeFalse();
            subject1.Equals(subject5).Should().BeFalse();
        }

        [Fact]
        public void ConnectionId_should_return_ConnectionId()
        {
            var subject = new ConnectionDescription(__connectionId, __isMasterResult, __buildInfoResult);

            subject.ConnectionId.Should().Be(__connectionId);
        }

        [Fact]
        public void MaxBatchCount_should_return_isMasterResult_MaxBatchCount()
        {
            var subject = new ConnectionDescription(__connectionId, __isMasterResult, __buildInfoResult);

            subject.MaxBatchCount.Should().Be(__isMasterResult.MaxBatchCount);
        }

        [Fact]
        public void MaxDocumentSize_should_return_isMasterResult_MaxDocumentSize()
        {
            var subject = new ConnectionDescription(__connectionId, __isMasterResult, __buildInfoResult);

            subject.MaxDocumentSize.Should().Be(__isMasterResult.MaxDocumentSize);
        }

        [Fact]
        public void MaxMessageSize_should_return_isMasterResult_MaxMessageSize()
        {
            var subject = new ConnectionDescription(__connectionId, __isMasterResult, __buildInfoResult);

            subject.MaxMessageSize.Should().Be(__isMasterResult.MaxMessageSize);
        }

        [Fact]
        public void ServerVersion_should_return_buildInfoResult_ServerVersion()
        {
            var subject = new ConnectionDescription(__connectionId, __isMasterResult, __buildInfoResult);

            subject.ServerVersion.Should().Be(__buildInfoResult.ServerVersion);
        }

        [Fact]
        public void WithConnectionId_should_return_new_instance_even_when_only_the_serverValue_differs()
        {
            var clusterId = new ClusterId();
            var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 1));
            var connectionId1 = new ConnectionId(serverId, 1);
            var connectionId2 = new ConnectionId(serverId, 1).WithServerValue(2);
            var isMasterResult = new IsMasterResult(new BsonDocument());
            var buildInfoResult = new BuildInfoResult(new BsonDocument("version", "2.6.0"));
            var subject = new ConnectionDescription(connectionId1, isMasterResult, buildInfoResult);

            var result = subject.WithConnectionId(connectionId2);

            result.Should().NotBeSameAs(subject);
            result.ConnectionId.Should().BeSameAs(connectionId2);
        }
    }
}