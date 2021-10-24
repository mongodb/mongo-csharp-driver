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
using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class WritableServerBindingTests
    {
        private Mock<ICluster> _mockCluster;

        public WritableServerBindingTests()
        {
            _mockCluster = new Mock<ICluster>();
        }

        [Fact]
        public void Constructor_should_throw_if_cluster_is_null()
        {
            Action act = () => new WritableServerBinding(null, NoCoreSession.NewHandle());

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_if_session_is_null()
        {
            var cluster = new Mock<ICluster>().Object;

            Action act = () => new WritableServerBinding(cluster, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ReadPreference_should_be_primary()
        {
            var subject = new WritableServerBinding(_mockCluster.Object, NoCoreSession.NewHandle());

            subject.ReadPreference.Should().Be(ReadPreference.Primary);
        }

        [Fact]
        public void Session_should_return_expected_result()
        {
            var cluster = new Mock<ICluster>().Object;
            var session = new Mock<ICoreSessionHandle>().Object;
            var subject = new WritableServerBinding(cluster, session);

            var result = subject.Session;

            result.Should().BeSameAs(session);
        }

        [Theory]
        [ParameterAttributeData]
        public void GetReadChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new WritableServerBinding(_mockCluster.Object, NoCoreSession.NewHandle());
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
        public void GetReadChannelSource_should_use_a_writable_server_selector_to_select_the_server_from_the_cluster(
            [Values(false, true)]
            bool async)
        {
            var subject = new WritableServerBinding(_mockCluster.Object, NoCoreSession.NewHandle());
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
                _mockCluster.Setup(c => c.SelectServerAsync(It.IsAny<WritableServerSelector>(), CancellationToken.None)).Returns(Task.FromResult(selectedServer));

                subject.GetReadChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();

                _mockCluster.Verify(c => c.SelectServerAsync(It.IsAny<WritableServerSelector>(), CancellationToken.None), Times.Once);
            }
            else
            {
                _mockCluster.Setup(c => c.SelectServer(It.IsAny<WritableServerSelector>(), CancellationToken.None)).Returns(selectedServer);

                subject.GetReadChannelSource(CancellationToken.None);

                _mockCluster.Verify(c => c.SelectServer(It.IsAny<WritableServerSelector>(), CancellationToken.None), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void GetWriteChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new WritableServerBinding(_mockCluster.Object, NoCoreSession.NewHandle());
            subject.Dispose();

            Action act;
            if (async)
            {
                act = () => subject.GetWriteChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.GetWriteChannelSource(CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetWriteChannelSourceAsync_should_use_a_writable_server_selector_to_select_the_server_from_the_cluster(
            [Values(false, true)]
            bool async)
        {
            var subject = new WritableServerBinding(_mockCluster.Object, NoCoreSession.NewHandle());
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
                _mockCluster.Setup(c => c.SelectServerAsync(It.IsAny<WritableServerSelector>(), CancellationToken.None)).Returns(Task.FromResult(selectedServer));

                subject.GetWriteChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();

                _mockCluster.Verify(c => c.SelectServerAsync(It.IsAny<WritableServerSelector>(), CancellationToken.None), Times.Once);
            }
            else
            {
                _mockCluster.Setup(c => c.SelectServer(It.IsAny<WritableServerSelector>(), CancellationToken.None)).Returns(selectedServer);

                subject.GetWriteChannelSource(CancellationToken.None);

                _mockCluster.Verify(c => c.SelectServer(It.IsAny<WritableServerSelector>(), CancellationToken.None), Times.Once);
            }
        }

        [Fact]
        public void Dispose_should_call_dispose_on_owned_resources()
        {
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = new WritableServerBinding(_mockCluster.Object, mockSession.Object);

            subject.Dispose();

            _mockCluster.Verify(c => c.Dispose(), Times.Never);
            mockSession.Verify(m => m.Dispose(), Times.Once);
        }
    }

    public static class WritableServerBindingReflector
    {
        public static ICluster _cluster(this WritableServerBinding obj)
        {
            var fieldInfo = typeof(WritableServerBinding).GetField("_cluster", BindingFlags.NonPublic | BindingFlags.Instance);
            return (ICluster)fieldInfo.GetValue(obj);
        }
    }
}
