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
    public class WriteBindingTests
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
            Action act = () => new WriteBinding(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void GetWriteConnectionSourceAsync_should_throw_if_disposed()
        {
            var subject = new WriteBinding(_cluster);
            subject.Dispose();

            Action act = () => subject.GetWriteConnectionSourceAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetReadConnectionSourceAsync_should_get_the_connection_source_from_the_read_binding()
        {
            var subject = new WriteBinding(_cluster);

            subject.GetWriteConnectionSourceAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            _cluster.ReceivedWithAnyArgs().SelectServerAsync(null, default(TimeSpan), default(CancellationToken));
        }

        [Test]
        public void Dispose_should_call_dispose_on_read_binding_and_write_binding()
        {
            var subject = new WriteBinding(_cluster);

            subject.Dispose();

            _cluster.DidNotReceive().Dispose();
        }
    }
}