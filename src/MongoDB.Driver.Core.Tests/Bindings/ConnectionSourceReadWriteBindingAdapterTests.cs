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

namespace MongoDB.Driver.Core.Tests.Bindings
{
    [TestFixture]
    public class ConnectionSourceReadWriteBindingAdapterTests
    {
        private IConnectionSourceHandle _connectionSource;

        [SetUp]
        public void Setup()
        {
            _connectionSource = Substitute.For<IConnectionSourceHandle>();
        }

        [Test]
        public void Constructor_should_throw_if_connectionSource_is_null()
        {
            Action act = () => new ConnectionSourceReadWriteBindingAdapter(null, ReadPreference.Primary);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_if_readPreference_is_null()
        {
            Action act = () => new ConnectionSourceReadWriteBindingAdapter(_connectionSource, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_fork_connectionSource()
        {
            var subject = new ConnectionSourceReadWriteBindingAdapter(_connectionSource, ReadPreference.Primary);

            _connectionSource.Received().Fork();
        }

        [Test]
        public void GetReadConnectionSourceAsync_should_throw_if_disposed()
        {
            var subject = new ConnectionSourceReadWriteBindingAdapter(_connectionSource, ReadPreference.Primary);
            subject.Dispose();

            Action act = () => subject.GetReadConnectionSourceAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetReadConnectionSourceAsync_should_fork_the_connectionSource()
        {
            var subject = new ConnectionSourceReadWriteBindingAdapter(_connectionSource, ReadPreference.Primary);

            subject.GetReadConnectionSourceAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);

            _connectionSource.Received().Fork();
        }

        [Test]
        public void GetWriteConnectionSourceAsync_should_throw_if_disposed()
        {
            var subject = new ConnectionSourceReadWriteBindingAdapter(_connectionSource, ReadPreference.Primary);
            subject.Dispose();

            Action act = () => subject.GetWriteConnectionSourceAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetWriteConnectionSourceAsync_should_fork_the_connectionSource()
        {
            var subject = new ConnectionSourceReadWriteBindingAdapter(_connectionSource, ReadPreference.Primary);

            subject.GetWriteConnectionSourceAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);

            _connectionSource.Received().Fork();
        }

        [Test]
        public void Dispose_should_call_dispose_on_connection_source()
        {
            var fork = Substitute.For<IConnectionSourceHandle>();
            _connectionSource.Fork().Returns(fork);
            var subject = new ConnectionSourceReadWriteBindingAdapter(_connectionSource, ReadPreference.Primary);

            subject.Dispose();

            fork.Received().Dispose();
            _connectionSource.DidNotReceive().Dispose();
        }
    }
}