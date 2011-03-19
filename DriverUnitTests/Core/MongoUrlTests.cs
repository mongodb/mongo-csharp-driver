﻿/* Copyright 2010-2011 10gen Inc.
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

using MongoDB.Driver;

namespace MongoDB.DriverUnitTests {
    [TestFixture]
    public class MongoUrlTests {
        [Test]
        public void TestDefaults() {
            string connectionString = "mongodb://localhost";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.DefaultCredentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Servers.Single().Host);
            Assert.AreEqual(27017, url.Servers.Single().Port);
            Assert.IsNull(url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(MongoDefaults.ConnectTimeout, url.ConnectTimeout);
            Assert.AreEqual(false, url.IPv6);
            Assert.AreEqual(MongoDefaults.MaxConnectionIdleTime, url.MaxConnectionIdleTime);
            Assert.AreEqual(MongoDefaults.MaxConnectionLifeTime, url.MaxConnectionLifeTime);
            Assert.AreEqual(MongoDefaults.MaxConnectionPoolSize, url.MaxConnectionPoolSize);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(MongoDefaults.SocketTimeout, url.SocketTimeout);
            Assert.AreEqual(MongoDefaults.WaitQueueMultiple, url.WaitQueueMultiple);
            Assert.AreEqual(MongoDefaults.WaitQueueSize, url.WaitQueueSize);
            Assert.AreEqual(MongoDefaults.WaitQueueTimeout, url.WaitQueueTimeout);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestHost() {
            string connectionString = "mongodb://mongo.xyz.com";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.DefaultCredentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("mongo.xyz.com", url.Servers.Single().Host);
            Assert.AreEqual(27017, url.Servers.Single().Port);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestHostWithPort() {
            string connectionString = "mongodb://mongo.xyz.com:12345";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.DefaultCredentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("mongo.xyz.com", url.Servers.Single().Host);
            Assert.AreEqual(12345, url.Servers.Single().Port);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestTwoHosts() {
            string connectionString = "mongodb://mongo1.xyz.com,mongo2.xyz.com";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.DefaultCredentials);
            Assert.AreEqual(2, url.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", url.Servers.First().Host);
            Assert.AreEqual(27017, url.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", url.Servers.Skip(1).Single().Host);
            Assert.AreEqual(27017, url.Servers.Skip(1).Single().Port);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestTwoHostsWithPorts() {
            string connectionString = "mongodb://mongo1.xyz.com:12345,mongo2.xyz.com:23456";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.DefaultCredentials);
            Assert.AreEqual(2, url.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", url.Servers.First().Host);
            Assert.AreEqual(12345, url.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", url.Servers.Skip(1).Single().Host);
            Assert.AreEqual(23456, url.Servers.Skip(1).Single().Port);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestUsernamePasswordLocalhostDatabase() {
            string connectionString = "mongodb://username:password@localhost/database";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual("username", url.DefaultCredentials.Username);
            Assert.AreEqual("password", url.DefaultCredentials.Password);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Servers.Single().Host);
            Assert.AreEqual(27017, url.Servers.Single().Port);
            Assert.AreEqual("database", url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestUsernamePasswordTwoHostsDatabase() {
            string connectionString = "mongodb://username:password@mongo1.xyz.com,mongo2.xyz.com/database";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual("username", url.DefaultCredentials.Username);
            Assert.AreEqual("password", url.DefaultCredentials.Password);
            Assert.AreEqual(2, url.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", url.Servers.First().Host);
            Assert.AreEqual(27017, url.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", url.Servers.Skip(1).Single().Host);
            Assert.AreEqual(27017, url.Servers.Skip(1).Single().Port);
            Assert.AreEqual("database", url.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestUsernamePasswordTwoHostsWithPortsDatabase() {
            string connectionString = "mongodb://username:password@mongo1.xyz.com:12345,mongo2.xyz.com:23456/database";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual("username", url.DefaultCredentials.Username);
            Assert.AreEqual("password", url.DefaultCredentials.Password);
            Assert.AreEqual(2, url.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", url.Servers.First().Host);
            Assert.AreEqual(12345, url.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", url.Servers.Skip(1).Single().Host);
            Assert.AreEqual(23456, url.Servers.Skip(1).Single().Port);
            Assert.AreEqual("database", url.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestConnectionMode() {
            string connectionString = "mongodb://localhost/?connect=direct";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual("mongodb://localhost", url.ToString()); // connect=direct dropped

            connectionString = "mongodb://localhost/?connect=replicaSet";
            url = new MongoUrl(connectionString);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestConnectTimeout() {
            string connectionString = "mongodb://localhost/?connectTimeout=123";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(TimeSpan.FromSeconds(123), url.ConnectTimeout);
            Assert.AreEqual(connectionString + "s", url.ToString()); // "s" units added

            connectionString = "mongodb://localhost/?connectTimeout=123ms";
            url = new MongoUrl(connectionString);
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), url.ConnectTimeout);
            Assert.AreEqual(connectionString, url.ToString());

            connectionString = "mongodb://localhost/?connectTimeout=123s";
            url = new MongoUrl(connectionString);
            Assert.AreEqual(TimeSpan.FromSeconds(123), url.ConnectTimeout);
            Assert.AreEqual(connectionString, url.ToString());

            connectionString = "mongodb://localhost/?connectTimeout=123m";
            url = new MongoUrl(connectionString);
            Assert.AreEqual(TimeSpan.FromMinutes(123), url.ConnectTimeout);
            Assert.AreEqual(connectionString, url.ToString());

            connectionString = "mongodb://localhost/?connectTimeout=123h";
            url = new MongoUrl(connectionString);
            Assert.AreEqual(TimeSpan.FromHours(123), url.ConnectTimeout);
            Assert.AreEqual(connectionString, url.ToString());

            connectionString = "mongodb://localhost/?connectTimeoutMS=123";
            url = new MongoUrl(connectionString);
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), url.ConnectTimeout);
            Assert.AreEqual("mongodb://localhost/?connectTimeout=123ms", url.ToString()); // changed to "ms" suffix
        }

        [Test]
        public void TestIpV6() {
            string connectionString = "mongodb://localhost/?ipv6=true";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(true, url.IPv6);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestMaxConnectionIdleTime() {
            string connectionString = "mongodb://localhost/?maxIdleTime=123ms";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), url.MaxConnectionIdleTime);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestMaxConnectionLifeTime() {
            string connectionString = "mongodb://localhost/?maxLifeTime=123ms";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), url.MaxConnectionLifeTime);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestMaxConnectionPoolSize() {
            string connectionString = "mongodb://localhost/?maxPoolSize=123";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(123, url.MaxConnectionPoolSize);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestMinConnectionPoolSize() {
            string connectionString = "mongodb://localhost/?minPoolSize=123";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(123, url.MinConnectionPoolSize);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestReplicaSetName() {
            string connectionString = "mongodb://localhost/?replicaSet=name";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual("name", url.ReplicaSetName);
            Assert.AreEqual("mongodb://localhost/?connect=replicaSet;replicaSet=name", url.ToString()); // connect=replicaSet added
        }

        [Test]
        public void TestSafeModeFalse() {
            string connectionString = "mongodb://localhost/?safe=false";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual("mongodb://localhost", url.ToString()); // safe=false dropped
        }

        [Test]
        public void TestSafeModeTrue() {
            string connectionString = "mongodb://localhost/?safe=true";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(SafeMode.True, url.SafeMode);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestSafeModeFSyncFalse() {
            string connectionString = "mongodb://localhost/?safe=true;fsync=false";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(SafeMode.True, url.SafeMode);
            Assert.AreEqual("mongodb://localhost/?safe=true", url.ToString()); // fsync=false dropped
        }

        [Test]
        public void TestSafeModeFSyncTrue() {
            string connectionString = "mongodb://localhost/?safe=true;fsync=true";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(SafeMode.FSyncTrue, url.SafeMode);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestSafeModeW2() {
            string connectionString = "mongodb://localhost/?w=2";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(SafeMode.W2, url.SafeMode);
            Assert.AreEqual("mongodb://localhost/?safe=true;w=2", url.ToString()); // safe=true added
        }

        [Test]
        public void TestSafeModeTrueW2() {
            string connectionString = "mongodb://localhost/?safe=true;w=2";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(SafeMode.W2, url.SafeMode);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestSafeModeTrueW2WTimeout() {
            string connectionString = "mongodb://localhost/?safe=true;w=2;wtimeout=2s";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(SafeMode.Create(2, TimeSpan.FromSeconds(2)), url.SafeMode);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestSafeModeTrueFSyncTrueW2() {
            string connectionString = "mongodb://localhost/?safe=true;fsync=true;w=2";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(SafeMode.Create(true, true, 2), url.SafeMode);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestSafeModeTrueFSyncTrueW2WTimeout() {
            string connectionString = "mongodb://localhost/?safe=true;fsync=true;w=2;wtimeout=2s";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(SafeMode.Create(true, true, 2, TimeSpan.FromSeconds(2)), url.SafeMode);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestSlaveOkFalse() {
            string connectionString = "mongodb://localhost/?slaveOk=false";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual("mongodb://localhost", url.ToString()); // slaveOk=false dropped
        }

        [Test]
        public void TestSlaveOkTrue() {
            string connectionString = "mongodb://localhost/?slaveOk=true";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(true, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestSocketTimeout() {
            string connectionString = "mongodb://localhost/?socketTimeout=123ms";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), url.SocketTimeout);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestWaitQueueMultiple() {
            string connectionString = "mongodb://localhost/?waitQueueMultiple=2";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(2, url.WaitQueueMultiple);
            Assert.AreEqual(0, url.WaitQueueSize);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestWaitQueueSize() {
            string connectionString = "mongodb://localhost/?waitQueueSize=123";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(0, url.WaitQueueMultiple);
            Assert.AreEqual(123, url.WaitQueueSize);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestWaitQueueTimeout() {
            string connectionString = "mongodb://localhost/?waitQueueTimeout=123ms";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual(TimeSpan.FromMilliseconds(123), url.WaitQueueTimeout);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestAll() {
            string connectionString = "mongodb://localhost/?connect=replicaSet;replicaSet=name;slaveOk=true;safe=true;fsync=true;w=2;wtimeout=2s";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.DefaultCredentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual("name", url.ReplicaSetName);
            Assert.AreEqual(SafeMode.Create(true, true, 2, TimeSpan.FromSeconds(2)), url.SafeMode);
            Assert.AreEqual(true, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }
    }
}
