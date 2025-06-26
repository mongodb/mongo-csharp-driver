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
    public class SingleServerReadWriteBindingTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var server = new Mock<IServer>().Object;
            var session = new Mock<ICoreSessionHandle>().Object;

            var result = new SingleServerReadWriteBinding(server, TimeSpan.FromMilliseconds(42), session);

            result._disposed().Should().BeFalse();
            result._server().Should().BeSameAs(server);
            result.Session.Should().BeSameAs(session);
        }

        [Fact]
        public void constructor_should_throw_when_server_is_null()
        {
            var session = new Mock<ICoreSessionHandle>().Object;

            var exception = Record.Exception(() => new SingleServerReadWriteBinding(null, TimeSpan.FromMilliseconds(42), session));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("server");
        }

        [Theory]
        [MemberData(nameof(InvalidRoundTripCases))]
        public void constructor_should_throw_when_roundTripTime_is_invalid(TimeSpan roundTripTime)
        {
            var server = new Mock<IServer>().Object;
            var session = new Mock<ICoreSessionHandle>().Object;

            var exception = Record.Exception(() => new SingleServerReadWriteBinding(server, roundTripTime, session));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("roundTripTime");
        }

        public static IEnumerable<object[]> InvalidRoundTripCases =
        [
            [TimeSpan.Zero],
            [TimeSpan.FromMilliseconds(-5)]
        ];

        [Fact]
        public void constructor_should_throw_when_session_is_null()
        {
            var server = new Mock<IServer>().Object;

            var exception = Record.Exception(() => new SingleServerReadWriteBinding(server, TimeSpan.FromMilliseconds(42), null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("session");
        }

        [Fact]
        public void ReadPreference_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.ReadPreference;

            result.Should().Be(ReadPreference.Primary);
        }

        [Fact]
        public void Session_should_return_expected_result()
        {
            var session = new Mock<ICoreSessionHandle>().Object;
            var subject = CreateSubject(session: session);

            var result = subject.Session;

            result.Should().Be(session);
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
            var server = Mock.Of<IServer>();
            var roundTripTime = TimeSpan.FromMilliseconds(5);
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = CreateSubject(server, roundTripTime, mockSession.Object);
            var forkedSession = new Mock<ICoreSessionHandle>().Object;
            mockSession.Setup(m => m.Fork()).Returns(forkedSession);

            var result = async ?
                await subject.GetReadChannelSourceAsync(OperationContext.NoTimeout) :
                subject.GetReadChannelSource(OperationContext.NoTimeout);

            var newHandle = result.Should().BeOfType<ChannelSourceHandle>().Subject;
            var referenceCounted = newHandle._reference();
            var source = referenceCounted.Instance.Should().BeOfType<ServerChannelSource>().Subject;
            source.Server.Should().Be(server);
            source.RoundTripTime.Should().Be(roundTripTime);
            source.Session.Should().BeSameAs(forkedSession);
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

        [Theory]
        [ParameterAttributeData]
        public async Task GetWriteChannelSource_should_return_expected_result(
            [Values(false, true)] bool async)
        {
            var server = Mock.Of<IServer>();
            var roundTripTime = TimeSpan.FromMilliseconds(5);
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = CreateSubject(server, roundTripTime, mockSession.Object);
            var forkedSession = new Mock<ICoreSessionHandle>().Object;
            mockSession.Setup(m => m.Fork()).Returns(forkedSession);

            var result = async ?
                await subject.GetWriteChannelSourceAsync(OperationContext.NoTimeout) :
                subject.GetWriteChannelSource(OperationContext.NoTimeout);

            var newHandle = result.Should().BeOfType<ChannelSourceHandle>().Subject;
            var referenceCounted = newHandle._reference();
            var source = referenceCounted.Instance.Should().BeOfType<ServerChannelSource>().Subject;
            source.Server.Should().Be(server);
            source.RoundTripTime.Should().Be(roundTripTime);
            source.Session.Should().BeSameAs(forkedSession);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetWriteChannelSource_should_throw_when_disposed(
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
        private SingleServerReadWriteBinding CreateSubject(IServer server = null, TimeSpan? roundTripTime = null, ICoreSessionHandle session = null)
        {
            return new SingleServerReadWriteBinding(
                server ?? new Mock<IServer>().Object,
                roundTripTime ?? TimeSpan.FromMilliseconds(42),
                session ?? new Mock<ICoreSessionHandle>().Object);
        }
    }

    internal static class SingleServerReadWriteBindingReflector
    {
        public static bool _disposed(this SingleServerReadWriteBinding obj)
            => (bool)Reflector.GetFieldValue(obj, "_disposed");

        public static IServer _server(this SingleServerReadWriteBinding obj)
            => (IServer)Reflector.GetFieldValue(obj, "_server");
    }
}
