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
    public class ConnectionSourceHandleTests
    {
        private IConnectionSource _connectionSource;

        [SetUp]
        public void Setup()
        {
            _connectionSource = Substitute.For<IConnectionSource>();
        }

        [Test]
        public void Constructor_should_throw_if_connectionSource_is_null()
        {
            Action act = () => new ConnectionSourceHandle(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void GetConnectionAsync_should_throw_if_disposed()
        {
            var subject = new ConnectionSourceHandle(_connectionSource);
            subject.Dispose();

            Action act = () => subject.GetConnectionAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetConnectionAsync_should_delegate_to_reference()
        {
            var subject = new ConnectionSourceHandle(_connectionSource);

            subject.GetConnectionAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);

            _connectionSource.Received().GetConnectionAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);            
        }

        [Test]
        public void Fork_should_throw_if_disposed()
        {
            var subject = new ConnectionSourceHandle(_connectionSource);
            subject.Dispose();

            Action act = () => subject.Fork();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void Disposing_of_handle_after_fork_should_not_dispose_of_connectionSource()
        {
            var subject = new ConnectionSourceHandle(_connectionSource);

            var forked = subject.Fork();

            subject.Dispose();

            _connectionSource.DidNotReceive().Dispose();
        }

        [Test]
        public void Disposing_of_fork_before_disposing_of_subject_hould_not_dispose_of_connectionSource()
        {
            var subject = new ConnectionSourceHandle(_connectionSource);

            var forked = subject.Fork();

            forked.Dispose();

            _connectionSource.DidNotReceive().Dispose();
        }

        [Test]
        public void Disposing_of_last_handle_should_dispose_of_connectioSource()
        {
            var subject = new ConnectionSourceHandle(_connectionSource);

            var forked = subject.Fork();

            subject.Dispose();
            forked.Dispose();

            _connectionSource.Received().Dispose();
        }
    }
}