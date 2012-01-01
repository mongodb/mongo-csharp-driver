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
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoUrlBuilderTests
    {
        private static MongoServerAddress __localhost = new MongoServerAddress("localhost");

        [Test]
        public void TestDefaults()
        {
            var builder = new MongoUrlBuilder();
            Assert.AreEqual(MongoDefaults.ComputedWaitQueueSize, builder.ComputedWaitQueueSize);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(MongoDefaults.ConnectTimeout, builder.ConnectTimeout);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(null, builder.DefaultCredentials);
            Assert.AreEqual(MongoDefaults.GuidRepresentation, builder.GuidRepresentation);
            Assert.AreEqual(false, builder.IPv6);
            Assert.AreEqual(MongoDefaults.MaxConnectionIdleTime, builder.MaxConnectionIdleTime);
            Assert.AreEqual(MongoDefaults.MaxConnectionLifeTime, builder.MaxConnectionLifeTime);
            Assert.AreEqual(MongoDefaults.MaxConnectionPoolSize, builder.MaxConnectionPoolSize);
            Assert.AreEqual(MongoDefaults.MinConnectionPoolSize, builder.MinConnectionPoolSize);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(null, builder.Server);
            Assert.AreEqual(null, builder.Servers);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(MongoDefaults.SocketTimeout, builder.SocketTimeout);
            Assert.AreEqual(MongoDefaults.WaitQueueMultiple, builder.WaitQueueMultiple);
            Assert.AreEqual(MongoDefaults.WaitQueueSize, builder.WaitQueueSize);
            Assert.AreEqual(MongoDefaults.WaitQueueTimeout, builder.WaitQueueTimeout);

            var connectionString = "mongodb://";
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestHost()
        {
            var builder = new MongoUrlBuilder() { Server = new MongoServerAddress("mongo.xyz.com") };
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("mongo.xyz.com", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);

            var connectionString = "mongodb://mongo.xyz.com";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestHostWithPort()
        {
            var builder = new MongoUrlBuilder() { Server = new MongoServerAddress("mongo.xyz.com", 12345) };
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("mongo.xyz.com", builder.Server.Host);
            Assert.AreEqual(12345, builder.Server.Port);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);

            var connectionString = "mongodb://mongo.xyz.com:12345";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestTwoHosts()
        {
            var builder = new MongoUrlBuilder() { Servers = new[] { new MongoServerAddress("mongo1.xyz.com"), new MongoServerAddress("mongo2.xyz.com") } };
            var servers = builder.Servers.ToArray();
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("mongo1.xyz.com", servers[0].Host);
            Assert.AreEqual(27017, servers[0].Port);
            Assert.AreEqual("mongo2.xyz.com", servers[1].Host);
            Assert.AreEqual(27017, servers[1].Port);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);

            var connectionString = "mongodb://mongo1.xyz.com,mongo2.xyz.com";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestTwoHostsWithPorts()
        {
            var builder = new MongoUrlBuilder() { Servers = new[] { new MongoServerAddress("mongo1.xyz.com", 12345), new MongoServerAddress("mongo2.xyz.com", 23456) } };
            var servers = builder.Servers.ToArray();
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("mongo1.xyz.com", servers[0].Host);
            Assert.AreEqual(12345, servers[0].Port);
            Assert.AreEqual("mongo2.xyz.com", servers[1].Host);
            Assert.AreEqual(23456, servers[1].Port);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);

            var connectionString = "mongodb://mongo1.xyz.com:12345,mongo2.xyz.com:23456";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestUsernamePassword()
        {
            var builder = new MongoUrlBuilder()
            {
                Server = new MongoServerAddress("localhost"),
                DefaultCredentials = new MongoCredentials("username", "password")
            };
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual("username", builder.DefaultCredentials.Username);
            Assert.AreEqual("password", builder.DefaultCredentials.Password);

            var connectionString = "mongodb://username:password@localhost";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestUsernamePasswordEscaped()
        {
            var builder = new MongoUrlBuilder()
            {
                Server = new MongoServerAddress("localhost"),
                DefaultCredentials = new MongoCredentials("usern:me", "p@ssword")
            };
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual("usern:me", builder.DefaultCredentials.Username);
            Assert.AreEqual("p@ssword", builder.DefaultCredentials.Password);

            var connectionString = "mongodb://usern%3Ame:p%40ssword@localhost";
            Assert.AreEqual(connectionString, builder.ToString());

            builder = new MongoUrlBuilder(connectionString);
            Assert.AreEqual("usern:me", builder.DefaultCredentials.Username);
            Assert.AreEqual("p@ssword", builder.DefaultCredentials.Password);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestUsernamePasswordLocalhostDatabase()
        {
            var builder = new MongoUrlBuilder()
            {
                Server = new MongoServerAddress("localhost"),
                DefaultCredentials = new MongoCredentials("username", "password"),
                DatabaseName = "database"
            };
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual("username", builder.DefaultCredentials.Username);
            Assert.AreEqual("password", builder.DefaultCredentials.Password);
            Assert.AreEqual("database", builder.DatabaseName);

            var connectionString = "mongodb://username:password@localhost/database";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestUsernamePasswordTwoHostsDatabase()
        {
            var builder = new MongoUrlBuilder()
            {
                Servers = new[] { new MongoServerAddress("mongo1.xyz.com"), new MongoServerAddress("mongo2.xyz.com") },
                DefaultCredentials = new MongoCredentials("username", "password"),
                DatabaseName = "database"
            };
            var servers = builder.Servers.ToArray();
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("mongo1.xyz.com", servers[0].Host);
            Assert.AreEqual(27017, servers[0].Port);
            Assert.AreEqual("mongo2.xyz.com", servers[1].Host);
            Assert.AreEqual(27017, servers[1].Port);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual("username", builder.DefaultCredentials.Username);
            Assert.AreEqual("password", builder.DefaultCredentials.Password);
            Assert.AreEqual("database", builder.DatabaseName);

            var connectionString = "mongodb://username:password@mongo1.xyz.com,mongo2.xyz.com/database";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestUsernamePasswordTwoHostsWithPortsDatabase()
        {
            var builder = new MongoUrlBuilder()
            {
                Servers = new[] { new MongoServerAddress("mongo1.xyz.com", 12345), new MongoServerAddress("mongo2.xyz.com", 23456) },
                DefaultCredentials = new MongoCredentials("username", "password"),
                DatabaseName = "database"
            };
            var servers = builder.Servers.ToArray();
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("mongo1.xyz.com", servers[0].Host);
            Assert.AreEqual(12345, servers[0].Port);
            Assert.AreEqual("mongo2.xyz.com", servers[1].Host);
            Assert.AreEqual(23456, servers[1].Port);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual("username", builder.DefaultCredentials.Username);
            Assert.AreEqual("password", builder.DefaultCredentials.Password);
            Assert.AreEqual("database", builder.DatabaseName);

            var connectionString = "mongodb://username:password@mongo1.xyz.com:12345,mongo2.xyz.com:23456/database";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestConnectionMode()
        {
            var connectionString = "mongodb://localhost";
            var builder = new MongoUrlBuilder("mongodb://localhost") { ConnectionMode = ConnectionMode.Direct };
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());

            connectionString = "mongodb://localhost/?connect=replicaSet";
            builder = new MongoUrlBuilder("mongodb://localhost") { ConnectionMode = ConnectionMode.ReplicaSet };
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestConnectTimeout()
        {
            var connectionString = "mongodb://localhost/?connectTimeout=123ms";
            var builder = new MongoUrlBuilder("mongodb://localhost") { ConnectTimeout = TimeSpan.FromMilliseconds(123) };
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), builder.ConnectTimeout);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());

            connectionString = "mongodb://localhost/?connectTimeout=123s";
            builder = new MongoUrlBuilder("mongodb://localhost") { ConnectTimeout = TimeSpan.FromSeconds(123) };
            Assert.AreEqual(TimeSpan.FromSeconds(123), builder.ConnectTimeout);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());

            connectionString = "mongodb://localhost/?connectTimeout=123m";
            builder = new MongoUrlBuilder("mongodb://localhost") { ConnectTimeout = TimeSpan.FromMinutes(123) };
            Assert.AreEqual(TimeSpan.FromMinutes(123), builder.ConnectTimeout);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());

            connectionString = "mongodb://localhost/?connectTimeout=123h";
            builder = new MongoUrlBuilder("mongodb://localhost") { ConnectTimeout = TimeSpan.FromHours(123) };
            Assert.AreEqual(TimeSpan.FromHours(123), builder.ConnectTimeout);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestIpV6()
        {
            var connectionString = "mongodb://localhost";
            var builder = new MongoUrlBuilder("mongodb://localhost") { IPv6 = false };
            Assert.AreEqual(false, builder.IPv6);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());

            connectionString = "mongodb://localhost/?ipv6=true";
            builder = new MongoUrlBuilder("mongodb://localhost") { IPv6 = true };
            Assert.AreEqual(true, builder.IPv6);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestGuidRepresentationCSharpLegacy()
        {
            var connectionString = "mongodb://localhost/?guids=CSharpLegacy";
            var builder = new MongoUrlBuilder("mongodb://localhost") { GuidRepresentation = GuidRepresentation.CSharpLegacy };
            Assert.AreEqual(GuidRepresentation.CSharpLegacy, builder.GuidRepresentation);
            Assert.AreEqual("mongodb://localhost", builder.ToString());
            Assert.AreEqual("mongodb://localhost", new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestGuidRepresentationPythonLegacy()
        {
            var connectionString = "mongodb://localhost/?guids=PythonLegacy";
            var builder = new MongoUrlBuilder("mongodb://localhost") { GuidRepresentation = GuidRepresentation.PythonLegacy };
            Assert.AreEqual(GuidRepresentation.PythonLegacy, builder.GuidRepresentation);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestGuidRepresentationJavaLegacy()
        {
            var connectionString = "mongodb://localhost/?guids=JavaLegacy";
            var builder = new MongoUrlBuilder("mongodb://localhost") { GuidRepresentation = GuidRepresentation.JavaLegacy };
            Assert.AreEqual(GuidRepresentation.JavaLegacy, builder.GuidRepresentation);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestMaxConnectionIdleTime()
        {
            var connectionString = "mongodb://localhost/?maxIdleTime=123ms";
            var builder = new MongoUrlBuilder("mongodb://localhost") { MaxConnectionIdleTime = TimeSpan.FromMilliseconds(123) };
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), builder.MaxConnectionIdleTime);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestMaxConnectionLifeTime()
        {
            var connectionString = "mongodb://localhost/?maxLifeTime=123ms";
            var builder = new MongoUrlBuilder("mongodb://localhost") { MaxConnectionLifeTime = TimeSpan.FromMilliseconds(123) };
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), builder.MaxConnectionLifeTime);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestMaxConnectionPoolSize()
        {
            string connectionString = "mongodb://localhost/?maxPoolSize=123";
            var builder = new MongoUrlBuilder("mongodb://localhost") { MaxConnectionPoolSize = 123 };
            Assert.AreEqual(123, builder.MaxConnectionPoolSize);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestMinConnectionPoolSize()
        {
            var connectionString = "mongodb://localhost/?minPoolSize=123";
            var builder = new MongoUrlBuilder("mongodb://localhost") { MinConnectionPoolSize = 123 };
            Assert.AreEqual(123, builder.MinConnectionPoolSize);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestReplicaSetName()
        {
            var connectionString = "mongodb://localhost/?connect=replicaSet;replicaSet=name";
            var builder = new MongoUrlBuilder("mongodb://localhost") { ReplicaSetName = "name" };
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual("name", builder.ReplicaSetName);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSafeModeFalse()
        {
            var connectionString = "mongodb://localhost";
            var builder = new MongoUrlBuilder("mongodb://localhost") { SafeMode = SafeMode.False };
            Assert.AreEqual(SafeMode.False, builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSafeModeTrue()
        {
            var connectionString = "mongodb://localhost/?safe=true";
            var builder = new MongoUrlBuilder("mongodb://localhost") { SafeMode = SafeMode.True };
            Assert.AreEqual(SafeMode.True, builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSafeModeFSyncTrue()
        {
            var connectionString = "mongodb://localhost/?safe=true;fsync=true";
            var builder = new MongoUrlBuilder("mongodb://localhost") { SafeMode = SafeMode.FSyncTrue };
            Assert.AreEqual(SafeMode.FSyncTrue, builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSafeModeJTrue()
        {
            var connectionString = "mongodb://localhost/?safe=true;j=true";
            var builder = new MongoUrlBuilder("mongodb://localhost") { SafeMode = new SafeMode(true) { J = true } };
            Assert.AreEqual(true, builder.SafeMode.J);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSafeModeWMajority()
        {
            var connectionString = "mongodb://localhost/?safe=true;w=majority";
            var builder = new MongoUrlBuilder("mongodb://localhost") { SafeMode = new SafeMode(true) { WMode = "majority" } };
            Assert.AreEqual("majority", builder.SafeMode.WMode);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSafeModeW2()
        {
            var connectionString = "mongodb://localhost/?safe=true;w=2";
            var builder = new MongoUrlBuilder("mongodb://localhost") { SafeMode = SafeMode.W2 };
            Assert.AreEqual(SafeMode.W2, builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSafeModeTrueW2WTimeout()
        {
            var connectionString = "mongodb://localhost/?safe=true;w=2;wtimeout=2s";
            var builder = new MongoUrlBuilder("mongodb://localhost") { SafeMode = SafeMode.Create(2, TimeSpan.FromSeconds(2)) };
            Assert.AreEqual(SafeMode.Create(2, TimeSpan.FromSeconds(2)), builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSafeModeTrueFSyncTrueW2()
        {
            var connectionString = "mongodb://localhost/?safe=true;fsync=true;w=2";
            var builder = new MongoUrlBuilder("mongodb://localhost") { SafeMode = SafeMode.Create(true, true, 2) };
            Assert.AreEqual(SafeMode.Create(true, true, 2), builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSafeModeTrueFSyncTrueW2WTimeout()
        {
            var connectionString = "mongodb://localhost/?safe=true;fsync=true;w=2;wtimeout=2s";
            var builder = new MongoUrlBuilder("mongodb://localhost") { SafeMode = SafeMode.Create(true, true, 2, TimeSpan.FromSeconds(2)) };
            Assert.AreEqual(SafeMode.Create(true, true, 2, TimeSpan.FromSeconds(2)), builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSlaveOkFalse()
        {
            var connectionString = "mongodb://localhost";
            var builder = new MongoUrlBuilder("mongodb://localhost") { SlaveOk = false };
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSlaveOkTrue()
        {
            var connectionString = "mongodb://localhost/?slaveOk=true";
            var builder = new MongoUrlBuilder("mongodb://localhost") { SlaveOk = true };
            Assert.AreEqual(true, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSocketTimeout()
        {
            var connectionString = "mongodb://localhost/?socketTimeout=123ms";
            var builder = new MongoUrlBuilder("mongodb://localhost") { SocketTimeout = TimeSpan.FromMilliseconds(123) };
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), builder.SocketTimeout);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestWaitQueueMultiple()
        {
            var connectionString = "mongodb://localhost/?waitQueueMultiple=2";
            var builder = new MongoUrlBuilder("mongodb://localhost") { WaitQueueMultiple = 2 };
            Assert.AreEqual(2, builder.WaitQueueMultiple);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestWaitQueueSize()
        {
            var connectionString = "mongodb://localhost/?waitQueueSize=123";
            var builder = new MongoUrlBuilder("mongodb://localhost") { WaitQueueSize = 123 };
            Assert.AreEqual(123, builder.WaitQueueSize);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }

        [Test]
        public void TestWaitQueueTimeout()
        {
            var connectionString = "mongodb://localhost/?waitQueueTimeout=123ms";
            var builder = new MongoUrlBuilder("mongodb://localhost") { WaitQueueTimeout = TimeSpan.FromMilliseconds(123) };
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), builder.WaitQueueTimeout);
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoUrlBuilder(connectionString).ToString());
        }
    }
}
