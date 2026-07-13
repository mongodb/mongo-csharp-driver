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
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
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
    public class RetryableReadOperationExecutorTests
    {
        [Theory]
        // No retries if retryRequested == false
        [InlineData(false, false, true, false, false, 1, false)]
        // No retries if in transaction
        [InlineData(false, true, true, true, false, 1, false)]
        [InlineData(false, true, true, true, true, 1, false)]
        // No retries if isOperationRetryable == false (and not a backpressure exception)
        [InlineData(false, true, false, false, false, 1, false)]
        [InlineData(false, true, false, false, true, 1, false)]
        // No timeout configured - should retry once
        [InlineData(true, true, true, false, false, 1, false)]
        [InlineData(false, true, true, false, false, 2, false)]
        // Timeout configured - should retry as many times as possible
        [InlineData(true, true, true, false, true, 1, false)]
        [InlineData(true, true, true, false, true, 2, false)]
        [InlineData(true, true, true, false, true, 10, false)]
        // Previous overload error seen - cap retries at MaxAdaptiveRetries (default 2)
        [InlineData(true, true, true, false, false, 1, true)]
        [InlineData(true, true, true, false, false, 2, true)]
        [InlineData(false, true, true, false, false, 3, true)]
        public void ShouldRetry_should_return_expected_result(
            bool expected,
            bool isRetryRequested,
            bool isOperationRetryable,
            bool isInTransaction,
            bool hasTimeout,
            int attempt,
            bool overloadErrorSeen)
        {
            var context = CreateContext(isRetryRequested);
            var exception = CoreExceptionHelper.CreateException(nameof(MongoNodeIsRecoveringException));
            using var session = CreateSession(isInTransaction);
            using var operationContext = new OperationContext(session, hasTimeout ? TimeSpan.FromSeconds(42) : null);
            var random = Mock.Of<IRandom>();

            var result = RetryableReadOperationExecutorReflector.ShouldRetry(
                operationContext, context, isOperationRetryable, exception, attempt, random, overloadErrorSeen, out _);

            result.Should().Be(expected);
        }

        [Theory]
        // Non-retryable exception
        [InlineData(false, true, true, false, false, 1, false)]
        [InlineData(false, true, true, false, true, 1, false)]
        public void ShouldRetry_with_non_retryable_exception_should_return_false(
            bool expected,
            bool isRetryRequested,
            bool isOperationRetryable,
            bool isInTransaction,
            bool hasTimeout,
            int attempt,
            bool overloadErrorSeen)
        {
            var context = CreateContext(isRetryRequested);
            var exception = new InvalidOperationException("Non-retryable exception");
            using var session = CreateSession(isInTransaction);
            using var operationContext = new OperationContext(session, hasTimeout ? TimeSpan.FromSeconds(42) : null);
            var random = Mock.Of<IRandom>();

            var result = RetryableReadOperationExecutorReflector.ShouldRetry(
                operationContext, context, isOperationRetryable, exception, attempt, random, overloadErrorSeen, out _);

            result.Should().Be(expected);
        }

        [Theory]
        // SystemOverloaded + RetryableError: backpressure retry path, capped at MaxAdaptiveRetries (default 2)
        [InlineData(true, 1)]
        [InlineData(true, 2)]
        [InlineData(false, 3)]
        public void ShouldRetry_with_system_overloaded_exception_should_apply_backpressure_logic(
            bool expected,
            int attempt)
        {
            var context = CreateContext(retryRequested: true);
            var exception = CoreExceptionHelper.CreateMongoCommandExceptionWithLabels(2, "SystemOverloadedError", "RetryableError");
            using var session = CreateSession(false);
            using var operationContext = new OperationContext(session);
            var randomMock = new Mock<IRandom>();
            randomMock.Setup(r => r.NextDouble()).Returns(0.5);

            var result = RetryableReadOperationExecutorReflector.ShouldRetry(
                operationContext, context, isOperationRetryable: true, exception, attempt, randomMock.Object, overloadErrorSeen: false, out var backoff);

            result.Should().Be(expected);
            if (result)
            {
                backoff.Should().BeGreaterThan(TimeSpan.Zero);
            }
        }

        [Fact]
        public void ShouldRetry_with_system_overloaded_exception_should_not_retry_when_retryRequested_is_false()
        {
            var context = CreateContext(retryRequested: false);
            var exception = CoreExceptionHelper.CreateMongoCommandExceptionWithLabels(2, "SystemOverloadedError", "RetryableError");
            using var session = CreateSession(false);
            using var operationContext = new OperationContext(session);
            var random = Mock.Of<IRandom>();

            var result = RetryableReadOperationExecutorReflector.ShouldRetry(
                operationContext, context, isOperationRetryable: true, exception, attempt: 1, random, overloadErrorSeen: false, out _);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(1, 50, 0, 100)]
        [InlineData(2, 50, 0, 200)]
        [InlineData(1, 10000, 0, 10000)]
        [InlineData(2, 20000, 0, 10000)]
        public void ShouldRetry_with_baseBackoffMs_should_use_it_as_backoff_base(
            int attempt,
            int baseBackoffMs,
            int expectedRangeMinMs,
            int expectedRangeMaxMs)
        {
            var context = CreateContext(retryRequested: true);
            var result = BsonDocument.Parse($"{{ ok : 0, code : 2, baseBackoffMS : {baseBackoffMs} }}");
            var exception = CoreExceptionHelper.CreateMongoCommandExceptionWithLabels(result, "SystemOverloadedError", "RetryableError");
            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            var randomMock = new Mock<IRandom>();
            randomMock.Setup(r => r.NextDouble()).Returns(1.0);

            var didRetry = RetryableReadOperationExecutorReflector.ShouldRetry(
                operationContext, context, isOperationRetryable: true, exception, attempt, randomMock.Object, overloadErrorSeen: false, out var backoff);

            didRetry.Should().BeTrue();
            backoff.TotalMilliseconds.Should().BeInRange(expectedRangeMinMs, expectedRangeMaxMs);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Backoff_should_honor_CSOT_deadline(bool async)
        {
            using var session = NoCoreSession.NewHandle();
            using var operationContext = new OperationContext(session, timeout: TimeSpan.FromMilliseconds(200));
            var randomMock = new Mock<IRandom>();
            randomMock.Setup(r => r.NextDouble()).Returns(1.0);

            // baseBackoffMS = 5000 yields a first-retry backoff clamped to MaxBackoff (~10s), far larger than the 200ms deadline.
            var result = BsonDocument.Parse("{ ok : 0, code : 2, baseBackoffMS : 5000 }");
            var exception = CoreExceptionHelper.CreateMongoCommandExceptionWithLabels(result, "SystemOverloadedError", "RetryableError");
            var operationMock = new Mock<IRetryableReadOperation<int>>();
            operationMock.Setup(o => o.ExecuteAttempt(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>())).Throws(exception);
            operationMock.Setup(o => o.ExecuteAttemptAsync(It.IsAny<OperationContext>(), It.IsAny<RetryableReadContext>(), It.IsAny<int>(), It.IsAny<long?>())).ThrowsAsync(exception);
            var context = CreateExecutableContext(randomMock.Object);

            var stopwatch = Stopwatch.StartNew();
            var thrown = async
                ? await Record.ExceptionAsync(() => RetryableReadOperationExecutor.ExecuteAsync(operationContext, operationMock.Object, context))
                : Record.Exception(() => RetryableReadOperationExecutor.Execute(operationContext, operationMock.Object, context));
            stopwatch.Stop();

            // The retry loop must bail with the overload error once the backoff would exceed the remaining CSOT deadline,
            // rather than sleeping the full backoff (regression test for the async path, which lacked this guard).
            thrown.Should().BeOfType<MongoCommandException>();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        }

        // private methods
        private static RetryableReadContext CreateExecutableContext(IRandom random)
        {
            var serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));
            var serverDescription = new ServerDescription(serverId, serverId.EndPoint);
            var channel = new Mock<IChannelHandle>().Object;

            var channelSource = new Mock<IChannelSourceHandle>();
            channelSource.SetupGet(cs => cs.ServerDescription).Returns(serverDescription);
            channelSource.Setup(cs => cs.GetChannel(It.IsAny<OperationContext>())).Returns(channel);
            channelSource.Setup(cs => cs.GetChannelAsync(It.IsAny<OperationContext>())).ReturnsAsync(channel);

            var binding = new Mock<IReadBinding>();
            binding.Setup(b => b.GetReadChannelSource(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>())).Returns(channelSource.Object);
            binding.Setup(b => b.GetReadChannelSourceAsync(It.IsAny<OperationContext>(), It.IsAny<IReadOnlyCollection<ServerDescription>>())).ReturnsAsync(channelSource.Object);

            return new RetryableReadContext(binding.Object, retryRequested: true, RetryabilityHelper.OperationRetryBackpressureConstants.DefaultMaxRetries, enableOverloadRetargeting: false, random);
        }

        private static RetryableReadContext CreateContext(bool retryRequested)
        {
            var bindingMock = new Mock<IReadBinding>();
            return new RetryableReadContext(bindingMock.Object, retryRequested, RetryabilityHelper.OperationRetryBackpressureConstants.DefaultMaxRetries, enableOverloadRetargeting: false);
        }

        private static ICoreSessionHandle CreateSession(bool isInTransaction)
        {
            var sessionMock = new Mock<ICoreSessionHandle>();
            sessionMock.SetupGet(m => m.IsInTransaction).Returns(isInTransaction);
            return sessionMock.Object;
        }
    }

    internal static class RetryableReadOperationExecutorReflector
    {
        public static bool ShouldRetry(
            OperationContext operationContext,
            RetryableReadContext context,
            bool isOperationRetryable,
            Exception exception,
            int attempt,
            IRandom random,
            bool overloadErrorSeen,
            out TimeSpan backoff)
        {
            var methodInfo = typeof(RetryableReadOperationExecutor)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Single(m => m.Name == nameof(ShouldRetry) && m.GetParameters().Length == 8);

            var args = new object[] { operationContext, context, isOperationRetryable, exception, attempt, random, overloadErrorSeen, default(TimeSpan) };

            try
            {
                var result = (bool)methodInfo.Invoke(null, args);
                backoff = (TimeSpan)args[7];
                return result;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}
