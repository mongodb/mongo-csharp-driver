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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using Moq;
using Xunit;
using MongoDB.Bson.TestHelpers.XunitExtensions;

namespace MongoDB.Driver.Core.Bindings
{
    public class ServerChannelSourceTests
    {
        private Mock<IServer> _mockServer;

        public ServerChannelSourceTests()
        {
            _mockServer = new Mock<IServer>();
        }

        [Fact]
        public void Constructor_should_throw_when_server_is_null()
        {
            Action act = () => new ServerChannelSource(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ServerDescription_should_return_description_of_server()
        {
            var subject = new ServerChannelSource(_mockServer.Object);

            var desc = ServerDescriptionHelper.Disconnected(new ClusterId());

            _mockServer.SetupGet(s => s.Description).Returns(desc);

            var result = subject.ServerDescription;

            result.Should().BeSameAs(desc);
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChannel_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ServerChannelSource(_mockServer.Object);
            subject.Dispose();

            Action act;
            if (async)
            {
                act = () => subject.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.GetChannel(CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChannel_should_get_connection_from_server(
            [Values(false, true)]
            bool async)
        {
            var subject = new ServerChannelSource(_mockServer.Object);

            if (async)
            {
                subject.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult();

                _mockServer.Verify(s => s.GetChannelAsync(CancellationToken.None), Times.Once);
            }
            else
            {
                subject.GetChannel(CancellationToken.None);

                _mockServer.Verify(s => s.GetChannel(CancellationToken.None), Times.Once);
            }
        }
    }
}
