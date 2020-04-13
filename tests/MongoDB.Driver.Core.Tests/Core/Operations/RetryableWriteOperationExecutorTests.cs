/* Copyright 2020-present MongoDB Inc.
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

using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Operations
{
    public class RetryableWriteOperationExecutorTests
    {
        [Theory]
        [InlineData(false, false, false, false, false)]
        [InlineData(false, false, false, true, false)]
        [InlineData(false, false, true, false, false)]
        [InlineData(false, false, true, true, false)]
        [InlineData(false, true, false, false, false)]
        [InlineData(false, true, false, true, false)]
        [InlineData(false, true, true, false, false)]
        [InlineData(false, true, true, true, false)]
        [InlineData(true, false, false, false, false)]
        [InlineData(true, false, false, true, false)]
        [InlineData(true, false, true, false, false)]
        [InlineData(true, false, true, true, false)]
        [InlineData(true, true, false, false, false)]
        [InlineData(true, true, false, true, false)]
        [InlineData(true, true, true, false, true)]
        [InlineData(true, true, true, false, true)]
        public void DoesContextAllowRetries_should_return_expected_result(
            bool retryRequested,
            bool areRetryableWritesSupported,
            bool hasSessionId,
            bool isInTransaction,
            bool expectedResult)
        {
            var context = CreateContext(retryRequested, areRetryableWritesSupported, hasSessionId, isInTransaction);

            var result = RetryableWriteOperationExecutorReflector.DoesContextAllowRetries(context);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void IsOperationAcknowledged_should_return_expected_result(
            bool withWriteConcern,
            bool isAcknowledged,
            bool expectedResult)
        {
            var operation = CreateOperation(withWriteConcern, isAcknowledged);

            var result = RetryableWriteOperationExecutorReflector.IsOperationAcknowledged(operation);

            result.Should().Be(expectedResult);
        }

        // private methods
        private IWriteBinding CreateBinding(bool areRetryableWritesSupported, bool hasSessionId, bool isInTransaction)
        {
            var mockBinding = new Mock<IWriteBinding>();
            var session = CreateSession(hasSessionId, isInTransaction);
            var channelSource = CreateChannelSource(areRetryableWritesSupported);
            mockBinding.SetupGet(m => m.Session).Returns(session);
            mockBinding.Setup(m => m.GetWriteChannelSource(CancellationToken.None)).Returns(channelSource);
            return mockBinding.Object;
        }

        private IChannelHandle CreateChannel(bool areRetryableWritesSupported)
        {
            var mockChannel = new Mock<IChannelHandle>();
            var connectionDescription = CreateConnectionDescription(areRetryableWritesSupported);
            mockChannel.SetupGet(m => m.ConnectionDescription).Returns(connectionDescription);
            return mockChannel.Object;
        }

        private IChannelSourceHandle CreateChannelSource(bool areRetryableWritesSupported)
        {
            var mockChannelSource = new Mock<IChannelSourceHandle>();
            var channel = CreateChannel(areRetryableWritesSupported);
            mockChannelSource.Setup(m => m.GetChannel(CancellationToken.None)).Returns(channel);
            return mockChannelSource.Object;
        }

        private ConnectionDescription CreateConnectionDescription(bool areRetryableWritesSupported)
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            var connectionId = new ConnectionId(serverId, 1);
            var isMasterResultDocument = BsonDocument.Parse("{ ok : 1 }");
            if (areRetryableWritesSupported)
            {
                isMasterResultDocument["logicalSessionTimeoutMinutes"] = 1;
                isMasterResultDocument["msg"] = "isdbgrid"; // mongos
            }
            var isMasterResult = new IsMasterResult(isMasterResultDocument);
            var buildInfoResult = new BuildInfoResult(BsonDocument.Parse("{ ok : 1, version : '4.2.0' }"));
            var connectionDescription = new ConnectionDescription(connectionId, isMasterResult, buildInfoResult);
            return connectionDescription;
        }

        private RetryableWriteContext CreateContext(bool retryRequested, bool areRetryableWritesSupported, bool hasSessionId, bool isInTransaction)
        {
            var binding = CreateBinding(areRetryableWritesSupported, hasSessionId, isInTransaction);
            return RetryableWriteContext.Create(binding, retryRequested, CancellationToken.None);
        }

        private IRetryableWriteOperation<BsonDocument> CreateOperation(bool withWriteConcern, bool isAcknowledged)
        {
            var mockOperation = new Mock<IRetryableWriteOperation<BsonDocument>>();
            var writeConcern = withWriteConcern ? (isAcknowledged ? WriteConcern.Acknowledged : WriteConcern.Unacknowledged) : null;
            mockOperation.SetupGet(m => m.WriteConcern).Returns(writeConcern);
            return mockOperation.Object;
        }

        private ICoreSessionHandle CreateSession(bool hasSessionId, bool isInTransaction)
        {
            var mockSession = new Mock<ICoreSessionHandle>();
            mockSession.SetupGet(m => m.Id).Returns(hasSessionId ? new BsonDocument() : null);
            mockSession.SetupGet(m => m.IsInTransaction).Returns(isInTransaction);
            return mockSession.Object;
        }
    }

    // nested types
    public static class RetryableWriteOperationExecutorReflector
    {
        public static bool DoesContextAllowRetries(RetryableWriteContext context) =>
            (bool)Reflector.InvokeStatic(typeof(RetryableWriteOperationExecutor), nameof(DoesContextAllowRetries), context);

        public static bool IsOperationAcknowledged(IRetryableWriteOperation<BsonDocument> operation)
        {
            var methodInfoDefinition = typeof(RetryableWriteOperationExecutor).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.Name == nameof(IsOperationAcknowledged))
                .Single();
            var methodInfo = methodInfoDefinition.MakeGenericMethod(typeof(BsonDocument));
            try
            {
                return (bool)methodInfo.Invoke(null, new object[] { operation });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }
    }
}
