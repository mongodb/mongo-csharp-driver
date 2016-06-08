/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoServerAddressTests
    {
        [Theory]
        [InlineData("host")]
        [InlineData("192.168.0.1")]
        [InlineData("[2001:0db8:85a3:0042:0000:8a2e:0370:7334]")]
        public void TestConstructor_with_host(string host)
        {
            var address = new MongoServerAddress(host);
            Assert.Equal(host, address.Host);
            Assert.Equal(27017, address.Port);
        }

        [Theory]
        [InlineData("host", 27017)]
        [InlineData("host", 27018)]
        [InlineData("192.168.0.1", 27017)]
        [InlineData("192.168.0.1", 27018)]
        [InlineData("[2001:0db8:85a3:0042:0000:8a2e:0370:7334]", 27017)]
        [InlineData("[2001:0db8:85a3:0042:0000:8a2e:0370:7334]", 27018)]
        public void TestConstructor_with_host_and_port(string host, int port)
        {
            var address = new MongoServerAddress(host, port);
            Assert.Equal(host, address.Host);
            Assert.Equal(port, address.Port);
        }

        [Fact]
        public void TestEquals()
        {
            var a = new MongoServerAddress("host1");
            var b = new MongoServerAddress("host1");
            var c = new MongoServerAddress("host2");
            var n = (MongoServerAddress)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Theory]
        [InlineData("host", 27017, "host")]
        [InlineData("host", 27017, "host:27017")]
        [InlineData("host", 27018, "host:27018")]
        [InlineData("192.168.0.1", 27017, "192.168.0.1")]
        [InlineData("192.168.0.1", 27017, "192.168.0.1:27017")]
        [InlineData("192.168.0.1", 27018, "192.168.0.1:27018")]
        [InlineData("[2001:0db8:85a3:0042:0000:8a2e:0370:7334]", 27017, "[2001:0db8:85a3:0042:0000:8a2e:0370:7334]")]
        [InlineData("[2001:0db8:85a3:0042:0000:8a2e:0370:7334]", 27017, "[2001:0db8:85a3:0042:0000:8a2e:0370:7334]:27017")]
        [InlineData("[2001:0db8:85a3:0042:0000:8a2e:0370:7334]", 27018, "[2001:0db8:85a3:0042:0000:8a2e:0370:7334]:27018")]
        public void TestParse(string host, int port, string value)
        {
            var address = MongoServerAddress.Parse(value);
            Assert.Equal(host, address.Host);
            Assert.Equal(port, address.Port);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abc:def")]
        [InlineData("abc:123:456")]
        [InlineData("[]")]
        [InlineData("a[]")]
        [InlineData("[]b")]
        [InlineData("a[]b")]
        public void TestParse_InvalidValue(string value)
        {
            var expection = Record.Exception(() => MongoServerAddress.Parse(value));

            Assert.IsType<FormatException>(expection);
            var expectedMessage = string.Format("'{0}' is not a valid server address.", value);
            Assert.Equal(expectedMessage, expection.Message);
        }
    }
}