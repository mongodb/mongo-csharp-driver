/* Copyright 2010-2016 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoServerSettingsTests
    {
        private readonly MongoServerAddress _localHost = new MongoServerAddress("localhost");

        [Theory]
        [InlineData(false, AddressFamily.InterNetwork)]
        [InlineData(true, AddressFamily.InterNetworkV6)]
        public void TestAddressFamilyObsolete(bool ipv6, AddressFamily addressFamily)
        {
#pragma warning disable 618
            var settings = new MongoServerSettings { IPv6 = ipv6 };
            Assert.Equal(addressFamily, settings.AddressFamily);
#pragma warning restore
        }

        [Fact]
        public void TestApplicationName()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(null, settings.ApplicationName);

            var applicationName = "app2";
            settings.ApplicationName = applicationName;
            Assert.Equal(applicationName, settings.ApplicationName);

            settings.Freeze();
            Assert.Equal(applicationName, settings.ApplicationName);
            Assert.Throws<InvalidOperationException>(() => { settings.ApplicationName = applicationName; });
        }

        [Fact]
        public void TestApplicationName_too_long()
        {
            var subject = new MongoServerSettings();
            var value = new string('x', 129);

            var exception = Record.Exception(() => subject.ApplicationName = value);

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("value");
        }

        [Fact]
        public void TestClone()
        {
            // set everything to non default values to test that all settings are cloned
            var connectionString =
                "mongodb://user1:password1@somehost/?appname=app;" +
                "connect=direct;connectTimeout=123;uuidRepresentation=pythonLegacy;ipv6=true;heartbeatInterval=1m;heartbeatTimeout=2m;" +
                "maxIdleTime=124;maxLifeTime=125;maxPoolSize=126;minPoolSize=127;" +
                "readPreference=secondary;readPreferenceTags=a:1,b:2;readPreferenceTags=c:3,d:4;localThreshold=128;socketTimeout=129;" +
                "serverSelectionTimeout=20s;ssl=true;sslVerifyCertificate=false;waitqueuesize=130;waitQueueTimeout=131;" +
                "w=1;fsync=true;journal=true;w=2;wtimeout=131;gssapiServiceName=other";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var settings = MongoServerSettings.FromUrl(url);

            // a few settings can only be made in code
            settings.Credentials = new[] { MongoCredential.CreateMongoCRCredential("database", "username", "password").WithMechanismProperty("SERVICE_NAME", "other") };
            settings.SslSettings = new SslSettings { CheckCertificateRevocation = false };

            var clone = settings.Clone();
            Assert.Equal(settings, clone);
        }

        [Fact]
        public void TestConnectionMode()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(ConnectionMode.Automatic, settings.ConnectionMode);

            var connectionMode = ConnectionMode.Direct;
            settings.ConnectionMode = connectionMode;
            Assert.Equal(connectionMode, settings.ConnectionMode);

            settings.Freeze();
            Assert.Equal(connectionMode, settings.ConnectionMode);
            Assert.Throws<InvalidOperationException>(() => { settings.ConnectionMode = connectionMode; });
        }

        [Fact]
        public void TestConnectTimeout()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(MongoDefaults.ConnectTimeout, settings.ConnectTimeout);

            var connectTimeout = new TimeSpan(1, 2, 3);
            settings.ConnectTimeout = connectTimeout;
            Assert.Equal(connectTimeout, settings.ConnectTimeout);

            settings.Freeze();
            Assert.Equal(connectTimeout, settings.ConnectTimeout);
            Assert.Throws<InvalidOperationException>(() => { settings.ConnectTimeout = connectTimeout; });
        }

        [Fact]
        public void TestCredentials()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(0, settings.Credentials.Count());
        }

        [Fact]
        public void TestDefaults()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(null, settings.ApplicationName);
            Assert.Equal(ConnectionMode.Automatic, settings.ConnectionMode);
            Assert.Equal(MongoDefaults.ConnectTimeout, settings.ConnectTimeout);
            Assert.Equal(0, settings.Credentials.Count());
            Assert.Equal(MongoDefaults.GuidRepresentation, settings.GuidRepresentation);
            Assert.Equal(ServerSettings.DefaultHeartbeatInterval, settings.HeartbeatInterval);
            Assert.Equal(ServerSettings.DefaultHeartbeatTimeout, settings.HeartbeatTimeout);
            Assert.Equal(false, settings.IPv6);
            Assert.Equal(MongoDefaults.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.Equal(MongoDefaults.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.Equal(MongoDefaults.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.Equal(MongoDefaults.MinConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.Equal(MongoDefaults.OperationTimeout, settings.OperationTimeout);
            Assert.Equal(ReadPreference.Primary, settings.ReadPreference);
            Assert.Equal(null, settings.ReplicaSetName);
            Assert.Equal(_localHost, settings.Server);
            Assert.Equal(_localHost, settings.Servers.First());
            Assert.Equal(1, settings.Servers.Count());
            Assert.Equal(MongoDefaults.ServerSelectionTimeout, settings.ServerSelectionTimeout);
            Assert.Equal(MongoDefaults.SocketTimeout, settings.SocketTimeout);
            Assert.Equal(null, settings.SslSettings);
            Assert.Equal(false, settings.UseSsl);
            Assert.Equal(true, settings.VerifySslCertificate);
            Assert.Equal(MongoDefaults.ComputedWaitQueueSize, settings.WaitQueueSize);
            Assert.Equal(MongoDefaults.WaitQueueTimeout, settings.WaitQueueTimeout);
            Assert.Equal(WriteConcern.Unacknowledged, settings.WriteConcern);
        }

        [Fact]
        public void TestEquals()
        {
            var settings = new MongoServerSettings();
            var clone = settings.Clone();
            Assert.True(clone.Equals(settings));

            clone = settings.Clone();
            clone.ApplicationName = "app2";
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.ConnectionMode = ConnectionMode.Direct;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.ConnectTimeout = new TimeSpan(1, 2, 3);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.Credentials = new[] { MongoCredential.CreateMongoCRCredential("db2", "user2", "password2") };
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.Credentials = new[] { MongoCredential.CreateMongoCRCredential("db1", "user2", "password2") };
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.GuidRepresentation = GuidRepresentation.PythonLegacy;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.HeartbeatInterval = new TimeSpan(1, 2, 3);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.HeartbeatTimeout = new TimeSpan(1, 2, 3);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.IPv6 = !settings.IPv6;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.MaxConnectionIdleTime = new TimeSpan(1, 2, 3);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.MaxConnectionLifeTime = new TimeSpan(1, 2, 3);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.MaxConnectionPoolSize = settings.MaxConnectionPoolSize + 1;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.MinConnectionPoolSize = settings.MinConnectionPoolSize + 1;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.OperationTimeout = TimeSpan.FromMilliseconds(20);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.ReadPreference = ReadPreference.Secondary;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.ReplicaSetName = "abc";
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.LocalThreshold = new TimeSpan(1, 2, 3);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.Server = new MongoServerAddress("someotherhost");
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.ServerSelectionTimeout = new TimeSpan(1, 2, 3);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.SocketTimeout = new TimeSpan(1, 2, 3);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.SslSettings = new SslSettings { CheckCertificateRevocation = false };
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.UseSsl = !settings.UseSsl;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.VerifySslCertificate = !settings.VerifySslCertificate;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.WaitQueueSize = settings.WaitQueueSize + 1;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.WaitQueueTimeout = new TimeSpan(1, 2, 3);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.WriteConcern = WriteConcern.W2;
            Assert.False(clone.Equals(settings));
        }

        [Fact]
        public void TestFreeze()
        {
            var settings = new MongoServerSettings();

            Assert.False(settings.IsFrozen);
            var hashCode = settings.GetHashCode();
            var stringRepresentation = settings.ToString();

            settings.Freeze();
            Assert.True(settings.IsFrozen);
            Assert.Equal(hashCode, settings.GetHashCode());
            Assert.Equal(stringRepresentation, settings.ToString());
        }

        [Fact]
        public void TestFromClientSettings()
        {
            // set everything to non default values to test that all settings are converted
            var connectionString =
                "mongodb://user1:password1@somehost/?authSource=db;authMechanismProperties=CANONICALIZE_HOST_NAME:true;" +
                "appname=app;connect=direct;connectTimeout=123;uuidRepresentation=pythonLegacy;ipv6=true;heartbeatInterval=1m;heartbeatTimeout=2m;" +
                "maxIdleTime=124;maxLifeTime=125;maxPoolSize=126;minPoolSize=127;" +
                "readPreference=secondary;readPreferenceTags=a:1,b:2;readPreferenceTags=c:3,d:4;localThreshold=128;socketTimeout=129;" +
                "serverSelectionTimeout=20s;ssl=true;sslVerifyCertificate=false;waitqueuesize=130;waitQueueTimeout=131;" +
                "w=1;fsync=true;journal=true;w=2;wtimeout=131;gssapiServiceName=other";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var clientSettings = MongoClientSettings.FromUrl(url);

            var settings = MongoServerSettings.FromClientSettings(clientSettings);
            Assert.Equal(url.ApplicationName, settings.ApplicationName);
            Assert.Equal(url.ConnectionMode, settings.ConnectionMode);
            Assert.Equal(url.ConnectTimeout, settings.ConnectTimeout);
            Assert.Equal(1, settings.Credentials.Count());
            Assert.Equal(url.Username, settings.Credentials.Single().Username);
            Assert.Equal(url.AuthenticationMechanism, settings.Credentials.Single().Mechanism);
            Assert.Equal("other", settings.Credentials.Single().GetMechanismProperty<string>("SERVICE_NAME", "mongodb"));
            Assert.Equal(true, settings.Credentials.Single().GetMechanismProperty<bool>("CANONICALIZE_HOST_NAME", false));
            Assert.Equal(url.AuthenticationSource, settings.Credentials.Single().Source);
            Assert.Equal(new PasswordEvidence(builder.Password), settings.Credentials.Single().Evidence);
            Assert.Equal(url.GuidRepresentation, settings.GuidRepresentation);
            Assert.Equal(url.HeartbeatInterval, settings.HeartbeatInterval);
            Assert.Equal(url.HeartbeatTimeout, settings.HeartbeatTimeout);
            Assert.Equal(url.IPv6, settings.IPv6);
            Assert.Equal(url.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.Equal(url.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.Equal(url.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.Equal(url.MinConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.Equal(url.ReadPreference, settings.ReadPreference);
            Assert.Equal(url.ReplicaSetName, settings.ReplicaSetName);
            Assert.Equal(url.LocalThreshold, settings.LocalThreshold);
            Assert.True(url.Servers.SequenceEqual(settings.Servers));
            Assert.Equal(url.ServerSelectionTimeout, settings.ServerSelectionTimeout);
            Assert.Equal(url.SocketTimeout, settings.SocketTimeout);
            Assert.Equal(null, settings.SslSettings);
            Assert.Equal(url.UseSsl, settings.UseSsl);
            Assert.Equal(url.VerifySslCertificate, settings.VerifySslCertificate);
            Assert.Equal(url.ComputedWaitQueueSize, settings.WaitQueueSize);
            Assert.Equal(url.WaitQueueTimeout, settings.WaitQueueTimeout);
            Assert.Equal(url.GetWriteConcern(true), settings.WriteConcern);
        }

        [Fact]
        public void TestFromUrl()
        {
            // set everything to non default values to test that all settings are converted
            var connectionString =
                "mongodb://user1:password1@somehost/?authSource=db;appname=app;" +
                "connect=direct;connectTimeout=123;uuidRepresentation=pythonLegacy;ipv6=true;heartbeatInterval=1m;heartbeatTimeout=2m;" +
                "maxIdleTime=124;maxLifeTime=125;maxPoolSize=126;minPoolSize=127;" +
                "readPreference=secondary;readPreferenceTags=a:1,b:2;readPreferenceTags=c:3,d:4;localThreshold=128;socketTimeout=129;" +
                "serverSelectionTimeout=20s;ssl=true;sslVerifyCertificate=false;waitqueuesize=130;waitQueueTimeout=131;" +
                "w=1;fsync=true;journal=true;w=2;wtimeout=131";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();

            var settings = MongoServerSettings.FromUrl(url);
            Assert.Equal(url.ApplicationName, settings.ApplicationName);
            Assert.Equal(url.ConnectionMode, settings.ConnectionMode);
            Assert.Equal(url.ConnectTimeout, settings.ConnectTimeout);
            Assert.Equal(1, settings.Credentials.Count());
            Assert.Equal(url.Username, settings.Credentials.Single().Username);
            Assert.Equal(url.AuthenticationMechanism, settings.Credentials.Single().Mechanism);
            Assert.Equal(url.AuthenticationSource, settings.Credentials.Single().Source);
            Assert.Equal(new PasswordEvidence(url.Password), settings.Credentials.Single().Evidence);
            Assert.Equal(url.GuidRepresentation, settings.GuidRepresentation);
            Assert.Equal(url.HeartbeatInterval, settings.HeartbeatInterval);
            Assert.Equal(url.HeartbeatTimeout, settings.HeartbeatTimeout);
            Assert.Equal(url.IPv6, settings.IPv6);
            Assert.Equal(url.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.Equal(url.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.Equal(url.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.Equal(url.MinConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.Equal(url.ReadPreference, settings.ReadPreference);
            Assert.Equal(url.ReplicaSetName, settings.ReplicaSetName);
            Assert.Equal(url.LocalThreshold, settings.LocalThreshold);
            Assert.True(url.Servers.SequenceEqual(settings.Servers));
            Assert.Equal(url.ServerSelectionTimeout, settings.ServerSelectionTimeout);
            Assert.Equal(url.SocketTimeout, settings.SocketTimeout);
            Assert.Equal(null, settings.SslSettings);
            Assert.Equal(url.UseSsl, settings.UseSsl);
            Assert.Equal(url.VerifySslCertificate, settings.VerifySslCertificate);
            Assert.Equal(url.ComputedWaitQueueSize, settings.WaitQueueSize);
            Assert.Equal(url.WaitQueueTimeout, settings.WaitQueueTimeout);
            Assert.Equal(url.GetWriteConcern(false), settings.WriteConcern);
        }

        [Fact]
        public void TestFrozenCopy()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(false, settings.IsFrozen);

            var frozenCopy = settings.FrozenCopy();
            Assert.Equal(true, frozenCopy.IsFrozen);
            Assert.NotSame(settings, frozenCopy);
            Assert.Equal(settings, frozenCopy);

            var secondFrozenCopy = frozenCopy.FrozenCopy();
            Assert.Same(frozenCopy, secondFrozenCopy);
        }

        [Fact]
        public void TestGuidRepresentation()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(MongoDefaults.GuidRepresentation, settings.GuidRepresentation);

            var guidRepresentation = GuidRepresentation.PythonLegacy;
            settings.GuidRepresentation = guidRepresentation;
            Assert.Equal(guidRepresentation, settings.GuidRepresentation);

            settings.Freeze();
            Assert.Equal(guidRepresentation, settings.GuidRepresentation);
            Assert.Throws<InvalidOperationException>(() => { settings.GuidRepresentation = guidRepresentation; });
        }

        [Fact]
        public void TestHeartbeatInterval()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(ServerSettings.DefaultHeartbeatInterval, settings.HeartbeatInterval);

            var heartbeatInterval = new TimeSpan(1, 2, 3);
            settings.HeartbeatInterval = heartbeatInterval;
            Assert.Equal(heartbeatInterval, settings.HeartbeatInterval);

            settings.Freeze();
            Assert.Equal(heartbeatInterval, settings.HeartbeatInterval);
            Assert.Throws<InvalidOperationException>(() => { settings.HeartbeatInterval = heartbeatInterval; });
        }

        [Fact]
        public void TestHeartbeatTimeout()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(ServerSettings.DefaultHeartbeatTimeout, settings.HeartbeatTimeout);

            var heartbeatTimeout = new TimeSpan(1, 2, 3);
            settings.HeartbeatTimeout = heartbeatTimeout;
            Assert.Equal(heartbeatTimeout, settings.HeartbeatTimeout);

            settings.Freeze();
            Assert.Equal(heartbeatTimeout, settings.HeartbeatTimeout);
            Assert.Throws<InvalidOperationException>(() => { settings.HeartbeatTimeout = heartbeatTimeout; });
        }

        [Fact]
        public void TestIPv6()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(false, settings.IPv6);

            var ipv6 = true;
            settings.IPv6 = ipv6;
            Assert.Equal(ipv6, settings.IPv6);

            settings.Freeze();
            Assert.Equal(ipv6, settings.IPv6);
            Assert.Throws<InvalidOperationException>(() => { settings.IPv6 = ipv6; });
        }

        [Fact]
        public void TestMaxConnectionIdleTime()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(MongoDefaults.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);

            var maxConnectionIdleTime = new TimeSpan(1, 2, 3);
            settings.MaxConnectionIdleTime = maxConnectionIdleTime;
            Assert.Equal(maxConnectionIdleTime, settings.MaxConnectionIdleTime);

            settings.Freeze();
            Assert.Equal(maxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.Throws<InvalidOperationException>(() => { settings.MaxConnectionIdleTime = maxConnectionIdleTime; });
        }

        [Fact]
        public void TestMaxConnectionLifeTime()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(MongoDefaults.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);

            var maxConnectionLifeTime = new TimeSpan(1, 2, 3);
            settings.MaxConnectionLifeTime = maxConnectionLifeTime;
            Assert.Equal(maxConnectionLifeTime, settings.MaxConnectionLifeTime);

            settings.Freeze();
            Assert.Equal(maxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.Throws<InvalidOperationException>(() => { settings.MaxConnectionLifeTime = maxConnectionLifeTime; });
        }

        [Fact]
        public void TestMaxConnectionPoolSize()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(MongoDefaults.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);

            var maxConnectionPoolSize = 123;
            settings.MaxConnectionPoolSize = maxConnectionPoolSize;
            Assert.Equal(maxConnectionPoolSize, settings.MaxConnectionPoolSize);

            settings.Freeze();
            Assert.Equal(maxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.Throws<InvalidOperationException>(() => { settings.MaxConnectionPoolSize = maxConnectionPoolSize; });
        }

        [Fact]
        public void TestMinConnectionPoolSize()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(MongoDefaults.MinConnectionPoolSize, settings.MinConnectionPoolSize);

            var minConnectionPoolSize = 123;
            settings.MinConnectionPoolSize = minConnectionPoolSize;
            Assert.Equal(minConnectionPoolSize, settings.MinConnectionPoolSize);

            settings.Freeze();
            Assert.Equal(minConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.Throws<InvalidOperationException>(() => { settings.MinConnectionPoolSize = minConnectionPoolSize; });
        }

        [Fact]
        public void TestOperationTimeout()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(MongoDefaults.OperationTimeout, settings.OperationTimeout);

            var operationTimeout = new TimeSpan(1, 2, 3);
            settings.OperationTimeout = operationTimeout;
            Assert.Equal(operationTimeout, settings.OperationTimeout);

            settings.Freeze();
            Assert.Equal(operationTimeout, settings.OperationTimeout);
            Assert.Throws<InvalidOperationException>(() => { settings.OperationTimeout = operationTimeout; });
        }

        [Fact]
        public void TestReadPreference()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(ReadPreference.Primary, settings.ReadPreference);

            var readPreference = ReadPreference.Primary;
            settings.ReadPreference = readPreference;
            Assert.Same(readPreference, settings.ReadPreference);

            settings.Freeze();
            Assert.Equal(readPreference, settings.ReadPreference);
            Assert.Throws<InvalidOperationException>(() => { settings.ReadPreference = readPreference; });
        }

        [Fact]
        public void TestReplicaSetName()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(null, settings.ReplicaSetName);

            var replicaSetName = "abc";
            settings.ReplicaSetName = replicaSetName;
            Assert.Same(replicaSetName, settings.ReplicaSetName);

            settings.Freeze();
            Assert.Same(replicaSetName, settings.ReplicaSetName);
            Assert.Throws<InvalidOperationException>(() => { settings.ReplicaSetName = replicaSetName; });
        }

        [Fact]
        public void TestLocalThreshold()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(MongoDefaults.LocalThreshold, settings.LocalThreshold);

            var localThreshold = new TimeSpan(1, 2, 3);
            settings.LocalThreshold = localThreshold;
            Assert.Equal(localThreshold, settings.LocalThreshold);

            settings.Freeze();
            Assert.Equal(localThreshold, settings.LocalThreshold);
            Assert.Throws<InvalidOperationException>(() => { settings.LocalThreshold = localThreshold; });
        }

        [Fact]
        public void TestServer()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(_localHost, settings.Server);
            Assert.True(new[] { _localHost }.SequenceEqual(settings.Servers));

            var server = new MongoServerAddress("server");
            var servers = new[] { server };
            settings.Server = server;
            Assert.Equal(server, settings.Server);
            Assert.True(servers.SequenceEqual(settings.Servers));

            settings.Freeze();
            Assert.Equal(server, settings.Server);
            Assert.True(servers.SequenceEqual(settings.Servers));
            Assert.Throws<InvalidOperationException>(() => { settings.Server = server; });
        }

        [Fact]
        public void TestServersWithOneServer()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(_localHost, settings.Server);
            Assert.True(new[] { _localHost }.SequenceEqual(settings.Servers));

            var server = new MongoServerAddress("server");
            var servers = new[] { server };
            settings.Servers = servers;
            Assert.Equal(server, settings.Server);
            Assert.True(servers.SequenceEqual(settings.Servers));

            settings.Freeze();
            Assert.Equal(server, settings.Server);
            Assert.True(servers.SequenceEqual(settings.Servers));
            Assert.Throws<InvalidOperationException>(() => { settings.Server = server; });
        }

        [Fact]
        public void TestServersWithTwoServers()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(_localHost, settings.Server);
            Assert.True(new[] { _localHost }.SequenceEqual(settings.Servers));

            var servers = new MongoServerAddress[]
            {
                new MongoServerAddress("server1"),
                new MongoServerAddress("server2")
            };
            settings.Servers = servers;
            Assert.Throws<InvalidOperationException>(() => { var s = settings.Server; });
            Assert.True(servers.SequenceEqual(settings.Servers));

            settings.Freeze();
            Assert.Throws<InvalidOperationException>(() => { var s = settings.Server; });
            Assert.True(servers.SequenceEqual(settings.Servers));
            Assert.Throws<InvalidOperationException>(() => { settings.Servers = servers; });
        }

        [Fact]
        public void TestServerSelectionTimeout()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(MongoDefaults.ServerSelectionTimeout, settings.ServerSelectionTimeout);

            var serverSelectionTimeout = new TimeSpan(1, 2, 3);
            settings.ServerSelectionTimeout = serverSelectionTimeout;
            Assert.Equal(serverSelectionTimeout, settings.ServerSelectionTimeout);

            settings.Freeze();
            Assert.Equal(serverSelectionTimeout, settings.ServerSelectionTimeout);
            Assert.Throws<InvalidOperationException>(() => { settings.ServerSelectionTimeout = serverSelectionTimeout; });
        }

        [Fact]
        public void TestSocketTimeout()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(MongoDefaults.SocketTimeout, settings.SocketTimeout);

            var socketTimeout = new TimeSpan(1, 2, 3);
            settings.SocketTimeout = socketTimeout;
            Assert.Equal(socketTimeout, settings.SocketTimeout);

            settings.Freeze();
            Assert.Equal(socketTimeout, settings.SocketTimeout);
            Assert.Throws<InvalidOperationException>(() => { settings.SocketTimeout = socketTimeout; });
        }

        [Fact]
        public void TestSslSettings()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(null, settings.SslSettings);

            var sslSettings = new SslSettings { CheckCertificateRevocation = false };
            settings.SslSettings = sslSettings;
            Assert.Equal(sslSettings, settings.SslSettings);

            settings.Freeze();
            Assert.Equal(sslSettings, settings.SslSettings);
            Assert.Throws<InvalidOperationException>(() => { settings.SslSettings = sslSettings; });
        }

        [Fact]
        public void TestUseSsl()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(false, settings.UseSsl);

            var useSsl = true;
            settings.UseSsl = useSsl;
            Assert.Equal(useSsl, settings.UseSsl);

            settings.Freeze();
            Assert.Equal(useSsl, settings.UseSsl);
            Assert.Throws<InvalidOperationException>(() => { settings.UseSsl = useSsl; });
        }

        [Fact]
        public void TestVerifySslCertificate()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(true, settings.VerifySslCertificate);

            var verifySslCertificate = false;
            settings.VerifySslCertificate = verifySslCertificate;
            Assert.Equal(verifySslCertificate, settings.VerifySslCertificate);

            settings.Freeze();
            Assert.Equal(verifySslCertificate, settings.VerifySslCertificate);
            Assert.Throws<InvalidOperationException>(() => { settings.VerifySslCertificate = verifySslCertificate; });
        }

        [Fact]
        public void TestWaitQueueSize()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(MongoDefaults.ComputedWaitQueueSize, settings.WaitQueueSize);

            var waitQueueSize = 123;
            settings.WaitQueueSize = waitQueueSize;
            Assert.Equal(waitQueueSize, settings.WaitQueueSize);

            settings.Freeze();
            Assert.Equal(waitQueueSize, settings.WaitQueueSize);
            Assert.Throws<InvalidOperationException>(() => { settings.WaitQueueSize = waitQueueSize; });
        }

        [Fact]
        public void TestWaitQueueTimeout()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(MongoDefaults.WaitQueueTimeout, settings.WaitQueueTimeout);

            var waitQueueTimeout = new TimeSpan(1, 2, 3);
            settings.WaitQueueTimeout = waitQueueTimeout;
            Assert.Equal(waitQueueTimeout, settings.WaitQueueTimeout);

            settings.Freeze();
            Assert.Equal(waitQueueTimeout, settings.WaitQueueTimeout);
            Assert.Throws<InvalidOperationException>(() => { settings.WaitQueueTimeout = waitQueueTimeout; });
        }

        [Fact]
        public void TestWriteConcern()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(WriteConcern.Unacknowledged, settings.WriteConcern);

            var writeConcern = new WriteConcern();
            settings.WriteConcern = writeConcern;
            Assert.Same(writeConcern, settings.WriteConcern);

            settings.Freeze();
            Assert.Equal(writeConcern, settings.WriteConcern);
            Assert.Throws<InvalidOperationException>(() => { settings.WriteConcern = writeConcern; });
        }

        [Fact]
        public void ToClusterKey_should_copy_relevant_values()
        {
            var credentials = new[] { MongoCredential.CreateMongoCRCredential("source", "username", "password") };
            var servers = new[] { new MongoServerAddress("localhost") };
            var sslSettings = new SslSettings
            {
                CheckCertificateRevocation = true,
                EnabledSslProtocols = SslProtocols.Tls
            };

            var subject = new MongoServerSettings
            {
                ApplicationName = "app",
                ConnectionMode = ConnectionMode.Direct,
                ConnectTimeout = TimeSpan.FromSeconds(1),
                Credentials = credentials,
                GuidRepresentation = GuidRepresentation.Standard,
                HeartbeatInterval = TimeSpan.FromMinutes(1),
                HeartbeatTimeout = TimeSpan.FromMinutes(2),
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

            result.ApplicationName.Should().Be(subject.ApplicationName);
            result.ConnectionMode.Should().Be(subject.ConnectionMode);
            result.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            result.Credentials.Should().Equal(subject.Credentials);
            result.HeartbeatInterval.Should().Be(subject.HeartbeatInterval);
            result.HeartbeatTimeout.Should().Be(subject.HeartbeatTimeout);
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
