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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class ReadPreferenceBindingTests
    {
        private Mock<IClusterInternal> _mockCluster;

        public ReadPreferenceBindingTests()
        {
            _mockCluster = new Mock<IClusterInternal>();
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
        public async Task GetReadChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary, NoCoreSession.NewHandle());
            subject.Dispose();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.GetReadChannelSourceAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => subject.GetReadChannelSource(OperationContext.NoTimeout));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_use_a_read_preference_server_selector_to_select_the_server_from_the_cluster(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary, NoCoreSession.NewHandle());
            var selectedServer = (new Mock<IServer>().Object, TimeSpan.FromMilliseconds(42));

            var clusterId = new ClusterId();
            var endPoint = new DnsEndPoint("localhost", 27017);
            var initialClusterDescription = new ClusterDescription(
                clusterId,
                false,
                null,
                ClusterType.Unknown,
                [new ServerDescription(new ServerId(clusterId, endPoint), endPoint)]);

            var finalClusterDescription = initialClusterDescription.WithType(ClusterType.Standalone);
            _mockCluster.SetupSequence(c => c.Description).Returns(initialClusterDescription).Returns(finalClusterDescription);

            if (async)
            {
                _mockCluster.Setup(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<ReadPreferenceServerSelector>())).Returns(Task.FromResult(selectedServer));

                await subject.GetReadChannelSourceAsync(OperationContext.NoTimeout);

                _mockCluster.Verify(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<ReadPreferenceServerSelector>()), Times.Once);
            }
            else
            {
                _mockCluster.Setup(c => c.SelectServer(It.IsAny<OperationContext>(), It.IsAny<ReadPreferenceServerSelector>())).Returns(selectedServer);

                subject.GetReadChannelSource(OperationContext.NoTimeout);

                _mockCluster.Verify(c => c.SelectServer(It.IsAny<OperationContext>(), It.IsAny<ReadPreferenceServerSelector>()), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_fork_the_session(
            [Values(false, true)] bool async)
        {
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary, mockSession.Object);
            var selectedServer = (new Mock<IServer>().Object, TimeSpan.FromMilliseconds(42));
            _mockCluster.Setup(m => m.SelectServer(It.IsAny<OperationContext>(), It.IsAny<IServerSelector>())).Returns(selectedServer);
            _mockCluster.Setup(m => m.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<IServerSelector>())).Returns(Task.FromResult(selectedServer));
            var forkedSession = new Mock<ICoreSessionHandle>().Object;
            mockSession.Setup(m => m.Fork()).Returns(forkedSession);

            var clusterId = new ClusterId();
            var endPoint = new DnsEndPoint("localhost", 27017);
            var initialClusterDescription = new ClusterDescription(
                clusterId,
                false,
                null,
                ClusterType.Unknown,
                [new ServerDescription(new ServerId(clusterId, endPoint), endPoint)]);

            var finalClusterDescription = initialClusterDescription.WithType(ClusterType.Standalone);
            _mockCluster.SetupSequence(c => c.Description).Returns(initialClusterDescription).Returns(finalClusterDescription);

            var result = async ?
                await subject.GetReadChannelSourceAsync(OperationContext.NoTimeout) :
                subject.GetReadChannelSource(OperationContext.NoTimeout);

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

    internal static class ReadPreferenceBindingReflector
    {
        public static IClusterInternal _cluster(this ReadPreferenceBinding obj)
            => (IClusterInternal)Reflector.GetFieldValue(obj, "_cluster");
    }
}
