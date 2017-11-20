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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ServerSessionPoolTests
    {
        [Fact]
        public void constructor_should_intialize_instance()
        {
            var client = new Mock<IMongoClient>().Object;

            var result = new ServerSessionPool(client);

            result._client().Should().BeSameAs(client);
            result._pool().Count.Should().Be(0);
        }

        [Fact]
        public void constructor_should_throw_when_client_is_null()
        {
            var exception = Record.Exception(() => new ServerSessionPool(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("client");
        }

        [Theory]
        [InlineData(new int[0], -1)]
        [InlineData(new[] { 0 }, -1)]
        [InlineData(new[] { 1 }, 0)]
        [InlineData(new[] { 0, 0 }, -1)]
        [InlineData(new[] { 0, 1 }, 1)]
        [InlineData(new[] { 1, 0 }, 0)]
        [InlineData(new[] { 1, 1 }, 1)]
        [InlineData(new[] { 0, 0, 0 }, -1)]
        [InlineData(new[] { 0, 0, 1 }, 2)]
        [InlineData(new[] { 0, 1, 0 }, 1)]
        [InlineData(new[] { 0, 1, 1 }, 2)]
        [InlineData(new[] { 1, 0, 0 }, 0)]
        [InlineData(new[] { 1, 0, 1 }, 2)]
        [InlineData(new[] { 1, 1, 0 }, 1)]
        [InlineData(new[] { 1, 1, 1 }, 2)]
        [InlineData(new[] { 0, 0, 0, 0 }, -1)]
        [InlineData(new[] { 0, 0, 0, 1 }, 3)]
        [InlineData(new[] { 0, 0, 1, 0 }, 2)]
        [InlineData(new[] { 0, 0, 1, 1 }, 3)]
        [InlineData(new[] { 0, 1, 0, 0 }, 1)]
        [InlineData(new[] { 0, 1, 0, 1 }, 3)]
        [InlineData(new[] { 0, 1, 1, 0 }, 2)]
        [InlineData(new[] { 0, 1, 1, 1 }, 3)]
        [InlineData(new[] { 1, 0, 0, 0 }, 0)]
        [InlineData(new[] { 1, 0, 0, 1 }, 3)]
        [InlineData(new[] { 1, 0, 1, 0 }, 2)]
        [InlineData(new[] { 1, 0, 1, 1 }, 3)]
        [InlineData(new[] { 1, 1, 0, 0 }, 1)]
        [InlineData(new[] { 1, 1, 0, 1 }, 3)]
        [InlineData(new[] { 1, 1, 1, 0 }, 2)]
        [InlineData(new[] { 1, 1, 1, 1 }, 3)]
        public void AcquireSession_should_return_expected_result(int[] pooledSessionWasRecentlyUsed, int acquiredIndex)
        {
            var subject = CreateSubject();
            var mockPooledSessions = new List<Mock<IServerSession>>();
            foreach (var r in pooledSessionWasRecentlyUsed)
            {
                mockPooledSessions.Add(CreateMockSession(r == 1));
            }
            subject._pool().AddRange(mockPooledSessions.Select(m => m.Object));

            var result = subject.AcquireSession();

            var wrapper = result.Should().BeOfType<ServerSessionPool.ReleaseOnDisposeServerSession>().Subject;
            wrapper._disposed().Should().BeFalse();
            wrapper._pool().Should().BeSameAs(subject);

            if (acquiredIndex == -1)
            {
                mockPooledSessions.Select(m => m.Object).Should().NotContain(wrapper.Wrapped);
                subject._pool().Count.Should().Be(0);
            }
            else
            {
                wrapper.Wrapped.Should().BeSameAs(mockPooledSessions[acquiredIndex].Object);
                subject._pool().Count.Should().Be(acquiredIndex);
            }

            for (var i = 0; i < mockPooledSessions.Count; i++)
            {
                var mockPooledSession = mockPooledSessions[i];
                if (acquiredIndex == -1)
                {
                    mockPooledSession.Verify(m => m.Dispose(), Times.Once);
                }
                else if (i < acquiredIndex)
                {
                    mockPooledSession.Verify(m => m.Dispose(), Times.Never);
                    subject._pool()[i].Should().BeSameAs(mockPooledSession.Object);
                }
                else if (i == acquiredIndex)
                {
                    mockPooledSession.Verify(m => m.Dispose(), Times.Never);
                }
                else
                {
                    mockPooledSession.Verify(m => m.Dispose(), Times.Once);
                }
            }
        }

        [Theory]
        [InlineData(new int[0], 0, 0)]
        [InlineData(new int[0], 0, 1)]
        [InlineData(new[] { 0 }, 1, 0)]
        [InlineData(new[] { 0 }, 1, 1)]
        [InlineData(new[] { 1 }, 0, 0)]
        [InlineData(new[] { 1 }, 0, 1)]
        [InlineData(new[] { 0, 0 }, 2, 0)]
        [InlineData(new[] { 0, 0 }, 2, 1)]
        [InlineData(new[] { 0, 1 }, 1, 0)]
        [InlineData(new[] { 0, 1 }, 1, 1)]
        [InlineData(new[] { 1, 0 }, 0, 0)]
        [InlineData(new[] { 1, 0 }, 0, 1)]
        [InlineData(new[] { 1, 1 }, 0, 0)]
        [InlineData(new[] { 1, 1 }, 0, 1)]
        [InlineData(new[] { 0, 0, 0 }, 3, 0)]
        [InlineData(new[] { 0, 0, 0 }, 3, 1)]
        [InlineData(new[] { 0, 0, 1 }, 2, 0)]
        [InlineData(new[] { 0, 0, 1 }, 2, 1)]
        [InlineData(new[] { 0, 1, 0 }, 1, 0)]
        [InlineData(new[] { 0, 1, 0 }, 1, 1)]
        [InlineData(new[] { 0, 1, 1 }, 1, 0)]
        [InlineData(new[] { 0, 1, 1 }, 1, 1)]
        [InlineData(new[] { 1, 0, 0 }, 0, 0)]
        [InlineData(new[] { 1, 0, 0 }, 0, 1)]
        [InlineData(new[] { 1, 0, 1 }, 0, 0)]
        [InlineData(new[] { 1, 0, 1 }, 0, 1)]
        [InlineData(new[] { 1, 1, 0 }, 0, 0)]
        [InlineData(new[] { 1, 1, 0 }, 0, 1)]
        [InlineData(new[] { 1, 1, 1 }, 0, 0)]
        [InlineData(new[] { 1, 1, 1 }, 0, 1)]
        public void ReleaseSession_should_have_expected_result(int[] pooledSessionWasRecentlyUsed, int removeCount, int releasedSessionWasRecentlyUsed)
        {
            var subject = CreateSubject();
            var mockPooledSessions = new List<Mock<IServerSession>>();
            foreach (var r in pooledSessionWasRecentlyUsed)
            {
                mockPooledSessions.Add(CreateMockSession(r == 1));
            }
            subject._pool().AddRange(mockPooledSessions.Select(m => m.Object));
            var mockReleasedSession = CreateMockSession(releasedSessionWasRecentlyUsed == 1);

            subject.ReleaseSession(mockReleasedSession.Object);

            var expectedNewPoolSize = mockPooledSessions.Count - removeCount + (releasedSessionWasRecentlyUsed == 1 ? 1 : 0);
            subject._pool().Count().Should().Be(expectedNewPoolSize);

            for (var i = 0; i < mockPooledSessions.Count; i++)
            {
                mockPooledSessions[i].Verify(m => m.Dispose(), Times.Exactly(i < removeCount ? 1 : 0));
            }

            for (var i = 0; i < mockPooledSessions.Count - removeCount; i++)
            {
                subject._pool()[i].Should().BeSameAs(mockPooledSessions[i + removeCount].Object);
            }

            if (releasedSessionWasRecentlyUsed == 1)
            {
                mockReleasedSession.Verify(m => m.Dispose(), Times.Never);
                subject._pool().Last().Should().BeSameAs(mockReleasedSession.Object);
            }
            else
            {
                mockReleasedSession.Verify(m => m.Dispose(), Times.Once);
            }
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(1741, true)]
        [InlineData(1739, false)]
        public void IsAboutToExpire_should_return_expected_result(int? lastUsedSecondsAgo, bool expectedResult)
        {
            var subject = CreateSubject();
            var mockSession = new Mock<IServerSession>();
            var lastUsedAt = lastUsedSecondsAgo == null ? (DateTime?)null : DateTime.UtcNow.AddSeconds(-lastUsedSecondsAgo.Value);
            mockSession.SetupGet(m => m.LastUsedAt).Returns(lastUsedAt);

            var result = subject.IsAboutToExpire(mockSession.Object);

            result.Should().Be(expectedResult);
        }

        // private methods
        private Mock<IServerSession> CreateMockExpiredSession()
        {
            var mockSession = new Mock<IServerSession>();
            mockSession.SetupGet(m => m.LastUsedAt).Returns(DateTime.UtcNow.AddHours(-1));
            return mockSession;
        }

        private Mock<IServerSession> CreateMockRecentlyUsedSession()
        {
            var mockSession = new Mock<IServerSession>();
            mockSession.SetupGet(m => m.LastUsedAt).Returns(DateTime.UtcNow);
            return mockSession;
        }

        private Mock<IServerSession> CreateMockSession(bool recentlyUsed)
        {
            return recentlyUsed ? CreateMockRecentlyUsedSession() : CreateMockExpiredSession();
        }

        private ServerSessionPool CreateSubject()
        {
            var clusterId = new ClusterId();
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            var serverDescription = new ServerDescription(
                serverId,
                endPoint,
                logicalSessionTimeout: TimeSpan.FromMinutes(30),
                type: ServerType.ShardRouter,
                version: new SemanticVersion(3, 6, 0),
                wireVersionRange: new Range<int>(2, 6));

            var connectionMode = ClusterConnectionMode.Automatic;
            var type = ClusterType.Sharded;
            var servers = new[] { serverDescription };
            var clusterDescription = new ClusterDescription(clusterId, connectionMode, type, servers);

            var mockCluster = new Mock<ICluster>();
            mockCluster.SetupGet(m => m.Description).Returns(clusterDescription);

            var mockClient = new Mock<IMongoClient>();
            mockClient.SetupGet(m => m.Cluster).Returns(mockCluster.Object);

            return new ServerSessionPool(mockClient.Object);
        }
    }

    internal static class ServerSessionPoolReflector
    {
        public static IMongoClient _client(this ServerSessionPool obj)
        {
            var fieldInfo = typeof(ServerSessionPool).GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IMongoClient)fieldInfo.GetValue(obj);
        }

        public static List<IServerSession> _pool(this ServerSessionPool obj)
        {
            var fieldInfo = typeof(ServerSessionPool).GetField("_pool", BindingFlags.NonPublic | BindingFlags.Instance);
            return (List<IServerSession>)fieldInfo.GetValue(obj);
        }

        public static bool IsAboutToExpire(this ServerSessionPool obj, IServerSession session)
        {
            var methodInfo = typeof(ServerSessionPool).GetMethod("IsAboutToExpire", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)methodInfo.Invoke(obj, new object[] { session });
        }
    }

    public class ReleaseOnDisposeServerSessionTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var pool = new Mock<IServerSessionPool>().Object;
            var wrapped = new Mock<IServerSession>().Object;

            var result = new ServerSessionPool.ReleaseOnDisposeServerSession(wrapped, pool);

            result.Wrapped.Should().BeSameAs(wrapped);
            result._pool().Should().BeSameAs(pool);
            result._disposed().Should().BeFalse();
            result._ownsWrapped().Should().BeFalse();
        }

        [Fact]
        public void constructor_should_throw_when_wrapped_is_null()
        {
            var pool = new Mock<IServerSessionPool>().Object;

            var exception = Record.Exception(() => new ServerSessionPool.ReleaseOnDisposeServerSession(null, pool));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("wrapped");
        }

        [Fact]
        public void constructor_should_throw_when_pool_is_null()
        {
            var wrapped = new Mock<IServerSession>().Object;

            var exception = Record.Exception(() => new ServerSessionPool.ReleaseOnDisposeServerSession(wrapped, null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("pool");
        }

        [Fact]
        public void Dispose_should_set_disposed_flag()
        {
            var pool = new Mock<IServerSessionPool>().Object;
            var mockWrapped = new Mock<IServerSession>();
            var subject = new ServerSessionPool.ReleaseOnDisposeServerSession(mockWrapped.Object, pool);

            subject.Dispose();

            subject._disposed().Should().BeTrue();
        }

        [Fact]
        public void Dispose_should_call_ReleaseSession()
        {
            var mockPool = new Mock<IServerSessionPool>();
            var mockWrapped = new Mock<IServerSession>();
            var subject = new ServerSessionPool.ReleaseOnDisposeServerSession(mockWrapped.Object, mockPool.Object);

            subject.Dispose();

            mockPool.Verify(m => m.ReleaseSession(mockWrapped.Object), Times.Once);
            mockWrapped.Verify(m => m.Dispose(), Times.Never);
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            var mockPool = new Mock<IServerSessionPool>();
            var mockWrapped = new Mock<IServerSession>();
            var subject = new ServerSessionPool.ReleaseOnDisposeServerSession(mockWrapped.Object, mockPool.Object);

            subject.Dispose();
            subject.Dispose();

            mockPool.Verify(m => m.ReleaseSession(mockWrapped.Object), Times.Once);
            mockWrapped.Verify(m => m.Dispose(), Times.Never);
        }
    }

    internal static class ReleaseOnDisposeServerSessionReflector
    {
        public static IServerSessionPool _pool(this ServerSessionPool.ReleaseOnDisposeServerSession obj)
        {
            var fieldInfo = typeof(ServerSessionPool.ReleaseOnDisposeServerSession).GetField("_pool", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IServerSessionPool)fieldInfo.GetValue(obj);
        }
    }
}
