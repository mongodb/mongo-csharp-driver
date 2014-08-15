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
    public class ReadBindingTests
    {
        private ICluster _cluster;

        [SetUp]
        public void Setup()
        {
            _cluster = Substitute.For<ICluster>();
        }

        [Test]
        public void Constructor_should_throw_if_cluster_is_null()
        {
            Action act = () => new ReadBinding(null, ReadPreference.Primary);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_if_readPreference_is_null()
        {
            Action act = () => new ReadWriteBinding(_cluster, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void GetReadConnectionSourceAsync_should_throw_if_disposed()
        {
            var subject = new ReadBinding(_cluster, ReadPreference.Primary);
            subject.Dispose();

            Action act = () => subject.GetReadConnectionSourceAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetReadConnectionSourceAsync_should_get_the_connection_source_from_the_read_binding()
        {
            var subject = new ReadBinding(_cluster, ReadPreference.Primary);

            subject.GetReadConnectionSourceAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            _cluster.ReceivedWithAnyArgs().SelectServerAsync(null, default(TimeSpan), default(CancellationToken));
        }

        [Test]
        public void Dispose_should_not_call_dispose_on_the_cluster()
        {
            var subject = new ReadBinding(_cluster, ReadPreference.Primary);

            subject.Dispose();

            _cluster.DidNotReceive().Dispose();
        }
    }
}