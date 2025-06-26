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
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class SingleServerReadBindingTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var server = new Mock<IServer>().Object;
            var roundTripTime = TimeSpan.FromMilliseconds(42);
            var readPreference = ReadPreference.Primary;
            var session = new Mock<ICoreSessionHandle>().Object;

            var result = new SingleServerReadBinding(server, roundTripTime, readPreference, session);

            result._disposed().Should().BeFalse();
            result.ReadPreference.Should().BeSameAs(readPreference);
            result._server().Should().BeSameAs(server);
            result.Session.Should().BeSameAs(session);
        }

        [Fact]
        public void constructor_should_throw_when_server_is_null()
        {
            var roundTripTime = TimeSpan.FromMilliseconds(42);
            var readPreference = ReadPreference.Primary;
            var session = new Mock<ICoreSessionHandle>().Object;

            var exception = Record.Exception(() => new SingleServerReadBinding(null, roundTripTime, readPreference, session));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("server");
        }

        [Theory]
        [MemberData(nameof(InvalidRoundTripCases))]
        public void constructor_should_throw_when_roundTripTime_is_invalid(TimeSpan roundTripTime)
        {
            var server = new Mock<IServer>().Object;
            var readPreference = ReadPreference.Primary;
            var session = new Mock<ICoreSessionHandle>().Object;

            var exception = Record.Exception(() => new SingleServerReadBinding(server, roundTripTime, readPreference, session));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("roundTripTime");
        }

        public static IEnumerable<object[]> InvalidRoundTripCases =
        [
            [TimeSpan.Zero],
            [TimeSpan.FromMilliseconds(-5)]
        ];

        [Fact]
        public void constructor_should_throw_when_readPreference_is_null()
        {
            var server = new Mock<IServer>().Object;
            var roundTripTime = TimeSpan.FromMilliseconds(42);
            var session = new Mock<ICoreSessionHandle>().Object;

            var exception = Record.Exception(() => new SingleServerReadBinding(server, roundTripTime, null, session));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("readPreference");
        }

        [Fact]
        public void constructor_should_throw_when_session_is_null()
        {
            var server = new Mock<IServer>().Object;
            var roundTripTime = TimeSpan.FromMilliseconds(42);
            var readPreference = ReadPreference.Primary;

            var exception = Record.Exception(() => new SingleServerReadBinding(server, roundTripTime, readPreference, null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("session");
        }

        [Fact]
        public void ReadPreference_should_return_expected_result()
        {
            var readPreference = ReadPreference.Secondary;
            var subject = CreateSubject(readPreference: readPreference);

            var result = subject.ReadPreference;

            result.Should().BeSameAs(readPreference);
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
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = CreateSubject(session: mockSession.Object);

            subject.Dispose();

            subject._disposed().Should().BeTrue();
            mockSession.Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = CreateSubject(session: mockSession.Object);

            subject.Dispose();
            subject.Dispose();

            mockSession.Verify(m => m.Dispose(), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_return_expected_result(
            [Values(false, true)] bool async)
        {
            var server = CreateMockServer();
            var roundTripTime = TimeSpan.FromMilliseconds(42);
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = CreateSubject(server.Object, roundTripTime, session: mockSession.Object);
            var forkedSession = new Mock<ICoreSessionHandle>().Object;
            mockSession.Setup(m => m.Fork()).Returns(forkedSession);

            var result = async ?
                await subject.GetReadChannelSourceAsync(OperationContext.NoTimeout) :
                subject.GetReadChannelSource(OperationContext.NoTimeout);

            var newHandle = result.Should().BeOfType<ChannelSourceHandle>().Subject;
            var referenceCounted = newHandle._reference();
            var source = referenceCounted.Instance.Should().BeOfType<ServerChannelSource>().Subject;
            source.Session.Should().BeSameAs(forkedSession);
            source.Server.Should().Be(server.Object);
            source.RoundTripTime.Should().Be(roundTripTime);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_throw_when_disposed(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            subject.Dispose();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.GetReadChannelSourceAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => subject.GetReadChannelSource(OperationContext.NoTimeout));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        // private methods
        private SingleServerReadBinding CreateSubject(IServer server = null, TimeSpan? roundTripTime = null, ReadPreference readPreference = null, ICoreSessionHandle session = null)
        {
            return new SingleServerReadBinding(
                server ?? CreateMockServer().Object,
                roundTripTime ?? TimeSpan.FromMilliseconds(5),
                readPreference ?? ReadPreference.Primary,
                session ?? new Mock<ICoreSessionHandle>().Object);
        }

        private Mock<IServer> CreateMockServer()
        {
            var mockServer = new Mock<IServer>();
            mockServer.Setup(s => s.Description).Returns(new ServerDescription(
                new ServerId(new Clusters.ClusterId(), new DnsEndPoint("localhost", 20017)),
                new DnsEndPoint("localhost", 20017),
                state: ServerState.Connected));

            return mockServer;
        }
    }

    internal static class SingleServerReadBindingReflector
    {
        public static bool _disposed(this SingleServerReadBinding obj)
            => (bool)Reflector.GetFieldValue(obj, "_disposed");

        public static IServer _server(this SingleServerReadBinding obj)
            => (IServer)Reflector.GetFieldValue(obj, "_server");
    }
}
