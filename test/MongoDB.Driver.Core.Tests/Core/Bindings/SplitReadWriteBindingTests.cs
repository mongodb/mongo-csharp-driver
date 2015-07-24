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
    public class SplitReadWriteBindingTests
    {
        private IReadBinding _readBinding;
        private IWriteBinding _writeBinding;

        [SetUp]
        public void Setup()
        {
            _readBinding = Substitute.For<IReadBinding>();
            _writeBinding = Substitute.For<IWriteBinding>();
        }

        [Test]
        public void Constructor_should_throw_if_readBinding_is_null()
        {
            Action act = () => new SplitReadWriteBinding(null, _writeBinding);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_if_readPreference_is_null()
        {
            Action act = () => new SplitReadWriteBinding(_readBinding, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void GetReadChannelSourceAsync_should_throw_if_disposed()
        {
            var subject = new SplitReadWriteBinding(_readBinding, _writeBinding);
            subject.Dispose();

            Action act = () => subject.GetReadChannelSourceAsync(CancellationToken.None);

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetReadChannelSourceAsync_should_get_the_connection_source_from_the_read_binding()
        {
            var subject = new SplitReadWriteBinding(_readBinding, _writeBinding);

            subject.GetReadChannelSourceAsync(CancellationToken.None);

            _readBinding.Received().GetReadChannelSourceAsync(CancellationToken.None);
        }

        [Test]
        public void GetWriteChannelSourceAsync_should_throw_if_disposed()
        {
            var subject = new SplitReadWriteBinding(_readBinding, _writeBinding);
            subject.Dispose();

            Action act = () => subject.GetWriteChannelSourceAsync(CancellationToken.None);

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetWriteChannelSourceAsync_should_get_the_connection_source_from_the_write_binding()
        {
            var subject = new SplitReadWriteBinding(_readBinding, _writeBinding);

            subject.GetWriteChannelSourceAsync(CancellationToken.None);

            _writeBinding.Received().GetWriteChannelSourceAsync(CancellationToken.None);
        }

        [Test]
        public void Dispose_should_call_dispose_on_read_binding_and_write_binding()
        {
            var subject = new SplitReadWriteBinding(_readBinding, _writeBinding);

            subject.Dispose();

            _readBinding.Received().Dispose();
            _writeBinding.Received().Dispose();
        }
    }
}