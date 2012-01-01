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
        [Test]
        public void TestDefaults()
        {
            string connectionString = "server=localhost";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Servers.Single().Host);
            Assert.AreEqual(27017, builder.Servers.Single().Port);
            Assert.IsNull(builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(MongoDefaults.ConnectTimeout, builder.ConnectTimeout);
            Assert.AreEqual(MongoDefaults.GuidRepresentation, builder.GuidRepresentation);
            Assert.AreEqual(false, builder.IPv6);
            Assert.AreEqual(MongoDefaults.MaxConnectionIdleTime, builder.MaxConnectionIdleTime);
            Assert.AreEqual(MongoDefaults.MaxConnectionLifeTime, builder.MaxConnectionLifeTime);
            Assert.AreEqual(MongoDefaults.MaxConnectionPoolSize, builder.MaxConnectionPoolSize);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.IsNull(builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(MongoDefaults.SocketTimeout, builder.SocketTimeout);
            Assert.AreEqual(MongoDefaults.WaitQueueMultiple, builder.WaitQueueMultiple);
            Assert.AreEqual(MongoDefaults.WaitQueueSize, builder.WaitQueueSize);
            Assert.AreEqual(MongoDefaults.WaitQueueTimeout, builder.WaitQueueTimeout);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestHost()
        {
            string connectionString = "server=mongo.xyz.com";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("mongo.xyz.com", builder.Servers.Single().Host);
            Assert.AreEqual(27017, builder.Servers.Single().Port);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestHostWithPort()
        {
            string connectionString = "server=mongo.xyz.com:12345";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("mongo.xyz.com", builder.Servers.Single().Host);
            Assert.AreEqual(12345, builder.Servers.Single().Port);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestTwoHosts()
        {
            string connectionString = "server=mongo1.xyz.com,mongo2.xyz.com";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(2, builder.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", builder.Servers.First().Host);
            Assert.AreEqual(27017, builder.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", builder.Servers.Skip(1).Single().Host);
            Assert.AreEqual(27017, builder.Servers.Skip(1).Single().Port);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestTwoHostsWithPorts()
        {
            string connectionString = "server=mongo1.xyz.com:12345,mongo2.xyz.com:23456";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(2, builder.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", builder.Servers.First().Host);
            Assert.AreEqual(12345, builder.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", builder.Servers.Skip(1).Single().Host);
            Assert.AreEqual(23456, builder.Servers.Skip(1).Single().Port);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestUsernamePasswordLocalhostDatabase()
        {
            string connectionString = "server=localhost;database=database;username=username;password=password";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual("username", builder.Username);
            Assert.AreEqual("password", builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Servers.Single().Host);
            Assert.AreEqual(27017, builder.Servers.Single().Port);
            Assert.AreEqual("database", builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestUsernamePasswordTwoHostsDatabase()
        {
            string connectionString = "server=mongo1.xyz.com,mongo2.xyz.com;database=database;username=username;password=password";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual("username", builder.Username);
            Assert.AreEqual("password", builder.Password);
            Assert.AreEqual(2, builder.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", builder.Servers.First().Host);
            Assert.AreEqual(27017, builder.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", builder.Servers.Skip(1).Single().Host);
            Assert.AreEqual(27017, builder.Servers.Skip(1).Single().Port);
            Assert.AreEqual("database", builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestUsernamePasswordTwoHostsWithPortsDatabase()
        {
            string connectionString = "server=mongo1.xyz.com:12345,mongo2.xyz.com:23456;database=database;username=username;password=password";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual("username", builder.Username);
            Assert.AreEqual("password", builder.Password);
            Assert.AreEqual(2, builder.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", builder.Servers.First().Host);
            Assert.AreEqual(12345, builder.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", builder.Servers.Skip(1).Single().Host);
            Assert.AreEqual(23456, builder.Servers.Skip(1).Single().Port);
            Assert.AreEqual("database", builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestConnectionMode()
        {
            string connectionString = "server=localhost;connect=direct";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(connectionString, builder.ToString());

            connectionString = "server=localhost;connect=replicaSet";
            builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestConnectTimeout()
        {
            string connectionString = "server=localhost;connectTimeout=123";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(TimeSpan.FromSeconds(123), builder.ConnectTimeout);
            Assert.AreEqual(connectionString + "s", builder.ToString()); // "s" units added

            connectionString = "server=localhost;connectTimeout=123ms";
            builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), builder.ConnectTimeout);
            Assert.AreEqual(connectionString, builder.ToString());

            connectionString = "server=localhost;connectTimeout=123s";
            builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(TimeSpan.FromSeconds(123), builder.ConnectTimeout);
            Assert.AreEqual(connectionString, builder.ToString());

            connectionString = "server=localhost;connectTimeout=123m";
            builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(TimeSpan.FromMinutes(123), builder.ConnectTimeout);
            Assert.AreEqual(connectionString, builder.ToString());

            connectionString = "server=localhost;connectTimeout=123h";
            builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(TimeSpan.FromHours(123), builder.ConnectTimeout);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestGuidRepresentationCSharpLegacy()
        {
            string connectionString = "server=localhost;guids=CSharpLegacy";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(GuidRepresentation.CSharpLegacy, builder.GuidRepresentation);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestGuidRepresentationPythonLegacy()
        {
            string connectionString = "server=localhost;guids=PythonLegacy";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(GuidRepresentation.PythonLegacy, builder.GuidRepresentation);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestGuidRepresentationJavaLegacy()
        {
            string connectionString = "server=localhost;guids=JavaLegacy";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(GuidRepresentation.JavaLegacy, builder.GuidRepresentation);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestIpV6()
        {
            string connectionString = "server=localhost;ipv6=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(true, builder.IPv6);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestMaxConnectionIdleTime()
        {
            string connectionString = "server=localhost;maxIdleTime=123ms";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), builder.MaxConnectionIdleTime);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestMaxConnectionLifeTime()
        {
            string connectionString = "server=localhost;maxLifeTime=123ms";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), builder.MaxConnectionLifeTime);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestMaxConnectionPoolSize()
        {
            string connectionString = "server=localhost;maxPoolSize=123";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(123, builder.MaxConnectionPoolSize);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestMinConnectionPoolSize()
        {
            string connectionString = "server=localhost;minPoolSize=123";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(123, builder.MinConnectionPoolSize);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestReplicaSetName()
        {
            string connectionString = "server=localhost;replicaSet=name";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual("name", builder.ReplicaSetName);
            Assert.AreEqual("server=localhost;connect=replicaSet;replicaSet=name", builder.ToString()); // connect=replicaSet added
        }

        [Test]
        public void TestSafeModeFalse()
        {
            string connectionString = "server=localhost;safe=false";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(SafeMode.False, builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeTrue()
        {
            string connectionString = "server=localhost;safe=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(SafeMode.True, builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeFSyncFalse()
        {
            string connectionString = "server=localhost;safe=true;fsync=false";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(false, builder.SafeMode.FSync);
            Assert.AreEqual("server=localhost;safe=true", builder.ToString()); // fsync=false dropped
        }

        [Test]
        public void TestSafeModeFSyncTrue()
        {
            string connectionString = "server=localhost;safe=true;fsync=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(true, builder.SafeMode.FSync);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeJFalse()
        {
            string connectionString = "server=localhost;safe=true;j=false";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(false, builder.SafeMode.J);
            Assert.AreEqual("server=localhost;safe=true", builder.ToString()); // j=false dropped
        }

        [Test]
        public void TestSafeModeJTrue()
        {
            string connectionString = "server=localhost;safe=true;j=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(true, builder.SafeMode.J);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeW2()
        {
            string connectionString = "server=localhost;w=2";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(SafeMode.W2, builder.SafeMode);
            Assert.AreEqual("server=localhost;safe=true;w=2", builder.ToString()); // safe=true added
        }

        [Test]
        public void TestSafeModeTrueW2()
        {
            string connectionString = "server=localhost;safe=true;w=2";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(SafeMode.W2, builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeWMajority()
        {
            string connectionString = "server=localhost;w=majority";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(new SafeMode(true) { WMode = "majority" }, builder.SafeMode);
            Assert.AreEqual("server=localhost;safe=true;w=majority", builder.ToString()); // safe=true added
        }

        [Test]
        public void TestSafeModeTrueWMajority()
        {
            string connectionString = "server=localhost;safe=true;w=majority";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(new SafeMode(true) { WMode = "majority" }, builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeTrueW2WTimeout()
        {
            string connectionString = "server=localhost;safe=true;w=2;wtimeout=2s";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(SafeMode.Create(2, TimeSpan.FromSeconds(2)), builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeTrueFSyncTrueW2()
        {
            string connectionString = "server=localhost;safe=true;fsync=true;w=2";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(SafeMode.Create(true, true, 2), builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeTrueFSyncTrueW2WTimeout()
        {
            string connectionString = "server=localhost;safe=true;fsync=true;w=2;wtimeout=2s";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(SafeMode.Create(true, true, 2, TimeSpan.FromSeconds(2)), builder.SafeMode);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSlaveOkFalse()
        {
            string connectionString = "server=localhost;slaveOk=false";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSlaveOkTrue()
        {
            string connectionString = "server=localhost;slaveOk=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(true, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSocketTimeout()
        {
            string connectionString = "server=localhost;socketTimeout=123ms";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), builder.SocketTimeout);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestWaitQueueMultiple()
        {
            string connectionString = "server=localhost;waitQueueMultiple=2";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(2, builder.WaitQueueMultiple);
            Assert.AreEqual(0, builder.WaitQueueSize);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestWaitQueueSize()
        {
            string connectionString = "server=localhost;waitQueueSize=123";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(0, builder.WaitQueueMultiple);
            Assert.AreEqual(123, builder.WaitQueueSize);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestWaitQueueTimeout()
        {
            string connectionString = "server=localhost;waitQueueTimeout=123ms";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), builder.WaitQueueTimeout);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestAll()
        {
            string connectionString = "server=localhost;connect=replicaSet;replicaSet=name;slaveOk=true;safe=true;fsync=true;j=true;w=2;wtimeout=2s;guids=PythonLegacy";
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
            Assert.AreEqual(new SafeMode(true) { FSync = true, J = true, W = 2, WTimeout = TimeSpan.FromSeconds(2) }, builder.SafeMode);
            Assert.AreEqual(true, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }
    }
}
