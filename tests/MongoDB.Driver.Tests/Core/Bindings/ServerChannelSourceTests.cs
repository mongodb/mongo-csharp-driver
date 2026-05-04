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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using Moq;
using Xunit;
using MongoDB.TestHelpers.XunitExtensions;

namespace MongoDB.Driver.Core.Bindings
{
    public class ServerChannelSourceTests
    {
        [Fact]
        public void Constructor_should_throw_when_server_is_null()
        {
            var exception = Record.Exception(() => new ServerChannelSource(null));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("server");
        }

        [Fact]
        public void ServerDescription_should_return_description_of_server()
        {
            var desc = ServerDescriptionHelper.Disconnected(new ClusterId());
            var serverMock = new Mock<IServer>();
            serverMock.SetupGet(s => s.Description).Returns(desc);

            var subject = new ServerChannelSource(serverMock.Object);
            var result = subject.ServerDescription;

            result.Should().BeSameAs(desc);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetChannel_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ServerChannelSource(Mock.Of<IServer>());
            subject.Dispose();

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            var exception = async ?
                await Record.ExceptionAsync(() => subject.GetChannelAsync(operationContext)) :
                Record.Exception(() => subject.GetChannel(operationContext));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetChannel_should_get_connection_from_server(
            [Values(false, true)]
            bool async)
        {
            var serverMock = new Mock<IServer>();
            var subject = new ServerChannelSource(serverMock.Object);

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            if (async)
            {
                await subject.GetChannelAsync(operationContext);

                serverMock.Verify(s => s.GetChannelAsync(It.IsAny<OperationContext>()), Times.Once);
            }
            else
            {
                subject.GetChannel(operationContext);

                serverMock.Verify(s => s.GetChannel(It.IsAny<OperationContext>()), Times.Once);
            }
        }

        [Fact]
        public void Dispose_should_dispose_session()
        {
            var subject = new ServerChannelSource(Mock.Of<IServer>());

            subject.Dispose();
        }
    }
}
