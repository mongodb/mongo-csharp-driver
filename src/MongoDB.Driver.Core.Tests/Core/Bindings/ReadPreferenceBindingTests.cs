/* Copyright 2013-2016 MongoDB Inc.
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
        public void GetReadChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary);
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
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary);
            var selectedServer = new Mock<IServer>().Object;

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

        [Fact]
        public void Dispose_should_not_call_dispose_on_the_cluster()
        {
            var subject = new ReadPreferenceBinding(_mockCluster.Object, ReadPreference.Primary);

            subject.Dispose();

            _mockCluster.Verify(c => c.Dispose(), Times.Never);
        }
    }
}