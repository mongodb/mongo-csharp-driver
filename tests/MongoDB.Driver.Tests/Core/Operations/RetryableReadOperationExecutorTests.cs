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
using System.Threading;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.TestHelpers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Operations
{
    public class RetryableReadOperationExecutorTests
    {
        //TODO Add right test
        // [Theory]
        // // No retries if retryRequested == false
        // [InlineData(false, false, false, true, false, 1)]
        // [InlineData(false, false, false, true, true, 1)]
        // // No retries if in transaction
        // [InlineData(false, true, true, true, false, 1)]
        // [InlineData(false, true, true, true, true, 1)]
        // // No retries in non-retriable exception
        // [InlineData(false, true, false, false, false, 1)]
        // [InlineData(false, true, false, false, true, 1)]
        // // No timeout configured - should retry once
        // [InlineData(true, true, false, true, false, 1)]
        // [InlineData(false, true, false, true, false, 2)]
        // // Timeout configured - should retry as many times as possible
        // [InlineData(true, true, false, true, true, 1)]
        // [InlineData(true, true, false, true, true, 2)]
        // [InlineData(true, true, false, true, true, 10)]
        // public void IsRetryableRead_should_return_expected_result(
        //     bool expected,
        //     bool isRetryRequested,
        //     bool isInTransaction,
        //     bool isRetriableException,
        //     bool hasTimeout,
        //     int attempt)
        // {
        //     var retryableReadContext = CreateSubject(isRetryRequested, isInTransaction);
        //     var exception = CoreExceptionHelper.CreateException(isRetriableException ? nameof(MongoNodeIsRecoveringException) : nameof(IOException));
        //     var operationContext = new OperationContext(hasTimeout ? TimeSpan.FromSeconds(42) : null, CancellationToken.None);
        //
        //     var result = RetryableReadOperationExecutorReflector.IsRetryableRead(operationContext, retryableReadContext, exception, attempt);
        //
        //     Assert.Equal(expected, result);
        // }

        private static RetryableReadContext CreateSubject(bool retryRequested, bool isInTransaction)
        {
            var sessionMock = new Mock<ICoreSessionHandle>();
            sessionMock.SetupGet(m => m.IsInTransaction).Returns(isInTransaction);
            var bindingMock = new Mock<IReadBinding>();
            bindingMock.SetupGet(m => m.Session).Returns(sessionMock.Object);
            return new RetryableReadContext(bindingMock.Object, retryRequested);
        }

        private static class RetryableReadOperationExecutorReflector
        {
            public static bool IsRetryableRead(OperationContext operationContext, RetryableReadContext context, Exception exception, int attempt)
                => (bool)Reflector.InvokeStatic(typeof(RetryableReadOperationExecutor), nameof(IsRetryableRead), operationContext, context, exception, attempt);
        }
    }
}

