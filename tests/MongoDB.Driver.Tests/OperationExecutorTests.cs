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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        [Theory]
        [ParameterAttributeData]
        public async Task StartImplicitSession_should_call_cluster_StartSession([Values(true, false)]bool isAsync)
        {
            var subject = CreateSubject(out var clusterMock, out _);
            if (isAsync)
            {
                await subject.StartImplicitSessionAsync(CancellationToken.None);
            }
            else
            {
                subject.StartImplicitSession(CancellationToken.None);
            }

            clusterMock.Verify(c => c.StartSession(It.Is<CoreSessionOptions>(v => v.IsImplicit && v.IsCausallyConsistent == false && v.IsSnapshot == false)));
        }

        [Theory]
        [MemberData(nameof(ImplicitSessionTestCases))]
        public async Task ExecuteReadOperation_should_start_and_dispose_implicit_session_if_needed(bool shouldCreateSession, bool isAsync, IClientSessionHandle session)
        {
            var subject = CreateSubject(out var clusterMock, out var implicitSessionMock);
            var readOperation = Mock.Of<IReadOperation<object>>();
            var readOperationOptions = new ReadOperationOptions();

            _ = isAsync ?
                await subject.ExecuteReadOperationAsync(readOperation, readOperationOptions, session, false, cancellationToken: CancellationToken.None) :
                subject.ExecuteReadOperation(readOperation, readOperationOptions, session, false, cancellationToken: CancellationToken.None);

            var times = shouldCreateSession ? Times.Once() : Times.Never();
            clusterMock.Verify(c => c.StartSession(It.Is<CoreSessionOptions>(v => v.IsImplicit && v.IsCausallyConsistent == false && v.IsSnapshot == false)), times);
            implicitSessionMock.Verify(s => s.Dispose(), times);
        }

        [Theory]
        [MemberData(nameof(ImplicitSessionTestCases))]
        public async Task ExecuteWriteOperation_should_start_and_dispose_implicit_session_if_needed(bool shouldCreateSession, bool isAsync, IClientSessionHandle session)
        {
            var subject = CreateSubject(out var clusterMock, out var implicitSessionMock);
            var writeOperation = Mock.Of<IWriteOperation<object>>();
            var writeOperationOptions = new WriteOperationOptions();

            _ = isAsync ?
                await subject.ExecuteWriteOperationAsync(writeOperation, writeOperationOptions, session, false, cancellationToken: CancellationToken.None) :
                subject.ExecuteWriteOperation(writeOperation, writeOperationOptions, session, false, cancellationToken: CancellationToken.None);

            var times = shouldCreateSession ? Times.Once() : Times.Never();
            clusterMock.Verify(c => c.StartSession(It.Is<CoreSessionOptions>(v => v.IsImplicit && v.IsCausallyConsistent == false && v.IsSnapshot == false)), times);
            implicitSessionMock.Verify(s => s.Dispose(), times);
        }

        private static IEnumerable<object[]> ImplicitSessionTestCases()
        {
            yield return [ true, false, null ];
            yield return [ true, true, null ];

            var implicitSession = new Mock<IClientSessionHandle>();
            implicitSession.SetupGet(s => s.IsImplicit).Returns(true);
            implicitSession.SetupGet(s => s.WrappedCoreSession).Returns(CreateCoreSessionMock(true).Object);
            yield return [ false, false, implicitSession.Object ];
            yield return [ false, true, implicitSession.Object ];

            var regularSession = new Mock<IClientSessionHandle>();
            regularSession.SetupGet(s => s.WrappedCoreSession).Returns(CreateCoreSessionMock(false).Object);
            yield return [ false, false, regularSession.Object ];
            yield return [ false, true, regularSession.Object ];
        }

        private OperationExecutor CreateSubject(out Mock<IClusterInternal> clusterMock, out Mock<ICoreSessionHandle> implicitSessionMock)
        {
            implicitSessionMock = CreateCoreSessionMock(true);
            clusterMock = new Mock<IClusterInternal>();
            clusterMock.Setup(c => c.StartSession(It.IsAny<CoreSessionOptions>())).Returns(implicitSessionMock.Object);
            var clientMock = new Mock<IMongoClient>();
            clientMock.SetupGet(c => c.Cluster).Returns(clusterMock.Object);
            return new OperationExecutor(clientMock.Object);
        }

        private static Mock<ICoreSessionHandle> CreateCoreSessionMock(bool isImplicit)
        {
            var sessionMock = new Mock<ICoreSessionHandle>();
            sessionMock.SetupGet(s => s.IsImplicit).Returns(isImplicit);
            sessionMock.Setup(s => s.Fork()).Returns(() => CreateCoreSessionMock(isImplicit).Object);
            return sessionMock;
        }
    }
}

