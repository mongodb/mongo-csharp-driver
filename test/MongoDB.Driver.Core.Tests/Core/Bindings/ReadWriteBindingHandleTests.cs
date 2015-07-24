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
    public class ReadWriteBindingHandleTests
    {
        private IReadWriteBinding _readWriteBinding;

        [SetUp]
        public void Setup()
        {
            _readWriteBinding = Substitute.For<IReadWriteBinding>();
        }

        [Test]
        public void Constructor_should_throw_if_readWriteBinding_is_null()
        {
            Action act = () => new ReadWriteBindingHandle(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void GetReadChannelSourceAsync_should_throw_if_disposed()
        {
            var subject = new ReadWriteBindingHandle(_readWriteBinding);
            subject.Dispose();

            Action act = () => subject.GetReadChannelSourceAsync(CancellationToken.None);

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetReadChannelSourceAsync_should_delegate_to_reference()
        {
            var subject = new ReadWriteBindingHandle(_readWriteBinding);

            subject.GetReadChannelSourceAsync(CancellationToken.None);

            _readWriteBinding.Received().GetReadChannelSourceAsync(CancellationToken.None);
        }

        [Test]
        public void GetWriteChannelSourceAsync_should_throw_if_disposed()
        {
            var subject = new ReadWriteBindingHandle(_readWriteBinding);
            subject.Dispose();

            Action act = () => subject.GetWriteChannelSourceAsync(CancellationToken.None);

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetWriteChannelSourceAsync_should_delegate_to_reference()
        {
            var subject = new ReadWriteBindingHandle(_readWriteBinding);

            subject.GetWriteChannelSourceAsync(CancellationToken.None);

            _readWriteBinding.Received().GetWriteChannelSourceAsync(CancellationToken.None);
        }

        [Test]
        public void Fork_should_throw_if_disposed()
        {
            var subject = new ReadWriteBindingHandle(_readWriteBinding);
            subject.Dispose();

            Action act = () => subject.Fork();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void Disposing_of_handle_after_fork_should_not_dispose_of_channelSource()
        {
            var subject = new ReadWriteBindingHandle(_readWriteBinding);

            var forked = subject.Fork();

            subject.Dispose();

            _readWriteBinding.DidNotReceive().Dispose();

            forked.Dispose();

            _readWriteBinding.Received().Dispose();
        }

        [Test]
        public void Disposing_of_fork_before_disposing_of_subject_hould_not_dispose_of_channelSource()
        {
            var subject = new ReadWriteBindingHandle(_readWriteBinding);

            var forked = subject.Fork();

            forked.Dispose();

            _readWriteBinding.DidNotReceive().Dispose();

            subject.Dispose();

            _readWriteBinding.Received().Dispose();
        }

        [Test]
        public void Disposing_of_last_handle_should_dispose_of_connectioSource()
        {
            var subject = new ReadWriteBindingHandle(_readWriteBinding);

            var forked = subject.Fork();

            subject.Dispose();
            forked.Dispose();

            _readWriteBinding.Received().Dispose();
        }
    }
}