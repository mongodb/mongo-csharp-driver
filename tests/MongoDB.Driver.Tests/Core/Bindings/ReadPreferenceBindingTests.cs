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
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
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
            Action act = () => new ReadPreferenceBinding(null, ReadPreference.Primary);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_if_readPreference_is_null()
        {
            Action act = () => new ReadPreferenceBinding(_mockCluster.Object, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary);
            subject.Dispose();

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            var exception = async ?
                await Record.ExceptionAsync(() => subject.GetReadChannelSourceAsync(operationContext)) :
                Record.Exception(() => subject.GetReadChannelSource(operationContext));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_use_a_read_preference_server_selector_to_select_the_server_from_the_cluster(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary);
            var selectedServer = new Mock<IServer>().Object;

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

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            if (async)
            {
                _mockCluster.Setup(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<ReadPreferenceServerSelector>())).Returns(Task.FromResult(selectedServer));

                await subject.GetReadChannelSourceAsync(operationContext);

                _mockCluster.Verify(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<ReadPreferenceServerSelector>()), Times.Once);
            }
            else
            {
                _mockCluster.Setup(c => c.SelectServer(It.IsAny<OperationContext>(), It.IsAny<ReadPreferenceServerSelector>())).Returns(selectedServer);

                subject.GetReadChannelSource(operationContext);

                _mockCluster.Verify(c => c.SelectServer(It.IsAny<OperationContext>(), It.IsAny<ReadPreferenceServerSelector>()), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_fork_the_session(
            [Values(false, true)] bool async)
        {
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary);
            var selectedServer = new Mock<IServer>().Object;
            _mockCluster.Setup(m => m.SelectServer(It.IsAny<OperationContext>(), It.IsAny<IServerSelector>())).Returns(selectedServer);
            _mockCluster.Setup(m => m.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<IServerSelector>())).Returns(Task.FromResult(selectedServer));

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

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            var result = async ?
                await subject.GetReadChannelSourceAsync(operationContext) :
                subject.GetReadChannelSource(operationContext);

            var handle = result.Should().BeOfType<ChannelSourceHandle>().Subject;
            handle._reference().Should().BeOfType<ReferenceCounted<IChannelSource>>();
        }

        [Fact]
        public void Dispose_should_not_call_dispose_on_the_cluster()
        {
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary);

            subject.Dispose();

            _mockCluster.Verify(c => c.Dispose(), Times.Never);
        }
    }

    internal static class ReadPreferenceBindingReflector
    {
        public static IClusterInternal _cluster(this ReadPreferenceBinding obj)
        {
            var fieldInfo = typeof(ReadPreferenceBinding).GetField("_cluster", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IClusterInternal)fieldInfo.GetValue(obj);
        }
    }
}
