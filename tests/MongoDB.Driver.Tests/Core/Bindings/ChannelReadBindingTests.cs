/* Copyright 2010-present MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class ChannelReadBindingTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var server = new Mock<IServer>().Object;
            var channel = new Mock<IChannelHandle>().Object;
            var readPreference = ReadPreference.Primary;

            var result = new ChannelReadBinding(server, channel, readPreference);

            result._channel().Should().BeSameAs(channel);
            result._disposed().Should().BeFalse();
            result.ReadPreference.Should().BeSameAs(readPreference);
            result._server().Should().BeSameAs(server);
        }

        [Fact]
        public void constructor_should_throw_when_server_is_null()
        {
            var channel = new Mock<IChannelHandle>().Object;
            var readPreference = ReadPreference.Primary;

            var exception = Record.Exception(() => new ChannelReadBinding(null, channel, readPreference));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("server");
        }

        [Fact]
        public void constructor_should_throw_when_channel_is_null()
        {
            var server = new Mock<IServer>().Object;
            var readPreference = ReadPreference.Primary;

            var exception = Record.Exception(() => new ChannelReadBinding(server, null, readPreference));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("channel");
        }

        [Fact]
        public void constructor_should_throw_when_readPreference_is_null()
        {
            var server = new Mock<IServer>().Object;
            var channel = new Mock<IChannelHandle>().Object;

            var exception = Record.Exception(() => new ChannelReadBinding(server, channel, null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("readPreference");
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
        public void Dispose_should_have_expected_result()
        {
            var mockChannel = new Mock<IChannelHandle>();
            var subject = CreateSubject(channel: mockChannel.Object);

            subject.Dispose();

            subject._disposed().Should().BeTrue();
            mockChannel.Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            var mockChannel = new Mock<IChannelHandle>();
            var subject = CreateSubject(channel: mockChannel.Object);

            subject.Dispose();
            subject.Dispose();

            mockChannel.Verify(m => m.Dispose(), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_return_expected_result(
            [Values(false, true)] bool async)
        {
            var mockChannel = new Mock<IChannelHandle>();
            var subject = CreateSubject(channel: mockChannel.Object);

            var forkedChannel = new Mock<IChannelHandle>().Object;
            mockChannel.Setup(m => m.Fork()).Returns(forkedChannel);

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            var result = async ?
                await subject.GetReadChannelSourceAsync(operationContext) :
                subject.GetReadChannelSource(operationContext);

            var newHandle = result.Should().BeOfType<ChannelSourceHandle>().Subject;
            var referenceCounted = newHandle._reference();
            var newSource = referenceCounted.Instance.Should().BeOfType<ChannelChannelSource>().Subject;
            newSource._channel().Should().Be(forkedChannel);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_throw_when_disposed(
            [Values(false, true)] bool async)
        {
            var subject = CreateDisposedSubject();
            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            var exception = async ?
                await Record.ExceptionAsync(() => subject.GetReadChannelSourceAsync(operationContext)) :
                Record.Exception(() => subject.GetReadChannelSource(operationContext));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        // private methods
        private ChannelReadBinding CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private ChannelReadBinding CreateSubject(IServer server = null, IChannelHandle channel = null, ReadPreference readPreference = null)
        {
            return new ChannelReadBinding(
                server ?? new Mock<IServer>().Object,
                channel ?? new Mock<IChannelHandle>().Object,
                readPreference ?? ReadPreference.Primary);
        }
    }

    internal static class ChannelReadBindingReflector
    {
        public static IChannelHandle _channel(this ChannelReadBinding obj)
        {
            var fieldInfo = typeof(ChannelReadBinding).GetField("_channel", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IChannelHandle)fieldInfo.GetValue(obj);
        }

        public static bool _disposed(this ChannelReadBinding obj)
        {
            var fieldInfo = typeof(ChannelReadBinding).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)fieldInfo.GetValue(obj);
        }

        public static IServer _server(this ChannelReadBinding obj)
        {
            var fieldInfo = typeof(ChannelReadBinding).GetField("_server", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IServer)fieldInfo.GetValue(obj);
        }
    }
}
