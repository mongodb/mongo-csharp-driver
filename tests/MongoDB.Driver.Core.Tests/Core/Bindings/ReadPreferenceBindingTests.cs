/* Copyright 2013-present MongoDB Inc.
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
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class ReadPreferenceBindingTests
    {
        private Mock<ICluster> _mockCluster;

        public ReadPreferenceBindingTests()
        {
            _mockCluster = new Mock<ICluster>();
        }

        [Fact]
        public void Constructor_should_throw_if_cluster_is_null()
        {
            Action act = () => new ReadPreferenceBinding(null, ReadPreference.Primary, NoCoreSession.NewHandle());

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_if_readPreference_is_null()
        {
            Action act = () => new ReadPreferenceBinding(_mockCluster.Object, null, NoCoreSession.NewHandle());

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_if_session_is_null()
        {
            Action act = () => new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Session_should_return_expected_result()
        {
            var session = new Mock<ICoreSessionHandle>().Object;
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary, session);

            var result = subject.Session;

            result.Should().BeSameAs(session);
        }

        [Theory]
        [ParameterAttributeData]
        public void GetReadChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary, NoCoreSession.NewHandle());
            subject.Dispose();

            Action act;
            if (async)
            {
                act = () => subject.GetReadChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.GetReadChannelSource(CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetReadChannelSource_should_use_a_read_preference_server_selector_to_select_the_server_from_the_cluster(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary, NoCoreSession.NewHandle());
            var selectedServer = new Mock<IServer>().Object;

            var clusterId = new ClusterId();
            var endPoint = new DnsEndPoint("localhost", 27017);
#pragma warning disable CS0618 // Type or member is obsolete
            var initialClusterDescription = new ClusterDescription(
                clusterId,
                ClusterConnectionMode.Automatic,
                ClusterType.Unknown,
                new[] { new ServerDescription(new ServerId(clusterId, endPoint), endPoint) });
#pragma warning restore CS0618 // Type or member is obsolete
            var finalClusterDescription = initialClusterDescription.WithType(ClusterType.Standalone);
            _mockCluster.SetupSequence(c => c.Description).Returns(initialClusterDescription).Returns(finalClusterDescription);

            if (async)
            {
                _mockCluster.Setup(c => c.SelectServerAsync(It.IsAny<ReadPreferenceServerSelector>(), CancellationToken.None)).Returns(Task.FromResult(selectedServer));

                subject.GetReadChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();

                _mockCluster.Verify(c => c.SelectServerAsync(It.IsAny<ReadPreferenceServerSelector>(), CancellationToken.None), Times.Once);
            }
            else
            {
                _mockCluster.Setup(c => c.SelectServer(It.IsAny<ReadPreferenceServerSelector>(), CancellationToken.None)).Returns(selectedServer);

                subject.GetReadChannelSource(CancellationToken.None);

                _mockCluster.Verify(c => c.SelectServer(It.IsAny<ReadPreferenceServerSelector>(), CancellationToken.None), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void GetReadChannelSource_should_fork_the_session(
            [Values(false, true)] bool async)
        {
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary, mockSession.Object);
            var cancellationToken = new CancellationTokenSource().Token;

            var selectedServer = new Mock<IServer>().Object;
            _mockCluster.Setup(m => m.SelectServer(It.IsAny<IServerSelector>(), cancellationToken)).Returns(selectedServer);
            _mockCluster.Setup(m => m.SelectServerAsync(It.IsAny<IServerSelector>(), cancellationToken)).Returns(Task.FromResult(selectedServer));
            var forkedSession = new Mock<ICoreSessionHandle>().Object;
            mockSession.Setup(m => m.Fork()).Returns(forkedSession);

            var clusterId = new ClusterId();
            var endPoint = new DnsEndPoint("localhost", 27017);
#pragma warning disable CS0618 // Type or member is obsolete
            var initialClusterDescription = new ClusterDescription(
                clusterId,
                ClusterConnectionMode.Automatic,
                ClusterType.Unknown,
                new[] { new ServerDescription(new ServerId(clusterId, endPoint), endPoint) });
#pragma warning restore CS0618 // Type or member is obsolete
            var finalClusterDescription = initialClusterDescription.WithType(ClusterType.Standalone);
            _mockCluster.SetupSequence(c => c.Description).Returns(initialClusterDescription).Returns(finalClusterDescription);

            IChannelSourceHandle result;
            if (async)
            {
                result = subject.GetReadChannelSourceAsync(cancellationToken).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.GetReadChannelSource(cancellationToken);
            }

            var handle = result.Should().BeOfType<ChannelSourceHandle>().Subject;
            var referenceCounted = handle._reference().Should().BeOfType<ReferenceCounted<IChannelSource>>().Subject;
            var source = referenceCounted.Instance;
            source.Session.Should().BeSameAs(forkedSession);
        }

        [Fact]
        public void Dispose_should_not_call_dispose_on_the_cluster()
        {
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary, NoCoreSession.NewHandle());

            subject.Dispose();

            _mockCluster.Verify(c => c.Dispose(), Times.Never);
        }

        [Fact]
        public void Dispose_should_call_dispose_on_the_session()
        {
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary, mockSession.Object);

            subject.Dispose();

            mockSession.Verify(c => c.Dispose(), Times.Once);
        }
    }

    public static class ReadPreferenceBindingReflector
    {
        public static ICluster _cluster(this ReadPreferenceBinding obj)
        {
            var fieldInfo = typeof(ReadPreferenceBinding).GetField("_cluster", BindingFlags.NonPublic | BindingFlags.Instance);
            return (ICluster)fieldInfo.GetValue(obj);
        }
    }
}
