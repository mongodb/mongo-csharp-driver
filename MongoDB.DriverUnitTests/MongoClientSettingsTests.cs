/* Copyright 2010-2013 10gen Inc.
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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoClientSettingsTests
    {
        private readonly MongoServerAddress _localHost = new MongoServerAddress("localhost");

        [Test]
        public void TestClone()
        {
            // set everything to non default values to test that all settings are cloned
            var connectionString =
                "mongodb://user1:password1@somehost/?" +
                "connect=direct;connectTimeout=123;uuidRepresentation=pythonLegacy;ipv6=true;" +
                "maxIdleTime=124;maxLifeTime=125;maxPoolSize=126;minPoolSize=127;" +
                "readPreference=secondary;readPreferenceTags=a:1,b:2;readPreferenceTags=c:3,d:4;secondaryAcceptableLatency=128;socketTimeout=129;" +
                "ssl=true;sslVerifyCertificate=false;waitqueuesize=130;waitQueueTimeout=131;" +
                "w=1;fsync=true;journal=true;w=2;wtimeout=131";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var settings = MongoClientSettings.FromUrl(url);

            // a few settings can only be made in code
            settings.Credentials = new[] { MongoCredential.CreateMongoCRCredential("database", "username", "password") };
            settings.SslSettings = new SslSettings { CheckCertificateRevocation = false };

            var clone = settings.Clone();
            Assert.AreEqual(settings, clone);
        }

        [Test]
        public void TestConnectionMode()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(ConnectionMode.Automatic, settings.ConnectionMode);

            var connectionMode = ConnectionMode.Direct;
            settings.ConnectionMode = connectionMode;
            Assert.AreEqual(connectionMode, settings.ConnectionMode);

            settings.Freeze();
            Assert.AreEqual(connectionMode, settings.ConnectionMode);
            Assert.Throws<InvalidOperationException>(() => { settings.ConnectionMode = connectionMode; });
        }

        [Test]
        public void TestConnectTimeout()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(MongoDefaults.ConnectTimeout, settings.ConnectTimeout);

            var connectTimeout = new TimeSpan(1, 2, 3);
            settings.ConnectTimeout = connectTimeout;
            Assert.AreEqual(connectTimeout, settings.ConnectTimeout);

            settings.Freeze();
            Assert.AreEqual(connectTimeout, settings.ConnectTimeout);
            Assert.Throws<InvalidOperationException>(() => { settings.ConnectTimeout = connectTimeout; });
        }

        [Test]
        public void TestCredentials()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(0, settings.Credentials.Count());
        }

        [Test]
        public void TestDefaults()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(ConnectionMode.Automatic, settings.ConnectionMode);
            Assert.AreEqual(MongoDefaults.ConnectTimeout, settings.ConnectTimeout);
            Assert.AreEqual(0, settings.Credentials.Count());
            Assert.AreEqual(MongoDefaults.GuidRepresentation, settings.GuidRepresentation);
            Assert.AreEqual(false, settings.IPv6);
            Assert.AreEqual(MongoDefaults.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.AreEqual(MongoDefaults.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.AreEqual(MongoDefaults.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.AreEqual(MongoDefaults.MinConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.AreEqual(ReadPreference.Primary, settings.ReadPreference);
            Assert.AreEqual(null, settings.ReplicaSetName);
            Assert.AreEqual(_localHost, settings.Server);
            Assert.AreEqual(_localHost, settings.Servers.First());
            Assert.AreEqual(1, settings.Servers.Count());
            Assert.AreEqual(MongoDefaults.SocketTimeout, settings.SocketTimeout);
            Assert.AreEqual(null, settings.SslSettings);
            Assert.AreEqual(false, settings.UseSsl);
            Assert.AreEqual(true, settings.VerifySslCertificate);
            Assert.AreEqual(MongoDefaults.ComputedWaitQueueSize, settings.WaitQueueSize);
            Assert.AreEqual(MongoDefaults.WaitQueueTimeout, settings.WaitQueueTimeout);
            Assert.AreEqual(WriteConcern.Acknowledged, settings.WriteConcern);
        }

        [Test]
        public void TestEquals()
        {
            var settings = new MongoClientSettings();
            var clone = settings.Clone();
            Assert.IsTrue(clone.Equals(settings));

            clone = settings.Clone();
            clone.ConnectionMode = ConnectionMode.Direct;
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.ConnectTimeout = new TimeSpan(1, 2, 3);
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.Credentials = new[] { MongoCredential.CreateMongoCRCredential("db2", "user2", "password2") };
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.Credentials = new[] { MongoCredential.CreateMongoCRCredential("db", "user2", "password2") };
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.GuidRepresentation = GuidRepresentation.PythonLegacy;
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.IPv6 = !settings.IPv6;
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.MaxConnectionIdleTime = new TimeSpan(1, 2, 3);
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.MaxConnectionLifeTime = new TimeSpan(1, 2, 3);
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.MaxConnectionPoolSize = settings.MaxConnectionPoolSize + 1;
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.MinConnectionPoolSize = settings.MinConnectionPoolSize + 1;
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.ReadPreference = ReadPreference.Secondary;
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.ReplicaSetName = "abc";
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.Server = new MongoServerAddress("someotherhost");
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.SocketTimeout = new TimeSpan(1, 2, 3);
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.SslSettings = new SslSettings { CheckCertificateRevocation = false };
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.UseSsl = !settings.UseSsl;
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.VerifySslCertificate = !settings.VerifySslCertificate;
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.WaitQueueSize = settings.WaitQueueSize + 1;
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.WaitQueueTimeout = new TimeSpan(1, 2, 3);
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.WriteConcern = WriteConcern.W2;
            Assert.IsFalse(clone.Equals(settings));
        }

        [Test]
        public void TestFreeze()
        {
            var settings = new MongoClientSettings();
            settings.ReadPreference = new ReadPreference();
            settings.WriteConcern = new WriteConcern();

            Assert.IsFalse(settings.IsFrozen);
            Assert.IsFalse(settings.ReadPreference.IsFrozen);
            Assert.IsFalse(settings.WriteConcern.IsFrozen);
            var hashCode = settings.GetHashCode();
            var stringRepresentation = settings.ToString();

            settings.Freeze();
            Assert.IsTrue(settings.IsFrozen);
            Assert.IsTrue(settings.ReadPreference.IsFrozen);
            Assert.IsTrue(settings.WriteConcern.IsFrozen);
            Assert.AreEqual(hashCode, settings.GetHashCode());
            Assert.AreEqual(stringRepresentation, settings.ToString());
        }

        [Test]
        public void TestFromMongoConnectionStringBuilder()
        {
            // set everything to non default values to test that all settings are converted
            var connectionString =
                "server=somehost;username=user1;password=password1;" +
                "connect=direct;connectTimeout=123;uuidRepresentation=pythonLegacy;ipv6=true;" +
                "maxIdleTime=124;maxLifeTime=125;maxPoolSize=126;minPoolSize=127;" +
                "readPreference=secondary;readPreferenceTags=a:1,b:2|c:3,d:4;secondaryAcceptableLatency=128;socketTimeout=129;" +
                "ssl=true;sslVerifyCertificate=false;waitqueuesize=130;waitQueueTimeout=131;" +
                "w=1;fsync=true;journal=true;w=2;wtimeout=131";
            var builder = new MongoConnectionStringBuilder(connectionString);

            var settings = MongoClientSettings.FromConnectionStringBuilder(builder);
            Assert.AreEqual(builder.ConnectionMode, settings.ConnectionMode);
            Assert.AreEqual(builder.ConnectTimeout, settings.ConnectTimeout);
            Assert.AreEqual(1, settings.Credentials.Count());
            Assert.AreEqual(builder.Username, settings.Credentials.Single().Username);
            Assert.AreEqual("admin", settings.Credentials.Single().Source);
            Assert.AreEqual(builder.Password, ((PasswordEvidence)settings.Credentials.Single().Evidence).Password);
            Assert.AreEqual(builder.GuidRepresentation, settings.GuidRepresentation);
            Assert.AreEqual(builder.IPv6, settings.IPv6);
            Assert.AreEqual(builder.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.AreEqual(builder.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.AreEqual(builder.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.AreEqual(builder.MinConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.AreEqual(builder.ReadPreference, settings.ReadPreference);
            Assert.AreEqual(builder.ReplicaSetName, settings.ReplicaSetName);
            Assert.IsTrue(builder.Servers.SequenceEqual(settings.Servers));
            Assert.AreEqual(builder.SocketTimeout, settings.SocketTimeout);
            Assert.AreEqual(null, settings.SslSettings);
            Assert.AreEqual(builder.UseSsl, settings.UseSsl);
            Assert.AreEqual(builder.VerifySslCertificate, settings.VerifySslCertificate);
            Assert.AreEqual(builder.ComputedWaitQueueSize, settings.WaitQueueSize);
            Assert.AreEqual(builder.WaitQueueTimeout, settings.WaitQueueTimeout);
            Assert.AreEqual(builder.GetWriteConcern(true), settings.WriteConcern);
        }

        [Test]
        public void TestFromUrl()
        {
            // set everything to non default values to test that all settings are converted
            var connectionString =
                "mongodb://user1:password1@somehost/?authSource=db;" +
                "connect=direct;connectTimeout=123;uuidRepresentation=pythonLegacy;ipv6=true;" +
                "maxIdleTime=124;maxLifeTime=125;maxPoolSize=126;minPoolSize=127;" +
                "readPreference=secondary;readPreferenceTags=a:1,b:2;readPreferenceTags=c:3,d:4;secondaryAcceptableLatency=128;socketTimeout=129;" +
                "ssl=true;sslVerifyCertificate=false;waitqueuesize=130;waitQueueTimeout=131;" +
                "w=1;fsync=true;journal=true;w=2;wtimeout=131";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();

            var settings = MongoClientSettings.FromUrl(url);
            Assert.AreEqual(url.ConnectionMode, settings.ConnectionMode);
            Assert.AreEqual(url.ConnectTimeout, settings.ConnectTimeout);
            Assert.AreEqual(1, settings.Credentials.Count());
            Assert.AreEqual(url.Username, settings.Credentials.Single().Username);
            Assert.AreEqual(url.AuthenticationMechanism, settings.Credentials.Single().Mechanism);
            Assert.AreEqual(url.AuthenticationSource, settings.Credentials.Single().Source);
            Assert.AreEqual(url.Password, ((PasswordEvidence)settings.Credentials.Single().Evidence).Password);
            Assert.AreEqual(url.GuidRepresentation, settings.GuidRepresentation);
            Assert.AreEqual(url.IPv6, settings.IPv6);
            Assert.AreEqual(url.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.AreEqual(url.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.AreEqual(url.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.AreEqual(url.MinConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.AreEqual(url.ReadPreference, settings.ReadPreference);
            Assert.AreEqual(url.ReplicaSetName, settings.ReplicaSetName);
            Assert.IsTrue(url.Servers.SequenceEqual(settings.Servers));
            Assert.AreEqual(url.SocketTimeout, settings.SocketTimeout);
            Assert.AreEqual(null, settings.SslSettings);
            Assert.AreEqual(url.UseSsl, settings.UseSsl);
            Assert.AreEqual(url.VerifySslCertificate, settings.VerifySslCertificate);
            Assert.AreEqual(url.ComputedWaitQueueSize, settings.WaitQueueSize);
            Assert.AreEqual(url.WaitQueueTimeout, settings.WaitQueueTimeout);
            Assert.AreEqual(url.GetWriteConcern(true), settings.WriteConcern);
        }

        [Test]
        public void TestFrozenCopy()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(false, settings.IsFrozen);

            var frozenCopy = settings.FrozenCopy();
            Assert.AreEqual(true, frozenCopy.IsFrozen);
            Assert.AreNotSame(settings, frozenCopy);
            Assert.AreEqual(settings, frozenCopy);

            var secondFrozenCopy = frozenCopy.FrozenCopy();
            Assert.AreSame(frozenCopy, secondFrozenCopy);
        }

        [Test]
        public void TestGuidRepresentation()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(MongoDefaults.GuidRepresentation, settings.GuidRepresentation);

            var guidRepresentation = GuidRepresentation.PythonLegacy;
            settings.GuidRepresentation = guidRepresentation;
            Assert.AreEqual(guidRepresentation, settings.GuidRepresentation);

            settings.Freeze();
            Assert.AreEqual(guidRepresentation, settings.GuidRepresentation);
            Assert.Throws<InvalidOperationException>(() => { settings.GuidRepresentation = guidRepresentation; });
        }

        [Test]
        public void TestIPv6()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(false, settings.IPv6);

            var ipv6 = true;
            settings.IPv6 = ipv6;
            Assert.AreEqual(ipv6, settings.IPv6);

            settings.Freeze();
            Assert.AreEqual(ipv6, settings.IPv6);
            Assert.Throws<InvalidOperationException>(() => { settings.IPv6 = ipv6; });
        }

        [Test]
        public void TestMaxConnectionIdleTime()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(MongoDefaults.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);

            var maxConnectionIdleTime = new TimeSpan(1, 2, 3);
            settings.MaxConnectionIdleTime = maxConnectionIdleTime;
            Assert.AreEqual(maxConnectionIdleTime, settings.MaxConnectionIdleTime);

            settings.Freeze();
            Assert.AreEqual(maxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.Throws<InvalidOperationException>(() => { settings.MaxConnectionIdleTime = maxConnectionIdleTime; });
        }

        [Test]
        public void TestMaxConnectionLifeTime()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(MongoDefaults.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);

            var maxConnectionLifeTime = new TimeSpan(1, 2, 3);
            settings.MaxConnectionLifeTime = maxConnectionLifeTime;
            Assert.AreEqual(maxConnectionLifeTime, settings.MaxConnectionLifeTime);

            settings.Freeze();
            Assert.AreEqual(maxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.Throws<InvalidOperationException>(() => { settings.MaxConnectionLifeTime = maxConnectionLifeTime; });
        }

        [Test]
        public void TestMaxConnectionPoolSize()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(MongoDefaults.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);

            var maxConnectionPoolSize = 123;
            settings.MaxConnectionPoolSize = maxConnectionPoolSize;
            Assert.AreEqual(maxConnectionPoolSize, settings.MaxConnectionPoolSize);

            settings.Freeze();
            Assert.AreEqual(maxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.Throws<InvalidOperationException>(() => { settings.MaxConnectionPoolSize = maxConnectionPoolSize; });
        }

        [Test]
        public void TestMinConnectionPoolSize()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(MongoDefaults.MinConnectionPoolSize, settings.MinConnectionPoolSize);

            var minConnectionPoolSize = 123;
            settings.MinConnectionPoolSize = minConnectionPoolSize;
            Assert.AreEqual(minConnectionPoolSize, settings.MinConnectionPoolSize);

            settings.Freeze();
            Assert.AreEqual(minConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.Throws<InvalidOperationException>(() => { settings.MinConnectionPoolSize = minConnectionPoolSize; });
        }

        [Test]
        public void TestReadPreference()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(ReadPreference.Primary, settings.ReadPreference);

            var readPreference = new ReadPreference();
            settings.ReadPreference = readPreference;
            Assert.AreSame(readPreference, settings.ReadPreference);
            Assert.IsFalse(settings.ReadPreference.IsFrozen);

            settings.Freeze();
            Assert.AreEqual(readPreference, settings.ReadPreference);
            Assert.IsTrue(settings.ReadPreference.IsFrozen);
            Assert.Throws<InvalidOperationException>(() => { settings.ReadPreference = readPreference; });
        }

        [Test]
        public void TestReplicaSetName()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(null, settings.ReplicaSetName);

            var replicaSetName = "abc";
            settings.ReplicaSetName = replicaSetName;
            Assert.AreSame(replicaSetName, settings.ReplicaSetName);

            settings.Freeze();
            Assert.AreSame(replicaSetName, settings.ReplicaSetName);
            Assert.Throws<InvalidOperationException>(() => { settings.ReplicaSetName = replicaSetName; });
        }

        [Test]
        public void TestServer()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(_localHost, settings.Server);
            Assert.IsTrue(new[] { _localHost }.SequenceEqual(settings.Servers));

            var server = new MongoServerAddress("server");
            var servers = new[] { server };
            settings.Server = server;
            Assert.AreEqual(server, settings.Server);
            Assert.IsTrue(servers.SequenceEqual(settings.Servers));

            settings.Freeze();
            Assert.AreEqual(server, settings.Server);
            Assert.IsTrue(servers.SequenceEqual(settings.Servers));
            Assert.Throws<InvalidOperationException>(() => { settings.Server = server; });
        }

        [Test]
        public void TestServersWithOneServer()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(_localHost, settings.Server);
            Assert.IsTrue(new[] { _localHost }.SequenceEqual(settings.Servers));

            var server = new MongoServerAddress("server");
            var servers = new[] { server };
            settings.Servers = servers;
            Assert.AreEqual(server, settings.Server);
            Assert.IsTrue(servers.SequenceEqual(settings.Servers));

            settings.Freeze();
            Assert.AreEqual(server, settings.Server);
            Assert.IsTrue(servers.SequenceEqual(settings.Servers));
            Assert.Throws<InvalidOperationException>(() => { settings.Server = server; });
        }

        [Test]
        public void TestServersWithTwoServers()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(_localHost, settings.Server);
            Assert.IsTrue(new[] { _localHost }.SequenceEqual(settings.Servers));

            var servers = new MongoServerAddress[]
            {
                new MongoServerAddress("server1"),
                new MongoServerAddress("server2")
            };
            settings.Servers = servers;
            Assert.Throws<InvalidOperationException>(() => { var s = settings.Server; });
            Assert.IsTrue(servers.SequenceEqual(settings.Servers));

            settings.Freeze();
            Assert.Throws<InvalidOperationException>(() => { var s = settings.Server; });
            Assert.IsTrue(servers.SequenceEqual(settings.Servers));
            Assert.Throws<InvalidOperationException>(() => { settings.Servers = servers; });
        }

        [Test]
        public void TestSocketTimeout()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(MongoDefaults.SocketTimeout, settings.SocketTimeout);

            var socketTimeout = new TimeSpan(1, 2, 3);
            settings.SocketTimeout = socketTimeout;
            Assert.AreEqual(socketTimeout, settings.SocketTimeout);

            settings.Freeze();
            Assert.AreEqual(socketTimeout, settings.SocketTimeout);
            Assert.Throws<InvalidOperationException>(() => { settings.SocketTimeout = socketTimeout; });
        }

        [Test]
        public void TestSslSettings()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(null, settings.SslSettings);

            var sslSettings = new SslSettings { CheckCertificateRevocation = false };
            settings.SslSettings = sslSettings;
            Assert.AreEqual(sslSettings, settings.SslSettings);

            settings.Freeze();
            Assert.AreEqual(sslSettings, settings.SslSettings);
            Assert.Throws<InvalidOperationException>(() => { settings.SslSettings = sslSettings; });
        }

        [Test]
        public void TestUseSsl()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(false, settings.UseSsl);

            var useSsl = true;
            settings.UseSsl = useSsl;
            Assert.AreEqual(useSsl, settings.UseSsl);

            settings.Freeze();
            Assert.AreEqual(useSsl, settings.UseSsl);
            Assert.Throws<InvalidOperationException>(() => { settings.UseSsl = useSsl; });
        }

        [Test]
        public void TestVerifySslCertificate()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(true, settings.VerifySslCertificate);

            var verifySslCertificate = false;
            settings.VerifySslCertificate = verifySslCertificate;
            Assert.AreEqual(verifySslCertificate, settings.VerifySslCertificate);

            settings.Freeze();
            Assert.AreEqual(verifySslCertificate, settings.VerifySslCertificate);
            Assert.Throws<InvalidOperationException>(() => { settings.VerifySslCertificate = verifySslCertificate; });
        }

        [Test]
        public void TestWaitQueueSize()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(MongoDefaults.ComputedWaitQueueSize, settings.WaitQueueSize);

            var waitQueueSize = 123;
            settings.WaitQueueSize = waitQueueSize;
            Assert.AreEqual(waitQueueSize, settings.WaitQueueSize);

            settings.Freeze();
            Assert.AreEqual(waitQueueSize, settings.WaitQueueSize);
            Assert.Throws<InvalidOperationException>(() => { settings.WaitQueueSize = waitQueueSize; });
        }

        [Test]
        public void TestWaitQueueTimeout()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(MongoDefaults.WaitQueueTimeout, settings.WaitQueueTimeout);

            var waitQueueTimeout = new TimeSpan(1, 2, 3);
            settings.WaitQueueTimeout = waitQueueTimeout;
            Assert.AreEqual(waitQueueTimeout, settings.WaitQueueTimeout);

            settings.Freeze();
            Assert.AreEqual(waitQueueTimeout, settings.WaitQueueTimeout);
            Assert.Throws<InvalidOperationException>(() => { settings.WaitQueueTimeout = waitQueueTimeout; });
        }

        [Test]
        public void TestWriteConcern()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(WriteConcern.Acknowledged, settings.WriteConcern);

            var writeConcern = new WriteConcern();
            settings.WriteConcern = writeConcern;
            Assert.AreSame(writeConcern, settings.WriteConcern);
            Assert.IsFalse(settings.WriteConcern.IsFrozen);

            settings.Freeze();
            Assert.AreEqual(writeConcern, settings.WriteConcern);
            Assert.IsTrue(settings.WriteConcern.IsFrozen);
            Assert.Throws<InvalidOperationException>(() => { settings.WriteConcern = writeConcern; });
        }
    }
}
