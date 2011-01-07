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
    public class MongoConnectionStringBuilderTests {
        [Test]
        public void TestLocalHost() {
            string connectionString = "server=localhost";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Servers.Single().Host);
            Assert.AreEqual(27017, builder.Servers.Single().Port);
            Assert.IsNull(builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestHost() {
            string connectionString = "server=mongo.xyz.com";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("mongo.xyz.com", builder.Servers.Single().Host);
            Assert.AreEqual(27017, builder.Servers.Single().Port);
            Assert.IsNull(builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestHostWithPort() {
            string connectionString = "server=mongo.xyz.com:12345";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("mongo.xyz.com", builder.Servers.Single().Host);
            Assert.AreEqual(12345, builder.Servers.Single().Port);
            Assert.IsNull(builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestTwoHosts() {
            string connectionString = "server=mongo1.xyz.com,mongo2.xyz.com";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(2, builder.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", builder.Servers.First().Host);
            Assert.AreEqual(27017, builder.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", builder.Servers.Skip(1).Single().Host);
            Assert.AreEqual(27017, builder.Servers.Skip(1).Single().Port);
            Assert.IsNull(builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestTwoHostsWithPorts() {
            string connectionString = "server=mongo1.xyz.com:12345,mongo2.xyz.com:23456";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.IsNull(builder.Username);
            Assert.IsNull(builder.Password);
            Assert.AreEqual(2, builder.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", builder.Servers.First().Host);
            Assert.AreEqual(12345, builder.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", builder.Servers.Skip(1).Single().Host);
            Assert.AreEqual(23456, builder.Servers.Skip(1).Single().Port);
            Assert.IsNull(builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestUsernamePasswordLocalhostDatabase() {
            string connectionString = "server=localhost;database=dbname;username=userx;password=pwd";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual("userx", builder.Username);
            Assert.AreEqual("pwd", builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Servers.Single().Host);
            Assert.AreEqual(27017, builder.Servers.Single().Port);
            Assert.AreEqual("dbname", builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestUsernamePasswordTwoHostsDatabase() {
            string connectionString = "server=mongo1.xyz.com,mongo2.xyz.com;database=dbname;username=userx;password=pwd";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual("userx", builder.Username);
            Assert.AreEqual("pwd", builder.Password);
            Assert.AreEqual(2, builder.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", builder.Servers.First().Host);
            Assert.AreEqual(27017, builder.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", builder.Servers.Skip(1).Single().Host);
            Assert.AreEqual(27017, builder.Servers.Skip(1).Single().Port);
            Assert.AreEqual("dbname", builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestUsernamePasswordTwoHostsWithPortsDatabase() {
            string connectionString = "server=mongo1.xyz.com:12345,mongo2.xyz.com:23456;database=dbname;username=userx;password=pwd";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual("userx", builder.Username);
            Assert.AreEqual("pwd", builder.Password);
            Assert.AreEqual(2, builder.Servers.Count());
            Assert.AreEqual("mongo1.xyz.com", builder.Servers.First().Host);
            Assert.AreEqual(12345, builder.Servers.First().Port);
            Assert.AreEqual("mongo2.xyz.com", builder.Servers.Skip(1).Single().Host);
            Assert.AreEqual(23456, builder.Servers.Skip(1).Single().Port);
            Assert.AreEqual("dbname", builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestDirectConnectionMode() {
            string connectionString = "server=localhost;connect=direct";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestReplicaSetConnectionMode() {
            string connectionString = "server=localhost;connect=replicaSet";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestReplicaSetName() {
            string connectionString = "server=localhost;replicaSet=name";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual("name", builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual("server=localhost;connect=replicaSet;replicaSet=name", builder.ToString()); // connect=replicaSet added
        }

        [Test]
        public void TestSafeModeFalse() {
            string connectionString = "server=localhost;safe=false";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeTrue() {
            string connectionString = "server=localhost;safe=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(SafeMode.True, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeFSyncFalse() {
            string connectionString = "server=localhost;safe=true;fsync=false";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(SafeMode.True, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual("server=localhost;safe=true", builder.ToString()); // fsync=false dropped
        }

        [Test]
        public void TestSafeModeFSyncTrue() {
            string connectionString = "server=localhost;safe=true;fsync=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(SafeMode.FSyncTrue, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeW2() {
            string connectionString = "server=localhost;w=2";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(SafeMode.W2, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual("server=localhost;safe=true;w=2", builder.ToString()); // safe=true added
        }

        [Test]
        public void TestSafeModeTrueW2() {
            string connectionString = "server=localhost;safe=true;w=2";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(SafeMode.W2, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeTrueW2WTimeout() {
            string connectionString = "server=localhost;safe=true;w=2;wtimeout=2s";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(SafeMode.Create(2, TimeSpan.FromSeconds(2)), builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeTrueFSyncTrueW2() {
            string connectionString = "server=localhost;safe=true;fsync=true;w=2";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(SafeMode.Create(true, true, 2), builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSafeModeTrueFSyncTrueW2WTimeout() {
            string connectionString = "server=localhost;safe=true;fsync=true;w=2;wtimeout=2s";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(SafeMode.Create(true, true, 2, TimeSpan.FromSeconds(2)), builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSlaveOkFalse() {
            string connectionString = "server=localhost;slaveOk=false";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(false, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestSlaveOkTrue() {
            string connectionString = "server=localhost;slaveOk=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.Direct, builder.ConnectionMode);
            Assert.AreEqual(null, builder.ReplicaSetName);
            Assert.AreEqual(null, builder.SafeMode);
            Assert.AreEqual(true, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }

        [Test]
        public void TestAll() {
            string connectionString = "server=localhost;connect=replicaSet;replicaSet=name;safe=true;fsync=true;w=2;wtimeout=2s;slaveOk=true";
            var builder = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(null, builder.Username);
            Assert.AreEqual(null, builder.Password);
            Assert.AreEqual(1, builder.Servers.Count());
            Assert.AreEqual("localhost", builder.Server.Host);
            Assert.AreEqual(27017, builder.Server.Port);
            Assert.AreEqual(null, builder.DatabaseName);
            Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
            Assert.AreEqual("name", builder.ReplicaSetName);
            Assert.AreEqual(SafeMode.Create(true, true, 2, TimeSpan.FromSeconds(2)), builder.SafeMode);
            Assert.AreEqual(true, builder.SlaveOk);
            Assert.AreEqual(connectionString, builder.ToString());
        }
    }
}
