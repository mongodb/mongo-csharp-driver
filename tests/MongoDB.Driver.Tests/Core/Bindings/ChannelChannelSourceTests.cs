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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class ChannelChannelSourceTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var server = new Mock<IServer>().Object;
            var roundTripTime = TimeSpan.FromSeconds(42);
            var channel = new Mock<IChannelHandle>().Object;
            var session = new Mock<ICoreSessionHandle>().Object;

            var result = new ChannelChannelSource(server, roundTripTime, channel, session);

            result._channel().Should().BeSameAs(channel);
            result._disposed().Should().BeFalse();
            result.Server.Should().BeSameAs(server);
            result.RoundTripTime.Should().Be(roundTripTime);
            result.Session.Should().BeSameAs(session);
        }

        [Fact]
        public void constructor_should_throw_when_server_is_null()
        {
            var channel = new Mock<IChannelHandle>().Object;
            var session = new Mock<ICoreSessionHandle>().Object;

            var exception = Record.Exception(() => new ChannelChannelSource(null, TimeSpan.FromSeconds(42), channel, session));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("server");
        }

        [Theory]
        [MemberData(nameof(InvalidRoundTripCases))]
        public void constructor_should_throw_when_round_trip_time_is_invalid(TimeSpan roundTripTime)
        {
            var server = new Mock<IServer>().Object;
            var channel = new Mock<IChannelHandle>().Object;
            var session = new Mock<ICoreSessionHandle>().Object;

            var exception = Record.Exception(() => new ChannelChannelSource(server, roundTripTime, channel, session));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("roundTripTime");
        }

        public static IEnumerable<object[]> InvalidRoundTripCases =
        [
            [TimeSpan.Zero],
            [TimeSpan.FromMilliseconds(-5)]
        ];

        [Fact]
        public void constructor_should_throw_when_channel_is_null()
        {
            var server = new Mock<IServer>().Object;
            var session = new Mock<ICoreSessionHandle>().Object;

            var exception = Record.Exception(() => new ChannelChannelSource(server, TimeSpan.FromSeconds(42), null, session));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("channel");
        }

        [Fact]
        public void constructor_should_throw_when_session_is_null()
        {
            var server = new Mock<IServer>().Object;
            var channel = new Mock<IChannelHandle>().Object;

            var exception = Record.Exception(() => new ChannelChannelSource(server, TimeSpan.FromSeconds(42), channel, null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("session");
        }

        [Fact]
        public void Server_should_return_expected_result()
        {
            var server = new Mock<IServer>().Object;
            var subject = CreateSubject(server: server);

            var result = subject.Server;

            result.Should().BeSameAs(server);
        }

        [Fact]
        public void ServerDescription_should_call_server()
        {
            var mockServer = new Mock<IServer>();
            var subject = CreateSubject(server: mockServer.Object);

            var result = subject.ServerDescription;

            mockServer.VerifyGet(m => m.Description, Times.Once);
        }

        [Fact]
        public void Session_should_return_expected_result()
        {
            var session = new Mock<ICoreSessionHandle>().Object;
            var subject = CreateSubject(session: session);

            var result = subject.Session;

            result.Should().BeSameAs(session);
        }

        [Fact]
        public void Dispose_should_have_expected_result()
        {
            var mockChannel = new Mock<IChannelHandle>();
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = CreateSubject(channel: mockChannel.Object, session: mockSession.Object);

            subject.Dispose();

            subject._disposed().Should().BeTrue();
            mockChannel.Verify(m => m.Dispose(), Times.Once);
            mockSession.Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            var mockChannel = new Mock<IChannelHandle>();
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = CreateSubject(channel: mockChannel.Object, session: mockSession.Object);

            subject.Dispose();
            subject.Dispose();

            mockChannel.Verify(m => m.Dispose(), Times.Once);
            mockSession.Verify(m => m.Dispose(), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetChannel_should_return_expected_result(
            [Values(false, true)] bool async)
        {
            var mockChannel = new Mock<IChannelHandle>();
            var subject = CreateSubject(channel: mockChannel.Object);
            var expectedResult = new Mock<IChannelHandle>().Object;
            mockChannel.Setup(m => m.Fork()).Returns(expectedResult);

            var result = async ?
                await subject.GetChannelAsync(OperationContext.NoTimeout) :
                subject.GetChannel(OperationContext.NoTimeout);

            result.Should().BeSameAs(expectedResult);
            mockChannel.Verify(m => m.Fork(), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetChannel_should_throw_when_disposed(
            [Values(false, true)] bool async)
        {
            var subject = CreateDisposedSubject();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.GetChannelAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => subject.GetChannel(OperationContext.NoTimeout));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        // private methods
        private ChannelChannelSource CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private ChannelChannelSource CreateSubject(IServer server = null, IChannelHandle channel = null, ICoreSessionHandle session = null)
        {
            return new ChannelChannelSource(
                server ?? new Mock<IServer>().Object,
                TimeSpan.FromSeconds(42),
                channel ?? new Mock<IChannelHandle>().Object,
                session ?? new Mock<ICoreSessionHandle>().Object);
        }
    }

    internal static class ChannelChannelSourceReflector
    {
        public static IChannelHandle _channel(this ChannelChannelSource obj)
            => (IChannelHandle)Reflector.GetFieldValue(obj, "_channel");

        public static bool _disposed(this ChannelChannelSource obj)
            => (bool)Reflector.GetFieldValue(obj, "_disposed");
    }
}
