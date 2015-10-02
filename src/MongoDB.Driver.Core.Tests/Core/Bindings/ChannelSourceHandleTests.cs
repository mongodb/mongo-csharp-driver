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
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Bindings
{
    [TestFixture]
    public class ChannelSourceHandleTests
    {
        private IChannelSource _channelSource;

        [SetUp]
        public void Setup()
        {
            _channelSource = Substitute.For<IChannelSource>();
        }

        [Test]
        public void Constructor_should_throw_if_channelSource_is_null()
        {
            Action act = () => new ChannelSourceHandle(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void GetChannel_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ChannelSourceHandle(_channelSource);
            subject.Dispose();

            Action act;
            if (async)
            {
                act = () => subject.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.GetChannel(CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetChannel_should_delegate_to_reference(
            [Values(false, true)]
            bool async)
        {
            var subject = new ChannelSourceHandle(_channelSource);

            if (async)
            {
                subject.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult();

                _channelSource.Received().GetChannelAsync(CancellationToken.None);
            }
            else
            {
                subject.GetChannel(CancellationToken.None);

                _channelSource.Received().GetChannel(CancellationToken.None);
            }
        }

        [Test]
        public void Fork_should_throw_if_disposed()
        {
            var subject = new ChannelSourceHandle(_channelSource);
            subject.Dispose();

            Action act = () => subject.Fork();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void Disposing_of_handle_after_fork_should_not_dispose_of_channelSource()
        {
            var subject = new ChannelSourceHandle(_channelSource);

            var forked = subject.Fork();

            subject.Dispose();

            _channelSource.DidNotReceive().Dispose();

            forked.Dispose();

            _channelSource.Received().Dispose();
        }

        [Test]
        public void Disposing_of_fork_before_disposing_of_subject_hould_not_dispose_of_channelSource()
        {
            var subject = new ChannelSourceHandle(_channelSource);

            var forked = subject.Fork();

            forked.Dispose();

            _channelSource.DidNotReceive().Dispose();

            subject.Dispose();

            _channelSource.Received().Dispose();
        }

        [Test]
        public void Disposing_of_last_handle_should_dispose_of_connectioSource()
        {
            var subject = new ChannelSourceHandle(_channelSource);

            var forked = subject.Fork();

            subject.Dispose();
            forked.Dispose();

            _channelSource.Received().Dispose();
        }
    }
}