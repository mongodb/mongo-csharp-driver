﻿/* Copyright 2017-present MongoDB Inc.
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

using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;
using Moq;
using Xunit;

namespace MongoDB.Driver.Legacy.Tests
{
    public class DefaultLegacyOperationExecutorTests
    {
        [Fact]
        public void ExecuteReadOperation_should_execute_operation()
        {
            var subject = CreateSubject();
            var binding = new Mock<IReadBinding>().Object;
            var mockOperation = new Mock<IReadOperation<BsonDocument>>();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var result = subject.ExecuteReadOperation(binding, mockOperation.Object, cancellationToken);

            mockOperation.Verify(m => m.Execute(binding, cancellationToken), Times.Once);
        }

        [Fact]
        public void ExecuteReadOperationAsync_should_execute_operation()
        {
            var subject = CreateSubject();
            var binding = new Mock<IReadBinding>().Object;
            var mockOperation = new Mock<IReadOperation<BsonDocument>>();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var result = subject.ExecuteReadOperationAsync(binding, mockOperation.Object, cancellationToken);

            mockOperation.Verify(m => m.ExecuteAsync(binding, cancellationToken), Times.Once);
        }

        [Fact]
        public void ExecuteWriteOperation_should_execute_operation()
        {
            var subject = CreateSubject();
            var binding = new Mock<IWriteBinding>().Object;
            var mockOperation = new Mock<IWriteOperation<BsonDocument>>();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var result = subject.ExecuteWriteOperation(binding, mockOperation.Object, cancellationToken);

            mockOperation.Verify(m => m.Execute(binding, cancellationToken), Times.Once);
        }

        [Fact]
        public void ExecuteWriteOperationAsync_should_execute_operation()
        {
            var subject = CreateSubject();
            var binding = new Mock<IWriteBinding>().Object;
            var mockOperation = new Mock<IWriteOperation<BsonDocument>>();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var result = subject.ExecuteWriteOperationAsync(binding, mockOperation.Object, cancellationToken);

            mockOperation.Verify(m => m.ExecuteAsync(binding, cancellationToken), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void StartImplicitSession_should_return_expected_result(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            IClientSessionHandle result;
            if (async)
            {
                result = subject.StartImplicitSession(cancellationToken);
            }
            else
            {
                result = subject.StartImplicitSessionAsync(cancellationToken).GetAwaiter().GetResult();
            }

            result.Client.Should().BeNull();
            result.Options.Should().BeNull();
            result.WrappedCoreSession.Should().NotBeNull();

            var coreSessionHandle = result.WrappedCoreSession.Should().BeOfType<CoreSessionHandle>().Subject;
            var referenceCountedCoreSession = coreSessionHandle.Wrapped.Should().BeOfType<ReferenceCountedCoreSession>().Subject;
            referenceCountedCoreSession.Wrapped.Should().BeOfType<NoCoreSession>();
        }

        // private methods
        private DefaultLegacyOperationExecutor CreateSubject()
        {
            return new DefaultLegacyOperationExecutor();
        }
    }
}
