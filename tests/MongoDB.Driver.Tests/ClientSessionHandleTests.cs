/* Copyright 2017-present MongoDB Inc.
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

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.TestHelpers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ClientSessionHandleTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var client = Mock.Of<IMongoClient>();
            var options = new ClientSessionOptions();
            var coreSession = CreateCoreSession();

            var result = new ClientSessionHandle(client, options, coreSession);

            result.Client.Should().BeSameAs(client);
            result.Options.Should().BeSameAs(options);
            result.WrappedCoreSession.Should().BeSameAs(coreSession);
            result._disposed().Should().BeFalse();

            var serverSession = result.ServerSession.Should().BeOfType<ServerSession>().Subject;
            serverSession._coreServerSession().Should().BeSameAs(coreSession.ServerSession);
        }

        [Fact]
        public void Client_returns_expected_result()
        {
            var client = Mock.Of<IMongoClient>();
            var subject = CreateSubject(client: client);

            var result = subject.Client;

            result.Should().BeSameAs(client);
        }

        [Fact]
        public void ClusterTime_should_return_expected_result()
        {
            var subject = CreateSubject();
            var value = new BsonDocument();
            var mockCoreSession = Mock.Get(subject.WrappedCoreSession);
            mockCoreSession.SetupGet(m => m.ClusterTime).Returns(value);

            var result = subject.ClusterTime;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsImplicit_should_call_coreSession(
            [Values(false, true)] bool value)
        {
            var subject = CreateSubject();
            var mockCoreSession = Mock.Get(subject.WrappedCoreSession);
            mockCoreSession.SetupGet(m => m.IsImplicit).Returns(value);

            var result = subject.IsImplicit;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsInTransaction_should_call_coreSession(
            [Values(false, true)] bool value)
        {
            var subject = CreateSubject();
            var mockCoreSession = Mock.Get(subject.WrappedCoreSession);
            mockCoreSession.SetupGet(m => m.IsInTransaction).Returns(value);

            var result = subject.IsInTransaction;

            result.Should().Be(value);
        }

        [Fact]
        public void OperationTime_should_call_coreSession()
        {
            var subject = CreateSubject();
            var value = new BsonTimestamp(0);
            var mockCoreSession = Mock.Get(subject.WrappedCoreSession);
            mockCoreSession.SetupGet(m => m.OperationTime).Returns(value);

            var result = subject.OperationTime;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void Options_returns_expected_result()
        {
            var options = new ClientSessionOptions();
            var subject = CreateSubject(options: options);

            var result = subject.Options;

            result.Should().BeSameAs(options);
        }

        [Fact]
        public void ServerSession_returns_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.ServerSession;

            result.Should().BeSameAs(subject._serverSession());
        }

        [Fact]
        public void WrappedCoreSession_returns_expected_result()
        {
            var coreSession = CreateCoreSession();
            var subject = CreateSubject(coreSession: coreSession);

            var result = subject.WrappedCoreSession;

            result.Should().BeSameAs(coreSession);
        }

        [Fact]
        public void AbortTransactionAsync_should_call_coreSession()
        {
            var subject = CreateSubject();
            var cancellationToken = new CancellationToken();
            var task = Task.FromResult(true);
            Mock.Get(subject.WrappedCoreSession).Setup(m => m.AbortTransactionAsync(cancellationToken)).Returns(task);

            var result = subject.AbortTransactionAsync(cancellationToken);

            result.Should().BeSameAs(task);
            Mock.Get(subject.WrappedCoreSession).Verify(m => m.AbortTransactionAsync(cancellationToken), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void AdvanceClusterTime_should_call_coreSession(
           [Values(false, true)] bool value)
        {
            var subject = CreateSubject();
            var newClusterTime = new BsonDocument();

            subject.AdvanceClusterTime(newClusterTime);

            Mock.Get(subject.WrappedCoreSession).Verify(m => m.AdvanceClusterTime(newClusterTime), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void AdvanceOperationTime_should_call_coreSession(
           [Values(false, true)] bool value)
        {
            var subject = CreateSubject();
            var newOperationTime = new BsonTimestamp(0);

            subject.AdvanceOperationTime(newOperationTime);

            Mock.Get(subject.WrappedCoreSession).Verify(m => m.AdvanceOperationTime(newOperationTime), Times.Once);
        }

        [Fact]
        public void CommitTransaction_should_call_coreSession()
        {
            var subject = CreateSubject();
            var cancellationToken = new CancellationToken();

            subject.CommitTransaction(cancellationToken);

            Mock.Get(subject.WrappedCoreSession).Verify(m => m.CommitTransaction(cancellationToken), Times.Once);
        }

        [Fact]
        public void CommitTransactionAsync_should_call_coreSession()
        {
            var subject = CreateSubject();
            var cancellationToken = new CancellationToken();
            var task = Task.FromResult(true);
            Mock.Get(subject.WrappedCoreSession).Setup(m => m.CommitTransactionAsync(cancellationToken)).Returns(task);

            var result = subject.CommitTransactionAsync(cancellationToken);

            result.Should().BeSameAs(task);
            Mock.Get(subject.WrappedCoreSession).Verify(m => m.CommitTransactionAsync(cancellationToken), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void Dispose_should_have_expected_result(
            [Values(1, 2)] int timesCalled)
        {
            var subject = CreateSubject();

            for (var i = 0; i < timesCalled; i++)
            {
                subject.Dispose();
            }

            subject._disposed().Should().BeTrue();
            Mock.Get(subject.WrappedCoreSession).Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void Fork_should_return_expected_result()
        {
            var cluster = Mock.Of<ICluster>();
            var coreServerSession = new CoreServerSession();
            var options = new ClientSessionOptions();
            var coreSession = new CoreSession(cluster, coreServerSession, options.ToCore());
            var coreSessionHandle = new CoreSessionHandle(coreSession);
            var subject = CreateSubject(coreSession: coreSessionHandle);
            coreSessionHandle.ReferenceCount().Should().Be(1);

            var result = subject.Fork();

            result.Client.Should().BeSameAs(subject.Client);
            result.Options.Should().BeSameAs(subject.Options);
            result.WrappedCoreSession.Should().NotBeSameAs(subject.WrappedCoreSession);
            var coreSessionHandle1 = (CoreSessionHandle)subject.WrappedCoreSession;
            var coreSessionHandle2 = (CoreSessionHandle)result.WrappedCoreSession;
            coreSessionHandle2.Wrapped.Should().BeSameAs(coreSessionHandle1.Wrapped);
            coreSessionHandle.ReferenceCount().Should().Be(2);
        }

        [Fact]
        public void StartTransaction_should_call_coreSession()
        {
            var subject = CreateSubject();
            var transactionOptions = new TransactionOptions();

            subject.StartTransaction(transactionOptions);

            Mock.Get(subject.WrappedCoreSession).Verify(m => m.StartTransaction(It.IsAny<TransactionOptions>()), Times.Once);
        }

        // private methods
        private ICoreSessionHandle CreateCoreSession(
            ICoreServerSession serverSession = null,
            CoreSessionOptions options = null)
        {
            serverSession = serverSession ?? new CoreServerSession();
            options = options ?? new CoreSessionOptions();

            var mockCoreSession = new Mock<ICoreSessionHandle>();
            mockCoreSession.SetupGet(m => m.Options).Returns(options);
            mockCoreSession.SetupGet(m => m.ServerSession).Returns(serverSession);
            mockCoreSession.Setup(m => m.Fork()).Returns(() => CreateCoreSession(serverSession: serverSession, options: options));
            return mockCoreSession.Object;
        }

        private ClientSessionHandle CreateSubject(
            IMongoClient client = null,
            ClientSessionOptions options = null,
            ICoreSessionHandle coreSession = null)
        {
            client = client ?? Mock.Of<IMongoClient>();
            options = options ?? new ClientSessionOptions();
            coreSession = coreSession ?? CreateCoreSession(options: options.ToCore());
            return new ClientSessionHandle(client, options, coreSession);
        }
    }

    internal static class ClientSessionHandleReflector
    {
        public static bool _disposed(this ClientSessionHandle obj)
        {
            var fieldInfo = typeof(ClientSessionHandle).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)fieldInfo.GetValue(obj);
        }

        public static IServerSession _serverSession(this ClientSessionHandle obj)
        {
            var fieldInfo = typeof(ClientSessionHandle).GetField("_serverSession", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IServerSession)fieldInfo.GetValue(obj);
        }
    }
}
