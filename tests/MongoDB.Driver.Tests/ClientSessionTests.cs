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
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ClientSessionTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void constructor_should_initialize_instance(bool isImplicit)
        {
            var client = new Mock<IMongoClient>().Object;
            var options = new ClientSessionOptions();
            var serverSession = new Mock<IServerSession>().Object;

            var subject = new ClientSession(client, options, serverSession, isImplicit);

            subject.Client.Should().BeSameAs(client);
            subject.Options.Should().BeSameAs(options);
            subject.ServerSession.Should().BeSameAs(serverSession);
            subject.IsImplicit.Should().Be(isImplicit);
            subject.ClusterTime.Should().BeNull();
            subject.OperationTime.Should().BeNull();
            subject._disposed().Should().BeFalse();
        }

        [Fact]
        public void constructor_should_throw_when_client_is_null()
        {
            var options = new ClientSessionOptions();
            var serverSession = new Mock<IServerSession>().Object;

            var exception = Record.Exception(() => new ClientSession(null, options, serverSession, false));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("client");
        }

        [Fact]
        public void constructor_should_throw_when_options_is_null()
        {
            var client = new Mock<IMongoClient>().Object;
            var serverSession = new Mock<IServerSession>().Object;

            var exception = Record.Exception(() => new ClientSession(client, null, serverSession, false));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("options");
        }

        [Fact]
        public void constructor_should_throw_when_serverSession_is_null()
        {
            var client = new Mock<IMongoClient>().Object;
            var options = new ClientSessionOptions();

            var exception = Record.Exception(() => new ClientSession(client, options, null, false));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("serverSession");
        }

        [Fact]
        public void Client_should_return_expected_result()
        {
            var client = new Mock<IMongoClient>().Object;
            var subject = CreateSubject(client: client);

            var result = subject.Client;

            result.Should().BeSameAs(client);
        }

        [Fact]
        public void Client_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.Client);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void ClusterTime_should_return_expected_result()
        {
            var subject = CreateSubject();
            var clusterClock = subject._clusterClock();
            var newClusterTime = CreateClusterTime();
            clusterClock.AdvanceClusterTime(newClusterTime);

            var result = subject.ClusterTime;

            result.Should().BeSameAs(newClusterTime);
        }

        [Fact]
        public void ClusterTime_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.ClusterTime);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsImplicit_should_return_expected_result(bool isImplicit)
        {
            var subject = CreateSubject(isImplicit: isImplicit);

            var result = subject.IsImplicit;

            result.Should().Be(isImplicit);
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
        public void OperationTime_should_return_expected_result()
        {
            var subject = CreateSubject();
            var operationClock = subject._operationClock();
            var newOperationTime = new BsonTimestamp(1L);
            operationClock.AdvanceOperationTime(newOperationTime);

            var result = subject.OperationTime;

            result.Should().BeSameAs(newOperationTime);
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
        public void Options_should_return_expected_result()
        {
            var options = new ClientSessionOptions();
            var subject = CreateSubject(options: options);

            var result = subject.Options;

            result.Should().BeSameAs(options);
        }

        [Fact]
        public void Options_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.Options);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void ServerSession_should_return_expected_result()
        {
            var serverSession = new Mock<IServerSession>().Object;
            var subject = CreateSubject(serverSession: serverSession);

            var result = subject.ServerSession;

            result.Should().BeSameAs(serverSession);
        }

        [Fact]
        public void ServerSession_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.ServerSession);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void AdvanceClusterTime_should_have_expected_result()
        {
            var subject = CreateSubject();
            var newClusterTime = CreateClusterTime();
            var clusterClock = subject._clusterClock();

            subject.AdvanceClusterTime(newClusterTime);

            clusterClock.ClusterTime.Should().Be(newClusterTime);
        }

        [Fact]
        public void AdvanceClusterTime_should_throw_when_newClusterTime_is_null()
        {
            var subject = CreateSubject();

            var exception = Record.Exception(() => subject.AdvanceClusterTime(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("newClusterTime");
        }

        [Fact]
        public void AdvanceClusterTime_should_throw_when_disposedl()
        {
            var subject = CreateDisposedSubject();
            var newClusterTime = CreateClusterTime();

            var exception = Record.Exception(() => subject.AdvanceClusterTime(newClusterTime));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void AdvanceOperationTime_should_have_expected_result()
        {
            var subject = CreateSubject();
            var newOperationTime = new BsonTimestamp(1L);
            var operationClock = subject._operationClock();

            subject.AdvanceOperationTime(newOperationTime);

            operationClock.OperationTime.Should().Be(newOperationTime);
        }

        [Fact]
        public void AdvanceOperationTime_should_throw_when_newClusterTime_is_null()
        {
            var subject = CreateSubject();

            var exception = Record.Exception(() => subject.AdvanceOperationTime(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("newOperationTime");
        }

        [Fact]
        public void AdvanceOperationTime_should_throw_when_disposedl()
        {
            var subject = CreateDisposedSubject();
            var newOperationTime = new BsonTimestamp(1L);

            var exception = Record.Exception(() => subject.AdvanceOperationTime(newOperationTime));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void Dispose_should_have_expected_result()
        {
            var subject = CreateSubject();

            subject.Dispose();

            subject._disposed().Should().BeTrue();
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            var mockServerSession = new Mock<IServerSession>();
            var subject = CreateSubject(serverSession: mockServerSession.Object);

            subject.Dispose();
            subject.Dispose();

            mockServerSession.Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_should_Dispose_server_session()
        {
            var mockServerSession = new Mock<IServerSession>();
            var subject = CreateSubject(serverSession: mockServerSession.Object);

            subject.Dispose();

            mockServerSession.Verify(m => m.Dispose(), Times.Once);
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

        private ClientSession CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private ClientSession CreateSubject(
            IMongoClient client = null,
            ClientSessionOptions options = null,
            IServerSession serverSession = null,
            bool isImplicit = false)
        {
            return new ClientSession(
                client ?? new Mock<IMongoClient>().Object,
                options ?? new ClientSessionOptions(),
                serverSession ?? new Mock<IServerSession>().Object,
                isImplicit);
        }
    }

    internal static class ClientSessionReflector
    {
        public static IClusterClock _clusterClock(this ClientSession obj)
        {
            var fieldInfo = typeof(ClientSession).GetField("_clusterClock", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IClusterClock)fieldInfo.GetValue(obj);
        }

        public static bool _disposed(this ClientSession obj)
        {
            var fieldInfo = typeof(ClientSession).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)fieldInfo.GetValue(obj);
        }

        public static IOperationClock _operationClock(this ClientSession obj)
        {
            var fieldInfo = typeof(ClientSession).GetField("_operationClock", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IOperationClock)fieldInfo.GetValue(obj);
        }
    }
}
