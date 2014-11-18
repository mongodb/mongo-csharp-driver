/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Bindings
{
    [TestFixture]
    public class ChannelSourceReadWriteBindingTests
    {
        private IChannelSourceHandle _channelSource;

        [SetUp]
        public void Setup()
        {
            _channelSource = Substitute.For<IChannelSourceHandle>();
        }

        [Test]
        public void Constructor_should_throw_if_channelSource_is_null()
        {
            Action act = () => new ChannelSourceReadWriteBinding(null, ReadPreference.Primary);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_if_readPreference_is_null()
        {
            Action act = () => new ChannelSourceReadWriteBinding(_channelSource, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_not_fork_channelSource()
        {
            new ChannelSourceReadWriteBinding(_channelSource, ReadPreference.Primary);

            _channelSource.DidNotReceive().Fork();
        }

        [Test]
        public void GetReadChannelSourceAsync_should_throw_if_disposed()
        {
            var subject = new ChannelSourceReadWriteBinding(_channelSource, ReadPreference.Primary);
            subject.Dispose();

            Action act = () => subject.GetReadChannelSourceAsync(CancellationToken.None);

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetReadChannelSourceAsync_should_fork_the_channelSource()
        {
            var subject = new ChannelSourceReadWriteBinding(_channelSource, ReadPreference.Primary);

            subject.GetReadChannelSourceAsync(CancellationToken.None);

            _channelSource.Received().Fork();
        }

        [Test]
        public void GetWriteChannelSourceAsync_should_throw_if_disposed()
        {
            var subject = new ChannelSourceReadWriteBinding(_channelSource, ReadPreference.Primary);
            subject.Dispose();

            Action act = () => subject.GetWriteChannelSourceAsync(CancellationToken.None);

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetWriteChannelSourceAsync_should_fork_the_channelSource()
        {
            var subject = new ChannelSourceReadWriteBinding(_channelSource, ReadPreference.Primary);

            subject.GetWriteChannelSourceAsync(CancellationToken.None);

            _channelSource.Received().Fork();
        }

        [Test]
        public void Dispose_should_call_dispose_on_connection_source()
        {
            var subject = new ChannelSourceReadWriteBinding(_channelSource, ReadPreference.Primary);

            subject.Dispose();

            _channelSource.Received().Dispose();
        }
    }
}