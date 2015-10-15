/* Copyright 2010-2015 MongoDB Inc.
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
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
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
                "maxIdleTime=124;maxLifeTime=125;maxPoolSize=126;minPoolSize=127;readConcernLevel=majority;" +
                "readPreference=secondary;readPreferenceTags=a:1,b:2;readPreferenceTags=c:3,d:4;localThreshold=128;socketTimeout=129;" +
                "serverSelectionTimeout=20s;ssl=true;sslVerifyCertificate=false;waitqueuesize=130;waitQueueTimeout=131;" +
                "w=1;fsync=true;journal=true;w=2;wtimeout=131;gssapiServiceName=other";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var settings = MongoClientSettings.FromUrl(url);

            // a few settings can only be made in code
            settings.Credentials = new[] { MongoCredential.CreateMongoCRCredential("database", "username", "password").WithMechanismProperty("SERVICE_NAME", "other") };
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
            Assert.AreEqual(ReadConcern.Default, settings.ReadConcern);
            Assert.AreEqual(ReadPreference.Primary, settings.ReadPreference);
            Assert.AreEqual(null, settings.ReplicaSetName);
            Assert.AreEqual(_localHost, settings.Server);
            Assert.AreEqual(_localHost, settings.Servers.First());
            Assert.AreEqual(1, settings.Servers.Count());
            Assert.AreEqual(MongoDefaults.ServerSelectionTimeout, settings.ServerSelectionTimeout);
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
            clone.ReadConcern = ReadConcern.Majority;
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
            clone.LocalThreshold = new TimeSpan(1, 2, 3);
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.Server = new MongoServerAddress("someotherhost");
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.ServerSelectionTimeout = new TimeSpan(1, 2, 3);
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

            Assert.IsFalse(settings.IsFrozen);
            var hashCode = settings.GetHashCode();
            var stringRepresentation = settings.ToString();

            settings.Freeze();
            Assert.IsTrue(settings.IsFrozen);
            Assert.AreEqual(hashCode, settings.GetHashCode());
            Assert.AreEqual(stringRepresentation, settings.ToString());
        }

        [Test]
        public void TestFromUrl()
        {
            // set everything to non default values to test that all settings are converted
            var connectionString =
                "mongodb://user1:password1@somehost/?authSource=db;authMechanismProperties=CANONICALIZE_HOST_NAME:true;" +
                "connect=direct;connectTimeout=123;uuidRepresentation=pythonLegacy;ipv6=true;" +
                "maxIdleTime=124;maxLifeTime=125;maxPoolSize=126;minPoolSize=127;readConcernLevel=majority;" +
                "readPreference=secondary;readPreferenceTags=a:1,b:2;readPreferenceTags=c:3,d:4;localThreshold=128;socketTimeout=129;" +
                "serverSelectionTimeout=20s;ssl=true;sslVerifyCertificate=false;waitqueuesize=130;waitQueueTimeout=131;" +
                "w=1;fsync=true;journal=true;w=2;wtimeout=131;gssapiServiceName=other";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();

            var settings = MongoClientSettings.FromUrl(url);
            Assert.AreEqual(url.ConnectionMode, settings.ConnectionMode);
            Assert.AreEqual(url.ConnectTimeout, settings.ConnectTimeout);
            Assert.AreEqual(1, settings.Credentials.Count());
            Assert.AreEqual(url.Username, settings.Credentials.Single().Username);
            Assert.AreEqual(url.AuthenticationMechanism, settings.Credentials.Single().Mechanism);
            Assert.AreEqual("other", settings.Credentials.Single().GetMechanismProperty<string>("SERVICE_NAME", "mongodb"));
            Assert.AreEqual(true, settings.Credentials.Single().GetMechanismProperty<bool>("CANONICALIZE_HOST_NAME", false));
            Assert.AreEqual(url.AuthenticationSource, settings.Credentials.Single().Source);
            Assert.AreEqual(new PasswordEvidence(url.Password), settings.Credentials.Single().Evidence);
            Assert.AreEqual(url.GuidRepresentation, settings.GuidRepresentation);
            Assert.AreEqual(url.IPv6, settings.IPv6);
            Assert.AreEqual(url.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.AreEqual(url.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.AreEqual(url.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.AreEqual(url.MinConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.AreEqual(url.ReadConcernLevel, settings.ReadConcern.Level);
            Assert.AreEqual(url.ReadPreference, settings.ReadPreference);
            Assert.AreEqual(url.ReplicaSetName, settings.ReplicaSetName);
            Assert.AreEqual(url.LocalThreshold, settings.LocalThreshold);
            Assert.IsTrue(url.Servers.SequenceEqual(settings.Servers));
            Assert.AreEqual(url.ServerSelectionTimeout, settings.ServerSelectionTimeout);
            Assert.AreEqual(url.SocketTimeout, settings.SocketTimeout);
            Assert.AreEqual(null, settings.SslSettings);
            Assert.AreEqual(url.UseSsl, settings.UseSsl);
            Assert.AreEqual(url.VerifySslCertificate, settings.VerifySslCertificate);
            Assert.AreEqual(url.ComputedWaitQueueSize, settings.WaitQueueSize);
            Assert.AreEqual(url.WaitQueueTimeout, settings.WaitQueueTimeout);
            Assert.AreEqual(url.GetWriteConcern(true), settings.WriteConcern);
        }

        [Test]
        public void TestFromUrlWithMongoDBX509()
        {
            var url = new MongoUrl("mongodb://username@localhost/?authMechanism=MONGODB-X509");
            var settings = MongoClientSettings.FromUrl(url);

            var credential = settings.Credentials.Single();
            Assert.AreEqual("MONGODB-X509", credential.Mechanism);
            Assert.AreEqual("username", credential.Username);
            Assert.IsInstanceOf<ExternalEvidence>(credential.Evidence);
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
        public void TestReadConcern()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(ReadConcern.Default, settings.ReadConcern);

            var readConcern = ReadConcern.Majority;
            settings.ReadConcern = readConcern;
            Assert.AreSame(readConcern, settings.ReadConcern);

            settings.Freeze();
            Assert.AreEqual(readConcern, settings.ReadConcern);
            Assert.Throws<InvalidOperationException>(() => { settings.ReadConcern = ReadConcern.Default; });
        }

        [Test]
        public void TestReadPreference()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(ReadPreference.Primary, settings.ReadPreference);

            var readPreference = ReadPreference.Primary;
            settings.ReadPreference = readPreference;
            Assert.AreSame(readPreference, settings.ReadPreference);

            settings.Freeze();
            Assert.AreEqual(readPreference, settings.ReadPreference);
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
        public void TestLocalThreshold()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(MongoDefaults.LocalThreshold, settings.LocalThreshold);

            var localThreshold = new TimeSpan(1, 2, 3);
            settings.LocalThreshold = localThreshold;
            Assert.AreEqual(localThreshold, settings.LocalThreshold);

            settings.Freeze();
            Assert.AreEqual(localThreshold, settings.LocalThreshold);
            Assert.Throws<InvalidOperationException>(() => { settings.LocalThreshold = localThreshold; });
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
        public void TestSocketConfigurator()
        {
            var settings = DriverTestConfiguration.Client.Settings.Clone();
            var socketConfiguratorWasCalled = false;
            Action<Socket> socketConfigurator = s => { socketConfiguratorWasCalled = true; };
            settings.ClusterConfigurator = cb => cb.ConfigureTcp(tcp => tcp.With(socketConfigurator: socketConfigurator));
            var subject = new MongoClient(settings);

            SpinWait.SpinUntil(() => subject.Cluster.Description.State == ClusterState.Connected, TimeSpan.FromSeconds(4));

            Assert.That(socketConfiguratorWasCalled, Is.True);
        }

        [Test]
        public void TestServerSelectionTimeout()
        {
            var settings = new MongoClientSettings();
            Assert.AreEqual(MongoDefaults.ServerSelectionTimeout, settings.ServerSelectionTimeout);

            var serverSelectionTimeout = new TimeSpan(1, 2, 3);
            settings.ServerSelectionTimeout = serverSelectionTimeout;
            Assert.AreEqual(serverSelectionTimeout, settings.ServerSelectionTimeout);

            settings.Freeze();
            Assert.AreEqual(serverSelectionTimeout, settings.ServerSelectionTimeout);
            Assert.Throws<InvalidOperationException>(() => { settings.ServerSelectionTimeout = serverSelectionTimeout; });
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

            settings.Freeze();
            Assert.AreEqual(writeConcern, settings.WriteConcern);
            Assert.Throws<InvalidOperationException>(() => { settings.WriteConcern = writeConcern; });
        }

        [Test]
        public void ToClusterKey_should_copy_relevant_values()
        {
            var credentials = new[] { MongoCredential.CreateMongoCRCredential("source", "username", "password") };
            var servers = new[] { new MongoServerAddress("localhost") };
            var sslSettings = new SslSettings
            {
                CheckCertificateRevocation = true,
                EnabledSslProtocols = SslProtocols.Ssl3
            };

            var subject = new MongoClientSettings
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectTimeout = TimeSpan.FromSeconds(1),
                Credentials = credentials,
                GuidRepresentation = GuidRepresentation.Standard,
                IPv6 = true,
                MaxConnectionIdleTime = TimeSpan.FromSeconds(2),
                MaxConnectionLifeTime = TimeSpan.FromSeconds(3),
                MaxConnectionPoolSize = 10,
                MinConnectionPoolSize = 5,
                ReplicaSetName = "rs",
                LocalThreshold = TimeSpan.FromMilliseconds(20),
                Servers = servers,
                ServerSelectionTimeout = TimeSpan.FromSeconds(6),
                SocketTimeout = TimeSpan.FromSeconds(4),
                SslSettings = sslSettings,
                UseSsl = true,
                VerifySslCertificate = true,
                WaitQueueSize = 20,
                WaitQueueTimeout = TimeSpan.FromSeconds(5)
            };

            var result = subject.ToClusterKey();

            result.ConnectionMode.Should().Be(subject.ConnectionMode);
            result.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            result.Credentials.Should().Equal(subject.Credentials);
            result.IPv6.Should().Be(subject.IPv6);
            result.MaxConnectionIdleTime.Should().Be(subject.MaxConnectionIdleTime);
            result.MaxConnectionLifeTime.Should().Be(subject.MaxConnectionLifeTime);
            result.MaxConnectionPoolSize.Should().Be(subject.MaxConnectionPoolSize);
            result.MinConnectionPoolSize.Should().Be(subject.MinConnectionPoolSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.LocalThreshold.Should().Be(subject.LocalThreshold);
            result.Servers.Should().Equal(subject.Servers);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
            result.SocketTimeout.Should().Be(subject.SocketTimeout);
            result.SslSettings.Should().Be(subject.SslSettings);
            result.UseSsl.Should().Be(subject.UseSsl);
            result.VerifySslCertificate.Should().Be(subject.VerifySslCertificate);
            result.WaitQueueSize.Should().Be(subject.WaitQueueSize);
            result.WaitQueueTimeout.Should().Be(subject.WaitQueueTimeout);
        }
    }
}
