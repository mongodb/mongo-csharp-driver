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
using System.Net.Sockets;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.Misc
{
    [TestFixture]
    public class DnsEndPointParserTests
    {
        [Test]
        public void Parse_should_throw_an_ArgumentNullException_when_value_is_null()
        {
            Action act = () => DnsEndPointParser.Parse(null, AddressFamily.InterNetwork);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        [TestCase("gob:2::212")]
        [TestCase("localhost:-1")]
        [TestCase("localhost:66000")]
        [TestCase(":21")]
        public void Parse_should_throw_an_ArgumentException_when_value_is_not_a_valid_end_point(string value)
        {
            Action act = () => DnsEndPointParser.Parse(value, AddressFamily.InterNetwork);

            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        [TestCase("localhost", 21, "localhost:21")]
        [TestCase("abc.test.com", 21, "abc.test.com:21")]
        [TestCase("123.test.com", 21, "123.test.com:21")]
        [TestCase("127.0.0.1", 21, "127.0.0.1:21")]
        [TestCase("[::1]", 21, "[::1]:21")]
        public void ToString_should_print_host_and_port(string host, int port, string expectedToString)
        {
            var result = DnsEndPointParser.ToString(new DnsEndPoint(host, port));

            result.Should().Be(expectedToString);
        }

        [Test]
        [TestCase("gob:2::212")]
        [TestCase("localhost:-1")]
        [TestCase("localhost:66000")]
        [TestCase(":21")]
        public void TryParse_should_return_false_when_the_end_point_is_invalid(string value)
        {
            DnsEndPoint result;
            var success = DnsEndPointParser.TryParse(value, AddressFamily.InterNetwork, out result);

            success.Should().BeFalse();
        }

        [Test]
        [TestCase("localhost", "localhost", 27017)]
        [TestCase("localhost:28017", "localhost", 28017)]
        [TestCase("act.test.com", "act.test.com", 27017)]
        [TestCase("act.test.com:28017", "act.test.com", 28017)]
        [TestCase("123.test.com", "123.test.com", 27017)]
        [TestCase("123.test.com:28017", "123.test.com", 28017)]
        public void TryParse_should_parse_a_hostname(string value, string expectedHost, int expectedPort)
        {
            DnsEndPoint result;
            var success = DnsEndPointParser.TryParse(value, AddressFamily.InterNetwork, out result);

            success.Should().BeTrue();
            result.Host.Should().Be(expectedHost);
            result.Port.Should().Be(expectedPort);
            result.AddressFamily.Should().Be(AddressFamily.InterNetwork);
        }

        [Test]
        [TestCase("127.0.0.1", "127.0.0.1", 27017)]
        [TestCase("127.0.0.1:28017", "127.0.0.1", 28017)]
        public void TryParse_should_parse_an_ipv4_address(string value, string expectedHost, int expectedPort)
        {
            DnsEndPoint result;
            var success = DnsEndPointParser.TryParse(value, AddressFamily.InterNetwork, out result);

            success.Should().BeTrue();
            result.Host.Should().Be(expectedHost);
            result.Port.Should().Be(expectedPort);
            result.AddressFamily.Should().Be(AddressFamily.InterNetwork);
        }

        [Test]
        [TestCase("[FE80:0000:0000:0000:0202:B3FF:FE1E:8329]", "[FE80:0000:0000:0000:0202:B3FF:FE1E:8329]", 27017)]
        [TestCase("[FE80:0000:0000:0000:0202:B3FF:FE1E:8329]:28017", "[FE80:0000:0000:0000:0202:B3FF:FE1E:8329]", 28017)]
        public void TryParse_should_parse_an_ipv6_address(string value, string expectedHost, int expectedPort)
        {
            DnsEndPoint result;
            var success = DnsEndPointParser.TryParse(value, AddressFamily.InterNetworkV6, out result);

            success.Should().BeTrue();
            result.Host.Should().Be(expectedHost);
            result.Port.Should().Be(expectedPort);
            result.AddressFamily.Should().Be(AddressFamily.InterNetworkV6);
        }
    }
}