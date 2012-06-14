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
    public class MongoConnectionStringBuilderTests
    {
        private MongoServerAddress __localhost = new MongoServerAddress("localhost");
        [Test]
        public void TestDefaults()
        {
            var builder = new MongoConnectionStringBuilder();
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(null, builder.Server);
            Assert.AreEqual(null, builder.Servers);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(MongoDefaults.ConnectTimeout, builder.ConnectTimeout);
            Assert.AreEqual(MongoDefaults.GuidRepresentation, builder.GuidRepresentation);
            Assert.AreEqual(false, builder.IPv6);
            Assert.AreEqual(MongoDefaults.MaxConnectionIdleTime, builder.MaxConnectionIdleTime);
            Assert.AreEqual(MongoDefaults.MaxConnectionLifeTime, builder.MaxConnectionLifeTime);
            Assert.AreEqual(MongoDefaults.MaxConnectionPoolSize, builder.MaxConnectionPoolSize);
            Assert.AreEqual(MongoDefaults.MinConnectionPoolSize, builder.MinConnectionPoolSize);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(MongoDefaults.SocketTimeout, builder.SocketTimeout);
            Assert.AreEqual(MongoDefaults.WaitQueueMultiple, builder.WaitQueueMultiple);
            Assert.AreEqual(MongoDefaults.WaitQueueSize, builder.WaitQueueSize);
            Assert.AreEqual(MongoDefaults.WaitQueueTimeout, builder.WaitQueueTimeout);
            Assert.AreEqual(MongoDefaults.ComputedWaitQueueSize, builder.ComputedWaitQueueSize);

            var connectionString = ""; // not actually a valid connection string because it's missing the host
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestHost()
        {
            var builder = new MongoConnectionStringBuilder() { Server = new MongoServerAddress("mongo.xyz.com") };
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("mongo.xyz.com", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);

            var connectionString = "server=mongo.xyz.com";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestHostWithPort()
        {
            var builder = new MongoConnectionStringBuilder() { Server = new MongoServerAddress("mongo.xyz.com", 12345) };
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("mongo.xyz.com", builder.Server.Host);
            Assert.AreEqual(12345, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);

            var connectionString = "server=mongo.xyz.com:12345";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestTwoHosts()
        {
            var builder = new MongoConnectionStringBuilder()
            {
                Servers = new MongoServerAddress[]
                {                
                    new MongoServerAddress("mongo1.xyz.com"),
                    new MongoServerAddress("mongo2.xyz.com")
                }
            };
            var servers = builder.Servers.ToArray();
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("mongo1.xyz.com", servers[0].Host);
            Assert.AreEqual(27017, servers[0].Port);
            Assert.AreEqual("mongo2.xyz.com", servers[1].Host);
            Assert.AreEqual(27017, servers[1].Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);

            var connectionString = "server=mongo1.xyz.com,mongo2.xyz.com";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestTwoHostsWithPorts()
        {
            var builder = new MongoConnectionStringBuilder()
            {
                Servers = new MongoServerAddress[]
                {                
                    new MongoServerAddress("mongo1.xyz.com", 12345),
                    new MongoServerAddress("mongo2.xyz.com", 23456)
                }
            };
            var servers = builder.Servers.ToArray();
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("mongo1.xyz.com", servers[0].Host);
            Assert.AreEqual(12345, servers[0].Port);
            Assert.AreEqual("mongo2.xyz.com", servers[1].Host);
            Assert.AreEqual(23456, servers[1].Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);

            var connectionString = "server=mongo1.xyz.com:12345,mongo2.xyz.com:23456";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestUsernamePassword()
        {
            var builder = new MongoConnectionStringBuilder()
            {
                Server = __localhost,
                Username = "username",
                Password = "password"
            };
            Assert.AreEqual("username", builder.Username);
            Assert.AreEqual("password", builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);

            var connectionString = "server=localhost;username=username;password=password";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestUsernamePasswordEscaped()
        {
            var builder = new MongoConnectionStringBuilder()
            {
                Server = __localhost,
                Username = "usern;me",
                Password = "p;ssword"
            };
            Assert.AreEqual("usern;me", builder.Username);
            Assert.AreEqual("p;ssword", builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);

            var connectionString = "server=localhost;username=\"usern;me\";password=\"p;ssword\"";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());

            builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual("usern;me", builder.Username);
            Assert.AreEqual("p;ssword", builder.Password);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestUsernamePasswordLocalhostDatabase()
        {
            var builder = new MongoConnectionStringBuilder()
            {
                Server = __localhost,
                Username = "username",
                Password = "password",
                DatabaseName = "database"
            };
            Assert.AreEqual("username", builder.Username);
            Assert.AreEqual("password", builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual("database", builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);

            var connectionString = "server=localhost;username=username;password=password;database=database";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestUsernamePasswordTwoHostsDatabase()
        {
            var builder = new MongoConnectionStringBuilder()
            {
                Servers = new MongoServerAddress[]
                {                
                    new MongoServerAddress("mongo1.xyz.com"),
                    new MongoServerAddress("mongo2.xyz.com")
                },
                Username = "username",
                Password = "password",
                DatabaseName = "database"
            };
            var servers = builder.Servers.ToArray();
            Assert.AreEqual("username", builder.Username);
            Assert.AreEqual("password", builder.Password);
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("mongo1.xyz.com", servers[0].Host);
            Assert.AreEqual(27017, servers[0].Port);
            Assert.AreEqual("mongo2.xyz.com", servers[1].Host);
            Assert.AreEqual(27017, servers[1].Port);
            Assert.AreEqual("database", builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);

            var connectionString = "server=mongo1.xyz.com,mongo2.xyz.com;username=username;password=password;database=database";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestUsernamePasswordTwoHostsWithPortsDatabase()
        {
            var builder = new MongoConnectionStringBuilder()
            {
                Servers = new MongoServerAddress[]
                {                
                    new MongoServerAddress("mongo1.xyz.com", 12345),
                    new MongoServerAddress("mongo2.xyz.com", 23456)
                },
                Username = "username",
                Password = "password",
                DatabaseName = "database"
            };
            var servers = builder.Servers.ToArray();
            Assert.AreEqual("username", builder.Username);
            Assert.AreEqual("password", builder.Password);
            Assert.AreEqual(2, servers.Length);
            Assert.AreEqual("mongo1.xyz.com", servers[0].Host);
            Assert.AreEqual(12345, servers[0].Port);
            Assert.AreEqual("mongo2.xyz.com", servers[1].Host);
            Assert.AreEqual(23456, servers[1].Port);
            Assert.AreEqual("database", builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);

            var connectionString = "server=mongo1.xyz.com:12345,mongo2.xyz.com:23456;username=username;password=password;database=database";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestConnectionModeDirect()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, ConnectionMode = ConnectionMode.Direct };
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);

            var connectionString = "server=localhost;connect=direct";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestConnectionModeReplicaSet()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, ConnectionMode = ConnectionMode.ReplicaSet };
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);

            var connectionString = "server=localhost;connect=replicaSet";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestConnectTimeout()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, ConnectTimeout = TimeSpan.FromMilliseconds(500) };
            Assert.AreEqual(TimeSpan.FromMilliseconds(500), builder.ConnectTimeout);
            var connectionString = "server=localhost;connectTimeout=500ms";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=500ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=0.5").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=0.5s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=00:00:00.500").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeoutMS=500").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, ConnectTimeout = TimeSpan.FromSeconds(30) };
            Assert.AreEqual(TimeSpan.FromSeconds(30), builder.ConnectTimeout);
            connectionString = "server=localhost;connectTimeout=30s";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=30000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=30s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=0.5m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=00:00:30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeoutMS=30000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, ConnectTimeout = TimeSpan.FromMinutes(30) };
            Assert.AreEqual(TimeSpan.FromMinutes(30), builder.ConnectTimeout);
            connectionString = "server=localhost;connectTimeout=30m";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=1800000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=1800").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=1800s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=30m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=0.5h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=00:30:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeoutMS=1800000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, ConnectTimeout = TimeSpan.FromHours(1) };
            Assert.AreEqual(TimeSpan.FromHours(1), builder.ConnectTimeout);
            connectionString = "server=localhost;connectTimeout=1h";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=3600000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=3600").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=3600s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=60m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=1h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=01:00:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeoutMS=3600000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, ConnectTimeout = new TimeSpan(1, 2, 3) };
            Assert.AreEqual(new TimeSpan(1, 2, 3), builder.ConnectTimeout);
            connectionString = "server=localhost;connectTimeout=01:02:03";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=3723000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=3723").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=3723s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeout=01:02:03").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;connectTimeoutMS=3723000").ToString());
        }

        [Test]
        public void TestGuidRepresentationCSharpLegacy()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, GuidRepresentation = GuidRepresentation.CSharpLegacy };
            Assert.AreEqual(GuidRepresentation.CSharpLegacy, builder.GuidRepresentation);

            var connectionString = "server=localhost;uuidRepresentation=CSharpLegacy";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;guids=CSharpLegacy").ToString());
        }

        [Test]
        public void TestGuidRepresentationPythonLegacy()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, GuidRepresentation = GuidRepresentation.PythonLegacy };
            Assert.AreEqual(GuidRepresentation.PythonLegacy, builder.GuidRepresentation);

            var connectionString = "server=localhost;uuidRepresentation=PythonLegacy";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;guids=PythonLegacy").ToString());
        }

        [Test]
        public void TestGuidRepresentationJavaLegacy()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, GuidRepresentation = GuidRepresentation.JavaLegacy };
            Assert.AreEqual(GuidRepresentation.JavaLegacy, builder.GuidRepresentation);

            var connectionString = "server=localhost;uuidRepresentation=JavaLegacy";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;guids=JavaLegacy").ToString());
        }

        [Test]
        public void TestGuidRepresentationStandard()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, GuidRepresentation = GuidRepresentation.Standard };
            Assert.AreEqual(GuidRepresentation.Standard, builder.GuidRepresentation);

            var connectionString = "server=localhost;uuidRepresentation=Standard";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;guids=Standard").ToString());
        }

        [Test]
        public void TestIpV6False()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, IPv6 = false };
            Assert.AreEqual(false, builder.IPv6);

            var connectionString = "server=localhost;ipv6=false";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestIpV6True()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, IPv6 = true };
            Assert.AreEqual(true, builder.IPv6);

            var connectionString = "server=localhost;ipv6=true";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestMaxConnectionIdleTime()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, MaxConnectionIdleTime = TimeSpan.FromMilliseconds(500) };
            Assert.AreEqual(TimeSpan.FromMilliseconds(500), builder.MaxConnectionIdleTime);
            var connectionString = "server=localhost;maxIdleTime=500ms";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=500ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=0.5").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=0.5s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=00:00:00.500").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTimeMS=500").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, MaxConnectionIdleTime = TimeSpan.FromSeconds(30) };
            Assert.AreEqual(TimeSpan.FromSeconds(30), builder.MaxConnectionIdleTime);
            connectionString = "server=localhost;maxIdleTime=30s";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=30000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=30s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=0.5m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=00:00:30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTimeMS=30000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, MaxConnectionIdleTime = TimeSpan.FromMinutes(30) };
            Assert.AreEqual(TimeSpan.FromMinutes(30), builder.MaxConnectionIdleTime);
            connectionString = "server=localhost;maxIdleTime=30m";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=1800000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=1800").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=1800s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=30m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=0.5h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=00:30:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTimeMS=1800000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, MaxConnectionIdleTime = TimeSpan.FromHours(1) };
            Assert.AreEqual(TimeSpan.FromHours(1), builder.MaxConnectionIdleTime);
            connectionString = "server=localhost;maxIdleTime=1h";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=3600000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=3600").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=3600s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=60m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=1h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=01:00:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTimeMS=3600000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, MaxConnectionIdleTime = new TimeSpan(1, 2, 3) };
            Assert.AreEqual(new TimeSpan(1, 2, 3), builder.MaxConnectionIdleTime);
            connectionString = "server=localhost;maxIdleTime=01:02:03";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=3723000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=3723").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=3723s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTime=01:02:03").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxIdleTimeMS=3723000").ToString());
        }

        [Test]
        public void TestMaxConnectionLifeTime()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, MaxConnectionLifeTime = TimeSpan.FromMilliseconds(500) };
            Assert.AreEqual(TimeSpan.FromMilliseconds(500), builder.MaxConnectionLifeTime);
            var connectionString = "server=localhost;maxLifeTime=500ms";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=500ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=0.5").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=0.5s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=00:00:00.500").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTimeMS=500").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, MaxConnectionLifeTime = TimeSpan.FromSeconds(30) };
            Assert.AreEqual(TimeSpan.FromSeconds(30), builder.MaxConnectionLifeTime);
            connectionString = "server=localhost;maxLifeTime=30s";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=30000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=30s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=0.5m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=00:00:30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTimeMS=30000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, MaxConnectionLifeTime = TimeSpan.FromMinutes(30) };
            Assert.AreEqual(TimeSpan.FromMinutes(30), builder.MaxConnectionLifeTime);
            connectionString = "server=localhost;maxLifeTime=30m";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=1800000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=1800").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=1800s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=30m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=0.5h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=00:30:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTimeMS=1800000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, MaxConnectionLifeTime = TimeSpan.FromHours(1) };
            Assert.AreEqual(TimeSpan.FromHours(1), builder.MaxConnectionLifeTime);
            connectionString = "server=localhost;maxLifeTime=1h";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=3600000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=3600").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=3600s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=60m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=1h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=01:00:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTimeMS=3600000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, MaxConnectionLifeTime = new TimeSpan(1, 2, 3) };
            Assert.AreEqual(new TimeSpan(1, 2, 3), builder.MaxConnectionLifeTime);
            connectionString = "server=localhost;maxLifeTime=01:02:03";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=3723000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=3723").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=3723s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTime=01:02:03").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;maxLifeTimeMS=3723000").ToString());
        }

        [Test]
        public void TestMaxConnectionPoolSize()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, MaxConnectionPoolSize = 123 };
            Assert.AreEqual(123, builder.MaxConnectionPoolSize);

            var connectionString = "server=localhost;maxPoolSize=123";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestMinConnectionPoolSize()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, MinConnectionPoolSize = 123 };
            Assert.AreEqual(123, builder.MinConnectionPoolSize);

            var connectionString = "server=localhost;minPoolSize=123";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestReplicaSetName()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, ReplicaSetName = "name" };
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual("name", builder.ReplicaSetName);

            var connectionString = "server=localhost;connect=replicaSet;replicaSet=name";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;replicaSet=name").ToString());
        }

        [Test]
        public void TestSafeModeFalse()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) };
            Assert.AreEqual(false, builder.SafeMode.Enabled);

            var connectionString = "server=localhost;safe=false";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSafeModeTrue()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(true) };
            Assert.AreEqual(true, builder.SafeMode.Enabled);

            var connectionString = "server=localhost;safe=true";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSafeModeFSyncFalse()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { FSync = false } };
            Assert.AreEqual(false, builder.SafeMode.Enabled);
            Assert.AreEqual(false, builder.SafeMode.FSync);

            var connectionString = "server=localhost;safe=false";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;safe=false;fsync=false").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=false").ToString());
        }

        [Test]
        public void TestSafeModeFSyncTrue()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { FSync = true } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(true, builder.SafeMode.FSync);

            var connectionString = "server=localhost;safe=true;fsync=true";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true").ToString());
        }

        [Test]
        public void TestSafeModeJFalse()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { Journal = false } };
            Assert.AreEqual(false, builder.SafeMode.Enabled);
            Assert.AreEqual(false, builder.SafeMode.Journal);

            var connectionString = "server=localhost;safe=false";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;safe=false;journal=false").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;safe=false;j=false").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;journal=false").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;j=false").ToString());
        }

        [Test]
        public void TestSafeModeJTrue()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { Journal = true } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(true, builder.SafeMode.Journal);

            var connectionString = "server=localhost;safe=true;journal=true";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;safe=true;j=true").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;journal=true").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;j=true").ToString());
        }

        [Test]
        public void TestSafeModeWMajority()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { WMode = "majority" } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual("majority", builder.SafeMode.WMode);

            var connectionString = "server=localhost;safe=true;w=majority";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=majority").ToString());
        }

        [Test]
        public void TestSafeModeW2()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { W = 2 } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(2, builder.SafeMode.W);

            var connectionString = "server=localhost;safe=true;w=2";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2").ToString());
        }

        [Test]
        public void TestSafeModeTrueW2WTimeout()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { W = 2, WTimeout = TimeSpan.FromMilliseconds(500) } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(2, builder.SafeMode.W);
            Assert.AreEqual(TimeSpan.FromMilliseconds(500), builder.SafeMode.WTimeout);
            var connectionString = "server=localhost;safe=true;w=2;wtimeout=500ms";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=500ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=0.5").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=0.5s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=00:00:00.500").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeoutMS=500").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { W = 2, WTimeout = TimeSpan.FromSeconds(30) } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(2, builder.SafeMode.W);
            Assert.AreEqual(TimeSpan.FromSeconds(30), builder.SafeMode.WTimeout);
            connectionString = "server=localhost;safe=true;w=2;wtimeout=30s";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=30000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=30s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=0.5m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=00:00:30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeoutMS=30000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { W = 2, WTimeout = TimeSpan.FromMinutes(30) } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(2, builder.SafeMode.W);
            Assert.AreEqual(TimeSpan.FromMinutes(30), builder.SafeMode.WTimeout);
            connectionString = "server=localhost;safe=true;w=2;wtimeout=30m";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=1800000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=1800").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=1800s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=30m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=0.5h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=00:30:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeoutMS=1800000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { W = 2, WTimeout = TimeSpan.FromHours(1) } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(2, builder.SafeMode.W);
            Assert.AreEqual(TimeSpan.FromHours(1), builder.SafeMode.WTimeout);
            connectionString = "server=localhost;safe=true;w=2;wtimeout=1h";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=3600000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=3600").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=3600s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=60m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=1h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=01:00:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeoutMS=3600000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { W = 2, WTimeout = new TimeSpan(1, 2, 3) } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(2, builder.SafeMode.W);
            Assert.AreEqual(new TimeSpan(1, 2, 3), builder.SafeMode.WTimeout);
            connectionString = "server=localhost;safe=true;w=2;wtimeout=01:02:03";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=3723000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=3723").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=3723s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeout=01:02:03").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2;wtimeoutMS=3723000").ToString());
        }

        [Test]
        public void TestSafeModeTrueFSyncFalseW2()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { FSync = false, W = 2 } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(false, builder.SafeMode.FSync);
            Assert.AreEqual(2, builder.SafeMode.W);

            var connectionString = "server=localhost;safe=true;w=2";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;safe=true;fsync=false;w=2").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;safe=true;w=2").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=false;w=2").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;w=2").ToString());
        }

        [Test]
        public void TestSafeModeTrueFSyncTrueW2()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { FSync = true, W = 2 } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(true, builder.SafeMode.FSync);
            Assert.AreEqual(2, builder.SafeMode.W);

            var connectionString = "server=localhost;safe=true;fsync=true;w=2";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;safe=true;fsync=true;w=2").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2").ToString());
        }

        [Test]
        public void TestSafeModeTrueFSyncTrueW2WTimeout()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { FSync = true, W =2, WTimeout = TimeSpan.FromMilliseconds(500) } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(true, builder.SafeMode.FSync);
            Assert.AreEqual(2, builder.SafeMode.W);
            Assert.AreEqual(TimeSpan.FromMilliseconds(500), builder.SafeMode.WTimeout);
            var connectionString = "server=localhost;safe=true;fsync=true;w=2;wtimeout=500ms";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=500ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=0.5").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=0.5s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=00:00:00.500").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeoutMS=500").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { FSync = true, W =2, WTimeout = TimeSpan.FromSeconds(30) } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(true, builder.SafeMode.FSync);
            Assert.AreEqual(2, builder.SafeMode.W);
            Assert.AreEqual(TimeSpan.FromSeconds(30), builder.SafeMode.WTimeout);
            connectionString = "server=localhost;safe=true;fsync=true;w=2;wtimeout=30s";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=30000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=30s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=0.5m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=00:00:30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeoutMS=30000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { FSync = true, W =2, WTimeout = TimeSpan.FromMinutes(30) } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(true, builder.SafeMode.FSync);
            Assert.AreEqual(2, builder.SafeMode.W);
            Assert.AreEqual(TimeSpan.FromMinutes(30), builder.SafeMode.WTimeout);
            connectionString = "server=localhost;safe=true;fsync=true;w=2;wtimeout=30m";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=1800000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=1800").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=1800s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=30m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=0.5h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=00:30:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeoutMS=1800000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { FSync = true, W =2, WTimeout = TimeSpan.FromHours(1) } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(true, builder.SafeMode.FSync);
            Assert.AreEqual(2, builder.SafeMode.W);
            Assert.AreEqual(TimeSpan.FromHours(1), builder.SafeMode.WTimeout);
            connectionString = "server=localhost;safe=true;fsync=true;w=2;wtimeout=1h";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=3600000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=3600").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=3600s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=60m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=1h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=01:00:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeoutMS=3600000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, SafeMode = new SafeMode(false) { FSync = true, W =2, WTimeout = new TimeSpan(1, 2, 3) } };
            Assert.AreEqual(true, builder.SafeMode.Enabled);
            Assert.AreEqual(true, builder.SafeMode.FSync);
            Assert.AreEqual(2, builder.SafeMode.W);
            Assert.AreEqual(new TimeSpan(1, 2, 3), builder.SafeMode.WTimeout);
            connectionString = "server=localhost;safe=true;fsync=true;w=2;wtimeout=01:02:03";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=3723000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=3723").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=3723s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeout=01:02:03").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;fsync=true;w=2;wtimeoutMS=3723000").ToString());
        }

        [Test]
        public void TestSlaveOkFalse()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SlaveOk = false };
            Assert.AreEqual(false, builder.SlaveOk);

            var connectionString = "server=localhost;slaveOk=false";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSlaveOkTrue()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SlaveOk = true };
            Assert.AreEqual(true, builder.SlaveOk);

            var connectionString = "server=localhost;slaveOk=true";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
        }

        [Test]
        public void TestSocketTimeout()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, SocketTimeout = TimeSpan.FromMilliseconds(500) };
            Assert.AreEqual(TimeSpan.FromMilliseconds(500), builder.SocketTimeout);
            var connectionString = "server=localhost;socketTimeout=500ms";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=500ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=0.5").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=0.5s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=00:00:00.500").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeoutMS=500").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, SocketTimeout = TimeSpan.FromSeconds(30) };
            Assert.AreEqual(TimeSpan.FromSeconds(30), builder.SocketTimeout);
            connectionString = "server=localhost;socketTimeout=30s";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=30000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=30s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=0.5m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=00:00:30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeoutMS=30000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, SocketTimeout = TimeSpan.FromMinutes(30) };
            Assert.AreEqual(TimeSpan.FromMinutes(30), builder.SocketTimeout);
            connectionString = "server=localhost;socketTimeout=30m";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=1800000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=1800").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=1800s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=30m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=0.5h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=00:30:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeoutMS=1800000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, SocketTimeout = TimeSpan.FromHours(1) };
            Assert.AreEqual(TimeSpan.FromHours(1), builder.SocketTimeout);
            connectionString = "server=localhost;socketTimeout=1h";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=3600000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=3600").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=3600s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=60m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=1h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=01:00:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeoutMS=3600000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, SocketTimeout = new TimeSpan(1, 2, 3) };
            Assert.AreEqual(new TimeSpan(1, 2, 3), builder.SocketTimeout);
            connectionString = "server=localhost;socketTimeout=01:02:03";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=3723000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=3723").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=3723s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeout=01:02:03").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;socketTimeoutMS=3723000").ToString());
        }

        [Test]
        public void TestWaitQueueMultiple()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, WaitQueueSize = 123, WaitQueueMultiple = 2.0 };
            Assert.AreEqual(2.0, builder.WaitQueueMultiple);
            Assert.AreEqual(0, builder.WaitQueueSize);

            var connectionString = "server=localhost;waitQueueMultiple=2";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueSize=123;waitQueueMultiple=2").ToString());
        }

        [Test]
        public void TestWaitQueueSize()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, WaitQueueMultiple = 2.0, WaitQueueSize = 123 };
            Assert.AreEqual(0.0, builder.WaitQueueMultiple);
            Assert.AreEqual(123, builder.WaitQueueSize);

            var connectionString = "server=localhost;waitQueueSize=123";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder(connectionString).ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueMultiple=2;waitQueueSize=123").ToString());
        }

        [Test]
        public void TestWaitQueueTimeout()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, WaitQueueTimeout = TimeSpan.FromMilliseconds(500) };
            Assert.AreEqual(TimeSpan.FromMilliseconds(500), builder.WaitQueueTimeout);
            var connectionString = "server=localhost;waitQueueTimeout=500ms";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=500ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=0.5").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=0.5s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=00:00:00.500").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeoutMS=500").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, WaitQueueTimeout = TimeSpan.FromSeconds(30) };
            Assert.AreEqual(TimeSpan.FromSeconds(30), builder.WaitQueueTimeout);
            connectionString = "server=localhost;waitQueueTimeout=30s";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=30000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=30s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=0.5m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=00:00:30").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeoutMS=30000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, WaitQueueTimeout = TimeSpan.FromMinutes(30) };
            Assert.AreEqual(TimeSpan.FromMinutes(30), builder.WaitQueueTimeout);
            connectionString = "server=localhost;waitQueueTimeout=30m";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=1800000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=1800").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=1800s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=30m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=0.5h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=00:30:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeoutMS=1800000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, WaitQueueTimeout = TimeSpan.FromHours(1) };
            Assert.AreEqual(TimeSpan.FromHours(1), builder.WaitQueueTimeout);
            connectionString = "server=localhost;waitQueueTimeout=1h";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=3600000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=3600").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=3600s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=60m").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=1h").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=01:00:00").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeoutMS=3600000").ToString());

            builder = new MongoConnectionStringBuilder() { Server = __localhost, WaitQueueTimeout = new TimeSpan(1, 2, 3) };
            Assert.AreEqual(new TimeSpan(1, 2, 3), builder.WaitQueueTimeout);
            connectionString = "server=localhost;waitQueueTimeout=01:02:03";
            Assert.AreEqual(connectionString, builder.ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=3723000ms").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=3723").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=3723s").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeout=01:02:03").ToString());
            Assert.AreEqual(connectionString, new MongoConnectionStringBuilder("server=localhost;waitQueueTimeoutMS=3723000").ToString());
        }


        [Test]
        public void TestComputedWaitQueueSizeUsingMultiple()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, MaxConnectionPoolSize = 100, WaitQueueMultiple = 2.0 };
            Assert.AreEqual(200, builder.ComputedWaitQueueSize);
        }

        [Test]
        public void TestComputedWaitQueueSizeUsingSize()
        {
            var builder = new MongoConnectionStringBuilder() { Server = __localhost, WaitQueueSize = 123 };
            Assert.AreEqual(123, builder.ComputedWaitQueueSize);
        }

        [Test]
        public void TestAll()
        {
            var connectionString = "server=localhost;connect=replicaSet;replicaSet=name;slaveOk=true;safe=true;fsync=true;journal=true;w=2;wtimeout=2s;uuidRepresentation=PythonLegacy";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual("name", builder.ReplicaSetName);
            Assert.AreEqual(GuidRepresentation.PythonLegacy, builder.GuidRepresentation);
            Assert.AreEqual(new SafeMode(true) { FSync = true, Journal = true, W = 2, WTimeout = TimeSpan.FromSeconds(2) }, builder.SafeMode);
            Assert.AreEqual(true, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }
    }
}
