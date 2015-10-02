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
    public class ReadBindingHandleTests
    {
        private IReadBinding _readBinding;

        [SetUp]
        public void Setup()
        {
            _readBinding = Substitute.For<IReadBinding>();
        }

        [Test]
        public void Constructor_should_throw_if_readWriteBinding_is_null()
        {
            Action act = () => new ReadBindingHandle(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void GetReadChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadBindingHandle(_readBinding);
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

        [Test]
        public void GetReadChannelSource_should_delegate_to_reference(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadBindingHandle(_readBinding);

            if (async)
            {
                subject.GetReadChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();

                _readBinding.Received().GetReadChannelSourceAsync(CancellationToken.None);
            }
            else
            {
                subject.GetReadChannelSource(CancellationToken.None);

                _readBinding.Received().GetReadChannelSource(CancellationToken.None);
            }
        }

        [Test]
        public void Fork_should_throw_if_disposed()
        {
            var subject = new ReadBindingHandle(_readBinding);
            subject.Dispose();

            Action act = () => subject.Fork();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void Disposing_of_handle_after_fork_should_not_dispose_of_channelSource()
        {
            var subject = new ReadBindingHandle(_readBinding);

            var forked = subject.Fork();

            subject.Dispose();

            _readBinding.DidNotReceive().Dispose();

            forked.Dispose();

            _readBinding.Received().Dispose();
        }

        [Test]
        public void Disposing_of_fork_before_disposing_of_subject_hould_not_dispose_of_channelSource()
        {
            var subject = new ReadBindingHandle(_readBinding);

            var forked = subject.Fork();

            forked.Dispose();

            _readBinding.DidNotReceive().Dispose();

            subject.Dispose();

            _readBinding.Received().Dispose();
        }

        [Test]
        public void Disposing_of_last_handle_should_dispose_of_connectioSource()
        {
            var subject = new ReadBindingHandle(_readBinding);

            var forked = subject.Fork();

            subject.Dispose();
            forked.Dispose();

            _readBinding.Received().Dispose();
        }
    }
}