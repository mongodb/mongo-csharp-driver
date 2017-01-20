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
using System.Net.Sockets;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class EndPointHelperTests
    {
        [Theory]
        [InlineData("localhost:27017", "localhost:27017", true)]
        [InlineData("localhost:27017", "localhost:27018", false)]
        [InlineData("localhost:27018", "localhost:27017", false)]
        [InlineData("127.0.0.1:27017", "localhost:27017", false)]
        [InlineData("localhost:27017", "127.0.0.1:27017", false)]
        [InlineData("127.0.0.1:27017", "127.0.0.1:27017", true)]
        [InlineData("127.0.0.1:27017", "127.0.0.1:27018", false)]
        [InlineData("127.0.0.1:27018", "127.0.0.1:27017", false)]
        [InlineData(null, "localhost:27017", false)]
        [InlineData("localhost:27017", null, false)]
        [InlineData(null, null, true)]
        public void Equals_should_return_true_when_endpoints_are_equal(string a, string b, bool expectedResult)
        {
            var endPoint1 = a == null ? null : EndPointHelper.Parse(a);
            var endPoint2 = b == null ? null : EndPointHelper.Parse(b);

            var result = EndPointHelper.Equals(endPoint1, endPoint2);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Parse_should_throw_an_ArgumentNullException_when_value_is_null()
        {
            Action act = () => EndPointHelper.Parse(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [InlineData("gob:2::212")]
        [InlineData("localhost:-1")]
        [InlineData("localhost:66000")]
        [InlineData(":21")]
        public void Parse_should_throw_an_ArgumentException_when_value_is_not_a_valid_end_point(string value)
        {
            Action act = () => EndPointHelper.Parse(value);

            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void ToString_should_return_expected_result_when_value_is_a_DnsEndPoint()
        {
            var endPoint = new DnsEndPoint("localhost", 27017);

            var result = EndPointHelper.ToString(endPoint);

            result.Should().Be("localhost:27017");
        }

        [Fact]
        public void ToString_should_return_expected_result_when_value_is_an_IPEndPoint()
        {
            var endPoint = new IPEndPoint(new IPAddress(new byte[] { 127, 0, 0, 1 }), 27017);

            var result = EndPointHelper.ToString(endPoint);

            result.Should().Be("127.0.0.1:27017");
        }

        [Theory]
        [InlineData("gob:2::212")]
        [InlineData("localhost:-1")]
        [InlineData("localhost:66000")]
        [InlineData(":21")]
        public void TryParse_should_return_false_when_the_end_point_is_invalid(string value)
        {
            EndPoint result;
            var success = EndPointHelper.TryParse(value, out result);

            success.Should().BeFalse();
        }

        [Theory]
        [InlineData("localhost", "localhost", 27017)]
        [InlineData("localhost:28017", "localhost", 28017)]
        [InlineData("act.test.com", "act.test.com", 27017)]
        [InlineData("act.test.com:28017", "act.test.com", 28017)]
        [InlineData("123.test.com", "123.test.com", 27017)]
        [InlineData("123.test.com:28017", "123.test.com", 28017)]
        public void TryParse_should_parse_a_hostname(string value, string expectedHost, int expectedPort)
        {
            EndPoint result;
            var success = EndPointHelper.TryParse(value, out result);

            success.Should().BeTrue();
            result.Should().Be(new DnsEndPoint(expectedHost, expectedPort));
            result.AddressFamily.Should().Be(AddressFamily.Unspecified);
        }

        [Theory]
        [InlineData("127.0.0.1", "127.0.0.1", 27017)]
        [InlineData("127.0.0.1:28017", "127.0.0.1", 28017)]
        public void TryParse_should_parse_an_ipv4_address(string value, string expectedAddress, int expectedPort)
        {
            EndPoint result;
            var success = EndPointHelper.TryParse(value, out result);

            success.Should().BeTrue();
            result.Should().Be(new IPEndPoint(IPAddress.Parse(expectedAddress), expectedPort));
            result.AddressFamily.Should().Be(AddressFamily.InterNetwork);
        }

        [Theory]
        [InlineData("[FE80:0000:0000:0000:0202:B3FF:FE1E:8329]", "[FE80:0000:0000:0000:0202:B3FF:FE1E:8329]", 27017)]
        [InlineData("[FE80:0000:0000:0000:0202:B3FF:FE1E:8329]:28017", "[FE80:0000:0000:0000:0202:B3FF:FE1E:8329]", 28017)]
        public void TryParse_should_parse_an_ipv6_address(string value, string expectedAddress, int expectedPort)
        {
            EndPoint result;
            var success = EndPointHelper.TryParse(value, out result);

            success.Should().BeTrue();
            result.Should().Be(new IPEndPoint(IPAddress.Parse(expectedAddress), expectedPort));
            result.AddressFamily.Should().Be(AddressFamily.InterNetworkV6);
        }
    }
}