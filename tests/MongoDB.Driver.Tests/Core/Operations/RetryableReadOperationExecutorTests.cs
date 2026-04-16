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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
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
            var context = CreateContext(isRetryRequested, isInTransaction);
            var exception = CoreExceptionHelper.CreateException(nameof(MongoNodeIsRecoveringException));
            var operationContext = hasTimeout
                ? new OperationContext(TimeSpan.FromSeconds(42), CancellationToken.None)
                : new OperationContext(null, CancellationToken.None);
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
            var context = CreateContext(isRetryRequested, isInTransaction);
            var exception = new InvalidOperationException("Non-retryable exception");
            var operationContext = hasTimeout
                ? new OperationContext(TimeSpan.FromSeconds(42), CancellationToken.None)
                : new OperationContext(null, CancellationToken.None);
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
            var context = CreateContext(retryRequested: true, isInTransaction: false);
            var exception = CoreExceptionHelper.CreateMongoCommandExceptionWithLabels(2, "SystemOverloadedError", "RetryableError");
            var operationContext = new OperationContext(null, CancellationToken.None);
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
            var context = CreateContext(retryRequested: false, isInTransaction: false);
            var exception = CoreExceptionHelper.CreateMongoCommandExceptionWithLabels(2, "SystemOverloadedError", "RetryableError");
            var operationContext = new OperationContext(null, CancellationToken.None);
            var random = Mock.Of<IRandom>();

            var result = RetryableReadOperationExecutorReflector.ShouldRetry(
                operationContext, context, isOperationRetryable: true, exception, attempt: 1, random, overloadErrorSeen: false, out _);

            result.Should().BeFalse();
        }

        // private methods
        private static RetryableReadContext CreateContext(bool retryRequested, bool isInTransaction)
        {
            var sessionMock = new Mock<ICoreSessionHandle>();
            sessionMock.SetupGet(m => m.IsInTransaction).Returns(isInTransaction);
            var bindingMock = new Mock<IReadBinding>();
            bindingMock.SetupGet(m => m.Session).Returns(sessionMock.Object);
            return new RetryableReadContext(bindingMock.Object, retryRequested, RetryabilityHelper.OperationRetryBackpressureConstants.DefaultMaxRetries, enableOverloadRetargeting: false);
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
