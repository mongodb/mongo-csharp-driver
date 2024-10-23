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
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoClientSettingsTests
    {
        private readonly MongoServerAddress _localHost = new MongoServerAddress("localhost");

        [Fact]
        public void TestAllowInsecureTls()
        {
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
            Assert.Equal(null, settings.ApplicationName);

            var applicationName = "app";
            settings.ApplicationName = applicationName;
            Assert.Equal(applicationName, settings.ApplicationName);

            settings.Freeze();
            Assert.Equal(applicationName, settings.ApplicationName);
            Assert.Throws<InvalidOperationException>(() => { settings.ApplicationName = applicationName; });
        }

        [Fact]
        public void TestApplicationName_too_long()
        {
            var subject = new MongoClientSettings();
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
                "connect=direct;connectTimeout=123;ipv6=true;heartbeatInterval=1m;heartbeatTimeout=2m;localThreshold=128;loadBalanced=false;" +
                "maxConnecting=3;maxIdleTime=124;maxLifeTime=125;maxPoolSize=126;minPoolSize=127;readConcernLevel=majority;" +
                "readPreference=secondary;readPreferenceTags=a:1,b:2;readPreferenceTags=c:3,d:4;socketTimeout=129;" +
                "serverMonitoringMode=Stream;serverSelectionTimeout=20s;ssl=true;sslVerifyCertificate=false;waitqueuesize=130;waitQueueTimeout=131;" +
                "w=1;fsync=true;journal=true;w=2;wtimeout=131;gssapiServiceName=other;tlsInsecure=true";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var settings = MongoClientSettings.FromUrl(url);

            // a few settings can only be made in code
            settings.Credential = MongoCredential.CreateCredential("database", "username", "password").WithMechanismProperty("SERVICE_NAME", "other");
            settings.SslSettings = new SslSettings { CheckCertificateRevocation = false };
            settings.ServerApi = new ServerApi(ServerApiVersion.V1, true, true);

            var clone = settings.Clone();
            Assert.Equal(settings, clone);
        }

        [Fact]
        public void TestCloneClusterSource()
        {
            var clusterSource = new Mock<IClusterSource>().Object;
            var settings = new MongoClientSettings()
            {
                ClusterSource = clusterSource
            };

            var clone = settings.Clone();
            Assert.Equal(settings, clone);
        }

        [Fact]
        public void TestCloneDirectConnection()
        {
            // set everything to non default values to test that all settings are cloned
            var connectionString =
                "mongodb://user1:password1@somehost/?directConnection=false";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var settings = MongoClientSettings.FromUrl(url);

            // a few settings can only be made in code
            settings.DirectConnection.Should().BeFalse();

            var cloned = settings.Clone();
            cloned.Should().Be(settings);
        }

        [Fact]
        public void TestCloneLoadBalanced()
        {
            var connectionString = "mongodb://somehost/?loadBalanced=true";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var settings = MongoClientSettings.FromUrl(url);

            var clone = settings.Clone();

            clone.Should().Be(settings);
        }

        [Fact]
        public void TestCloneTlsDisableCertificateRevocationCheck()
        {
            var connectionString = "mongodb://somehost/?tlsDisableCertificateRevocationCheck=true";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var settings = MongoClientSettings.FromUrl(url);

            var clone = settings.Clone();

            clone.Should().Be(settings);
        }

        [Fact]
        public void TestCloneTlsInsecure()
        {
            var connectionString = "mongodb://somehost/?tlsInsecure=true";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();
            var settings = MongoClientSettings.FromUrl(url);

            var clone = settings.Clone();

            clone.Should().Be(settings);
        }

        [Fact]
        public void TestClusterSource()
        {
            var settings = new MongoClientSettings();
            Assert.Equal(DefaultClusterSource.Instance, settings.ClusterSource);

            var newClusterSource = new Mock<IClusterSource>();
            settings.ClusterSource = newClusterSource.Object;
            Assert.Equal(newClusterSource.Object, settings.ClusterSource);

            settings.Freeze();
            Assert.Equal(newClusterSource.Object, settings.ClusterSource);
            Assert.Throws<InvalidOperationException>(() => { settings.ClusterSource = newClusterSource.Object; });
        }

        [Fact]
        public void TestCompressors()
        {
            var settings = new MongoClientSettings();
            Assert.Equal(Enumerable.Empty<CompressorConfiguration>(), settings.Compressors);

            var compressors = new[] { new CompressorConfiguration(CompressorType.Zlib) };
            settings.Compressors = compressors;
            Assert.Equal(compressors, settings.Compressors);

            settings.Freeze();
            Assert.Equal(compressors, settings.Compressors);
            Assert.Throws<InvalidOperationException>(() => { settings.Compressors = compressors; });
        }

        [Theory]
        [InlineData(new[]{CompressorType.Snappy}, "Compressors=[snappy]")]
        [InlineData(new[]{CompressorType.Zlib}, "Compressors=[zlib]")]
        [InlineData(new[]{CompressorType.ZStandard}, "Compressors=[zstd]")]
        [InlineData(new[]{CompressorType.ZStandard, CompressorType.Snappy, CompressorType.Zlib}, "Compressors=[zstd,snappy,zlib]")]
        public void Compressors_ToString_outputs_correct_compressors(CompressorType[] compressors, string expectedConnectionString)
        {
            var settings = new MongoClientSettings {Compressors = compressors.Select(x => new CompressorConfiguration(x)).ToList()};

            var result = settings.ToString();

            Assert.Contains(expectedConnectionString, result);
        }

        [Fact]
        public void TestConnectTimeout()
        {
            var settings = new MongoClientSettings();
            Assert.Equal(MongoDefaults.ConnectTimeout, settings.ConnectTimeout);

            var connectTimeout = new TimeSpan(1, 2, 3);
            settings.ConnectTimeout = connectTimeout;
            Assert.Equal(connectTimeout, settings.ConnectTimeout);

            settings.Freeze();
            Assert.Equal(connectTimeout, settings.ConnectTimeout);
            Assert.Throws<InvalidOperationException>(() => { settings.ConnectTimeout = connectTimeout; });
        }

        [Fact]
        public void TestDefaults()
        {
            var settings = new MongoClientSettings();
            Assert.Equal(false, settings.AllowInsecureTls);
            Assert.Equal(null, settings.ApplicationName);
            Assert.Equal(DefaultClusterSource.Instance, settings.ClusterSource);
            Assert.Equal(Enumerable.Empty<CompressorConfiguration>(), settings.Compressors);
            Assert.Equal(MongoDefaults.ConnectTimeout, settings.ConnectTimeout);
            Assert.Null(settings.Credential);
            Assert.Equal(false, settings.DirectConnection);
            Assert.Equal(ServerSettings.DefaultHeartbeatInterval, settings.HeartbeatInterval);
            Assert.Equal(ServerSettings.DefaultHeartbeatTimeout, settings.HeartbeatTimeout);
            Assert.Equal(false, settings.IPv6);
            Assert.Equal(false, settings.LoadBalanced);
            Assert.Equal(MongoInternalDefaults.ConnectionPool.MaxConnecting, settings.MaxConnecting);
            Assert.Equal(MongoDefaults.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.Equal(MongoDefaults.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.Equal(MongoDefaults.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.Equal(MongoDefaults.MinConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.Equal(ReadConcern.Default, settings.ReadConcern);
            Assert.Equal(ReadPreference.Primary, settings.ReadPreference);
            Assert.Equal(null, settings.ReplicaSetName);
            Assert.Equal(true, settings.RetryReads);
            Assert.Equal(true, settings.RetryWrites);
            Assert.Equal(ConnectionStringScheme.MongoDB, settings.Scheme);
            Assert.Equal(_localHost, settings.Server);
            Assert.Equal(_localHost, settings.Servers.First());
            Assert.Equal(null, settings.ServerApi);
            Assert.Equal(1, settings.Servers.Count());
            Assert.Equal(ServerMonitoringMode.Auto, settings.ServerMonitoringMode);
            Assert.Equal(MongoDefaults.ServerSelectionTimeout, settings.ServerSelectionTimeout);
            Assert.Equal(MongoDefaults.SocketTimeout, settings.SocketTimeout);
            Assert.Null(settings.SslSettings);
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
            Assert.Equal(WriteConcern.Acknowledged, settings.WriteConcern);
        }

        [Fact]
        public void TestDirectConnection()
        {
            var settings = new MongoClientSettings();
            settings.DirectConnection.Should().BeFalse();

            var directConnection = true;
            settings.DirectConnection = directConnection;
            settings.DirectConnection.Should().Be(directConnection);

            settings.Freeze();
            settings.DirectConnection.Should().Be(directConnection);
            var exception = Record.Exception(() => settings.DirectConnection = false);
            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void TestlibraryInfo()
        {
            var settings = new MongoClientSettings();
            settings.LibraryInfo.Should().BeNull();

            var libraryInfo = new LibraryInfo("lib_name", "1.0.0");
            settings.LibraryInfo = libraryInfo;

            settings.Freeze();
            settings.LibraryInfo.Should().Be(libraryInfo);
            var exception = Record.Exception(() => settings.LibraryInfo = null);
            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void TestEquals()
        {
            var settings = new MongoClientSettings();
            var clone = settings.Clone();
            Assert.True(clone.Equals(settings));

            clone = settings.Clone();
            clone.AllowInsecureTls = !settings.AllowInsecureTls;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.ApplicationName = "app2";
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.ClusterSource = (new Mock<IClusterSource>()).Object;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.Compressors = new[] { new CompressorConfiguration(CompressorType.Zlib) };
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.ConnectTimeout = new TimeSpan(1, 2, 3);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.Credential = MongoCredential.CreateCredential("db2", "user2", "password2");
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.Credential = MongoCredential.CreateCredential("db", "user2", "password2");
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.DirectConnection = true;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.LibraryInfo = new LibraryInfo("name", "version");
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
            clone.LocalThreshold = new TimeSpan(1, 2, 3);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.MaxConnecting = 3;
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
            clone.ReadConcern = ReadConcern.Majority;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.MinConnectionPoolSize = settings.MinConnectionPoolSize + 1;
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
            clone.ServerApi = new ServerApi(ServerApiVersion.V1, true, true);
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.Server = new MongoServerAddress("someotherhost");
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.ServerMonitoringMode = ServerMonitoringMode.Poll;
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

            // set non default values
            settings.AutoEncryptionOptions = new AutoEncryptionOptions(CollectionNamespace.FromFullName("encryption.__keyVault"), new Dictionary<string, IReadOnlyDictionary<string, object>>());
            settings.LoggingSettings = new LoggingSettings(null, 123);
            settings.ReadConcern = ReadConcern.Majority;
            settings.ReadEncoding = new UTF8Encoding(false, false);
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            settings.WriteConcern = WriteConcern.W2;
            settings.WriteEncoding = new UTF8Encoding(false, false);

            clone = settings.Clone();
            clone.AutoEncryptionOptions = clone.AutoEncryptionOptions.With(bypassAutoEncryption: settings.AutoEncryptionOptions.BypassAutoEncryption);
            clone.LoggingSettings = new LoggingSettings(null, settings.LoggingSettings.MaxDocumentSize);
            clone.ReadConcern = ReadConcern.FromBsonDocument(settings.ReadConcern.ToBsonDocument());
            clone.ReadEncoding = new UTF8Encoding(false, false);
            clone.ReadPreference = clone.ReadPreference.With(settings.ReadPreference.ReadPreferenceMode);
            clone.ServerApi = new ServerApi(settings.ServerApi.Version);
            clone.WriteConcern = WriteConcern.FromBsonDocument(settings.WriteConcern.ToBsonDocument());
            clone.WriteEncoding = new UTF8Encoding(false, false);

            Assert.True(clone.Equals(settings));
        }

        [Fact]
        public void TestFreeze()
        {
            var settings = new MongoClientSettings();

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
            AssertException(settings =>
            {
                settings.AllowInsecureTls = true;
                settings.SslSettings.CheckCertificateRevocation = true;
            });

            AssertException(settings =>
            {
                settings.DirectConnection = true;
                settings.Scheme = ConnectionStringScheme.MongoDBPlusSrv;
            });

            AssertException(settings =>
            {
                settings.DirectConnection = true;
                var endpoint = "test5.test.build.10gen.cc:53";
                settings.Servers = new[] { MongoServerAddress.Parse(endpoint), MongoServerAddress.Parse(endpoint) };
            });

            AssertException(settings =>
            {
                settings.LoadBalanced = true;
                var endpoint = "test5.test.build.10gen.cc:53";
                settings.Servers = new[] { MongoServerAddress.Parse(endpoint), MongoServerAddress.Parse(endpoint) };
            });

            AssertException(settings =>
            {
                settings.LoadBalanced = true;
                settings.DirectConnection = true;
            });

            AssertException(settings =>
            {
                settings.LoadBalanced = true;
                settings.ReplicaSetName = "test";
            });

            AssertException(settings =>
            {
                settings.Scheme = ConnectionStringScheme.MongoDB;
                settings.SrvMaxHosts = 2;
            });

            AssertException(settings =>
            {
                settings.Scheme = ConnectionStringScheme.MongoDB;
                settings.SrvServiceName = "customname";
            });

            void AssertException(Action<MongoClientSettings> setAction)
            {
                var settings = new MongoClientSettings();

                setAction(settings);

                var exception = Record.Exception(() => settings.Freeze());
                exception.Should().BeOfType<InvalidOperationException>();
            }
        }

        [Fact]
        public void TestFromUrl()
        {
            // set everything to non default values to test that all settings are converted
            // with the exception of tlsDisableCertificateRevocationCheck because setting that with tlsInsecure is
            // not allowed in a connection string
            var connectionString =
                "mongodb://user1:password1@somehost/?appname=app1;authSource=db;authMechanismProperties=CANONICALIZE_HOST_NAME:true;" +
                "compressors=zlib,snappy;zlibCompressionLevel=9;connectTimeout=123;directConnection=true;ipv6=true;heartbeatInterval=1m;heartbeatTimeout=2m;loadBalanced=false;localThreshold=128;" +
                "maxConnecting=3;maxIdleTime=124;maxLifeTime=125;maxPoolSize=126;minPoolSize=127;readConcernLevel=majority;" +
                "readPreference=secondary;readPreferenceTags=a:1,b:2;readPreferenceTags=c:3,d:4;retryReads=false;retryWrites=true;socketTimeout=129;" +
                "serverMonitoringMode=Stream;serverSelectionTimeout=20s;tls=true;sslVerifyCertificate=false;waitqueuesize=130;waitQueueTimeout=131;" +
                "w=1;fsync=true;journal=true;w=2;wtimeout=131;gssapiServiceName=other";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();

            var settings = MongoClientSettings.FromUrl(url);
            Assert.Equal(url.AllowInsecureTls, settings.AllowInsecureTls);
            Assert.Equal(url.ApplicationName, settings.ApplicationName);
            Assert.Equal(DefaultClusterSource.Instance, settings.ClusterSource);
            Assert.Equal(url.Compressors, settings.Compressors);
            Assert.Equal(url.ConnectTimeout, settings.ConnectTimeout);
            Assert.NotNull(settings.Credential);
            Assert.Equal(url.DirectConnection, settings.DirectConnection);
            Assert.Equal(url.Username, settings.Credential.Username);
            Assert.Equal(url.AuthenticationMechanism, settings.Credential.Mechanism);
            Assert.Equal("other", settings.Credential.GetMechanismProperty<string>("SERVICE_NAME", "mongodb"));
            Assert.Equal(true, settings.Credential.GetMechanismProperty<bool>("CANONICALIZE_HOST_NAME", false));
            Assert.Equal(url.AuthenticationSource, settings.Credential.Source);
            Assert.Equal(new PasswordEvidence(url.Password), settings.Credential.Evidence);
            Assert.Equal(url.HeartbeatInterval, settings.HeartbeatInterval);
            Assert.Equal(url.HeartbeatTimeout, settings.HeartbeatTimeout);
            Assert.Equal(url.IPv6, settings.IPv6);
            Assert.Equal(url.LoadBalanced, settings.LoadBalanced);
            Assert.Equal(url.LocalThreshold, settings.LocalThreshold);
            Assert.Equal(url.MaxConnecting, settings.MaxConnecting);
            Assert.Equal(url.MaxConnectionIdleTime, settings.MaxConnectionIdleTime);
            Assert.Equal(url.MaxConnectionLifeTime, settings.MaxConnectionLifeTime);
            Assert.Equal(url.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.Equal(url.MinConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.Equal(url.ReadConcernLevel, settings.ReadConcern.Level);
            Assert.Equal(url.ReadPreference, settings.ReadPreference);
            Assert.Equal(url.ReplicaSetName, settings.ReplicaSetName);
            Assert.Equal(url.RetryReads, settings.RetryReads);
            Assert.Equal(url.RetryWrites, settings.RetryWrites);
            Assert.Equal(url.Scheme, settings.Scheme);
            Assert.True(url.Servers.SequenceEqual(settings.Servers));
            Assert.Equal(ServerMonitoringMode.Stream, settings.ServerMonitoringMode);
            Assert.Equal(url.ServerSelectionTimeout, settings.ServerSelectionTimeout);
            Assert.Equal(url.SocketTimeout, settings.SocketTimeout);
#pragma warning disable 618
            Assert.Equal(url.TlsDisableCertificateRevocationCheck, !settings.SslSettings.CheckCertificateRevocation);
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
        public void TestFromUrlTlsDisableCertificateRevocationCheck()
        {
            var connectionString = "mongodb://the-next-generation/?tlsDisableCertificateRevocationCheck=true";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();

            var settings = MongoClientSettings.FromUrl(url);

            settings.SslSettings.Should().Be(new SslSettings { CheckCertificateRevocation = !url.TlsDisableCertificateRevocationCheck });
        }

        [Fact]
        public void TestFromUrlLoadBalanced()
        {
            var connectionString = "mongodb://the-next-generation/?loadBalanced=true";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();

            var settings = MongoClientSettings.FromUrl(url);

            settings.LoadBalanced.Should().Be(url.LoadBalanced);
        }

        [Fact]
        public void TestFromUrlTlsInsecure()
        {
            var connectionString = "mongodb://the-next-generation/?tlsInsecure=true";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();

            var settings = MongoClientSettings.FromUrl(url);

            settings.AllowInsecureTls.Should().Be(url.AllowInsecureTls);
        }

        [Theory]
        [InlineData(0, int.MaxValue)]
        [InlineData(126, 126)]
        public void TestFromUrlWithMaxPoolSize(int inputValue, int expectedValue)
        {
            var connectionString = $"mongodb://the-next-generation/?maxPoolSize={inputValue}";
            var builder = new MongoUrlBuilder(connectionString);
            var url = builder.ToMongoUrl();

            var settings = MongoClientSettings.FromUrl(url);

            settings.MaxConnectionPoolSize.Should().Be(expectedValue);
        }

        [Fact]
        public void TestFromUrlWithMongoDBX509()
        {
            var url = new MongoUrl("mongodb://username@localhost/?authMechanism=MONGODB-X509");
            var settings = MongoClientSettings.FromUrl(url);

            var credential = settings.Credential;
            Assert.Equal("MONGODB-X509", credential.Mechanism);
            Assert.Equal("username", credential.Username);
            Assert.IsType<ExternalEvidence>(credential.Evidence);
        }

        [Fact]
        public void TestFromUrlWithMongoDBX509_without_username()
        {
            var url = new MongoUrl("mongodb://localhost/?authMechanism=MONGODB-X509");
            var settings = MongoClientSettings.FromUrl(url);

            var credential = settings.Credential;
            Assert.Equal("MONGODB-X509", credential.Mechanism);
            Assert.Equal(null, credential.Username);
            Assert.IsType<ExternalEvidence>(credential.Evidence);
        }

        [Theory]
        [ParameterAttributeData]
        public void TestFromUrlWithMongoDBAWS_should_parse_credentials_correctly([Values(false, true)] bool escapeToken)
        {
            const string authMechanism = "MONGODB-AWS";
            const string username = "AKIAIOSFODNN7EXAMPLE";
            const string password = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYzEXAMPLEKEY";
            const string awsSessionToken = "AQoEXAMPLEH4aoAH0gNCAPyJxz4BlCFFxWNE1OPTgk5TthT+FvwqnKwRcOIfrRh3c/LTo6UDdyJwOOvEVPvLXCrrrUtdnniCEXAMPLE/IvU1dYUg2RVAJBanLiHb4IgRmpRV3zrkuWJOgQs8IZZaIv2BXIa2R4OlgkBN9bkUDNCJiBeb/AXlzBBko7b15fjrBs2+cTQtpZ3CYWFXG8C5zqx37wnOE49mRl/+OtkIKGO7fAE";

            var preparedToken = escapeToken ? Uri.EscapeDataString(awsSessionToken) : awsSessionToken;
            var uri = $"mongodb+srv://{username}:{Uri.EscapeDataString(password)}@awssessiontokentest.example.net/test?authSource=$external&authMechanism={authMechanism}&authMechanismProperties=AWS_SESSION_TOKEN:{preparedToken}";
            var url = new MongoUrl(uri);

            var result = MongoClientSettings.FromUrl(url).Credential;

            result.Mechanism.Should().Be(authMechanism);
            result.Username.Should().Be(username);
            result.GetMechanismProperty("AWS_SESSION_TOKEN", string.Empty).Should().Be(awsSessionToken);
        }

        [Fact]
        public void TestFrozenCopy()
        {
            var settings = new MongoClientSettings();
            Assert.Equal(false, settings.IsFrozen);

            var frozenCopy = settings.FrozenCopy();
            Assert.Equal(true, frozenCopy.IsFrozen);
            Assert.NotSame(settings, frozenCopy);
            Assert.Equal(settings, frozenCopy);

            var secondFrozenCopy = frozenCopy.FrozenCopy();
            Assert.Same(frozenCopy, secondFrozenCopy);
        }

        [Fact]
        public void TestHeartbeatInterval()
        {
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
            Assert.Equal(false, settings.IPv6);

            var ipv6 = true;
            settings.IPv6 = ipv6;
            Assert.Equal(ipv6, settings.IPv6);

            settings.Freeze();
            Assert.Equal(ipv6, settings.IPv6);
            Assert.Throws<InvalidOperationException>(() => { settings.IPv6 = ipv6; });
        }

        [Fact]
        public void TestLoadBalanced()
        {
            var settings = new MongoClientSettings();
            settings.LoadBalanced.Should().BeFalse();

            var loadBalanaced = true;
            settings.LoadBalanced = loadBalanaced;
            settings.LoadBalanced.Should().Be(loadBalanaced);

            settings.Freeze();
            settings.LoadBalanced.Should().Be(loadBalanaced);
            Record.Exception(() => { settings.LoadBalanced= loadBalanaced; }).Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void TestLocalThreshold()
        {
            var settings = new MongoClientSettings();
            Assert.Equal(MongoDefaults.LocalThreshold, settings.LocalThreshold);

            var localThreshold = new TimeSpan(1, 2, 3);
            settings.LocalThreshold = localThreshold;
            Assert.Equal(localThreshold, settings.LocalThreshold);

            settings.Freeze();
            Assert.Equal(localThreshold, settings.LocalThreshold);
            Assert.Throws<InvalidOperationException>(() => { settings.LocalThreshold = localThreshold; });
        }

        [Fact]
        public void TestLoggingSettings()
        {
            var settings = new MongoClientSettings();
            Assert.Equal(null, settings.LoggingSettings);

            settings.LoggingSettings = null;
            Assert.Equal(null, settings.LoggingSettings);

            var loggerFactory = new Mock<ILoggerFactory>();
            settings.LoggingSettings = new LoggingSettings(loggerFactory.Object);
            Assert.Equal(loggerFactory.Object, settings.LoggingSettings.LoggerFactory);

            settings.Freeze();
            Assert.Throws<InvalidOperationException>(() => { settings.LoggingSettings = new LoggingSettings(loggerFactory.Object); });
        }

        [Fact]
        public void TestMaxConnecting()
        {
            var settings = new MongoClientSettings();
            settings.MaxConnecting.Should().Be(MongoInternalDefaults.ConnectionPool.MaxConnecting);

            var maxConnecting = 3;
            settings.MaxConnecting = maxConnecting;
            settings.MaxConnecting.Should().Be(maxConnecting);

            var exception = Record.Exception(() => settings.MaxConnecting = 0);
            exception.Should().BeOfType<ArgumentOutOfRangeException>();

            exception = Record.Exception(() => settings.MaxConnecting = -1);
            exception.Should().BeOfType<ArgumentOutOfRangeException>();

            settings.Freeze();

            settings.MaxConnecting.Should().Be(maxConnecting);
            exception = Record.Exception(() => settings.MaxConnecting = maxConnecting);
            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void TestMaxConnectionIdleTime()
        {
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
            Assert.Equal(MongoDefaults.MaxConnectionPoolSize, settings.MaxConnectionPoolSize);

            var maxConnectionPoolSize = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.MaxConnectionPoolSize = maxConnectionPoolSize);

            maxConnectionPoolSize = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.MaxConnectionPoolSize = maxConnectionPoolSize);

            maxConnectionPoolSize = 123;
            settings.MaxConnectionPoolSize = maxConnectionPoolSize;
            Assert.Equal(maxConnectionPoolSize, settings.MaxConnectionPoolSize);

            settings.Freeze();
            Assert.Equal(maxConnectionPoolSize, settings.MaxConnectionPoolSize);
            Assert.Throws<InvalidOperationException>(() => { settings.MaxConnectionPoolSize = maxConnectionPoolSize; });
        }

        [Fact]
        public void TestMinConnectionPoolSize()
        {
            var settings = new MongoClientSettings();
            Assert.Equal(MongoDefaults.MinConnectionPoolSize, settings.MinConnectionPoolSize);

            var minConnectionPoolSize = 123;
            settings.MinConnectionPoolSize = minConnectionPoolSize;
            Assert.Equal(minConnectionPoolSize, settings.MinConnectionPoolSize);

            settings.Freeze();
            Assert.Equal(minConnectionPoolSize, settings.MinConnectionPoolSize);
            Assert.Throws<InvalidOperationException>(() => { settings.MinConnectionPoolSize = minConnectionPoolSize; });
        }

        [Fact]
        public void TestReadConcern()
        {
            var settings = new MongoClientSettings();
            Assert.Equal(ReadConcern.Default, settings.ReadConcern);

            var readConcern = ReadConcern.Majority;
            settings.ReadConcern = readConcern;
            Assert.Same(readConcern, settings.ReadConcern);

            settings.Freeze();
            Assert.Equal(readConcern, settings.ReadConcern);
            Assert.Throws<InvalidOperationException>(() => { settings.ReadConcern = ReadConcern.Default; });
        }

        [Fact]
        public void TestReadPreference()
        {
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
            Assert.Equal(true, settings.RetryReads);

            var retryReads = false;
            settings.RetryReads = retryReads;
            Assert.Equal(retryReads, settings.RetryReads);

            settings.Freeze();
            Assert.Equal(retryReads, settings.RetryReads);
            Assert.Throws<InvalidOperationException>(() => { settings.RetryReads = false; });
        }

        [Fact]
        public void TestRetryWrites()
        {
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
            Assert.Equal(ConnectionStringScheme.MongoDB, settings.Scheme);

            var scheme = ConnectionStringScheme.MongoDBPlusSrv;
            settings.Scheme = scheme;
            Assert.Equal(scheme, settings.Scheme);

            settings.Freeze();
            Assert.Equal(scheme, settings.Scheme);
            Assert.Throws<InvalidOperationException>(() => { settings.Scheme = scheme; });
        }

        [Fact]
        public void TestServer()
        {
            var settings = new MongoClientSettings();
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
        public void TestServerApi()
        {
            var settings = new MongoClientSettings();
            settings.ServerApi.Should().BeNull();

            var serverApi = new ServerApi(ServerApiVersion.V1, true, true);
            settings.ServerApi = serverApi;
            settings.ServerApi.Should().Be(serverApi);

            settings.Freeze();
            settings.ServerApi.Should().Be(serverApi);
            var exception = Record.Exception(() => { settings.ServerApi = serverApi; });

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void TestServersWithOneServer()
        {
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
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
        public void TestServersWithSrvMaxHosts()
        {
            var settings = new MongoClientSettings { SrvMaxHosts = 2 };

            var servers = new MongoServerAddress[]
            {
                new MongoServerAddress("server1"),
                new MongoServerAddress("server2"),
                new MongoServerAddress("server3")
            };
            settings.Servers = servers;

            settings.Servers.Should().HaveCount(2);
        }

        [Fact]
        public void TestSocketConfigurator()
        {
            RequireServer.Check();

            var settings = DriverTestConfiguration.Client.Settings.Clone();
            var socketConfiguratorWasCalled = false;
            Action<Socket> socketConfigurator = s => { socketConfiguratorWasCalled = true; };
            settings.ClusterConfigurator = cb => cb.ConfigureTcp(tcp => tcp.With(socketConfigurator: socketConfigurator));
            var subject = new MongoClient(settings);

            var isConnected = SpinWait.SpinUntil(() => subject.Cluster.Description.State == ClusterState.Connected, TimeSpan.FromSeconds(5));
            if (!isConnected)
            {
                throw new Exception($"The cluster connecting exceeded the timeout 5sec. ClusterDescription is {subject.Cluster.Description}.");
            }

            Assert.True(socketConfiguratorWasCalled, "socketConfigurator must be called.");
        }

        [Fact]
        public void TestServerMonitoringMode()
        {
            var settings = new MongoClientSettings();
            Assert.Equal(ServerMonitoringMode.Auto, settings.ServerMonitoringMode);

            var serverMonitoringMode = ServerMonitoringMode.Stream;
            settings.ServerMonitoringMode = serverMonitoringMode;
            Assert.Equal(serverMonitoringMode, settings.ServerMonitoringMode);

            settings.Freeze();
            Assert.Equal(serverMonitoringMode, settings.ServerMonitoringMode);
            Assert.Throws<InvalidOperationException>(() => { settings.ServerMonitoringMode = serverMonitoringMode; });
        }

        [Fact]
        public void TestServerSelectionTimeout()
        {
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
            settings.SslSettings.Should().BeNull();

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
#pragma warning disable 618
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
            Assert.Equal(true, settings.VerifySslCertificate);

            var verifySslCertificate = false;
            settings.VerifySslCertificate = verifySslCertificate;
            Assert.Equal(verifySslCertificate, settings.VerifySslCertificate);
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
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
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
            var settings = new MongoClientSettings();
            Assert.Equal(WriteConcern.Acknowledged, settings.WriteConcern);

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
            var credential = MongoCredential.CreateCredential("source", "username", "password");
            var serverApi = new ServerApi(ServerApiVersion.V1, true, true);
            var servers = new[] { new MongoServerAddress("localhost") };
            var sslSettings = new SslSettings
            {
                CheckCertificateRevocation = true,
                EnabledSslProtocols = SslProtocols.Tls12
            };

            var subject = new MongoClientSettings
            {
                AllowInsecureTls = false,
                ApplicationName = "app",
                ClusterConfigurator = clusterConfigurator,
                ConnectTimeout = TimeSpan.FromSeconds(1),
                Credential = credential,
                DirectConnection = true,
                LibraryInfo = new LibraryInfo("name", "version"),
                HeartbeatInterval = TimeSpan.FromSeconds(7),
                HeartbeatTimeout = TimeSpan.FromSeconds(8),
                IPv6 = true,
                LocalThreshold = TimeSpan.FromMilliseconds(20),
                LoggingSettings = new LoggingSettings(),
                MaxConnecting = 3,
                MaxConnectionIdleTime = TimeSpan.FromSeconds(2),
                MaxConnectionLifeTime = TimeSpan.FromSeconds(3),
                MaxConnectionPoolSize = 10,
                MinConnectionPoolSize = 5,
                ReplicaSetName = "rs",
                Scheme = ConnectionStringScheme.MongoDB,
                ServerApi = serverApi,
                Servers = servers,
                ServerMonitoringMode = ServerMonitoringMode.Poll,
                ServerSelectionTimeout = TimeSpan.FromSeconds(6),
                SocketTimeout = TimeSpan.FromSeconds(4),
                SslSettings = sslSettings,
                UseTls = true,
#pragma warning disable 618
                WaitQueueSize = 20,
#pragma warning restore 618
                WaitQueueTimeout = TimeSpan.FromSeconds(5)
            };

            var result = subject.ToClusterKey();

            result.AllowInsecureTls.Should().Be(subject.AllowInsecureTls);
            result.ApplicationName.Should().Be(subject.ApplicationName);
            result.ClusterConfigurator.Should().BeSameAs(clusterConfigurator);
            result.ConnectTimeout.Should().Be(subject.ConnectTimeout);
            result.Credential.Should().Be(subject.Credential);
            result.DirectConnection.Should().Be(subject.DirectConnection);
            result.LibraryInfo.Should().Be(subject.LibraryInfo);
            result.HeartbeatInterval.Should().Be(subject.HeartbeatInterval);
            result.HeartbeatTimeout.Should().Be(subject.HeartbeatTimeout);
            result.IPv6.Should().Be(subject.IPv6);
            result.LocalThreshold.Should().Be(subject.LocalThreshold);
            result.LoggingSettings.Should().Be(subject.LoggingSettings);
            result.MaxConnecting.Should().Be(subject.MaxConnecting);
            result.MaxConnectionIdleTime.Should().Be(subject.MaxConnectionIdleTime);
            result.MaxConnectionLifeTime.Should().Be(subject.MaxConnectionLifeTime);
            result.MaxConnectionPoolSize.Should().Be(subject.MaxConnectionPoolSize);
            result.MinConnectionPoolSize.Should().Be(subject.MinConnectionPoolSize);
            result.ReceiveBufferSize.Should().Be(MongoDefaults.TcpReceiveBufferSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.Scheme.Should().Be(subject.Scheme);
            result.SendBufferSize.Should().Be(MongoDefaults.TcpSendBufferSize);
            result.ServerApi.Should().Be(subject.ServerApi);
            result.Servers.Should().Equal(subject.Servers);
            result.ServerMonitoringMode.Should().Be(ServerMonitoringMode.Poll);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
            result.SocketTimeout.Should().Be(subject.SocketTimeout);
            result.SslSettings.Should().Be(subject.SslSettings);
            result.UseTls.Should().Be(subject.UseTls);
#pragma warning disable 618
            result.WaitQueueSize.Should().Be(subject.WaitQueueSize);
#pragma warning restore 618
            result.WaitQueueTimeout.Should().Be(subject.WaitQueueTimeout);
        }

        [Fact]
        public void TestSrvServiceName()
        {
            var subject = new MongoClientSettings { Scheme = ConnectionStringScheme.MongoDBPlusSrv };
            subject.SrvServiceName.Should().Be(MongoInternalDefaults.MongoClientSettings.SrvServiceName);

            var srvServicName = "customname";
            subject.SrvServiceName = srvServicName;
            subject.SrvServiceName.Should().Be(srvServicName);

            subject.Freeze();
            subject.SrvServiceName.Should().Be(srvServicName);

            var exception = Record.Exception(() => subject.SrvServiceName = srvServicName);
            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void TestSrvMaxHosts([Values(0, 1, 5)]int srvMaxHosts)
        {
            var subject = new MongoClientSettings { Scheme = ConnectionStringScheme.MongoDBPlusSrv };
            subject.SrvMaxHosts.Should().Be(0);

            subject.SrvMaxHosts = srvMaxHosts;
            subject.SrvMaxHosts.Should().Be(srvMaxHosts);

            subject.Freeze();
            subject.SrvMaxHosts.Should().Be(srvMaxHosts);

            var exception = Record.Exception(() => subject.SrvMaxHosts = int.MaxValue);
            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void Negative_SrvMaxHosts_should_throw()
        {
            var subject = new MongoClientSettings();

            var exception = Record.Exception(() => subject.SrvMaxHosts = -1);

            exception.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Freezing_instance_with_SrvMaxHosts_and_ReplicaSetName_should_throw()
        {
            var subject = new MongoClientSettings { SrvMaxHosts = 2, ReplicaSetName = "replSet0" };

            var exception = Record.Exception(() => subject.Freeze());

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void Freezing_instance_with_SrvMaxHosts_and_LoadBalanced_should_throw()
        {
            var subject = new MongoClientSettings { SrvMaxHosts = 2, LoadBalanced = true };

            var exception = Record.Exception(() => subject.Freeze());

            exception.Should().BeOfType<InvalidOperationException>();
        }
    }
}
