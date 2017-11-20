/* Copyright 2017 MongoDB Inc.
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
using System.Reflection;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
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
            var readPreference = ReadPreference.Primary;
            var session = new Mock<ICoreSessionHandle>().Object;

            var result = new SingleServerReadBinding(server, readPreference, session);

            result._disposed().Should().BeFalse();
            result.ReadPreference.Should().BeSameAs(readPreference);
            result._server().Should().BeSameAs(server);
            result.Session.Should().BeSameAs(session);
        }

        [Fact]
        public void constructor_should_throw_when_server_is_null()
        {
            var readPreference = ReadPreference.Primary;
            var session = new Mock<ICoreSessionHandle>().Object;

            var exception = Record.Exception(() => new SingleServerReadBinding(null, readPreference, session));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("server");
        }

        [Fact]
        public void constructor_should_throw_when_readPreference_is_null()
        {
            var server = new Mock<IServer>().Object;
            var session = new Mock<ICoreSessionHandle>().Object;

            var exception = Record.Exception(() => new SingleServerReadBinding(server, null, session));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("readPreference");
        }

        [Fact]
        public void constructor_should_throw_when_session_is_null()
        {
            var server = new Mock<IServer>().Object;
            var readPreference = ReadPreference.Primary;

            var exception = Record.Exception(() => new SingleServerReadBinding(server, readPreference, null));

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
        public void GetReadChannelSource_should_return_expected_result(
            [Values(false, true)] bool async)
        {
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = CreateSubject(session: mockSession.Object);
            var cancellationToken = new CancellationTokenSource().Token;

            var forkedSession = new Mock<ICoreSessionHandle>().Object;
            mockSession.Setup(m => m.Fork()).Returns(forkedSession);

            IChannelSourceHandle result;
            if (async)
            {
                result = subject.GetReadChannelSourceAsync(cancellationToken).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.GetReadChannelSource(cancellationToken);
            }

            var newHandle = result.Should().BeOfType<ChannelSourceHandle>().Subject;
            var referenceCounted = newHandle._reference();
            var source = referenceCounted.Instance.Should().BeOfType<ServerChannelSource>().Subject;
            source.Session.Should().BeSameAs(forkedSession);
        }

        [Theory]
        [ParameterAttributeData]
        public void GetReadChannelSource_should_throw_when_disposed(
            [Values(false, true)] bool async)
        {
            var subject = CreateDisposedSubject();
            var cancellationToken = new CancellationTokenSource().Token;

            var exception = Record.Exception(() =>
            {
                if (async)
                {
                    subject.GetReadChannelSourceAsync(cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.GetReadChannelSource(cancellationToken);
                }
            });

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        // private methods
        private SingleServerReadBinding CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private SingleServerReadBinding CreateSubject(IServer server = null, ReadPreference readPreference = null, ICoreSessionHandle session = null)
        {
            return new SingleServerReadBinding(
                server ?? new Mock<IServer>().Object,
                readPreference ?? ReadPreference.Primary,
                session ?? new Mock<ICoreSessionHandle>().Object);
        }
    }

    public static class SingleServerReadBindingReflector
    {
        public static bool _disposed(this SingleServerReadBinding obj)
        {
            var fieldInfo = typeof(SingleServerReadBinding).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)fieldInfo.GetValue(obj);
        }

        public static IServer _server(this SingleServerReadBinding obj)
        {
            var fieldInfo = typeof(SingleServerReadBinding).GetField("_server", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IServer)fieldInfo.GetValue(obj);
        }
    }
}
