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
using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.Connections
{
    [TestFixture]
    public class ConnectionIdTests
    {
        private static readonly ClusterId __clusterId = new ClusterId();
        private static readonly ServerId __serverId = new ServerId(__clusterId, new DnsEndPoint("localhost", 27017));

        [Test]
        public void Constructor_should_throw_an_ArgumentNullException_when_serverId_is_null()
        {
            Action act = () => new ConnectionId(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void IdSource_should_be_driver_when_using_the_single_argument_constructor()
        {
            var subject = new ConnectionId(__serverId);

            subject.Source.Should().Be(ConnectionIdSource.Driver);
        }

        [Test]
        public void Values_of_2_ids_should_not_be_the_same_when_automically_constructed()
        {
            var subject = new ConnectionId(__serverId);
            var subject2 = new ConnectionId(__serverId);

            subject.Value.Should().NotBe(subject2.Value);
        }

        [Test]
        public void IdSource_should_be_what_was_specified_in_the_constructor()
        {
            var subject = new ConnectionId(__serverId, 10, ConnectionIdSource.Server);

            subject.Source.Should().Be(ConnectionIdSource.Server);
        }

        [Test]
        public void Value_should_be_what_was_specified_in_the_constructor()
        {
            var subject = new ConnectionId(__serverId, 10, ConnectionIdSource.Server);

            subject.Value.Should().Be(10);
        }
    }
}