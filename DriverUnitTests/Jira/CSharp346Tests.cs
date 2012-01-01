/* Copyright 2010-2012 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;

namespace MongoDB.DriverUnitTests.Jira.CSharp346
{
    [TestFixture]
    public class CSharp346Tests
    {
        [Test]
        public void TestOneIPv6Address()
        {
            var connectionString = "mongodb://[::1:]/?safe=true";
            var url = new MongoUrl(connectionString);
            Assert.AreEqual("[::1:]", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(true, url.SafeMode.Enabled);

            connectionString = "server=[::1:];safe=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual("[::1:]", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(true, builder.SafeMode.Enabled);
        }

        [Test]
        public void TestOneIPv6AddressWithDefaultCredentials()
        {
            var connectionString = "mongodb://username:password@[::1:]/?safe=true";
            var url = new MongoUrl(connectionString);
            Assert.AreEqual("[::1:]", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual("username", url.DefaultCredentials.Username);
            Assert.AreEqual("password", url.DefaultCredentials.Password);
            Assert.AreEqual(true, url.SafeMode.Enabled);

            connectionString = "server=[::1:];username=username;password=password;safe=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual("[::1:]", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual("username", builder.Username);
            Assert.AreEqual("password", builder.Password);
            Assert.AreEqual(true, builder.SafeMode.Enabled);
        }

        [Test]
        public void TestOneIPv6AddressWithPort()
        {
            var connectionString = "mongodb://[::1:]:1234/?safe=true";
            var url = new MongoUrl(connectionString);
            Assert.AreEqual("[::1:]", url.Server.Host);
            Assert.AreEqual(1234, url.Server.Port);
            Assert.AreEqual(true, url.SafeMode.Enabled);

            connectionString = "server=[::1:]:1234;safe=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual("[::1:]", builder.Server.Host);
            Assert.AreEqual(1234, builder.Server.Port);
            Assert.AreEqual(true, builder.SafeMode.Enabled);
        }

        [Test]
        public void TestTwoIPv6Addresses()
        {
            var connectionString = "mongodb://[::1:],[::2:]/?safe=true";
            var url = new MongoUrl(connectionString);
            var servers = url.Servers.ToArray();
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("[::1:]", servers[0].Host);
            Assert.AreEqual(27017, servers[0].Port);
            Assert.AreEqual("[::2:]", servers[1].Host);
            Assert.AreEqual(27017, servers[1].Port);
            Assert.AreEqual(true, url.SafeMode.Enabled);

            connectionString = "servers=[::1:],[::2:];safe=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            servers = builder.Servers.ToArray();
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("[::1:]", servers[0].Host);
            Assert.AreEqual(27017, servers[0].Port);
            Assert.AreEqual("[::2:]", servers[1].Host);
            Assert.AreEqual(27017, servers[1].Port);
            Assert.AreEqual(true, builder.SafeMode.Enabled);
        }


        [Test]
        public void TestTwoIPv6AddressesWithDefaultCredentials()
        {
            var connectionString = "mongodb://username:password@[::1:],[::2:]/?safe=true";
            var url = new MongoUrl(connectionString);
            var servers = url.Servers.ToArray();
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("[::1:]", servers[0].Host);
            Assert.AreEqual(27017, servers[0].Port);
            Assert.AreEqual("[::2:]", servers[1].Host);
            Assert.AreEqual(27017, servers[1].Port);
            Assert.AreEqual("username", url.DefaultCredentials.Username);
            Assert.AreEqual("password", url.DefaultCredentials.Password);
            Assert.AreEqual(true, url.SafeMode.Enabled);

            connectionString = "servers=[::1:],[::2:];username=username;password=password;safe=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            servers = builder.Servers.ToArray();
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("[::1:]", servers[0].Host);
            Assert.AreEqual(27017, servers[0].Port);
            Assert.AreEqual("[::2:]", servers[1].Host);
            Assert.AreEqual(27017, servers[1].Port);
            Assert.AreEqual("username", url.DefaultCredentials.Username);
            Assert.AreEqual("password", url.DefaultCredentials.Password);
            Assert.AreEqual(true, builder.SafeMode.Enabled);
        }

        [Test]
        public void TestTwoIPv6AddressesWithPorts()
        {
            var connectionString = "mongodb://[::1:]:1234,[::2:]:2345/?safe=true";
            var url = new MongoUrl(connectionString);
            var servers = url.Servers.ToArray();
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("[::1:]", servers[0].Host);
            Assert.AreEqual(1234, servers[0].Port);
            Assert.AreEqual("[::2:]", servers[1].Host);
            Assert.AreEqual(2345, servers[1].Port);
            Assert.AreEqual(true, url.SafeMode.Enabled);

            connectionString = "servers=[::1:]:1234,[::2:]:2345;safe=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            servers = builder.Servers.ToArray();
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("[::1:]", servers[0].Host);
            Assert.AreEqual(1234, servers[0].Port);
            Assert.AreEqual("[::2:]", servers[1].Host);
            Assert.AreEqual(2345, servers[1].Port);
            Assert.AreEqual(true, builder.SafeMode.Enabled);
        }
    }
}
