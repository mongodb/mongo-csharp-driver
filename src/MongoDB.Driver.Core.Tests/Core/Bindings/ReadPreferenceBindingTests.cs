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
        public void GetReadChannelSource_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadPreferenceBinding(_cluster, ReadPreference.Primary);
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
        public void GetReadChannelSource_should_use_a_read_preference_server_selector_to_select_the_server_from_the_cluster(
            [Values(false, true)]
            bool async)
        {
            var subject = new ReadPreferenceBinding(_cluster, ReadPreference.Primary);

            if (async)
            {
                subject.GetReadChannelSourceAsync(CancellationToken.None).GetAwaiter().GetResult();

                _cluster.Received().SelectServerAsync(Arg.Any<ReadPreferenceServerSelector>(), CancellationToken.None);
            }
            else
            {
                subject.GetReadChannelSource(CancellationToken.None);

                _cluster.Received().SelectServer(Arg.Any<ReadPreferenceServerSelector>(), CancellationToken.None);
            }
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