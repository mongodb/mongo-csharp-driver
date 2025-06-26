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
using System.Collections.Generic;
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
            var roundTripTime = TimeSpan.FromMilliseconds(42);
            var session = new Mock<ICoreSessionHandle>().Object;
            var exception = Record.Exception(() => new ServerChannelSource(null, roundTripTime, session));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("server");
        }

        [Theory]
        [MemberData(nameof(InvalidRoundTripCases))]
        public void Constructor_should_throw_when_roundTripTime_is_invalid(TimeSpan roundTripTime)
        {
            var server = Mock.Of<IServer>();
            var session = Mock.Of<ICoreSessionHandle>();

            var exception = Record.Exception(() => new ServerChannelSource(server, roundTripTime, session));

            exception.Should().BeOfType<ArgumentOutOfRangeException>()
                .Subject.ParamName.Should().Be("roundTripTime");
        }

        public static IEnumerable<object[]> InvalidRoundTripCases =
        [
            [TimeSpan.Zero],
            [TimeSpan.FromMilliseconds(-5)]
        ];

        [Fact]
        public void Constructor_should_throw_when_session_is_null()
        {
            var server = Mock.Of<IServer>();
            var roundTripTime = TimeSpan.FromMilliseconds(42);

            var exception = Record.Exception(() => new ServerChannelSource(server, roundTripTime, null));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("session");
        }

        [Fact]
        public void ServerDescription_should_return_description_of_server()
        {
            var desc = ServerDescriptionHelper.Disconnected(new ClusterId());

            var serverMock = new Mock<IServer>();
            serverMock.SetupGet(s => s.Description).Returns(desc);
            var roundTripTime = TimeSpan.FromMilliseconds(42);
            var session = new Mock<ICoreSessionHandle>().Object;

            var subject = new ServerChannelSource(serverMock.Object, roundTripTime, session);
            var result = subject.ServerDescription;

            result.Should().BeSameAs(desc);
        }

        [Fact]
        public void Session_should_return_expected_result()
        {
            var session = new Mock<ICoreSessionHandle>().Object;
            var subject = new ServerChannelSource(Mock.Of<IServer>(), TimeSpan.FromMilliseconds(42), session);

            var result = subject.Session;

            result.Should().BeSameAs(session);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetChannel_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var session = new Mock<ICoreSessionHandle>().Object;
            var subject = new ServerChannelSource(Mock.Of<IServer>(), TimeSpan.FromMilliseconds(42), session);
            subject.Dispose();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.GetChannelAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => subject.GetChannel(OperationContext.NoTimeout));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetChannel_should_get_connection_from_server(
            [Values(false, true)]
            bool async)
        {
            var serverMock = new Mock<IServer>();
            var session = new Mock<ICoreSessionHandle>().Object;
            var subject = new ServerChannelSource(serverMock.Object, TimeSpan.FromMilliseconds(42), session);

            if (async)
            {
                await subject.GetChannelAsync(OperationContext.NoTimeout);

                serverMock.Verify(s => s.GetConnectionAsync(It.IsAny<OperationContext>()), Times.Once);
            }
            else
            {
                subject.GetChannel(OperationContext.NoTimeout);

                serverMock.Verify(s => s.GetConnection(It.IsAny<OperationContext>()), Times.Once);
            }
        }

        [Fact]
        public void Dispose_should_dispose_session()
        {
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = new ServerChannelSource(Mock.Of<IServer>(), TimeSpan.FromMilliseconds(42), mockSession.Object);

            subject.Dispose();

            mockSession.Verify(m => m.Dispose(), Times.Once);
        }
    }
}
