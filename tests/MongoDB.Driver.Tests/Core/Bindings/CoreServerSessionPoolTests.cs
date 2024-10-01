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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Encryption;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class CoreServerSessionPoolTests
    {
        [Fact]
        public void constructor_should_intialize_instance()
        {
            var cluster = new Mock<IClusterInternal>().Object;

            var result = new CoreServerSessionPool(cluster);

            result._cluster().Should().BeSameAs(cluster);
            result._pool().Count.Should().Be(0);
        }

        [Fact]
        public void constructor_should_throw_when_cluster_is_null()
        {
            var exception = Record.Exception(() => new CoreServerSessionPool(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("cluster");
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
            var mockPooledSessions = new List<Mock<ICoreServerSession>>();
            foreach (var r in pooledSessionWasRecentlyUsed)
            {
                mockPooledSessions.Add(CreateMockSession(r == 1));
            }
            subject._pool().AddRange(mockPooledSessions.Select(m => m.Object));

            var result = subject.AcquireSession();

            var wrapper = result.Should().BeOfType<CoreServerSessionPool.ReleaseOnDisposeCoreServerSession>().Subject;
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
            var mockPooledSessions = new List<Mock<ICoreServerSession>>();
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

        [Fact]
        public void IsAboutToExpire_should_never_expire_in_load_balancing_mode()
        {
            var subject = CreateSubject();
            var mockedCluster = new TestCluster(ClusterType.LoadBalanced);
            var mockedServerSessionPool = new CoreServerSessionPool(mockedCluster);
            var mockSession = new Mock<ICoreServerSession>();
            var lastUsedAt = DateTime.UtcNow.AddSeconds(1741);
            mockSession.SetupGet(m => m.LastUsedAt).Returns(lastUsedAt);

            var result = mockedServerSessionPool.IsAboutToExpire(mockSession.Object);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(1741, true)]
        [InlineData(1739, false)]
        public void IsAboutToExpire_should_return_expected_result(int? lastUsedSecondsAgo, bool expectedResult)
        {
            var subject = CreateSubject();
            var mockSession = new Mock<ICoreServerSession>();
            var lastUsedAt = lastUsedSecondsAgo == null ? (DateTime?)null : DateTime.UtcNow.AddSeconds(-lastUsedSecondsAgo.Value);
            mockSession.SetupGet(m => m.LastUsedAt).Returns(lastUsedAt);

            var result = subject.IsAboutToExpire(mockSession.Object);

            result.Should().Be(expectedResult);
        }

        // private methods
        private Mock<ICoreServerSession> CreateMockExpiredSession()
        {
            var mockSession = new Mock<ICoreServerSession>();
            mockSession.SetupGet(m => m.LastUsedAt).Returns(DateTime.UtcNow.AddHours(-1));
            return mockSession;
        }

        private Mock<ICoreServerSession> CreateMockRecentlyUsedSession()
        {
            var mockSession = new Mock<ICoreServerSession>();
            mockSession.SetupGet(m => m.LastUsedAt).Returns(DateTime.UtcNow);
            return mockSession;
        }

        private Mock<ICoreServerSession> CreateMockSession(bool recentlyUsed)
        {
            return recentlyUsed ? CreateMockRecentlyUsedSession() : CreateMockExpiredSession();
        }

        private CoreServerSessionPool CreateSubject()
        {
            var clusterId = new ClusterId();
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            var serverDescription = new ServerDescription(
                serverId,
                endPoint,
                logicalSessionTimeout: TimeSpan.FromMinutes(30),
                state: ServerState.Connected,
                type: ServerType.ShardRouter,
                version: new SemanticVersion(3, 6, 0),
                wireVersionRange: new Range<int>(6, 14));

            var clusterDescription = new ClusterDescription(clusterId, false, null, ClusterType.Sharded, [serverDescription]);

            var mockCluster = new Mock<IClusterInternal>();
            mockCluster.SetupGet(m => m.Description).Returns(clusterDescription);

            return new CoreServerSessionPool(mockCluster.Object);
        }

        private class TestCluster : IClusterInternal
        {
            public TestCluster(ClusterType clusterType)
            {
                Description = new ClusterDescription(new ClusterId(), false, null, clusterType, Enumerable.Empty<ServerDescription>());
            }

            public ClusterId ClusterId => throw new NotImplementedException();

            public ClusterDescription Description { get; }

            public ClusterSettings Settings => throw new NotImplementedException();

            public event EventHandler<ClusterDescriptionChangedEventArgs> DescriptionChanged;

            public ICoreServerSession AcquireServerSession() => throw new NotImplementedException();
            public void Dispose() => throw new NotImplementedException();
            public void Initialize() => DescriptionChanged?.Invoke(this, new ClusterDescriptionChangedEventArgs(Description, Description));
            public IServer SelectServer(IServerSelector selector, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task<IServer> SelectServerAsync(IServerSelector selector, CancellationToken cancellationToken) => throw new NotImplementedException();
            public ICoreSessionHandle StartSession(CoreSessionOptions options = null) => throw new NotImplementedException();
        }
    }

    internal static class CoreServerSessionPoolReflector
    {
        public static IClusterInternal _cluster(this CoreServerSessionPool obj)
        {
            var fieldInfo = typeof(CoreServerSessionPool).GetField("_cluster", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IClusterInternal)fieldInfo.GetValue(obj);
        }

        public static List<ICoreServerSession> _pool(this CoreServerSessionPool obj)
        {
            var fieldInfo = typeof(CoreServerSessionPool).GetField("_pool", BindingFlags.NonPublic | BindingFlags.Instance);
            return (List<ICoreServerSession>)fieldInfo.GetValue(obj);
        }

        public static bool IsAboutToExpire(this CoreServerSessionPool obj, ICoreServerSession session)
        {
            var methodInfo = typeof(CoreServerSessionPool).GetMethod("IsAboutToExpire", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)methodInfo.Invoke(obj, new object[] { session });
        }
    }

    public class ReleaseOnDisposeCoreServerSessionTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var pool = new Mock<ICoreServerSessionPool>().Object;
            var wrapped = new Mock<ICoreServerSession>().Object;

            var result = new CoreServerSessionPool.ReleaseOnDisposeCoreServerSession(wrapped, pool);

            result.Wrapped.Should().BeSameAs(wrapped);
            result._pool().Should().BeSameAs(pool);
            result._disposed().Should().BeFalse();
            result._ownsWrapped().Should().BeFalse();
        }

        [Fact]
        public void constructor_should_throw_when_wrapped_is_null()
        {
            var pool = new Mock<ICoreServerSessionPool>().Object;

            var exception = Record.Exception(() => new CoreServerSessionPool.ReleaseOnDisposeCoreServerSession(null, pool));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("wrapped");
        }

        [Fact]
        public void constructor_should_throw_when_pool_is_null()
        {
            var wrapped = new Mock<ICoreServerSession>().Object;

            var exception = Record.Exception(() => new CoreServerSessionPool.ReleaseOnDisposeCoreServerSession(wrapped, null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("pool");
        }

        [Fact]
        public void Dispose_should_set_disposed_flag()
        {
            var pool = new Mock<ICoreServerSessionPool>().Object;
            var mockWrapped = new Mock<ICoreServerSession>();
            var subject = new CoreServerSessionPool.ReleaseOnDisposeCoreServerSession(mockWrapped.Object, pool);

            subject.Dispose();

            subject._disposed().Should().BeTrue();
        }

        [Fact]
        public void Dispose_should_call_ReleaseSession()
        {
            var mockPool = new Mock<ICoreServerSessionPool>();
            var mockWrapped = new Mock<ICoreServerSession>();
            var subject = new CoreServerSessionPool.ReleaseOnDisposeCoreServerSession(mockWrapped.Object, mockPool.Object);

            subject.Dispose();

            mockPool.Verify(m => m.ReleaseSession(mockWrapped.Object), Times.Once);
            mockWrapped.Verify(m => m.Dispose(), Times.Never);
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            var mockPool = new Mock<ICoreServerSessionPool>();
            var mockWrapped = new Mock<ICoreServerSession>();
            var subject = new CoreServerSessionPool.ReleaseOnDisposeCoreServerSession(mockWrapped.Object, mockPool.Object);

            subject.Dispose();
            subject.Dispose();

            mockPool.Verify(m => m.ReleaseSession(mockWrapped.Object), Times.Once);
            mockWrapped.Verify(m => m.Dispose(), Times.Never);
        }
    }

    internal static class ReleaseOnDisposeCoreServerSessionReflector
    {
        public static ICoreServerSessionPool _pool(this CoreServerSessionPool.ReleaseOnDisposeCoreServerSession obj)
        {
            var fieldInfo = typeof(CoreServerSessionPool.ReleaseOnDisposeCoreServerSession).GetField("_pool", BindingFlags.NonPublic | BindingFlags.Instance);
            return (ICoreServerSessionPool)fieldInfo.GetValue(obj);
        }
    }
}
