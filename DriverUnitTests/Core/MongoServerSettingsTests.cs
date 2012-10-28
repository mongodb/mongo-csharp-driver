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
    public class MongoServerSettingsTests
    {
        [Test]
        public void TestAll()
        {
            var settings = new MongoServerSettings();
            settings.ConnectionMode = ConnectionMode.ReplicaSet;
            settings.ConnectTimeout = TimeSpan.FromSeconds(1);
            settings.DefaultCredentials = MongoCredentials.Create("username", "password");
            settings.GuidRepresentation = GuidRepresentation.CSharpLegacy;
            settings.IPv6 = true;
            settings.MaxConnectionIdleTime = TimeSpan.FromSeconds(2);
            settings.MaxConnectionLifeTime = TimeSpan.FromSeconds(3);
            settings.MaxConnectionPoolSize = 99;
            settings.MinConnectionPoolSize = 11;
            settings.ReadPreference = ReadPreference.Primary;
            settings.ReplicaSetName = "replicaname";
            settings.SafeMode = SafeMode.Create(5, TimeSpan.FromSeconds(4));
            settings.Server = new MongoServerAddress("server");
            settings.SocketTimeout = TimeSpan.FromSeconds(5);
            settings.UseSsl = true;
            settings.WaitQueueSize = 55;
            settings.WaitQueueTimeout = TimeSpan.FromSeconds(6);

            Assert.AreEqual(ConnectionMode.ReplicaSet, settings.ConnectionMode);
            Assert.AreEqual(TimeSpan.FromSeconds(1), settings.ConnectTimeout);
            Assert.AreEqual(MongoCredentials.Create("username", "password"), settings.DefaultCredentials);
            Assert.AreEqual(GuidRepresentation.CSharpLegacy, settings.GuidRepresentation);
            Assert.AreEqual(true, settings.IPv6);
            Assert.AreEqual(TimeSpan.FromSeconds(2), settings.MaxConnectionIdleTime);
            Assert.AreEqual(TimeSpan.FromSeconds(3), settings.MaxConnectionLifeTime);
            Assert.AreEqual(99, settings.MaxConnectionPoolSize);
            Assert.AreEqual(11, settings.MinConnectionPoolSize);
            Assert.AreEqual(ReadPreference.Primary, settings.ReadPreference);
            Assert.AreEqual("replicaname", settings.ReplicaSetName);
            Assert.AreEqual(SafeMode.Create(5, TimeSpan.FromSeconds(4)), settings.SafeMode);
            Assert.AreEqual(new MongoServerAddress("server"), settings.Server);
            Assert.IsTrue((new[] { new MongoServerAddress("server") }).SequenceEqual(settings.Servers));
            Assert.AreEqual(TimeSpan.FromSeconds(5), settings.SocketTimeout);
            Assert.AreEqual(true, settings.UseSsl);
            Assert.AreEqual(55, settings.WaitQueueSize);
            Assert.AreEqual(TimeSpan.FromSeconds(6), settings.WaitQueueTimeout);

            Assert.IsFalse(settings.IsFrozen);
            var hashCode = settings.GetHashCode();
            var stringRepresentation = settings.ToString();
            Assert.AreEqual(settings, settings);

            settings.Freeze();
            Assert.IsTrue(settings.IsFrozen);
            Assert.AreEqual(hashCode, settings.GetHashCode());
            Assert.AreEqual(stringRepresentation, settings.ToString());
        }

        [Test]
        public void TestDefaults()
        {
            var localhost = new MongoServerAddress("localhost");

            var settings = new MongoServerSettings();
            Assert.AreEqual(ConnectionMode.Automatic, settings.ConnectionMode);
            Assert.AreEqual(MongoDefaults.ConnectTimeout, settings.ConnectTimeout);
            Assert.AreEqual(null, settings.DefaultCredentials);
            Assert.AreEqual(MongoDefaults.GuidRepresentation, settings.GuidRepresentation);
            Assert.AreEqual(false, settings.IPv6);
            Assert.AreEqual(MongoDefaults.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.AreEqual(MongoDefaults.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.AreEqual(MongoDefaults.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.AreEqual(MongoDefaults.MinConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.AreEqual(ReadPreference.Primary, settings.ReadPreference);
            Assert.AreEqual(null, settings.ReplicaSetName);
            Assert.AreEqual(SafeMode.False, settings.SafeMode);
            Assert.AreEqual(1, settings.Servers.Count());
            Assert.AreEqual(localhost, settings.Server);
            Assert.AreEqual(localhost, settings.Servers.First());
            Assert.AreEqual(MongoDefaults.SocketTimeout, settings.SocketTimeout);
            Assert.AreEqual(false, settings.UseSsl);
            Assert.AreEqual(MongoDefaults.ComputedWaitQueueSize, settings.WaitQueueSize);
            Assert.AreEqual(MongoDefaults.WaitQueueTimeout, settings.WaitQueueTimeout);

            Assert.IsFalse(settings.IsFrozen);
            var hashCode = settings.GetHashCode();
            var stringRepresentation = settings.ToString();
            Assert.AreEqual(settings, settings);

            settings.Freeze();
            Assert.IsTrue(settings.IsFrozen);
            Assert.AreEqual(hashCode, settings.GetHashCode());
            Assert.AreEqual(stringRepresentation, settings.ToString());
        }

        [Test]
        public void TestOneServer()
        {
            var server = new MongoServerAddress("server");
            var servers = new[] { server };

            var settings = new MongoServerSettings();
            settings.Server = server;
            Assert.AreEqual(server, settings.Server);
            Assert.IsTrue(servers.SequenceEqual(settings.Servers));

            settings.Servers = servers;
            Assert.AreEqual(server, settings.Server);
            Assert.IsTrue(servers.SequenceEqual(settings.Servers));
        }

        [Test]
        public void TestTwoServers()
        {
            var servers = new MongoServerAddress[]
            {
                new MongoServerAddress("server1"),
                new MongoServerAddress("server2")
            };

            var settings = new MongoServerSettings();
            settings.Servers = servers;
            Assert.IsTrue(servers.SequenceEqual(settings.Servers));

            Assert.Throws<InvalidOperationException>(() => { var s = settings.Server; });
        }

        [Test]
        public void TestFrozenCopy()
        {
            var settings = new MongoServerSettings();
            var frozenCopy = settings.FrozenCopy();
            var secondFrozenCopy = frozenCopy.FrozenCopy();
            Assert.AreNotSame(settings, frozenCopy);
            Assert.AreSame(frozenCopy, secondFrozenCopy);
            Assert.AreEqual(false, settings.IsFrozen);
            Assert.AreEqual(true, frozenCopy.IsFrozen);
        }
    }
}
