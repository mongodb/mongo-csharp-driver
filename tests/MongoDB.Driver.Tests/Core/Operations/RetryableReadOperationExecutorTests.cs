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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Operations
{
    public class RetryableReadOperationExecutorTests
    {
        [Theory]
        // No retries if retryRequested == false
        [InlineData(false, false, false, true, false, 1)]
        [InlineData(false, false, false, true, true, 1)]
        // No retries if in transaction
        [InlineData(false, true, true, true, false, 1)]
        [InlineData(false, true, true, true, true, 1)]
        // No retries in non-retriable exception
        [InlineData(false, true, false, false, false, 1)]
        [InlineData(false, true, false, false, true, 1)]
        // No timeout configured - should retry once
        [InlineData(true, true, false, true, false, 1)]
        [InlineData(false, true, false, true, false, 2)]
        // Timeout configured - should retry as many times as possible
        [InlineData(true, true, false, true, true, 1)]
        [InlineData(true, true, false, true, true, 2)]
        [InlineData(true, true, false, true, true, 10)]
        public void IsRetryableRead_should_return_expected_result(
            bool expected,
            bool isRetryRequested,
            bool isInTransaction,
            bool isRetriableException,
            bool hasTimeout,
            int attempt)
        {
            var retryableReadContext = CreateSubject(isRetryRequested, isInTransaction);
            var exception = CoreExceptionHelper.CreateException(isRetriableException ? nameof(MongoNodeIsRecoveringException) : nameof(IOException));
            var operationContext = new OperationContext(hasTimeout ? TimeSpan.FromSeconds(42) : null, CancellationToken.None);

            var result = RetryableReadOperationExecutorReflector.IsRetryableRead(operationContext, retryableReadContext, exception, attempt);

            Assert.Equal(expected, result);
        }

        private static RetryableReadContext CreateSubject(bool retryRequested, bool isInTransaction)
        {
            var sessionMock = new Mock<ICoreSessionHandle>();
            sessionMock.SetupGet(m => m.IsInTransaction).Returns(isInTransaction);
            var bindingMock = new Mock<IReadBinding>();
            bindingMock.SetupGet(m => m.Session).Returns(sessionMock.Object);
            return new RetryableReadContext(bindingMock.Object, retryRequested);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Execute_should_apply_backoff_when_SystemOverloadedError_occurs(bool async)
        {
            // Arrange - Create operation that always throws SystemOverloadedError
            var operationMock = new Mock<IRetryableReadOperation<int>>();
            var exception = CreateSystemOverloadedErrorException();
            operationMock
                .Setup(o => o.ExecuteAttempt(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>()))
                .Throws(exception);
            operationMock
                .Setup(o => o.ExecuteAttemptAsync(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>()))
                .ThrowsAsync(exception);

            // Test with no backoff (jitter = 0)
            var noBackoffRandom = new Mock<IRandom>();
            noBackoffRandom.Setup(r => r.NextDouble()).Returns(0.0);
            var noBackoffContext = CreateRetryableReadContext(noBackoffRandom.Object);

            var stopwatch = Stopwatch.StartNew();
            Exception noBackoffException;
            if (async)
            {
                noBackoffException = await Record.ExceptionAsync(async () =>
                    await RetryableReadOperationExecutor.ExecuteAsync(new OperationContext(TimeSpan.FromSeconds(30), CancellationToken.None), operationMock.Object, noBackoffContext));
            }
            else
            {
                noBackoffException = Record.Exception(() =>
                    RetryableReadOperationExecutor.Execute(new OperationContext(TimeSpan.FromSeconds(30), CancellationToken.None), operationMock.Object, noBackoffContext));
            }
            stopwatch.Stop();
            var noBackoffTime = stopwatch.ElapsedMilliseconds;

            noBackoffException.Should().NotBeNull();
            noBackoffException.Should().BeOfType<MongoCommandException>();

            // Test with full backoff (jitter = 1)
            var withBackoffRandom = new Mock<IRandom>();
            withBackoffRandom.Setup(r => r.NextDouble()).Returns(1.0);
            var withBackoffContext = CreateRetryableReadContext(withBackoffRandom.Object);

            stopwatch.Restart();
            Exception withBackoffException;
            if (async)
            {
                withBackoffException = await Record.ExceptionAsync(async () =>
                    await RetryableReadOperationExecutor.ExecuteAsync(new OperationContext(TimeSpan.FromSeconds(30), CancellationToken.None), operationMock.Object, withBackoffContext));
            }
            else
            {
                withBackoffException = Record.Exception(() =>
                    RetryableReadOperationExecutor.Execute(new OperationContext(TimeSpan.FromSeconds(30), CancellationToken.None), operationMock.Object, withBackoffContext));
            }
            stopwatch.Stop();
            var withBackoffTime = stopwatch.ElapsedMilliseconds;

            withBackoffException.Should().NotBeNull();
            withBackoffException.Should().BeOfType<MongoCommandException>();

            // Assert - Backoff should add at least 2100ms
            // The sum of 5 backoffs with jitter=1 is approximately 3100ms
            // We allow a 1-second tolerance window, so the difference should be at least 2100ms
            var difference = withBackoffTime - noBackoffTime;
            Assert.True(difference >= 2100, $"Expected at least 2100ms difference, got {difference}ms (noBackoff: {noBackoffTime}ms, withBackoff: {withBackoffTime}ms)");
        }

        private static MongoCommandException CreateSystemOverloadedErrorException()
        {
            var result = BsonDocument.Parse("{ ok: 0, code: 2, codeName: 'SystemOverloaded', errmsg: 'System overloaded', errorLabels: ['SystemOverloadedError', 'RetryableError'] }");
            var connectionId = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
            return new MongoCommandException(connectionId, "System overloaded", new BsonDocument("insert", "test"), result);
        }

        private static RetryableReadContext CreateRetryableReadContext(IRandom random)
        {
            // Create mock session
            var sessionMock = new Mock<ICoreSessionHandle>();
            sessionMock.SetupGet(s => s.IsInTransaction).Returns(false);
            sessionMock.SetupGet(s => s.Id).Returns((BsonDocument)null);

            // Create mock server with TokenBucket
            var serverMock = new Mock<IServer>();
            var tokenBucket = new TokenBucket();
            serverMock.SetupGet(s => s.TokenBucket).Returns(tokenBucket);

            // Create server description
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(new ClusterId(), endPoint);
            var serverDescription = new ServerDescription(serverId, endPoint);

            // Create mock channel
            var channelMock = new Mock<IChannelHandle>();

            // Create mock channel source
            var channelSourceMock = new Mock<IChannelSourceHandle>();
            channelSourceMock.SetupGet(cs => cs.Server).Returns(serverMock.Object);
            channelSourceMock.SetupGet(cs => cs.ServerDescription).Returns(serverDescription);
            channelSourceMock.Setup(cs => cs.GetChannel(It.IsAny<OperationContext>())).Returns(channelMock.Object);
            channelSourceMock.Setup(cs => cs.GetChannelAsync(It.IsAny<OperationContext>())).ReturnsAsync(channelMock.Object);

            // Create mock binding
            var bindingMock = new Mock<IReadBinding>();
            bindingMock.SetupGet(b => b.Session).Returns(sessionMock.Object);
            bindingMock.Setup(b => b.GetReadChannelSource(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
                .Returns(channelSourceMock.Object);
            bindingMock.Setup(b => b.GetReadChannelSourceAsync(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>()))
                .ReturnsAsync(channelSourceMock.Object);

            // Create context with custom random - RetryableReadContext is sealed, so we use reflection
            var context = new RetryableReadContext(bindingMock.Object, retryRequested: true, random);

            // Use reflection to set the private _channelSource and _channel fields
            var contextType = typeof(RetryableReadContext);
            var channelSourceField = contextType.GetField("_channelSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var channelField = contextType.GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            channelSourceField.SetValue(context, channelSourceMock.Object);
            channelField.SetValue(context, channelMock.Object);

            return context;
        }

        private static class RetryableReadOperationExecutorReflector
        {
            public static bool IsRetryableRead(OperationContext operationContext, RetryableReadContext context, Exception exception, int attempt)
                => (bool)Reflector.InvokeStatic(typeof(RetryableReadOperationExecutor), nameof(IsRetryableRead), operationContext, context, exception, attempt);
        }
    }
}

