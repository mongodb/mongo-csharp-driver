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
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Bindings
{
    [TestFixture]
    public class WritableServerBindingTests
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
            Action act = () => new WritableServerBinding(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void ReadPreference_should_be_primary()
        {
            var subject = new WritableServerBinding(_cluster);

            subject.ReadPreference.Should().Be(ReadPreference.Primary);
        }

        [Test]
        public void GetReadChannelSourceAsync_should_throw_if_disposed()
        {
            var subject = new WritableServerBinding(_cluster);
            subject.Dispose();

            Action act = () => subject.GetReadChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetReadChannelSourceAsync_should_use_a_writable_server_selector_to_select_the_server_from_the_cluster()
        {
            var subject = new WritableServerBinding(_cluster);

            subject.GetReadChannelSourceAsync(CancellationToken.None).Wait();

            _cluster.Received().SelectServerAsync(Arg.Any<WritableServerSelector>(), CancellationToken.None);
        }

        [Test]
        public void GetWriteChannelSourceAsync_should_throw_if_disposed()
        {
            var subject = new WritableServerBinding(_cluster);
            subject.Dispose();

            Action act = () => subject.GetWriteChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetWriteChannelSourceAsync_should_use_a_writable_server_selector_to_select_the_server_from_the_cluster()
        {
            var subject = new WritableServerBinding(_cluster);

            subject.GetWriteChannelSourceAsync(CancellationToken.None).Wait();

            _cluster.Received().SelectServerAsync(Arg.Any<WritableServerSelector>(), CancellationToken.None);
        }

        [Test]
        public void Dispose_should_call_dispose_on_read_binding_and_write_binding()
        {
            var subject = new WritableServerBinding(_cluster);

            subject.Dispose();

            _cluster.DidNotReceive().Dispose();
        }
    }
}