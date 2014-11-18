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
    public class ReadPreferenceBindingTests
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
            Action act = () => new ReadPreferenceBinding(null, ReadPreference.Primary);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_if_readPreference_is_null()
        {
            Action act = () => new ReadPreferenceBinding(_cluster, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void GetReadChannelSourceAsync_should_throw_if_disposed()
        {
            var subject = new ReadPreferenceBinding(_cluster, ReadPreference.Primary);
            subject.Dispose();

            Action act = () => subject.GetReadChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetReadChannelSourceAsync_should_use_a_read_preference_server_selector_to_select_the_server_from_the_cluster()
        {
            var subject = new ReadPreferenceBinding(_cluster, ReadPreference.Primary);

            subject.GetReadChannelSourceAsync(CancellationToken.None).Wait();

            _cluster.Received().SelectServerAsync(Arg.Any<ReadPreferenceServerSelector>(), CancellationToken.None);
        }

        [Test]
        public void Dispose_should_not_call_dispose_on_the_cluster()
        {
            var subject = new ReadPreferenceBinding(_cluster, ReadPreference.Primary);

            subject.Dispose();

            _cluster.DidNotReceive().Dispose();
        }
    }
}