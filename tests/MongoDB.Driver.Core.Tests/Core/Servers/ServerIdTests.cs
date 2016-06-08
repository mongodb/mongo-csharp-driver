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
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Xunit;


namespace MongoDB.Driver.Core.Servers
{
    public class ServerIdTests
    {
        private static readonly ClusterId __clusterId = new ClusterId(1);
        private static readonly DnsEndPoint __endPoint = new DnsEndPoint("localhost", 27017);

        [Fact]
        public void Constructor_should_properly_initializes_instance()
        {
            var subject = new ServerId(__clusterId, __endPoint);
            subject.ClusterId.Should().Be(__clusterId);
            subject.EndPoint.Should().Be(__endPoint);
        }

        private ServerId CreateSubject(string notEqualField = null)
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);

            switch (notEqualField)
            {
                case "ClusterId": clusterId = new ClusterId(2); break;
                case "EndPoint": endPoint = new DnsEndPoint("localhost", 27018); break;
            }

            return new ServerId(clusterId, endPoint);
        }

        [Theory]
        [InlineData("ClusterId")]
        [InlineData("EndPoint")]
        public void Equals_should_return_false_if_any_field_is_not_equal(string notEqualField)
        {
            var subject1 = CreateSubject();
            var subject2 = CreateSubject(notEqualField);
            subject1.Equals(subject2).Should().BeFalse();
            subject1.Equals((object)subject2).Should().BeFalse();
            subject1.GetHashCode().Should().NotBe(subject2.GetHashCode());
        }

        [Fact]
        public void Equals_should_return_true_if_all_fiels_are_equal()
        {
            var subject1 = CreateSubject();
            var subject2 = CreateSubject();
            subject1.Equals(subject2).Should().BeTrue();
            subject1.Equals((object)subject2).Should().BeTrue();
            subject1.GetHashCode().Should().Be(subject2.GetHashCode());
        }

        [Fact]
        public void ToString_should_return_string_representation()
        {
            var subject = new ServerId(__clusterId, __endPoint);
            subject.ToString().Should().Be("{ ClusterId : 1, EndPoint : \"Unspecified/localhost:27017\" }");
        }
    }
}
