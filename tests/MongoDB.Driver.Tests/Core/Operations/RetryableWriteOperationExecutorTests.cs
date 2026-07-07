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
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
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
        public void DoesContextAllowRetries_should_return_expected_result(
            bool retryRequested,
            bool areRetryableWritesSupported,
            bool hasSessionId,
            bool isInTransaction,
            bool expectedResult)
        {
            var context = CreateContext(retryRequested, areRetryableWritesSupported, hasSessionId, isInTransaction);
            using var session = CreateSession(hasSessionId, isInTransaction);
            using var operationContext = new OperationContext(session);

            var result = RetryableWriteOperationExecutorReflector.DoesContextAllowRetries(operationContext, context, context.ChannelSource.ServerDescription);

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

        [Theory]
        [InlineData(1, 50, 0, 50)]
        [InlineData(2, 50, 0, 100)]
        [InlineData(1, 10000, 0, 10000)]
        [InlineData(2, 20000, 0, 10000)]
        public void ShouldRetry_with_retryAfterMs_should_use_it_as_backoff_base(
            int attempt,
            int retryAfterMs,
            int expectedRangeMinMs,
            int expectedRangeMaxMs)
        {
            var context = CreateContext(retryRequested: true, areRetryableWritesSupported: true, hasSessionId: true, isInTransaction: false);
            var result = BsonDocument.Parse($"{{ ok : 0, code : 2, retryAfterMS : {retryAfterMs} }}");
            var exception = CoreExceptionHelper.CreateMongoCommandExceptionWithLabels(result, "SystemOverloadedError", "RetryableError");
            var operationContext = new OperationContext(null, CancellationToken.None);
            var randomMock = new Mock<IRandom>();
            randomMock.Setup(r => r.NextDouble()).Returns(1.0);

            var didRetry = RetryableWriteOperationExecutorReflector.ShouldRetry(
                operationContext,
                errorDuringChannelAcquisition: false,
                context.ChannelSource.ServerDescription,
                WriteConcern.Acknowledged,
                context,
                exception,
                attempt,
                randomMock.Object,
                isEndTransactionOperation: false,
                isOperationRetryable: true,
                overloadErrorSeen: false,
                out var backoff);

            didRetry.Should().BeTrue();
            backoff.TotalMilliseconds.Should().BeInRange(expectedRangeMinMs, expectedRangeMaxMs);
        }

        // private methods
        private IWriteBinding CreateBinding(bool areRetryableWritesSupported, bool hasSessionId, bool isInTransaction)
        {
            var mockBinding = new Mock<IWriteBinding>();
            var channelSource = CreateChannelSource(areRetryableWritesSupported);
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
            var context = new RetryableWriteContext(binding, retryRequested, RetryabilityHelper.OperationRetryBackpressureConstants.DefaultMaxRetries, false);
            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            context.SelectServer(operationContext, null);
            context.AcquireChannel(operationContext);
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

        public static bool DoesContextAllowRetries(OperationContext operationContext, RetryableWriteContext context, ServerDescription server)
            => (bool)Reflector.InvokeStatic(typeof(RetryableWriteOperationExecutor), nameof(DoesContextAllowRetries), operationContext, context, server);

        public static bool IsOperationAcknowledged(WriteConcern writeConcern)
            => (bool)Reflector.InvokeStatic(typeof(RetryableWriteOperationExecutor), nameof(IsOperationAcknowledged), writeConcern);

        public static bool ShouldRetry(
            OperationContext operationContext,
            bool errorDuringChannelAcquisition,
            ServerDescription server,
            WriteConcern writeConcern,
            RetryableWriteContext context,
            Exception exception,
            int attempt,
            IRandom random,
            bool isEndTransactionOperation,
            bool isOperationRetryable,
            bool overloadErrorSeen,
            out TimeSpan backoff)
        {
            var methodInfo = typeof(RetryableWriteOperationExecutor)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Single(m => m.Name == nameof(ShouldRetry) && m.GetParameters().Length == 12);

            var args = new object[] { operationContext, errorDuringChannelAcquisition, server, writeConcern, context, exception, attempt, random, isEndTransactionOperation, isOperationRetryable, overloadErrorSeen, default(TimeSpan) };

            try
            {
                var result = (bool)methodInfo.Invoke(null, args);
                backoff = (TimeSpan)args[11];
                return result;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}
