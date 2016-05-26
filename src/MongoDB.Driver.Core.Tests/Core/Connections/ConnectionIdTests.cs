/* Copyright 2013-2016 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class ConnectionIdTests
    {
        private static readonly ClusterId __clusterId = new ClusterId();
        private static readonly ServerId __serverId = new ServerId(__clusterId, new DnsEndPoint("localhost", 27017));

        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_serverId_is_null()
        {
            Action act = () => new ConnectionId(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [InlineData(1, 2, 3, 1, 2, 3, true, true)]
        [InlineData(1, 2, 3, 4, 2, 3, false, false)]
        [InlineData(1, 2, 3, 1, 4, 3, false, false)]
        [InlineData(1, 2, 3, 1, 2, 4, true, false)]
        public void Equals_should_return_expected_result(
            int port1,
            int localValue1,
            int serverValue1,
            int port2,
            int localValue2,
            int serverValue2,
            bool expectedEqualsResult,
            bool expectedStructurallyEqualsResult)
        {
            var clusterId = new ClusterId();
            var serverId1 = new ServerId(clusterId, new DnsEndPoint("localhost", port1));
            var serverId2 = new ServerId(clusterId, new DnsEndPoint("localhost", port2));

            var subject1 = new ConnectionId(serverId1, localValue1).WithServerValue(serverValue1);
            var subject2 = new ConnectionId(serverId2, localValue2).WithServerValue(serverValue2);

            // note: Equals ignores the server values and StructurallyEquals compares all fields
            var equalsResult1 = subject1.Equals(subject2);
            var equalsResult2 = subject2.Equals(subject1);
            var structurallyEqualsResult1 = subject1.StructurallyEquals(subject2);
            var structurallyEqualsResult2 = subject2.StructurallyEquals(subject1);
            var hashCode1 = subject1.GetHashCode();
            var hashCode2 = subject2.GetHashCode();

            equalsResult1.Should().Be(expectedEqualsResult);
            equalsResult2.Should().Be(expectedEqualsResult);
            structurallyEqualsResult1.Should().Be(expectedStructurallyEqualsResult);
            structurallyEqualsResult2.Should().Be(expectedStructurallyEqualsResult);
            (hashCode1 == hashCode2).Should().Be(expectedEqualsResult);
        }

        [Fact]
        public void LocalValues_of_2_ids_should_not_be_the_same_when_automically_constructed()
        {
            var subject = new ConnectionId(__serverId);
            var subject2 = new ConnectionId(__serverId);

            subject.LocalValue.Should().NotBe(subject2.LocalValue);
        }

        [Fact]
        public void LocalValue_should_be_what_was_specified_in_the_constructor()
        {
            var subject = new ConnectionId(__serverId, 10);

            subject.LocalValue.Should().Be(10);
        }

        [Fact]
        public void WithServerValue_should_set_the_server_value_and_leave_the_LocalValue_alone()
        {
            var subject = new ConnectionId(__serverId, 10)
                .WithServerValue(11);

            subject.LocalValue.Should().Be(10);
            subject.ServerValue.Should().Be(11);
        }
    }
}