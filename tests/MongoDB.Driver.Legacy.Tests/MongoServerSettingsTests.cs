/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Core.Clusters;
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
        public void TestAllowInsecureTls()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(false, settings.AllowInsecureTls);

            var allowInsecureTls = true;
            settings.AllowInsecureTls = allowInsecureTls;
            Assert.Equal(allowInsecureTls, settings.AllowInsecureTls);
            settings.SslSettings.CheckCertificateRevocation.Should().BeFalse();

            settings.Freeze();
            Assert.Equal(allowInsecureTls, settings.AllowInsecureTls);
            Assert.Throws<InvalidOperationException>(() => { settings.AllowInsecureTls = allowInsecureTls; });
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
            // set everything to non default values to test except tlsDisableCertificateRevocationCheck
            // so that all settings are cloned (tlsDisableCertificateRevocationCheck cannot be set with tlsInsecure
            // in the connection string)
            var connectionString =
                "mongodb://user1:password1@somehost/?appname=app;" +
                "connect=direct;connectTimeout=123;ipv6=true;heartbeatInterval=1m;heartbeatTimeout=2m;localThreshold=128;" +
                "maxIdleTime=124;maxLifeTime=125;maxPoolSize=126;minPoolSize=127;" +
                "readPreference=secondary;readPreferenceTags=a:1,b:2;readPreferenceTags=c:3,d:4;socketTimeout=129;" +
                "serverSelectionTimeout=20s;ssl=true;sslVerifyCertificate=false;waitqueuesize=130;waitQueueTimeout=131;" +
                "w=1;fsync=true;journal=true;w=2;wtimeout=131;gssapiServiceName=other";
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                connectionString += ";uuidRepresentation=pythonLegacy";
            }
#pragma warning restore 618
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var settings = MongoServerSettings.FromUrl(url);

            // a few settings can only be made in code
#pragma warning disable 618
            settings.Credential = MongoCredential.CreateMongoCRCredential("database", "username", "password").WithMechanismProperty("SERVICE_NAME", "other");
#pragma warning restore 618
            settings.SslSettings = new SslSettings { CheckCertificateRevocation = !url.TlsDisableCertificateRevocationCheck };
            settings.SdamLogFilename = "unimatrix-zero";

            var clone = settings.Clone();

            Assert.Equal(settings, clone);
        }

#pragma warning disable CS0618 // Type or member is obsolete
        [Fact]
        public void TestCloneTlsDisableCertificateRevocationCheck()
        {
            var connectionString = "mongodb://somehost/?tlsDisableCertificateRevocationCheck=true";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var settings = MongoServerSettings.FromUrl(url);

            var clone = settings.Clone();

            clone.Should().Be(settings);
        }

        [Fact]
        public void TestCloneTlsInsecure()
        {
            var connectionString = "mongodb://somehost/?tlsInsecure=true";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var settings = MongoServerSettings.FromUrl(url);

            var clone = settings.Clone();

            clone.Should().Be(settings);
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
#pragma warning restore CS0618 // Type or member is obsolete

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
        public void TestCredential()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(null, settings.Credential);
        }

        [Fact]
        public void TestCredentials()
        {
            var settings = new MongoServerSettings();
#pragma warning disable 618
            Assert.Equal(0, settings.Credentials.Count());
#pragma warning restore
        }

        [Fact]
        public void TestDefaults()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(false, settings.AllowInsecureTls);
            Assert.Equal(null, settings.ApplicationName);
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Equal(ConnectionMode.Automatic, settings.ConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.Equal(MongoDefaults.ConnectTimeout, settings.ConnectTimeout);
#pragma warning disable 618
            Assert.Equal(0, settings.Credentials.Count());
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                Assert.Equal(MongoDefaults.GuidRepresentation, settings.GuidRepresentation);
            }
#pragma warning restore 618
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
            Assert.Equal(true, settings.RetryReads);
            Assert.Equal(true, settings.RetryWrites);
            Assert.Equal(ConnectionStringScheme.MongoDB, settings.Scheme);
            Assert.Equal(null, settings.SdamLogFilename);
            Assert.Equal(_localHost, settings.Server);
            Assert.Equal(_localHost, settings.Servers.First());
            Assert.Equal(1, settings.Servers.Count());
            Assert.Equal(MongoDefaults.ServerSelectionTimeout, settings.ServerSelectionTimeout);
            Assert.Equal(MongoDefaults.SocketTimeout, settings.SocketTimeout);
            settings.SslSettings.Should().BeNull();
#pragma warning disable 618
            Assert.Equal(false, settings.UseSsl);
#pragma warning restore 618
            Assert.Equal(false, settings.UseTls);
#pragma warning disable 618
            Assert.Equal(true, settings.VerifySslCertificate);
#pragma warning restore 618
#pragma warning disable 618
            Assert.Equal(MongoDefaults.ComputedWaitQueueSize, settings.WaitQueueSize);
#pragma warning restore 618
            Assert.Equal(MongoDefaults.WaitQueueTimeout, settings.WaitQueueTimeout);
            Assert.Equal(WriteConcern.Unacknowledged, settings.WriteConcern);
        }

        [Fact]
        public void TestDirectConnection()
        {
            var settings = new MongoClientSettings();
            settings.DirectConnection.Should().NotHaveValue();

            var directConnection = true;
            settings.DirectConnection = directConnection;
            settings.DirectConnection.Should().Be(directConnection);

            settings.Freeze();
            settings.DirectConnection.Should().Be(directConnection);
            var exception = Record.Exception(() => settings.DirectConnection = false);
            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Theory]
#pragma warning disable CS0618 // Type or member is obsolete
        [InlineData("connect", ConnectionMode.Automatic, "directConnection", true, true)]
        [InlineData("connect", ConnectionMode.Direct, "directConnection", false, true)]
        [InlineData("connect", ConnectionMode.Direct, "directConnection", null, true)]
        [InlineData("connect", ConnectionMode.ReplicaSet, "connect", ConnectionMode.ReplicaSet, false)]
        [InlineData("directConnection", false, "connect", ConnectionMode.Automatic, true)]
        [InlineData("directConnection", true, "connect", ConnectionMode.Direct, true)]
        [InlineData("directConnection", null, "connect", ConnectionMode.ReplicaSet, true)]
        [InlineData("directConnection", null, "directConnection", null, false)]
        [InlineData("directConnection", true, "directConnection", true, false)]
#pragma warning restore CS0618 // Type or member is obsolete
        public void TestThatUsingPropertyPairsWorksAsExpected(string property1, object value1, string property2, object value2, bool shouldFailOnSecondAttempt)
        {
            var settings = new MongoServerSettings();
#pragma warning disable CS0618 // Type or member is obsolete
            settings.ConnectionMode.Should().Be(ConnectionMode.Automatic);
            settings.ConnectionModeSwitch.Should().Be(ConnectionModeSwitch.NotSet);
#pragma warning restore CS0618 // Type or member is obsolete
            settings.DirectConnection.Should().NotHaveValue();

            var testSteps = new (string Property, object Value, bool ShouldFail)[]
            {
                (property1, value1, false),
                (property2, value2, shouldFailOnSecondAttempt)
            };

#pragma warning disable CS0618 // Type or member is obsolete
            ConnectionModeSwitch? firstConnectionModeSwitch = null;
#pragma warning restore CS0618 // Type or member is obsolete

            foreach (var propertySet in testSteps)
            {
                switch (propertySet.Property)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    case "connect":
                        {
                            // get
                            AssertException(Record.Exception(() => _ = settings.ConnectionMode), propertySet.ShouldFail);
                            // set
                            AssertException(Record.Exception(() => settings.ConnectionMode = (ConnectionMode)propertySet.Value), propertySet.ShouldFail);
                        }
                        break;
#pragma warning restore CS0618 // Type or member is obsolete
                    case "directConnection":
                        {
                            // get
                            AssertException(Record.Exception(() => _ = settings.DirectConnection), propertySet.ShouldFail);
                            // set
                            AssertException(Record.Exception(() => settings.DirectConnection = (bool?)propertySet.Value), propertySet.ShouldFail);
                        }
                        break;
                    default: throw new Exception($"Unexpected property {propertySet.Property}.");
                }

#pragma warning disable CS0618 // Type or member is obsolete
                if (!firstConnectionModeSwitch.HasValue)
                {
                    firstConnectionModeSwitch = settings.ConnectionModeSwitch;
                }
                settings.ConnectionModeSwitch.Should().Be(firstConnectionModeSwitch); // the exception won't change it
#pragma warning restore CS0618 // Type or member is obsolete
            }

            void AssertException(Exception ex, bool shouldFail)
            {
                if (shouldFail)
                {
                    ex.Should().BeOfType<InvalidOperationException>();
                }
                else
                {
                    ex.Should().BeNull();
                }
            }
        }

        [Fact]
        public void TestEquals()
        {
            var settings = new MongoServerSettings();
            var clone = settings.Clone();
            Assert.True(clone.Equals(settings));

            clone = settings.Clone();
            clone.AllowInsecureTls = !settings.AllowInsecureTls;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.ApplicationName = "app2";
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
#pragma warning disable CS0618
            clone.ConnectionMode = ConnectionMode.Direct;
#pragma warning restore CS0618
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.ConnectTimeout = new TimeSpan(1, 2, 3);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
#pragma warning disable 618
            clone.Credential = MongoCredential.CreateMongoCRCredential("db2", "user2", "password2");
#pragma warning restore 618
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
#pragma warning disable 618
            clone.Credential = MongoCredential.CreateMongoCRCredential("db1", "user2", "password2");
#pragma warning restore 618
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.DirectConnection = true;
            Assert.False(clone.Equals(settings));

#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                clone = settings.Clone();
                clone.GuidRepresentation = settings.GuidRepresentation == GuidRepresentation.CSharpLegacy ? GuidRepresentation.PythonLegacy : GuidRepresentation.CSharpLegacy;
                Assert.False(clone.Equals(settings));
            }
#pragma warning restore 618

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
            clone.LocalThreshold = new TimeSpan(1, 2, 3);
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
            clone.RetryReads = false;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.RetryWrites = false;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.Scheme = ConnectionStringScheme.MongoDBPlusSrv;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.SdamLogFilename = "osiris";
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
#pragma warning disable 618
            clone.UseSsl = !settings.UseSsl;
#pragma warning restore 618
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.UseTls = !settings.UseTls;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
#pragma warning disable 618
            clone.VerifySslCertificate = !settings.VerifySslCertificate;
#pragma warning restore 618
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
#pragma warning disable 618
            clone.WaitQueueSize = settings.WaitQueueSize + 1;
#pragma warning restore 618
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
        public void TestFreezeInvalid()
        {
            var settings = new MongoServerSettings();
            settings.AllowInsecureTls = true;
            settings.SslSettings.CheckCertificateRevocation = true;

            var exception = Record.Exception(() => settings.Freeze());

            exception.Should().BeOfType<InvalidOperationException>();

            settings = new MongoServerSettings();
            settings.DirectConnection = true;
            settings.Scheme = ConnectionStringScheme.MongoDBPlusSrv;

            exception = Record.Exception(() => settings.Freeze());

            exception.Should().BeOfType<InvalidOperationException>();

            settings = new MongoServerSettings();
            settings.DirectConnection = true;
            var endpoint = "test5.test.build.10gen.cc:53";
            settings.Servers = new[] { MongoServerAddress.Parse(endpoint), MongoServerAddress.Parse(endpoint) };

            exception = Record.Exception(() => settings.Freeze());

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void TestFromClientSettings()
        {
            // set everything to non default values to test that all settings are converted
            // except tlsDisableCertificateRevocationCheck because setting that with tlsInsecure is not allowed in
            // a connection string
            var connectionString =
                "mongodb://user1:password1@somehost/?authSource=db;authMechanismProperties=CANONICALIZE_HOST_NAME:true;" +
                "appname=app;connect=direct;connectTimeout=123;ipv6=true;heartbeatInterval=1m;heartbeatTimeout=2m;localThreshold=128;" +
                "maxIdleTime=124;maxLifeTime=125;maxPoolSize=126;minPoolSize=127;" +
                "readPreference=secondary;readPreferenceTags=a:1,b:2;readPreferenceTags=c:3,d:4;retryReads=false;retryWrites=true;socketTimeout=129;" +
                "serverSelectionTimeout=20s;ssl=true;sslVerifyCertificate=false;waitqueuesize=130;waitQueueTimeout=131;" +
                "w=1;fsync=true;journal=true;w=2;wtimeout=131;gssapiServiceName=other";
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                connectionString += ";uuidRepresentation=pythonLegacy";
            }
#pragma warning restore
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var clientSettings = MongoClientSettings.FromUrl(url);
            clientSettings.SdamLogFilename = "section-31";

            var settings = MongoServerSettings.FromClientSettings(clientSettings);

            Assert.Equal(url.AllowInsecureTls, settings.AllowInsecureTls);
            Assert.Equal(url.ApplicationName, settings.ApplicationName);
#pragma warning disable CS0618
            Assert.Equal(url.ConnectionMode, settings.ConnectionMode);
#pragma warning restore CS0618
            Assert.Equal(url.ConnectTimeout, settings.ConnectTimeout);
#pragma warning disable 618
            Assert.Equal(1, settings.Credentials.Count());
#pragma warning restore
            Assert.Equal(url.Username, settings.Credential.Username);
            Assert.Equal(url.AuthenticationMechanism, settings.Credential.Mechanism);
            Assert.Equal("other", settings.Credential.GetMechanismProperty<string>("SERVICE_NAME", "mongodb"));
            Assert.Equal(true, settings.Credential.GetMechanismProperty<bool>("CANONICALIZE_HOST_NAME", false));
            Assert.Equal(url.AuthenticationSource, settings.Credential.Source);
            Assert.Equal(new PasswordEvidence(builder.Password), settings.Credential.Evidence);
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                Assert.Equal(url.GuidRepresentation, settings.GuidRepresentation);
            }
#pragma warning restore 618
            Assert.Equal(url.HeartbeatInterval, settings.HeartbeatInterval);
            Assert.Equal(url.HeartbeatTimeout, settings.HeartbeatTimeout);
            Assert.Equal(url.IPv6, settings.IPv6);
            Assert.Equal(url.LocalThreshold, settings.LocalThreshold);
            Assert.Equal(url.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.Equal(url.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.Equal(url.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.Equal(url.MinConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.Equal(url.ReadPreference, settings.ReadPreference);
            Assert.Equal(url.ReplicaSetName, settings.ReplicaSetName);
            Assert.Equal(url.RetryReads, settings.RetryReads);
            Assert.Equal(url.RetryWrites, settings.RetryWrites);
            Assert.Equal(url.Scheme, settings.Scheme);
            Assert.Equal(clientSettings.SdamLogFilename, settings.SdamLogFilename);
            Assert.True(url.Servers.SequenceEqual(settings.Servers));
            Assert.Equal(url.ServerSelectionTimeout, settings.ServerSelectionTimeout);
            Assert.Equal(url.SocketTimeout, settings.SocketTimeout);
            Assert.Equal(url.TlsDisableCertificateRevocationCheck, !settings.SslSettings.CheckCertificateRevocation);
#pragma warning disable 618
            Assert.Equal(url.UseSsl, settings.UseSsl);
#pragma warning restore 618
            Assert.Equal(url.UseTls, settings.UseTls);
#pragma warning disable 618
            Assert.Equal(url.VerifySslCertificate, settings.VerifySslCertificate);
#pragma warning restore 618
#pragma warning disable 618
            Assert.Equal(url.ComputedWaitQueueSize, settings.WaitQueueSize);
#pragma warning restore 618
            Assert.Equal(url.WaitQueueTimeout, settings.WaitQueueTimeout);
            Assert.Equal(url.GetWriteConcern(true), settings.WriteConcern);
        }

        [Fact]
        public void TestFromClientSettingsTlsDisableCertificateRevocationCheck()
        {
            var connectionString = "mongodb://lcars/?tlsDisableCertificateRevocationCheck=true";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var clientSettings = MongoClientSettings.FromUrl(url);

            var settings = MongoServerSettings.FromClientSettings(clientSettings);

            settings.SslSettings.Should().Be(new SslSettings { CheckCertificateRevocation = !url.TlsDisableCertificateRevocationCheck });
        }

        [Fact]
        public void TestFromClientSettingsTlsInsecure()
        {
            var connectionString = "mongodb://lcars/?tlsInsecure=true";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var clientSettings = MongoClientSettings.FromUrl(url);

            var settings = MongoServerSettings.FromClientSettings(clientSettings);

            settings.AllowInsecureTls.Should().BeTrue();
        }

        [Fact]
        public void TestFromUrl()
        {
            // set everything to non default values to test that all settings are converted
            // with the exception of tlsDisableCertificateRevocationCheck because setting that with tlsInsecure is
            // not allowed in a connection string
            var connectionString =
                "mongodb://user1:password1@somehost/?authSource=db;appname=app;" +
                "connect=direct;connectTimeout=123;ipv6=true;heartbeatInterval=1m;heartbeatTimeout=2m;localThreshold=128;" +
                "maxIdleTime=124;maxLifeTime=125;maxPoolSize=126;minPoolSize=127;" +
                "readPreference=secondary;readPreferenceTags=a:1,b:2;readPreferenceTags=c:3,d:4;retryReads=false;retryWrites=true;socketTimeout=129;" +
                "serverSelectionTimeout=20s;ssl=true;sslVerifyCertificate=false;waitqueuesize=130;waitQueueTimeout=131;" +
                "w=1;fsync=true;journal=true;w=2;wtimeout=131";
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                connectionString += ";uuidRepresentation=pythonLegacy";
            }
#pragma warning restore 618
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();

            var settings = MongoServerSettings.FromUrl(url);

            Assert.Equal(url.AllowInsecureTls, settings.AllowInsecureTls);
            Assert.Equal(url.ApplicationName, settings.ApplicationName);
#pragma warning disable CS0618
            Assert.Equal(url.ConnectionMode, settings.ConnectionMode);
#pragma warning restore CS0618
            Assert.Equal(url.ConnectTimeout, settings.ConnectTimeout);
#pragma warning disable 618
            Assert.Equal(1, settings.Credentials.Count());
#pragma warning restore
            Assert.Equal(url.Username, settings.Credential.Username);
            Assert.Equal(url.AuthenticationMechanism, settings.Credential.Mechanism);
            Assert.Equal(url.AuthenticationSource, settings.Credential.Source);
            Assert.Equal(new PasswordEvidence(url.Password), settings.Credential.Evidence);
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                Assert.Equal(url.GuidRepresentation, settings.GuidRepresentation);
            }
#pragma warning restore 618
            Assert.Equal(url.HeartbeatInterval, settings.HeartbeatInterval);
            Assert.Equal(url.HeartbeatTimeout, settings.HeartbeatTimeout);
            Assert.Equal(url.IPv6, settings.IPv6);
            Assert.Equal(url.LocalThreshold, settings.LocalThreshold);
            Assert.Equal(url.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.Equal(url.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.Equal(url.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.Equal(url.MinConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.Equal(url.ReadPreference, settings.ReadPreference);
            Assert.Equal(url.ReplicaSetName, settings.ReplicaSetName);
            Assert.Equal(url.RetryReads, settings.RetryReads);
            Assert.Equal(url.RetryWrites, settings.RetryWrites);
            Assert.Equal(url.Scheme, settings.Scheme);
            Assert.True(url.Servers.SequenceEqual(settings.Servers));
            Assert.Equal(url.ServerSelectionTimeout, settings.ServerSelectionTimeout);
            Assert.Equal(url.SocketTimeout, settings.SocketTimeout);
            Assert.Equal(url.TlsDisableCertificateRevocationCheck, !settings.SslSettings.CheckCertificateRevocation);
#pragma warning disable 618
            Assert.Equal(url.UseSsl, settings.UseSsl);
#pragma warning restore 618
            Assert.Equal(url.UseTls, settings.UseTls);
#pragma warning disable 618
            Assert.Equal(url.VerifySslCertificate, settings.VerifySslCertificate);
#pragma warning restore 618
#pragma warning disable 618
            Assert.Equal(url.ComputedWaitQueueSize, settings.WaitQueueSize);
#pragma warning restore 618
            Assert.Equal(url.WaitQueueTimeout, settings.WaitQueueTimeout);
            Assert.Equal(url.GetWriteConcern(false), settings.WriteConcern);
        }

        [Fact]
        public void TestFromUrlTlsDisableCertificateRevocationCheck()
        {
            var connectionString = "mongodb://unimatrix-zero/?tlsDisableCertificateRevocationCheck=true";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();

            var settings = MongoServerSettings.FromUrl(url);

            settings.SslSettings.Should().Be(new SslSettings { CheckCertificateRevocation = !url.TlsDisableCertificateRevocationCheck });
        }

        [Fact]
        public void TestFromUrlTlsInsecure()
        {
            var connectionString = "mongodb://unimatrix-zero/?tlsInsecure=true";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();

            var settings = MongoServerSettings.FromUrl(url);

            settings.AllowInsecureTls.Should().Be(url.AllowInsecureTls);
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
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
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
#pragma warning restore 618
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
        public void TestRetryReads()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(true, settings.RetryReads);

            var retryReads = false;
            settings.RetryReads = retryReads;
            Assert.Equal(retryReads, settings.RetryReads);

            settings.Freeze();
            Assert.Equal(retryReads, settings.RetryReads);
            Assert.Throws<InvalidOperationException>(() => { settings.RetryReads = true; });
        }

        [Fact]
        public void TestRetryWrites()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(true, settings.RetryWrites);

            var retryWrites = false;
            settings.RetryWrites = retryWrites;
            Assert.Equal(retryWrites, settings.RetryWrites);

            settings.Freeze();
            Assert.Equal(retryWrites, settings.RetryWrites);
            Assert.Throws<InvalidOperationException>(() => { settings.RetryWrites = true; });
        }

        [Fact]
        public void TestScheme()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(ConnectionStringScheme.MongoDB, settings.Scheme);

            var scheme = ConnectionStringScheme.MongoDBPlusSrv;
            settings.Scheme = scheme;
            Assert.Equal(scheme, settings.Scheme);

            settings.Freeze();
            Assert.Equal(scheme, settings.Scheme);
            Assert.Throws<InvalidOperationException>(() => { settings.Scheme = ConnectionStringScheme.MongoDBPlusSrv; });
        }

        [Fact]
        public void TestSdamLogFileName()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(null, settings.SdamLogFilename);

            var sdamLogFileName = "advanced-potion-making";
            settings.SdamLogFilename = sdamLogFileName;
            Assert.Same(sdamLogFileName, settings.SdamLogFilename);

            settings.Freeze();
            Assert.Same(sdamLogFileName, settings.SdamLogFilename);
            Assert.Throws<InvalidOperationException>(() => { settings.SdamLogFilename = sdamLogFileName; });
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
            settings.SslSettings.Should().BeNull();

            var sslSettings = new SslSettings { CheckCertificateRevocation = false };
            settings.SslSettings = sslSettings;
            Assert.Equal(sslSettings, settings.SslSettings);

            settings.Freeze();
            Assert.Equal(sslSettings, settings.SslSettings);
            Assert.Throws<InvalidOperationException>(() => { settings.SslSettings = sslSettings; });
        }

        [Theory]
#pragma warning disable CS0618
        [InlineData("connect", ConnectionMode.ReplicaSet, "directConnection", null)]
        [InlineData("connect", ConnectionMode.Standalone, "directConnection", true)]
        [InlineData("connect", ConnectionMode.ShardRouter, "directConnection", false)]
        [InlineData("directConnection", null, "connect", ConnectionMode.ReplicaSet)]
        [InlineData("directConnection", true, "connect", ConnectionMode.Standalone)]
        [InlineData("directConnection", false, "connect", ConnectionMode.ShardRouter)]
        public void TestThatSettingNotExpectedPropertiesThrows(string property1, object value1, string property2, object value2)
        {
            var settings = new MongoClientSettings();
            settings.ConnectionMode.Should().Be(ConnectionMode.Automatic);
            settings.DirectConnection.Should().NotHaveValue();

            var testSteps = new (string Property, object Value, bool ShouldFail)[]
            {
                (property1, value1, false),
                (property2, value2, true)
            };

            foreach (var propertySet in testSteps)
            {
                switch (propertySet.Property)
                {
#pragma warning disable CS0618
                    case "connect":
                        {
                            // get
                            AssertException(Record.Exception(() => _ = settings.ConnectionMode), propertySet.ShouldFail);
                            // set
                            AssertException(Record.Exception(() => settings.ConnectionMode = (ConnectionMode)propertySet.Value), propertySet.ShouldFail);
                        }
                        break;
#pragma warning restore CS0618
                    case "directConnection":
                        {
                            // get
                            AssertException(Record.Exception(() => _ = settings.DirectConnection), propertySet.ShouldFail);
                            // set
                            AssertException(Record.Exception(() => settings.DirectConnection = (bool?)propertySet.Value), propertySet.ShouldFail);
                        }
                        break;
                    default: throw new Exception($"Unexpected property {propertySet.Property}.");
                }
            }

            void AssertException(Exception ex, bool shouldFail)
            {
                if (shouldFail)
                {
                    ex.Should().BeOfType<InvalidOperationException>();
                }
                else
                {
                    ex.Should().BeNull();
                }
            }
        }

        [Fact]
        public void TestUseSsl()
        {
#pragma warning disable 618
            var settings = new MongoServerSettings();
            Assert.Equal(false, settings.UseSsl);

            var useSsl = true;
            settings.UseSsl = useSsl;
            Assert.Equal(useSsl, settings.UseSsl);

            settings.Freeze();
            Assert.Equal(useSsl, settings.UseSsl);
            Assert.Throws<InvalidOperationException>(() => { settings.UseSsl = useSsl; });
#pragma warning restore 618
        }

        [Fact]
        public void TestUseTls()
        {
            var settings = new MongoServerSettings();
            Assert.Equal(false, settings.UseTls);

            var useTls = true;
            settings.UseTls = useTls;
            Assert.Equal(useTls, settings.UseTls);

            settings.Freeze();
            Assert.Equal(useTls, settings.UseTls);
            Assert.Throws<InvalidOperationException>(() => { settings.UseTls = useTls; });
        }

        [Fact]
        public void TestVerifySslCertificate()
        {
#pragma warning disable 618
            var settings = new MongoServerSettings();
            Assert.Equal(true, settings.VerifySslCertificate);

            var verifySslCertificate = false;
            settings.VerifySslCertificate = verifySslCertificate;
            Assert.Equal(verifySslCertificate, settings.VerifySslCertificate);
            settings.AllowInsecureTls.Should().BeTrue();
            settings.SslSettings.CheckCertificateRevocation.Should().BeFalse();

            settings.Freeze();
            Assert.Equal(verifySslCertificate, settings.VerifySslCertificate);
            Assert.Throws<InvalidOperationException>(() => { settings.VerifySslCertificate = verifySslCertificate; });
#pragma warning restore 618
        }

        [Fact]
        public void TestWaitQueueSize()
        {
#pragma warning disable 618
            var settings = new MongoServerSettings();
            Assert.Equal(MongoDefaults.ComputedWaitQueueSize, settings.WaitQueueSize);

            var waitQueueSize = 123;
            settings.WaitQueueSize = waitQueueSize;
            Assert.Equal(waitQueueSize, settings.WaitQueueSize);

            settings.Freeze();
            Assert.Equal(waitQueueSize, settings.WaitQueueSize);
            Assert.Throws<InvalidOperationException>(() => { settings.WaitQueueSize = waitQueueSize; });
#pragma warning restore 618
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
            var clusterConfigurator = new Action<ClusterBuilder>(b => { });
#pragma warning disable 618
            var credential = MongoCredential.CreateMongoCRCredential("source", "username", "password");
#pragma warning restore 618
            var servers = new[] { new MongoServerAddress("localhost") };
            var sslSettings = new SslSettings
            {
                CheckCertificateRevocation = true,
                EnabledSslProtocols = SslProtocols.Tls
            };

            var subject = new MongoServerSettings
            {
                AllowInsecureTls = false,
                ApplicationName = "app",
                ClusterConfigurator = clusterConfigurator,
#pragma warning disable CS0618
                ConnectionMode = ConnectionMode.Direct,
#pragma warning restore CS0618
                ConnectTimeout = TimeSpan.FromSeconds(1),
                Credential = credential,
                HeartbeatInterval = TimeSpan.FromMinutes(1),
                HeartbeatTimeout = TimeSpan.FromMinutes(2),
                IPv6 = true,
                LocalThreshold = TimeSpan.FromMilliseconds(20),
                MaxConnectionIdleTime = TimeSpan.FromSeconds(2),
                MaxConnectionLifeTime = TimeSpan.FromSeconds(3),
                MaxConnectionPoolSize = 10,
                MinConnectionPoolSize = 5,
                ReplicaSetName = "rs",
                Scheme = ConnectionStringScheme.MongoDBPlusSrv,
                SdamLogFilename = "navi",
                Servers = servers,
                ServerSelectionTimeout = TimeSpan.FromSeconds(6),
                SocketTimeout = TimeSpan.FromSeconds(4),
                SslSettings = sslSettings,
                UseTls = true,
#pragma warning disable 618
                WaitQueueSize = 20,
#pragma warning restore 618
                WaitQueueTimeout = TimeSpan.FromSeconds(5)
            };
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                subject.GuidRepresentation = GuidRepresentation.Standard;
            }
#pragma warning restore 618

            var result = subject.ToClusterKey();

            result.AllowInsecureTls.Should().Be(subject.AllowInsecureTls);
            result.ApplicationName.Should().Be(subject.ApplicationName);
            result.ClusterConfigurator.Should().BeSameAs(subject.ClusterConfigurator);
#pragma warning disable CS0618
            result.ConnectionMode.Should().Be(subject.ConnectionMode);
#pragma warning restore CS0618
            result.ConnectTimeout.Should().Be(subject.ConnectTimeout);
#pragma warning disable 618
            result.Credentials.Should().Equal(subject.Credentials);
#pragma warning restore
            result.HeartbeatInterval.Should().Be(subject.HeartbeatInterval);
            result.HeartbeatTimeout.Should().Be(subject.HeartbeatTimeout);
            result.IPv6.Should().Be(subject.IPv6);
            result.LocalThreshold.Should().Be(subject.LocalThreshold);
            result.MaxConnectionIdleTime.Should().Be(subject.MaxConnectionIdleTime);
            result.MaxConnectionLifeTime.Should().Be(subject.MaxConnectionLifeTime);
            result.MaxConnectionPoolSize.Should().Be(subject.MaxConnectionPoolSize);
            result.MinConnectionPoolSize.Should().Be(subject.MinConnectionPoolSize);
            result.ReceiveBufferSize.Should().Be(MongoDefaults.TcpReceiveBufferSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.Scheme.Should().Be(subject.Scheme);
            result.SdamLogFilename.Should().Be(subject.SdamLogFilename);
            result.SendBufferSize.Should().Be(MongoDefaults.TcpSendBufferSize);
            result.Servers.Should().Equal(subject.Servers);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
            result.SocketTimeout.Should().Be(subject.SocketTimeout);
            result.SslSettings.Should().Be(subject.SslSettings);
            result.UseTls.Should().Be(subject.UseTls);
#pragma warning disable 618
            result.WaitQueueSize.Should().Be(subject.WaitQueueSize);
#pragma warning restore 618
            result.WaitQueueTimeout.Should().Be(subject.WaitQueueTimeout);
        }
    }
}
