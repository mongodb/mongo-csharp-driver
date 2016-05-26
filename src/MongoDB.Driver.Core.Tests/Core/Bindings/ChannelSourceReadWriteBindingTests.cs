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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using NSubstitute;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class ChannelSourceReadWriteBindingTests
    {
        private IChannelSourceHandle _channelSource;

        public ChannelSourceReadWriteBindingTests()
        {
            _channelSource = Substitute.For<IChannelSourceHandle>();
        }

        [Fact]
        public void Constructor_should_throw_if_channelSource_is_null()
        {
            Action act = () => new ChannelSourceReadWriteBinding(null, ReadPreference.Primary);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_if_readPreference_is_null()
        {
            Action act = () => new ChannelSourceReadWriteBinding(_channelSource, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_not_fork_channelSource()
        {
            new ChannelSourceReadWriteBinding(_channelSource, ReadPreference.Primary);

            _channelSource.DidNotReceive().Fork();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetReadChannelSourceAsync_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ChannelSourceReadWriteBinding(_channelSource, ReadPreference.Primary);
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
            var subject = new ChannelSourceReadWriteBinding(_channelSource, ReadPreference.Primary);

            if (async)
            {
                subject.GetReadChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.GetReadChannelSource(CancellationToken.None);
            }

            _channelSource.Received().Fork();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetWriteChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ChannelSourceReadWriteBinding(_channelSource, ReadPreference.Primary);
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
            var subject = new ChannelSourceReadWriteBinding(_channelSource, ReadPreference.Primary);

            if (async)
            {
                subject.GetWriteChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.GetWriteChannelSource(CancellationToken.None);
            }

            _channelSource.Received().Fork();
        }

        [Fact]
        public void Dispose_should_call_dispose_on_connection_source()
        {
            var subject = new ChannelSourceReadWriteBinding(_channelSource, ReadPreference.Primary);

            subject.Dispose();

            _channelSource.Received().Dispose();
        }
    }
}