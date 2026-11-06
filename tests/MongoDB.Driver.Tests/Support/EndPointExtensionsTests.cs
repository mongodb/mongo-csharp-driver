/* Copyright 2010-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Driver.Support;
using Xunit;

namespace MongoDB.Driver.Tests.Support
{
    public class EndPointExtensionsTests
    {
        public static IEnumerable<object[]> ToMongoServerAddressTestCases()
        {
            yield return [new DnsEndPoint("localhost", 27017), "localhost", 27017];
            yield return [new DnsEndPoint("mongo-1.example.com", 27018), "mongo-1.example.com", 27018];
            yield return [new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27017), "127.0.0.1", 27017];
            yield return [new IPEndPoint(IPAddress.Parse("192.168.1.10"), 27018), "192.168.1.10", 27018];
            yield return [new IPEndPoint(IPAddress.Parse("::1"), 27017), "[::1]", 27017];
            yield return [new IPEndPoint(IPAddress.Parse("2001:db8::1"), 27018), "[2001:db8::1]", 27018];
        }

        [Theory]
        [MemberData(nameof(ToMongoServerAddressTestCases))]
        public void ToMongoServerAddress_should_map_supported_EndPoints(EndPoint endPoint, string expectedHost, int expectedPort)
        {
            var result = endPoint.ToMongoServerAddress();

            result.Host.Should().Be(expectedHost);
            result.Port.Should().Be(expectedPort);
        }

        [Fact]
        public void ToMongoServerAddress_should_throw_NotSupportedException_for_unsupported_EndPoint()
        {
            var endPoint = new UnsupportedEndPoint();

            var exception = Record.Exception(() => endPoint.ToMongoServerAddress());

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Fact]
        public void ToMongoServerAddress_should_throw_NotSupportedException_when_EndPoint_is_null()
        {
            EndPoint endPoint = null;

            var exception = Record.Exception(() => endPoint.ToMongoServerAddress());

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Fact]
        public void ToMongoServerAddresses_should_map_all_endpoints_preserving_order()
        {
            var endPoints = new EndPoint[]
            {
                new DnsEndPoint("localhost", 27017),
                new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27018),
                new IPEndPoint(IPAddress.Parse("::1"), 27019)
            };

            var result = endPoints.ToMongoServerAddresses().ToList();

            result.Should().Equal(
                new MongoServerAddress("localhost", 27017),
                new MongoServerAddress("127.0.0.1", 27018),
                new MongoServerAddress("[::1]", 27019));
        }

        [Fact]
        public void ToMongoServerAddresses_should_return_empty_for_empty_sequence()
        {
            var result = Enumerable.Empty<EndPoint>().ToMongoServerAddresses();

            result.Should().BeEmpty();
        }

        [Fact]
        public void ToMongoServerAddresses_should_throw_NotSupportedException_when_sequence_contains_unsupported_EndPoint()
        {
            var endPoints = new EndPoint[] { new DnsEndPoint("localhost", 27017), new UnsupportedEndPoint() };

            var exception = Record.Exception(() => endPoints.ToMongoServerAddresses().ToList());

            exception.Should().BeOfType<NotSupportedException>();
        }

        private sealed class UnsupportedEndPoint : EndPoint
        {
        }
    }
}
