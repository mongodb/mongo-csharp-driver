/* Copyright 2017-present MongoDB Inc.
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

using System.Net;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class ReadConcernHelperTests
    {
        [Fact]
        public void GetReadConcernForCommand_should_consider_session_supported_when_logicalSessionTimeoutMinutes_is_null_and_load_balanced_mode()
        {
            var session = CreateSession(
                isInTransaction: false,
                isCausallyConsistent: true,
                operationTime: new BsonTimestamp(1234));
            var connectionDescription = CreateConnectionDescription(logicalSessionTimeoutMinutes: false, serviceId: true);
            var readConcern = ReadConcern.FromBsonDocument(BsonDocument.Parse("{}"));

            var result = ReadConcernHelper.GetReadConcernForCommand(session, connectionDescription, readConcern);

            result.Should().Be(new BsonDocument("afterClusterTime", new BsonTimestamp(1234)));
        }

        [Theory]
        [InlineData(false, false, null, "{ level : 'majority' }", "{ level : 'majority' }")]
        [InlineData(false, false, 1234, "{ level : 'majority' }", "{ level : 'majority' }")]
        [InlineData(false, true, null, "{ level : 'majority' }", "{ level : 'majority' }")]
        [InlineData(false, true, 1234, "{ level : 'majority' }", "{ level : 'majority', afterClusterTime : Timestamp(0, 1234) }")]
        [InlineData(true, false, null, "{ level : 'majority' }", null)]
        [InlineData(true, false, 1234, "{ level : 'majority' }", null)]
        [InlineData(true, true, null, "{ level : 'majority' }", null)]
        [InlineData(true, true, 1234, "{ level : 'majority' }", null)]
        public void GetReadConcernForCommand_should_return_expected_result(
            bool isInTransaction,
            bool isCausallyConsistent,
            int? operationTime,
            string readConcernJson,
            string expectedResult)
        {
            var session = CreateSession(
                isInTransaction: isInTransaction,
                isCausallyConsistent: isCausallyConsistent,
                operationTime: operationTime.HasValue ? new BsonTimestamp(operationTime.Value) : null);
            var connectionDescription = CreateConnectionDescription(logicalSessionTimeoutMinutes: true);
            var readConcern = ReadConcern.FromBsonDocument(BsonDocument.Parse(readConcernJson));

            var result = ReadConcernHelper.GetReadConcernForCommand(session, connectionDescription, readConcern);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, null, "{ level : 'majority' }", "{ level : 'majority' }")]
        [InlineData(false, 1234, "{ level : 'majority' }", "{ level : 'majority' }")]
        [InlineData(true, null, "{ level : 'majority' }", "{ level : 'majority' }")]
        [InlineData(true, 1234, "{ level : 'majority' }", "{ level : 'majority', afterClusterTime : Timestamp(0, 1234) }")]
        public void GetReadConcernForFirstCommandInTransaction_should_return_expected_result(
            bool isCausallyConsistent,
            int? operationTime,
            string readConcernJson,
            string expectedResult)
        {
            var readConcern = ReadConcern.FromBsonDocument(BsonDocument.Parse(readConcernJson));
            var transactionOptions = new TransactionOptions(readConcern);
            var transaction = new CoreTransaction(1, transactionOptions);
            var session = CreateSession(
                currentTransaction: transaction,
                isCausallyConsistent: isCausallyConsistent,
                operationTime: operationTime.HasValue ? new BsonTimestamp(operationTime.Value) : null);
            var connectionDescription = CreateConnectionDescription(logicalSessionTimeoutMinutes: true);

            var result = ReadConcernHelper.GetReadConcernForFirstCommandInTransaction(session, connectionDescription);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, null, "{ level : 'majority' }")]
        [InlineData(false, 1234, "{ level : 'majority' }")]
        [InlineData(true, null, "{ level : 'majority' }")]
        [InlineData(true, 1234, "{ level : 'majority' }")]
        public void GetReadConcernForCommand_should_return_null_when_session_is_snapshot(
            bool isCausallyConsistent,
            int? operationTime,
            string readConcernJson)
        {
            var session = CreateSession(
                isSnapshot: true,
                isCausallyConsistent: isCausallyConsistent,
                operationTime: operationTime.HasValue ? new BsonTimestamp(operationTime.Value) : null);
            var connectionDescription = CreateConnectionDescription(logicalSessionTimeoutMinutes: true);
            var readConcern = ReadConcern.FromBsonDocument(BsonDocument.Parse(readConcernJson));

            var result = ReadConcernHelper.GetReadConcernForCommand(session, connectionDescription, readConcern);

            result.Should().BeNull();
        }

        [Theory]
        [InlineData(false, null, null)]
        [InlineData(true, null, "{ level : 'snapshot' }")]
        [InlineData(true, 1234, "{ level : 'snapshot', atClusterTime : Timestamp(0, 1234) }")]
        public void GetReadConcernForSnapshotSession_should_return_expected_result(
            bool isSnapshot,
            int? snapshotTime,
            string expectedResult)
        {
            var session = CreateSession(
                isSnapshot: isSnapshot,
                snapshotTime: snapshotTime.HasValue ? new BsonTimestamp(snapshotTime.Value) : null);
            var connectionDescription = CreateConnectionDescription(
                logicalSessionTimeoutMinutes: true,
                maxWireVersion: WireVersion.Server50);

            var result = ReadConcernHelper.GetReadConcernForSnapshotSession(session, connectionDescription);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, false, null, null)]
        [InlineData(false, false, 1234, null)]
        [InlineData(false, true, null, null)]
        [InlineData(false, true, 1234, "{ afterClusterTime : Timestamp(0, 1234) }")]
        [InlineData(true, false, null, null)]
        [InlineData(true, false, 1234, null)]
        [InlineData(true, true, null, null)]
        [InlineData(true, true, 1234, null)]
        public void GetReadConcernForWriteCommand_should_return_expected_result(
            bool isInTransaction,
            bool isCausallyConsistent,
            int? operationTime,
            string expectedResult)
        {
            var session = CreateSession(
                isInTransaction: isInTransaction,
                isCausallyConsistent: isCausallyConsistent,
                operationTime: operationTime.HasValue ? new BsonTimestamp(operationTime.Value) : null);
            var connectionDescription = CreateConnectionDescription(logicalSessionTimeoutMinutes: true);

            var result = ReadConcernHelper.GetReadConcernForWriteCommand(session, connectionDescription);

            result.Should().Be(expectedResult);
        }

        // private methods
        private ConnectionDescription CreateConnectionDescription(
            bool logicalSessionTimeoutMinutes = false,
            bool? serviceId = null,
            int? maxWireVersion = null)
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            var connectionId = new ConnectionId(serverId, 1);
            var wireVersion = maxWireVersion ?? (logicalSessionTimeoutMinutes ? WireVersion.Server40 : WireVersion.Server36);
            var helloResult = new BsonDocument
            {
                { "ok", 1 },
                { "logicalSessionTimeoutMinutes", 30, logicalSessionTimeoutMinutes },
                { "serviceId", ObjectId.GenerateNewId(), serviceId.HasValue },
                { "maxWireVersion", wireVersion }
            };
            return new ConnectionDescription(connectionId, new HelloResult(helloResult));
        }

        private ICoreSession CreateSession(
            CoreTransaction currentTransaction = null,
            bool isCausallyConsistent = false,
            bool isInTransaction = false,
            bool isSnapshot = false,
            BsonTimestamp operationTime = null,
            BsonTimestamp snapshotTime = null)
        {
            var mockSession = new Mock<ICoreSession>();
            mockSession.SetupGet(m => m.CurrentTransaction).Returns(currentTransaction);
            mockSession.SetupGet(m => m.IsCausallyConsistent).Returns(isCausallyConsistent);
            mockSession.SetupGet(m => m.IsInTransaction).Returns(isInTransaction);
            mockSession.SetupGet(m => m.IsSnapshot).Returns(isSnapshot);
            mockSession.SetupGet(m => m.OperationTime).Returns(operationTime);
            mockSession.SetupGet(m => m.SnapshotTime).Returns(snapshotTime);
            return mockSession.Object;
        }
    }
}
