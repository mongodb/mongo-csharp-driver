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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Servers;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class WritableServerBindingTests
    {
        private Mock<IClusterInternal> _mockCluster;

        public WritableServerBindingTests()
        {
            _mockCluster = new Mock<IClusterInternal>();
        }

        [Fact]
        public void Constructor_should_throw_if_cluster_is_null()
        {
            Action act = () => new WritableServerBinding(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ReadPreference_should_be_primary()
        {
            var subject = new WritableServerBinding(_mockCluster.Object);

            subject.ReadPreference.Should().Be(ReadPreference.Primary);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new WritableServerBinding(_mockCluster.Object);
            subject.Dispose();

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());

            var exception = async ?
                await Record.ExceptionAsync(() => subject.GetReadChannelSourceAsync(operationContext)) :
                Record.Exception(() => subject.GetReadChannelSource(operationContext));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_use_a_writable_server_selector_to_select_the_server_from_the_cluster(
            [Values(false, true)]
            bool async)
        {
            var subject = new WritableServerBinding(_mockCluster.Object);
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
                _mockCluster.Setup(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<WritableServerSelector>())).Returns(Task.FromResult(selectedServer));

                await subject.GetReadChannelSourceAsync(operationContext);

                _mockCluster.Verify(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<WritableServerSelector>()), Times.Once);
            }
            else
            {
                _mockCluster.Setup(c => c.SelectServer(It.IsAny<OperationContext>(), It.IsAny<WritableServerSelector>())).Returns(selectedServer);

                subject.GetReadChannelSource(operationContext);

                _mockCluster.Verify(c => c.SelectServer(It.IsAny<OperationContext>(), It.IsAny<WritableServerSelector>()), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetWriteChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new WritableServerBinding(_mockCluster.Object);
            subject.Dispose();

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            var exception = async ?
                await Record.ExceptionAsync(() => subject.GetWriteChannelSourceAsync(operationContext)) :
                Record.Exception(() => subject.GetWriteChannelSource(operationContext));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetWriteChannelSourceAsync_should_use_a_writable_server_selector_to_select_the_server_from_the_cluster(
            [Values(false, true)]
            bool async)
        {
            var subject = new WritableServerBinding(_mockCluster.Object);
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
                _mockCluster.Setup(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<WritableServerSelector>())).Returns(Task.FromResult(selectedServer));

                await subject.GetWriteChannelSourceAsync(operationContext);

                _mockCluster.Verify(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<WritableServerSelector>()), Times.Once);
            }
            else
            {
                _mockCluster.Setup(c => c.SelectServer(It.IsAny<OperationContext>(), It.IsAny<WritableServerSelector>())).Returns(selectedServer);

                subject.GetWriteChannelSource(operationContext);

                _mockCluster.Verify(c => c.SelectServer(It.IsAny<OperationContext>(), It.IsAny<WritableServerSelector>()), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetWriteChannelSource_should_use_a_deprioritized_servers_server_selector_to_select_the_server_from_the_cluster_when_deprioritized_servers_present(
            [Values(false, true)]
            bool async)
        {
            var subject = new WritableServerBinding(_mockCluster.Object);
            var selectedServer = new Mock<IServer>().Object;

            var clusterId = new ClusterId();
            var endPoint = new DnsEndPoint("localhost", 27017);
            var server = new ServerDescription(new ServerId(clusterId, endPoint), endPoint);
            var initialClusterDescription = new ClusterDescription(
                clusterId,
                false,
                null,
                ClusterType.Unknown,
                new[] { server });
            var finalClusterDescription = initialClusterDescription.WithType(ClusterType.Sharded);
            _mockCluster.SetupSequence(c => c.Description).Returns(initialClusterDescription).Returns(finalClusterDescription);

            var deprioritizedServers = new ServerDescription[] { server };
            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            if (async)
            {
                _mockCluster.Setup(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<DeprioritizedServersServerSelector>())).Returns(Task.FromResult(selectedServer));

                await subject.GetWriteChannelSourceAsync(operationContext, deprioritizedServers);

                _mockCluster.Verify(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<DeprioritizedServersServerSelector>()), Times.Once);
            }
            else
            {
                _mockCluster.Setup(c => c.SelectServer(It.IsAny<OperationContext>(), It.IsAny<DeprioritizedServersServerSelector>())).Returns(selectedServer);

                subject.GetWriteChannelSource(operationContext, deprioritizedServers);

                _mockCluster.Verify(c => c.SelectServer(It.IsAny<OperationContext>(), It.IsAny<DeprioritizedServersServerSelector>()), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetWriteChannelSource_with_mayUseSecondary_should_pass_mayUseSecondary_to_server_selector(
             [Values(false, true)]
            bool async)
        {
            var subject = new WritableServerBinding(_mockCluster.Object);
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

            var mockMayUseSecondary = new Mock<IMayUseSecondaryCriteria>();
            mockMayUseSecondary.SetupGet(x => x.ReadPreference).Returns(ReadPreference.SecondaryPreferred);
            mockMayUseSecondary.Setup(x => x.CanUseSecondary(It.IsAny<ServerDescription>())).Returns(true);
            var mayUseSecondary = mockMayUseSecondary.Object;

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            if (async)
            {
                _mockCluster.Setup(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<WritableServerSelector>())).Returns(Task.FromResult(selectedServer));

                await subject.GetWriteChannelSourceAsync(operationContext, mayUseSecondary);

                _mockCluster.Verify(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.Is<WritableServerSelector>(s => s.MayUseSecondary == mayUseSecondary)), Times.Once);
            }
            else
            {
                _mockCluster.Setup(c => c.SelectServer(It.IsAny<OperationContext>(), It.IsAny<WritableServerSelector>())).Returns(selectedServer);

                subject.GetWriteChannelSource(operationContext, mayUseSecondary);

                _mockCluster.Verify(c => c.SelectServer(It.IsAny<OperationContext>(), It.Is<WritableServerSelector>(s => s.MayUseSecondary == mayUseSecondary)), Times.Once);
            }
        }

        [Fact]
        public void Dispose_should_call_dispose_on_owned_resources()
        {
            var subject = new WritableServerBinding(_mockCluster.Object);

            subject.Dispose();

            _mockCluster.Verify(c => c.Dispose(), Times.Never);
        }
    }

    internal static class WritableServerBindingReflector
    {
        public static IClusterInternal _cluster(this WritableServerBinding obj)
        {
            var fieldInfo = typeof(WritableServerBinding).GetField("_cluster", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IClusterInternal)fieldInfo.GetValue(obj);
        }
    }
}
