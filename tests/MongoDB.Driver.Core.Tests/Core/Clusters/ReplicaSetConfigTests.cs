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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using Xunit;

namespace MongoDB.Driver.Core.Clusters
{
    public class ReplicaSetConfigTests
    {
        private static readonly EndPoint[] _endPoints =
            new[] { new DnsEndPoint("localhost", 27017), new DnsEndPoint("localhost", 27018) };
        private static readonly string _name = "rs1";
        private static readonly int _version = 10;

        [Fact]
        public void Constructor_should_throw_if_endpoints_is_null()
        {
            Action act = () => new ReplicaSetConfig(null, _name, _endPoints[0], _version);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_assign_properties_correctly()
        {
            var subject = new ReplicaSetConfig(_endPoints, _name, _endPoints[0], _version);

            subject.Members.Should().BeEquivalentTo(_endPoints);
            subject.Name.Should().Be(_name);
            subject.Primary.Should().Be(_endPoints[0]);
            subject.Version.Should().Be(_version);
        }

        [Fact]
        public void Equals_should_ignore_revision()
        {
            var subject1 = new ReplicaSetConfig(_endPoints, _name, _endPoints[0], _version);
            var subject2 = new ReplicaSetConfig(_endPoints, _name, _endPoints[0], _version);
            subject1.Equals(subject2).Should().BeTrue();
            subject1.Equals((object)subject2).Should().BeTrue();
            subject1.GetHashCode().Should().Be(subject2.GetHashCode());
        }

        [Fact]
        public void Equals_should_not_be_the_same_when_a_field_is_different()
        {
            var subject1 = new ReplicaSetConfig(_endPoints, _name, _endPoints[0], _version);

            var subject2 = new ReplicaSetConfig(new[] { _endPoints[0] }, _name, _endPoints[0], _version);
            var subject3 = new ReplicaSetConfig(_endPoints, null, _endPoints[0], _version);
            var subject4 = new ReplicaSetConfig(_endPoints, _name, _endPoints[1], _version);
            var subject5 = new ReplicaSetConfig(_endPoints, _name, null, _version);
            var subject6 = new ReplicaSetConfig(_endPoints, _name, _endPoints[0], _version + 1);
            var subject7 = new ReplicaSetConfig(_endPoints, _name, _endPoints[0], null);

            subject1.Should().NotBe(subject2);
            subject1.Should().NotBe(subject3);
            subject1.Should().NotBe(subject4);
            subject1.Should().NotBe(subject5);
            subject1.Should().NotBe(subject6);
            subject1.Should().NotBe(subject7);

            subject2.Should().NotBe(subject1);
            subject3.Should().NotBe(subject1);
            subject4.Should().NotBe(subject1);
            subject5.Should().NotBe(subject1);
            subject6.Should().NotBe(subject1);
            subject7.Should().NotBe(subject1);
        }

    }
}
