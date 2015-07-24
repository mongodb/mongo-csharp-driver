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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Bindings
{
    [TestFixture]
    public class ServerChannelSourceTests
    {
        private IServer _server;

        [SetUp]
        public void Setup()
        {
            _server = Substitute.For<IServer>();
        }

        [Test]
        public void Constructor_should_throw_when_server_is_null()
        {
            Action act = () => new ServerChannelSource(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void ServerDescription_should_return_description_of_server()
        {
            var subject = new ServerChannelSource(_server);

            var desc = ServerDescriptionHelper.Disconnected(new ClusterId());

            _server.Description.Returns(desc);

            var result = subject.ServerDescription;

            result.Should().BeSameAs(desc);
        }

        [Test]
        public void GetChannelAsync_should_throw_if_disposed()
        {
            var subject = new ServerChannelSource(_server);
            subject.Dispose();

            Action act = () => subject.GetChannelAsync(CancellationToken.None);

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetChannelAsync_should_get_connection_from_server()
        {
            var subject = new ServerChannelSource(_server);

            subject.GetChannelAsync(CancellationToken.None);

            _server.Received().GetChannelAsync(CancellationToken.None);
        }
    }
}
