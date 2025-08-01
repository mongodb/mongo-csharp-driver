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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class OperationExecutorTests
    {
        [Fact]
        public void StartImplicitSession_should_call_cluster_StartSession()
        {
            var subject = CreateSubject(out var clusterMock);

            subject.StartImplicitSession();

            clusterMock.Verify(c => c.StartSession(It.Is<CoreSessionOptions>(v => v.IsImplicit && v.IsCausallyConsistent == false && v.IsSnapshot == false)));
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ExecuteReadOperation_throws_on_null_operation([Values(true, false)] bool async)
        {
            var subject = CreateSubject(out _);
            var operationContext = new OperationContext(Timeout.InfiniteTimeSpan, CancellationToken.None);
            var session = Mock.Of<IClientSessionHandle>();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.ExecuteReadOperationAsync<object>(operationContext, session, null, ReadPreference.Primary, true)) :
                Record.Exception(() => subject.ExecuteReadOperation<object>(operationContext, session, null, ReadPreference.Primary, true));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("operation");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ExecuteReadOperation_throws_on_null_readPreference([Values(true, false)] bool async)
        {
            var subject = CreateSubject(out _);
            var operationContext = new OperationContext(Timeout.InfiniteTimeSpan, CancellationToken.None);
            var operation = Mock.Of<IReadOperation<object>>();
            var session = Mock.Of<IClientSessionHandle>();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.ExecuteReadOperationAsync(operationContext, session, operation, null, true)) :
                Record.Exception(() => subject.ExecuteReadOperation(operationContext, session, operation, null, true));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("readPreference");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ExecuteReadOperation_throws_on_null_session([Values(true, false)] bool async)
        {
            var subject = CreateSubject(out _);
            var operationContext = new OperationContext(Timeout.InfiniteTimeSpan, CancellationToken.None);
            var operation = Mock.Of<IReadOperation<object>>();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.ExecuteReadOperationAsync(operationContext, null, operation, ReadPreference.Primary, true)) :
                Record.Exception(() => subject.ExecuteReadOperation(operationContext, null, operation, ReadPreference.Primary, true));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("session");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ExecuteWriteOperation_throws_on_null_operation([Values(true, false)] bool async)
        {
            var subject = CreateSubject(out _);
            var operationContext = new OperationContext(Timeout.InfiniteTimeSpan, CancellationToken.None);
            var session = Mock.Of<IClientSessionHandle>();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.ExecuteWriteOperationAsync<object>(operationContext, session, null, true)) :
                Record.Exception(() => subject.ExecuteWriteOperation<object>(operationContext, session, null, true));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("operation");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ExecuteWriteOperation_throws_on_null_session([Values(true, false)] bool async)
        {
            var subject = CreateSubject(out _);
            var operationContext = new OperationContext(Timeout.InfiniteTimeSpan, CancellationToken.None);
            var operation = Mock.Of<IWriteOperation<object>>();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.ExecuteWriteOperationAsync(operationContext, null, operation, true)) :
                Record.Exception(() => subject.ExecuteWriteOperation(operationContext, null, operation, true));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("session");
        }

        private OperationExecutor CreateSubject(out Mock<IClusterInternal> clusterMock)
        {
            clusterMock = new Mock<IClusterInternal>();
            var clientMock = new Mock<IMongoClient>();
            clientMock.SetupGet(c => c.Cluster).Returns(clusterMock.Object);
            return new OperationExecutor(clientMock.Object);
        }
    }
}

