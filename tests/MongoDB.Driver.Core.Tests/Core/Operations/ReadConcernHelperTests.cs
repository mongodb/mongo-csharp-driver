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
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class ReadConcernHelperTests
    {
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
            var connectionDescription = CreateConnectionDescription(areSessionsSupported: true);
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
            var connectionDescription = CreateConnectionDescription(areSessionsSupported: true);

            var result = ReadConcernHelper.GetReadConcernForFirstCommandInTransaction(session, connectionDescription);

            result.Should().Be(expectedResult);
        }

        // private methods
        private ConnectionDescription CreateConnectionDescription(
            bool areSessionsSupported = false)
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            var connectionId = new ConnectionId(serverId, 1);
            var isMasterResult = new BsonDocument
            {
                { "ok", 1 },
                { "logicalSessionTimeoutMinutes", 30, areSessionsSupported }
            };
            var buildInfoResult = new BsonDocument
            {
                { "ok", 1 },
                { "version", areSessionsSupported ? "4.0" : "3.6" }
            };
            return new ConnectionDescription(connectionId, new IsMasterResult(isMasterResult), new BuildInfoResult(buildInfoResult));
        }

        private ICoreSession CreateSession(
            CoreTransaction currentTransaction = null,
            bool isCausallyConsistent = false,
            bool isInTransaction = false,
            BsonTimestamp operationTime = null)
        {
            var mockSession = new Mock<ICoreSession>();
            mockSession.SetupGet(m => m.CurrentTransaction).Returns(currentTransaction);
            mockSession.SetupGet(m => m.IsCausallyConsistent).Returns(isCausallyConsistent);
            mockSession.SetupGet(m => m.IsInTransaction).Returns(isInTransaction);
            mockSession.SetupGet(m => m.OperationTime).Returns(operationTime);
            return mockSession.Object;
        }
    }
}
