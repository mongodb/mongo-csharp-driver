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

namespace MongoDB.Driver.Core.Connections
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
        public void Equals_should_return_correct_results()
        {
            var serverId1 = __serverId;
            var serverId2 = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27018));
            var localValue1 = 10;
            var localValue2 = 11;

            var subject1 = new ConnectionId(serverId1, localValue1);
            var subject2 = new ConnectionId(serverId1, localValue1);
            var subject3 = new ConnectionId(serverId1, localValue2);
            var subject4 = new ConnectionId(serverId2, localValue1);

            subject1.Equals(subject2).Should().BeTrue();
            subject1.Equals(subject3).Should().BeFalse();
            subject1.Equals(subject4).Should().BeFalse();
        }

        [Test]
        public void LocalValues_of_2_ids_should_not_be_the_same_when_automically_constructed()
        {
            var subject = new ConnectionId(__serverId);
            var subject2 = new ConnectionId(__serverId);

            subject.LocalValue.Should().NotBe(subject2.LocalValue);
        }

        [Test]
        public void LocalValue_should_be_what_was_specified_in_the_constructor()
        {
            var subject = new ConnectionId(__serverId, 10);

            subject.LocalValue.Should().Be(10);
        }

        [Test]
        public void WithServerValue_should_set_the_server_value_and_leave_the_LocalValue_alone()
        {
            var subject = new ConnectionId(__serverId, 10)
                .WithServerValue(11);

            subject.LocalValue.Should().Be(10);
            subject.ServerValue.Should().Be(11);
        }
    }
}