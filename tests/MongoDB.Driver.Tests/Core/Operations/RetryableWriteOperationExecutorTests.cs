/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Operations
{
    public class RetryableWriteOperationExecutorTests
    {
        [Fact]
        public void AreRetryableWritesSupportedTest()
        {
            var serverDescription = CreateServerDescription(withLogicalSessionTimeout: false, isLoadBalanced: true);

            var result = RetryableWriteOperationExecutorReflector.AreRetryableWritesSupported(serverDescription);

            result.Should().BeTrue();
        }

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

            var result = RetryableWriteOperationExecutorReflector.DoesContextAllowRetries(context, context.ChannelSource.ServerDescription);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void IsOperationAcknowledged_should_return_expected_result(bool? isAcknowledged, bool expectedResult)
        {
            var writeConcern = isAcknowledged.HasValue ? (isAcknowledged.Value ? WriteConcern.Acknowledged : WriteConcern.Unacknowledged) : null;

            var result = RetryableWriteOperationExecutorReflector.IsOperationAcknowledged(writeConcern);

            result.Should().Be(expectedResult);
        }

        // private methods
        private IWriteBinding CreateBinding(bool areRetryableWritesSupported, bool hasSessionId, bool isInTransaction)
        {
            var mockBinding = new Mock<IWriteBinding>();
            var session = CreateSession(hasSessionId, isInTransaction);
            var channelSource = CreateChannelSource(areRetryableWritesSupported);
            mockBinding.SetupGet(m => m.Session).Returns(session);
            mockBinding.Setup(m => m.GetWriteChannelSource(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>())).Returns(channelSource);
            return mockBinding.Object;
        }

        private IChannelSourceHandle CreateChannelSource(bool areRetryableWritesSupported)
        {
            var mockChannelSource = new Mock<IChannelSourceHandle>();
            var channel = Mock.Of<IChannelHandle>();
            mockChannelSource.Setup(m => m.GetChannel(It.IsAny<OperationContext>())).Returns(channel);
            mockChannelSource.Setup(m => m.ServerDescription).Returns(CreateServerDescription(withLogicalSessionTimeout: areRetryableWritesSupported));
            return mockChannelSource.Object;
        }

        private ServerDescription CreateServerDescription(bool withLogicalSessionTimeout, bool isLoadBalanced = false)
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            TimeSpan? logicalSessionTimeout = withLogicalSessionTimeout ? TimeSpan.FromMinutes(1) : null;
            var serverType = isLoadBalanced ? ServerType.LoadBalanced : ServerType.ShardRouter;

            return new ServerDescription(serverId, endPoint, logicalSessionTimeout: logicalSessionTimeout, type: serverType);
        }

        private RetryableWriteContext CreateContext(bool retryRequested, bool areRetryableWritesSupported, bool hasSessionId, bool isInTransaction)
        {
            var binding = CreateBinding(areRetryableWritesSupported, hasSessionId, isInTransaction);
            var context = RetryableWriteContext.Create(binding, retryRequested);
            context.AcquireOrReplaceChannel(OperationContext.NoTimeout, null);
            return context;
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
    internal static class RetryableWriteOperationExecutorReflector
    {
        public static bool AreRetryableWritesSupported(ServerDescription serverDescription)
            => (bool)Reflector.InvokeStatic(typeof(RetryableWriteOperationExecutor), nameof(AreRetryableWritesSupported), serverDescription);

        public static bool DoesContextAllowRetries(RetryableWriteContext context, ServerDescription server)
            => (bool)Reflector.InvokeStatic(typeof(RetryableWriteOperationExecutor), nameof(DoesContextAllowRetries), context, server);

        public static bool IsOperationAcknowledged(WriteConcern writeConcern)
            => (bool)Reflector.InvokeStatic(typeof(RetryableWriteOperationExecutor), nameof(IsOperationAcknowledged), writeConcern);
    }
}
