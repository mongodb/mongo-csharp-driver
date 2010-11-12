/* Copyright 2010 10gen Inc.
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
        public void TestLocalHost() {
            string connectionString = "mongodb://localhost";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Servers.Single().Host);
            Assert.AreEqual(27017, url.Servers.Single().Port);
            Assert.IsNull(url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestHost() {
            string connectionString = "mongodb://mongo.xyz.com";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("mongo.xyz.com", url.Servers.Single().Host);
            Assert.AreEqual(27017, url.Servers.Single().Port);
            Assert.IsNull(url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestHostWithPort() {
            string connectionString = "mongodb://mongo.xyz.com:12345";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("mongo.xyz.com", url.Servers.Single().Host);
            Assert.AreEqual(12345, url.Servers.Single().Port);
            Assert.IsNull(url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestTwoHosts() {
            string connectionString = "mongodb://mongo1.xyz.com,mongo2.xyz.com";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(2, url.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", url.Servers.First().Host);
            Assert.AreEqual(27017, url.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", url.Servers.Skip(1).Single().Host);
            Assert.AreEqual(27017, url.Servers.Skip(1).Single().Port);
            Assert.IsNull(url.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestTwoHostsWithPorts() {
            string connectionString = "mongodb://mongo1.xyz.com:12345,mongo2.xyz.com:23456";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(2, url.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", url.Servers.First().Host);
            Assert.AreEqual(12345, url.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", url.Servers.Skip(1).Single().Host);
            Assert.AreEqual(23456, url.Servers.Skip(1).Single().Port);
            Assert.IsNull(url.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestUsernamePasswordLocalhostDatabase() {
            string connectionString = "mongodb://userx:pwd@localhost/dbname";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual("userx", url.Credentials.Username);
            Assert.AreEqual("pwd", url.Credentials.Password);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Servers.Single().Host);
            Assert.AreEqual(27017, url.Servers.Single().Port);
            Assert.AreEqual("dbname", url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestUsernamePasswordTwoHostsDatabase() {
            string connectionString = "mongodb://userx:pwd@mongo1.xyz.com,mongo2.xyz.com/dbname";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual("userx", url.Credentials.Username);
            Assert.AreEqual("pwd", url.Credentials.Password);
            Assert.AreEqual(2, url.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", url.Servers.First().Host);
            Assert.AreEqual(27017, url.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", url.Servers.Skip(1).Single().Host);
            Assert.AreEqual(27017, url.Servers.Skip(1).Single().Port);
            Assert.AreEqual("dbname", url.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestUsernamePasswordTwoHostsWithPortsDatabase() {
            string connectionString = "mongodb://userx:pwd@mongo1.xyz.com:12345,mongo2.xyz.com:23456/dbname";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.AreEqual("userx", url.Credentials.Username);
            Assert.AreEqual("pwd", url.Credentials.Password);
            Assert.AreEqual(2, url.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", url.Servers.First().Host);
            Assert.AreEqual(12345, url.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", url.Servers.Skip(1).Single().Host);
            Assert.AreEqual(23456, url.Servers.Skip(1).Single().Port);
            Assert.AreEqual("dbname", url.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestNoQueryString() {
            string connectionString = "mongodb://localhost";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestDirectConnectionMode() {
            string connectionString = "mongodb://localhost/?connect=direct";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual("mongodb://localhost", url.ToString()); // connect=direct dropped
        }

        [Test]
        public void TestReplicaSetConnectionMode() {
            string connectionString = "mongodb://localhost/?connect=replicaset";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestReplicaSetName() {
            string connectionString = "mongodb://localhost/?replicaset=name";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual("name", url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual("mongodb://localhost/?connect=replicaset;replicaset=name", url.ToString()); // connect=replicaset added
        }

        [Test]
        public void TestSafeModeFalse() {
            string connectionString = "mongodb://localhost/?safe=false";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual("mongodb://localhost", url.ToString()); // safe=false dropped
        }

        [Test]
        public void TestSafeModeTrue() {
            string connectionString = "mongodb://localhost/?safe=true";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.True, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestSafeModeFSyncFalse() {
            string connectionString = "mongodb://localhost/?safe=true;fsync=false";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.True, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual("mongodb://localhost/?safe=true", url.ToString()); // fsync=false dropped
        }

        [Test]
        public void TestSafeModeFSyncTrue() {
            string connectionString = "mongodb://localhost/?safe=true;fsync=true";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.FSyncTrue, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestSafeModeW2() {
            string connectionString = "mongodb://localhost/?w=2";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.W2, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual("mongodb://localhost/?safe=true;w=2", url.ToString()); // safe=true added
        }

        [Test]
        public void TestSafeModeTrueW2() {
            string connectionString = "mongodb://localhost/?safe=true;w=2";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.W2, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestSafeModeTrueW2WTimeout() {
            string connectionString = "mongodb://localhost/?safe=true;w=2;wtimeout=2000";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.Create(2, TimeSpan.FromMilliseconds(2000)), url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestSafeModeTrueFSyncTrueW2() {
            string connectionString = "mongodb://localhost/?safe=true;fsync=true;w=2";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.Create(true, true, 2), url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestSafeModeTrueFSyncTrueW2WTimeout() {
            string connectionString = "mongodb://localhost/?safe=true;fsync=true;w=2;wtimeout=2000";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.Create(true, true, 2, TimeSpan.FromMilliseconds(2000)), url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestSlaveOkFalse() {
            string connectionString = "mongodb://localhost/?slaveok=false";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(false, url.SlaveOk);
            Assert.AreEqual("mongodb://localhost", url.ToString()); // slaveok=false dropped
        }

        [Test]
        public void TestSlaveOkTrue() {
            string connectionString = "mongodb://localhost/?slaveok=true";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, url.ConnectionMode);
            Assert.AreEqual(null, url.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, url.SafeMode);
            Assert.AreEqual(true, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }

        [Test]
        public void TestAll() {
            string connectionString = "mongodb://localhost/?connect=replicaset;replicaset=name;safe=true;fsync=true;w=2;wtimeout=2000;slaveok=true";
            MongoUrl url = new MongoUrl(connectionString);
            Assert.IsNull(url.Credentials);
            Assert.AreEqual(1, url.Servers.Count());
            Assert.AreEqual("localhost", url.Server.Host);
            Assert.AreEqual(27017, url.Server.Port);
            Assert.AreEqual(null, url.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
            Assert.AreEqual("name", url.ReplicaSetName);
            Assert.AreEqual(SafeMode.Create(true, true, 2, TimeSpan.FromMilliseconds(2000)), url.SafeMode);
            Assert.AreEqual(true, url.SlaveOk);
            Assert.AreEqual(connectionString, url.ToString());
        }
    }
}
