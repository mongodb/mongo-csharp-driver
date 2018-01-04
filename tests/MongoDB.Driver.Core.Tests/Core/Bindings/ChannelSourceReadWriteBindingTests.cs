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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class ChannelSourceReadWriteBindingTests
    {
        private Mock<IChannelSourceHandle> _mockChannelSource;

        public ChannelSourceReadWriteBindingTests()
        {
            _mockChannelSource = new Mock<IChannelSourceHandle>();
        }

        [Fact]
        public void Constructor_should_throw_if_channelSource_is_null()
        {
            Action act = () => new ChannelSourceReadWriteBinding(null, ReadPreference.Primary, NoCoreSession.NewHandle());

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_if_readPreference_is_null()
        {
            Action act = () => new ChannelSourceReadWriteBinding(_mockChannelSource.Object, null, NoCoreSession.NewHandle());

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_if_session_is_null()
        {
            Action act = () => new ChannelSourceReadWriteBinding(_mockChannelSource.Object, ReadPreference.Primary, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_not_fork_channelSource()
        {
            new ChannelSourceReadWriteBinding(_mockChannelSource.Object, ReadPreference.Primary, NoCoreSession.NewHandle());

            _mockChannelSource.Verify(s => s.Fork(), Times.Never);
        }

        [Fact]
        public void Session_should_return_expected_result()
        {
            var session = new Mock<ICoreSessionHandle>().Object;
            var subject = new ChannelSourceReadWriteBinding(_mockChannelSource.Object, ReadPreference.Primary, session);

            var result = subject.Session;

            result.Should().BeSameAs(session);
        }

        [Theory]
        [ParameterAttributeData]
        public void GetReadChannelSourceAsync_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ChannelSourceReadWriteBinding(_mockChannelSource.Object, ReadPreference.Primary, NoCoreSession.NewHandle());
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
        public void GetReadChannelSource_should_fork_the_channelSource(
            [Values(false, true)]
            bool async)
        {
            var subject = new ChannelSourceReadWriteBinding(_mockChannelSource.Object, ReadPreference.Primary, NoCoreSession.NewHandle());

            if (async)
            {
                subject.GetReadChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.GetReadChannelSource(CancellationToken.None);
            }

            _mockChannelSource.Verify(f => f.Fork(), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void GetWriteChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ChannelSourceReadWriteBinding(_mockChannelSource.Object, ReadPreference.Primary, NoCoreSession.NewHandle());
            subject.Dispose();

            Action act;
            if (async)
            {
                act = () => subject.GetWriteChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.GetWriteChannelSource(CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetWriteChannelSource_should_fork_the_channelSource(
            [Values(false, true)]
            bool async)
        {
            var subject = new ChannelSourceReadWriteBinding(_mockChannelSource.Object, ReadPreference.Primary, NoCoreSession.NewHandle());

            if (async)
            {
                subject.GetWriteChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.GetWriteChannelSource(CancellationToken.None);
            }

            _mockChannelSource.Verify(f => f.Fork(), Times.Once);
        }

        [Fact]
        public void Dispose_should_call_dispose_on_connection_source()
        {
            var subject = new ChannelSourceReadWriteBinding(_mockChannelSource.Object, ReadPreference.Primary, NoCoreSession.NewHandle());

            subject.Dispose();

            _mockChannelSource.Verify(f => f.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_should_call_dispose_on_session()
        {
            var mockSession = new Mock<ICoreSessionHandle>();
            var subject = new ChannelSourceReadWriteBinding(_mockChannelSource.Object, ReadPreference.Primary, mockSession.Object);

            subject.Dispose();

            mockSession.Verify(f => f.Dispose(), Times.Once);
        }
    }
}