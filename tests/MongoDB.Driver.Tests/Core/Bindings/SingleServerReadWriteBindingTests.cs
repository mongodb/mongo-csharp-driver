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
    public class SingleServerReadWriteBindingTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var server = new Mock<IServer>().Object;

            var result = new SingleServerReadWriteBinding(server);

            result._disposed().Should().BeFalse();
            result._server().Should().BeSameAs(server);
        }

        [Fact]
        public void constructor_should_throw_when_server_is_null()
        {
            var exception = Record.Exception(() => new SingleServerReadWriteBinding(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("server");
        }

        [Fact]
        public void ReadPreference_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.ReadPreference;

            result.Should().Be(ReadPreference.Primary);
        }

        [Fact]
        public void Dispose_should_have_expected_result()
        {
            var subject = CreateSubject();

            subject.Dispose();

            subject._disposed().Should().BeTrue();
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            var subject = CreateSubject();

            subject.Dispose();
            subject.Dispose();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetReadChannelSource_should_return_expected_result(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            var result = async ?
                await subject.GetReadChannelSourceAsync(operationContext) :
                subject.GetReadChannelSource(operationContext);

            var newHandle = result.Should().BeOfType<ChannelSourceHandle>().Subject;
            var referenceCounted = newHandle._reference();
            referenceCounted.Instance.Should().BeOfType<ServerChannelSource>();
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

        [Theory]
        [ParameterAttributeData]
        public async Task GetWriteChannelSource_should_return_expected_result(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            using var operationContext = new OperationContext(NoCoreSession.NewHandle());
            var result = async ?
                await subject.GetWriteChannelSourceAsync(operationContext) :
                subject.GetWriteChannelSource(operationContext);

            var newHandle = result.Should().BeOfType<ChannelSourceHandle>().Subject;
            var referenceCounted = newHandle._reference();
            referenceCounted.Instance.Should().BeOfType<ServerChannelSource>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetWriteChannelSource_should_throw_when_disposed(
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
        private SingleServerReadWriteBinding CreateDisposedSubject()
        {
            var subject = CreateSubject();
            subject.Dispose();
            return subject;
        }

        private SingleServerReadWriteBinding CreateSubject(IServer server = null) =>
            new(server ?? new Mock<IServer>().Object);
    }

    internal static class SingleServerReadWriteBindingReflector
    {
        public static bool _disposed(this SingleServerReadWriteBinding obj)
        {
            var fieldInfo = typeof(SingleServerReadWriteBinding).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)fieldInfo.GetValue(obj);
        }

        public static IServer _server(this SingleServerReadWriteBinding obj)
        {
            var fieldInfo = typeof(SingleServerReadWriteBinding).GetField("_server", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IServer)fieldInfo.GetValue(obj);
        }
    }
}
