/* Copyright 2017 MongoDB Inc.
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
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ClientSessionWrappingCoreSessionTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var clientSession = new Mock<IClientSession>().Object;

            var subject = new ClientSessionWrappingCoreSession(clientSession);

            subject._clientSession().Should().BeSameAs(clientSession);
            subject._disposed().Should().BeFalse();
        }

        [Fact]
        public void constructor_should_throw_when_clientSession_is_null()
        {
            var exception = Record.Exception(() => new ClientSessionWrappingCoreSession(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("clientSession");
        }

        [Fact]
        public void ClusterTime_should_call_client_session()
        {
            Mock<IClientSession> mockClientSession;
            var subject = CreateSubject(out mockClientSession);

            var result = subject.ClusterTime;

            mockClientSession.Verify(m => m.ClusterTime, Times.Once);
        }

        [Fact]
        public void ClusterTime_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.ClusterTime);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void Id_should_call_client_session()
        {
            Mock<IClientSession> mockClientSession;
            var subject = CreateSubject(out mockClientSession);
            var mockServerSession = new Mock<IServerSession>();
            mockClientSession.SetupGet(m => m.ServerSession).Returns(mockServerSession.Object);

            var result = subject.Id;

            mockServerSession.Verify(m => m.Id, Times.Once);
        }

        [Fact]
        public void Id_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.Id);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void IsImplicit_should_call_client_session()
        {
            Mock<IClientSession> mockClientSession;
            var subject = CreateSubject(out mockClientSession);

            var result = subject.IsImplicit;

            mockClientSession.Verify(m => m.IsImplicit, Times.Once);
        }

        [Fact]
        public void IsImplicit_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.IsImplicit);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void OperationTime_should_call_client_session()
        {
            Mock<IClientSession> mockClientSession;
            var subject = CreateSubject(out mockClientSession);

            var result = subject.OperationTime;

            mockClientSession.Verify(m => m.OperationTime, Times.Once);
        }

        [Fact]
        public void OperationTime_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.OperationTime);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void AdvanceClusterTime_should_call_client_session()
        {
            Mock<IClientSession> mockClientSession;
            var subject = CreateSubject(out mockClientSession);
            var newClusterTime = CreateClusterTime();

            subject.AdvanceClusterTime(newClusterTime);

            mockClientSession.Verify(m => m.AdvanceClusterTime(newClusterTime), Times.Once);
        }

        [Fact]
        public void AdvanceClusterTime_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var newClusterTime = CreateClusterTime();

            var exception = Record.Exception(() => subject.AdvanceClusterTime(newClusterTime));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void AdvanceOperationTime_should_call_client_session()
        {
            Mock<IClientSession> mockClientSession;
            var subject = CreateSubject(out mockClientSession);
            var newOperationTime = new BsonTimestamp(1L);

            subject.AdvanceOperationTime(newOperationTime);

            mockClientSession.Verify(m => m.AdvanceOperationTime(newOperationTime), Times.Once);
        }

        [Fact]
        public void AdvanceOperationTime_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var newOperationTime = new BsonTimestamp(1L);

            var exception = Record.Exception(() => subject.AdvanceOperationTime(newOperationTime));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void Dispose_should_set_disposed_flag()
        {
            var subject = CreateSubject();

            subject.Dispose();

            subject._disposed().Should().BeTrue();
        }

        [Fact]
        public void Dispose_should_dispose_client_session()
        {
            Mock<IClientSession> mockClientSession;
            var subject = CreateSubject(out mockClientSession);

            subject.Dispose();

            mockClientSession.Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            Mock<IClientSession> mockClientSession;
            var subject = CreateSubject(out mockClientSession);

            subject.Dispose();
            subject.Dispose();

            mockClientSession.Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void WasUsed_should_call_server_session()
        {
            Mock<IServerSession> mockServerSession;
            var subject = CreateSubject(out  mockServerSession);

            subject.WasUsed();

            mockServerSession.Verify(m => m.WasUsed(), Times.Once);
        }

        [Fact]
        public void WasUsed_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.WasUsed());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        // private methods
        private BsonDocument CreateClusterTime(long timestamp = 1L)
        {
            return new BsonDocument
            {
                { "xyz", 1 },
                { "clusterTime", new BsonTimestamp(timestamp) }
            };
        }

        private ClientSessionWrappingCoreSession CreateDisposedSubject()
        {
            Mock<IClientSession> mockClientSession;
            var subject = CreateSubject(out mockClientSession);
            subject.Dispose();
            return subject;
        }

        private ClientSessionWrappingCoreSession CreateSubject()
        {
            var clientSession = new Mock<IClientSession>().Object;
            return new ClientSessionWrappingCoreSession(clientSession);
        }

        private ClientSessionWrappingCoreSession CreateSubject(out Mock<IClientSession> mockClientSession)
        {
            mockClientSession = new Mock<IClientSession>();
            return new ClientSessionWrappingCoreSession(mockClientSession.Object);
        }

        private ClientSessionWrappingCoreSession CreateSubject(out Mock<IServerSession> mockServerSession)
        {
            mockServerSession = new Mock<IServerSession>();
            var mockClientSession = new Mock<IClientSession>();
            mockClientSession.SetupGet(m => m.ServerSession).Returns(mockServerSession.Object);
            return new ClientSessionWrappingCoreSession(mockClientSession.Object);
        }
    }

    internal static class ClientSessionWrappingCoreSessionReflector
    {
        public static IClientSession _clientSession(this ClientSessionWrappingCoreSession obj)
        {
            var fieldInfo = typeof(ClientSessionWrappingCoreSession).GetField("_clientSession", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IClientSession)fieldInfo.GetValue(obj);
        }

        public static bool _disposed(this ClientSessionWrappingCoreSession obj)
        {
            var fieldInfo = typeof(ClientSessionWrappingCoreSession).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)fieldInfo.GetValue(obj);
        }
    }
}
