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
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Servers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    [Trait("Category", "ConnectionString")]
    public class MongoUrlBuilderTests
    {
        private MongoServerAddress _localhost = new MongoServerAddress("localhost");

        [Fact]
        public void MaxPoolSize_zero_should_be_handled_correctly()
        {
            var subject = new MongoUrlBuilder("mongodb://localhost/?maxPoolSize=0");

            var result = subject.MaxConnectionPoolSize;

            result.Should().Be(0);
        }

        [Fact]
        public void MaxPoolSize_missing_should_be_handled_correctly()
        {
            var subject = new MongoUrlBuilder("mongodb://localhost");

            var result = subject.MaxConnectionPoolSize;

            result.Should().Be(MongoDefaults.MaxConnectionPoolSize);
        }

        [Fact]
        public void TestAll()
        {
            var readPreference = new ReadPreference(ReadPreferenceMode.Secondary, new[] { new TagSet(new[] { new Tag("dc", "1") }) }, TimeSpan.FromSeconds(11));
            var authMechanismProperties = new Dictionary<string, string>
            {
                { "SERVICE_NAME", "other" },
                { "CANONICALIZE_HOST_NAME", "true" }
            };
            var zlibCompressor = new CompressorConfiguration(CompressorType.Zlib);
            zlibCompressor.Properties.Add("Level", 4);
            var built = new MongoUrlBuilder()
            {
                AllowInsecureTls = true,
                ApplicationName = "app",
                AuthenticationMechanism = "GSSAPI",
                AuthenticationMechanismProperties = authMechanismProperties,
                AuthenticationSource = "db",
                Compressors = new[] { zlibCompressor },
                DirectConnection = true,
                ConnectTimeout = TimeSpan.FromSeconds(1),
                DatabaseName = "database",
                FSync = true,
                HeartbeatInterval = TimeSpan.FromMinutes(1),
                HeartbeatTimeout = TimeSpan.FromMinutes(2),
                IPv6 = true,
                Journal = true,
                MaxConnecting = 3,
                MaxConnectionIdleTime = TimeSpan.FromSeconds(2),
                MaxConnectionLifeTime = TimeSpan.FromSeconds(3),
                MaxConnectionPoolSize = 4,
                MinConnectionPoolSize = 5,
                Password = "password",
                ReadConcernLevel = ReadConcernLevel.Majority,
                ReadPreference = readPreference,
                ReplicaSetName = "name",
                RetryReads = false,
                RetryWrites = true,
                LocalThreshold = TimeSpan.FromSeconds(6),
                Server = new MongoServerAddress("host"),
                ServerMonitoringMode = ServerMonitoringMode.Poll,
                ServerSelectionTimeout = TimeSpan.FromSeconds(10),
                SocketTimeout = TimeSpan.FromSeconds(7),
                Timeout = TimeSpan.FromSeconds(13),
                Username = "username",
#pragma warning disable 618
                UseSsl = true,
#pragma warning restore 618
                UseTls = true,
#pragma warning disable 618
                VerifySslCertificate = false,
#pragma warning restore 618
                W = 2,
#pragma warning disable 618
                WaitQueueSize = 123,
#pragma warning restore 618
                WaitQueueTimeout = TimeSpan.FromSeconds(8),
                WTimeout = TimeSpan.FromSeconds(9)
            };

            var connectionString = "mongodb://username:password@host/database?" + string.Join("&", new[] {
                "authMechanism=GSSAPI",
                "authMechanismProperties=SERVICE_NAME:other,CANONICALIZE_HOST_NAME:true",
                "authSource=db",
                "appname=app",
                "ipv6=true",
                "tls=true", // UseTls
                "tlsInsecure=true",
                "compressors=zlib",
                "zlibCompressionLevel=4",
                "directConnection=true",
                "replicaSet=name",
                "readConcernLevel=majority",
                "readPreference=secondary&readPreferenceTags=dc:1&maxStaleness=11s",
                "fsync=true",
                "journal=true",
                "w=2",
                "wtimeout=9s",
                "connectTimeout=1s",
                "heartbeatInterval=1m",
                "heartbeatTimeout=2m",
                "localThreshold=6s",
                "maxConnecting=3",
                "maxIdleTime=2s",
                "maxLifeTime=3s",
                "maxPoolSize=4",
                "minPoolSize=5",
                "serverMonitoringMode=Poll",
                "serverSelectionTimeout=10s",
                "socketTimeout=7s",
                "timeout=13s",
                "waitQueueSize=123",
                "waitQueueTimeout=8s",
                "retryReads=false",
                "retryWrites=true"
            });

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(true, builder.AllowInsecureTls);
                Assert.Equal("app", builder.ApplicationName);
                Assert.Equal("GSSAPI", builder.AuthenticationMechanism);
                Assert.Equal(authMechanismProperties, builder.AuthenticationMechanismProperties);
                Assert.Equal("db", builder.AuthenticationSource);
#pragma warning disable 618
                Assert.Equal(123, builder.ComputedWaitQueueSize);
#pragma warning restore 618
                Assert.Contains(
                    builder.Compressors,
                    x => x.Type == CompressorType.Zlib && x.Properties.ContainsKey("Level") && (int)x.Properties["Level"] == 4);
                Assert.Equal(TimeSpan.FromSeconds(1), builder.ConnectTimeout);
                Assert.Equal("database", builder.DatabaseName);
                Assert.Equal(true, builder.DirectConnection);
                Assert.Equal(true, builder.FSync);
                Assert.Equal(TimeSpan.FromMinutes(1), builder.HeartbeatInterval);
                Assert.Equal(TimeSpan.FromMinutes(2), builder.HeartbeatTimeout);
                Assert.Equal(true, builder.IPv6);
                Assert.Equal(true, builder.Journal);
                Assert.Equal(false, builder.LoadBalanced);
                Assert.Equal(TimeSpan.FromSeconds(2), builder.MaxConnectionIdleTime);
                Assert.Equal(TimeSpan.FromSeconds(3), builder.MaxConnectionLifeTime);
                Assert.Equal(3, builder.MaxConnecting);
                Assert.Equal(4, builder.MaxConnectionPoolSize);
                Assert.Equal(5, builder.MinConnectionPoolSize);
                Assert.Equal("password", builder.Password);
                Assert.Equal(ReadConcernLevel.Majority, builder.ReadConcernLevel);
                Assert.Equal(readPreference, builder.ReadPreference);
                Assert.Equal("name", builder.ReplicaSetName);
                Assert.Equal(false, builder.RetryReads);
                Assert.Equal(true, builder.RetryWrites);
                Assert.Equal(TimeSpan.FromSeconds(6), builder.LocalThreshold);
                Assert.Equal(ConnectionStringScheme.MongoDB, builder.Scheme);
                Assert.Equal(new MongoServerAddress("host", 27017), builder.Server);
                Assert.Equal(ServerMonitoringMode.Poll, builder.ServerMonitoringMode);
                Assert.Equal(TimeSpan.FromSeconds(10), builder.ServerSelectionTimeout);
                Assert.Equal(TimeSpan.FromSeconds(7), builder.SocketTimeout);
                Assert.Equal(TimeSpan.FromSeconds(13), builder.Timeout);
                Assert.Equal("username", builder.Username);
#pragma warning disable 618
                Assert.Equal(true, builder.UseSsl);
#pragma warning restore 618
                Assert.Equal(true, builder.UseTls);
#pragma warning disable 618
                Assert.Equal(false, builder.VerifySslCertificate);
#pragma warning restore 618
                Assert.Equal(2, ((WriteConcern.WCount)builder.W).Value);
#pragma warning disable 618
                Assert.Equal(0.0, builder.WaitQueueMultiple);
                Assert.Equal(123, builder.WaitQueueSize);
#pragma warning restore 618
                Assert.Equal(TimeSpan.FromSeconds(8), builder.WaitQueueTimeout);
                Assert.Equal(TimeSpan.FromSeconds(9), builder.WTimeout);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(false, "mongodb://localhost/?tlsInsecure={0}", new[] { "false", "False" })]
        [InlineData(true, "mongodb://localhost/?tlsInsecure={0}", new[] { "true", "True" })]
        public void TestAllowInsecureTls(bool? allowInsecureTls, string formatString, string[] values)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (allowInsecureTls != null) { built.AllowInsecureTls = allowInsecureTls.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]).Replace("/?tlsInsecure=false", "");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(allowInsecureTls ?? false, builder.AllowInsecureTls);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData(null, "mongodb://localhost")]
        [InlineData("app", "mongodb://localhost/?appname=app")]
        public void TestApplicationName(string applicationName, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, ApplicationName = applicationName };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(applicationName, builder.ApplicationName);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestApplicationName_too_long()
        {
            var subject = new MongoUrlBuilder();
            var value = new string('x', 129);

            var exception = Record.Exception(() => subject.ApplicationName = value);

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("value");
        }

        [Theory]
        [InlineData(null, "mongodb://localhost")]
        [InlineData("SCRAM-SHA-1", "mongodb://localhost/?authMechanism=SCRAM-SHA-1")]
        [InlineData("MONGODB-X509", "mongodb://localhost/?authMechanism=MONGODB-X509")]
        [InlineData("GSSAPI", "mongodb://localhost/?authMechanism=GSSAPI")]
        public void TestAuthMechanism(string mechanism, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, AuthenticationMechanism = mechanism };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(mechanism, builder.AuthenticationMechanism);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData(null, "mongodb://localhost")]
        [InlineData("db", "mongodb://localhost/?authSource=db")]
        public void TestAuthSource(string authSource, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, AuthenticationSource = authSource };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(authSource, builder.AuthenticationSource);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestComputedWaitQueueSize_UsingMultiple()
        {
#pragma warning disable 618
            var built = new MongoUrlBuilder { Server = _localhost, MaxConnectionPoolSize = 123, WaitQueueMultiple = 2.0 };
            var connectionString = "mongodb://localhost/?maxPoolSize=123&waitQueueMultiple=2";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(123, builder.MaxConnectionPoolSize);
                Assert.Equal(2.0, builder.WaitQueueMultiple);
                Assert.Equal(0, builder.WaitQueueSize);
                Assert.Equal(246, builder.ComputedWaitQueueSize);
                Assert.Equal(connectionString, builder.ToString());
            }
#pragma warning restore 618
        }

        [Fact]
        public void TestComputedWaitQueueSize_UsingSize()
        {
#pragma warning disable 618
            var built = new MongoUrlBuilder { Server = _localhost, WaitQueueSize = 123 };
            var connectionString = "mongodb://localhost/?waitQueueSize=123";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(0.0, builder.WaitQueueMultiple);
                Assert.Equal(123, builder.WaitQueueSize);
                Assert.Equal(123, builder.ComputedWaitQueueSize);
                Assert.Equal(connectionString, builder.ToString());
            }
#pragma warning restore 618
        }

        [Theory]
        [InlineData(null, "mongodb://localhost")]
        [InlineData(new[] { CompressorType.Zlib }, "mongodb://localhost/?compressors=zlib")]
        [InlineData(new[] { CompressorType.ZStandard }, "mongodb://localhost/?compressors=zstd")]
        [InlineData(new[] { CompressorType.Snappy }, "mongodb://localhost/?compressors=snappy")]
        [InlineData(new[] { CompressorType.Snappy, CompressorType.Zlib }, "mongodb://localhost/?compressors=snappy,zlib")]
        [InlineData(new[] { CompressorType.ZStandard, CompressorType.Snappy, CompressorType.Zlib }, "mongodb://localhost/?compressors=zstd,snappy,zlib")]
        public void TestCompressors(CompressorType[] compressors, string expectedConnectionString)
        {
            var subject = new MongoUrlBuilder { Server = _localhost };

            subject.Compressors = compressors?.Select(x => new CompressorConfiguration(x)).ToList();
            var connectionString = subject.ToString();

            Assert.Equal(expectedConnectionString, connectionString);
        }

        [Fact]
        public void TestCompressorProperties()
        {
            var subject = new MongoUrlBuilder { Server = _localhost };

            var zlibCompressor = new CompressorConfiguration(CompressorType.Zlib) { Properties = {{ "Level", 1 }} };
            subject.Compressors = new[] { zlibCompressor };
            var connectionString = subject.ToString();

            Assert.Equal("mongodb://localhost/?compressors=zlib&zlibCompressionLevel=1", connectionString);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(500, "mongodb://localhost/?connectTimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [InlineData(30000, "mongodb://localhost/?connectTimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [InlineData(1800000, "mongodb://localhost/?connectTimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [InlineData(3600000, "mongodb://localhost/?connectTimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [InlineData(3723000, "mongodb://localhost/?connectTimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestConnectTimeout(int? ms, string formatString, string[] values)
        {
            var connectTimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (connectTimeout != null) { built.ConnectTimeout = connectTimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]).Replace("/?connectTimeout=30s", "");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(connectTimeout ?? MongoDefaults.ConnectTimeout, builder.ConnectTimeout);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestConnectTimeout_Range()
        {
            var builder = new MongoUrlBuilder();
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.ConnectTimeout = TimeSpan.FromMilliseconds(-1); });
            builder.ConnectTimeout = TimeSpan.FromMilliseconds(0);
            builder.ConnectTimeout = TimeSpan.FromMilliseconds(1);
        }

        [Theory]
        [InlineData(null, null, "mongodb://localhost")]
        [InlineData("username@domain.com", "password", "mongodb://username%40domain.com:password@localhost")]
        [InlineData("username", "password", "mongodb://username:password@localhost")]
        [InlineData("usern;me", "p;ssword", "mongodb://usern%3Bme:p%3Bssword@localhost")]
        [InlineData("usern;me", null, "mongodb://usern%3Bme@localhost")]
        [InlineData("usern;me", "", "mongodb://usern%3Bme:@localhost")]
        public void TestCredential(string username, string password, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, Username = username, Password = password };

            foreach (var url in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(password, url.Password);
                Assert.Equal(username, url.Username);
                Assert.Equal(connectionString, url.ToString());
            }
        }

        [Theory]
        [InlineData(null, "mongodb://localhost")]
        [InlineData("database", "mongodb://localhost/database")]
        public void TestDatabaseName(string databaseName, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, DatabaseName = databaseName };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(databaseName, builder.DatabaseName);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestDefaults()
        {
            var built = new MongoUrlBuilder();
            var connectionString = "mongodb://localhost";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(false, builder.AllowInsecureTls);
                Assert.Equal(null, builder.ApplicationName);
                Assert.Equal(null, builder.AuthenticationMechanism);
                Assert.Equal(0, builder.AuthenticationMechanismProperties.Count());
                Assert.Equal(null, builder.AuthenticationSource);
                Assert.Equal(new CompressorConfiguration[0], builder.Compressors);
#pragma warning disable CS0618 // Type or member is obsolete
                Assert.Equal(MongoDefaults.ComputedWaitQueueSize, builder.ComputedWaitQueueSize);
#pragma warning restore CS0618 // Type or member is obsolete
                Assert.Equal(MongoDefaults.ConnectTimeout, builder.ConnectTimeout);
                Assert.Equal(null, builder.DatabaseName);
                Assert.Equal(false, builder.DirectConnection);
                Assert.Equal(null, builder.FSync);
                Assert.Equal(ServerSettings.DefaultHeartbeatInterval, builder.HeartbeatInterval);
                Assert.Equal(ServerSettings.DefaultHeartbeatTimeout, builder.HeartbeatTimeout);
                Assert.Equal(false, builder.IPv6);
                Assert.Equal(null, builder.Journal);
                Assert.Equal(false, builder.LoadBalanced);
                Assert.Equal(MongoInternalDefaults.ConnectionPool.MaxConnecting, builder.MaxConnecting);
                Assert.Equal(MongoDefaults.MaxConnectionIdleTime, builder.MaxConnectionIdleTime);
                Assert.Equal(MongoDefaults.MaxConnectionLifeTime, builder.MaxConnectionLifeTime);
                Assert.Equal(MongoDefaults.MaxConnectionPoolSize, builder.MaxConnectionPoolSize);
                Assert.Equal(MongoDefaults.MinConnectionPoolSize, builder.MinConnectionPoolSize);
                Assert.Equal(null, builder.Password);
                Assert.Equal(null, builder.ReadPreference);
                Assert.Equal(null, builder.ReplicaSetName);
                Assert.Equal(null, builder.RetryReads);
                Assert.Equal(null, builder.RetryWrites);
                Assert.Equal(MongoDefaults.LocalThreshold, builder.LocalThreshold);
                Assert.Equal(MongoDefaults.ServerSelectionTimeout, builder.ServerSelectionTimeout);
                Assert.Equal(MongoDefaults.SocketTimeout, builder.SocketTimeout);
                Assert.Equal(null, builder.Timeout);
                Assert.Equal(MongoInternalDefaults.MongoClientSettings.SrvServiceName, builder.SrvServiceName);
                Assert.Equal(null, builder.Username);
#pragma warning disable 618
                Assert.Equal(false, builder.UseSsl);
#pragma warning restore 618
                Assert.Equal(false, builder.UseTls);
#pragma warning disable 618
                Assert.Equal(true, builder.VerifySslCertificate);
#pragma warning restore 618
                Assert.Equal(null, builder.W);
#pragma warning disable 618
                Assert.Equal(MongoDefaults.WaitQueueMultiple, builder.WaitQueueMultiple);
                Assert.Equal(MongoDefaults.WaitQueueSize, builder.WaitQueueSize);
#pragma warning restore 618
                Assert.Equal(MongoDefaults.WaitQueueTimeout, builder.WaitQueueTimeout);
                Assert.Equal(null, builder.WTimeout);
                Assert.Equal(connectionString, builder.ToString());
            }
        }


        [Theory]
        [InlineData(true, "mongodb://localhost/?directConnection=true")]
        [InlineData(false, "mongodb://localhost")]
        public void TestDirectConnection(bool directConnection, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, DirectConnection = directConnection };

            directConnection.Should().Be(built.DirectConnection);
            built.ToString().Should().Be(connectionString);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(false, "mongodb://localhost/?fsync={0}", new[] { "false", "False" })]
        [InlineData(true, "mongodb://localhost/?fsync={0}", new[] { "true", "True" })]
        public void TestFSync(bool? fsync, string formatString, string[] values)
        {
            var built = new MongoUrlBuilder { Server = _localhost, FSync = fsync };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(fsync, builder.FSync);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData(false, false, "mongodb://localhost")]
        [InlineData(false, false, "mongodb://localhost/?safe=false")]
        [InlineData(false, true, "mongodb://localhost/?safe=true")]
        [InlineData(false, false, "mongodb://localhost/?w=0")]
        [InlineData(false, true, "mongodb://localhost/?w=1")]
        [InlineData(false, true, "mongodb://localhost/?w=2")]
        [InlineData(false, true, "mongodb://localhost/?w=mode")]
        [InlineData(true, true, "mongodb://localhost")]
        [InlineData(true, false, "mongodb://localhost/?safe=false")]
        [InlineData(true, true, "mongodb://localhost/?safe=true")]
        [InlineData(true, false, "mongodb://localhost/?w=0")]
        [InlineData(true, true, "mongodb://localhost/?w=1")]
        [InlineData(true, true, "mongodb://localhost/?w=2")]
        [InlineData(true, true, "mongodb://localhost/?w=mode")]
        public void TestGetWriteConcern_IsAcknowledged(bool acknowledgedDefault, bool acknowledged, string connectionString)
        {
            var builder = new MongoUrlBuilder(connectionString);
            var writeConcern = builder.GetWriteConcern(acknowledgedDefault);
            Assert.Equal(acknowledged, writeConcern.IsAcknowledged);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost")]
        [InlineData(false, "mongodb://localhost/?fsync=false")]
        [InlineData(true, "mongodb://localhost/?fsync=true")]
        public void TestGetWriteConcern_FSync(bool? fsync, string connectionString)
        {
            var builder = new MongoUrlBuilder(connectionString);
            var writeConcern = builder.GetWriteConcern(true);
            Assert.Equal(fsync, writeConcern.FSync);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" }, new[] { "" })]
        [InlineData(false, "mongodb://localhost/?{1}={0}", new[] { "false", "False" }, new[] { "journal", "j" })]
        [InlineData(true, "mongodb://localhost/?{1}={0}", new[] { "true", "True" }, new[] { "journal", "j" })]
        public void TestGetWriteConcern_Journal(bool? journal, string formatString, string[] values, string[] journalAliases)
        {
            var canonicalConnectionString = string.Format(formatString, values[0], "journal");
            foreach (var builder in EnumerateParsedBuilders(formatString, values, journalAliases))
            {
                var writeConcern = builder.GetWriteConcern(true);
                Assert.Equal(journal, writeConcern.Journal);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData(false, false, 0, "mongodb://localhost")]
        [InlineData(false, false, 0, "mongodb://localhost/?w=0")]
        [InlineData(false, true, 1, "mongodb://localhost/?w=1")]
        [InlineData(false, true, 2, "mongodb://localhost/?w=2")]
        [InlineData(false, true, "mode", "mongodb://localhost/?w=mode")]
        [InlineData(true, true, null, "mongodb://localhost")]
        [InlineData(true, false, 0, "mongodb://localhost/?w=0")]
        [InlineData(true, true, 1, "mongodb://localhost/?w=1")]
        [InlineData(true, true, 2, "mongodb://localhost/?w=2")]
        [InlineData(true, true, "mode", "mongodb://localhost/?w=mode")]
        public void TestGetWriteConcern_W(bool acknowledgedDefault, bool acknowledged, object wobj, string connectionString)
        {
            var w = (wobj == null) ? null : (wobj is int) ? (WriteConcern.WValue)new WriteConcern.WCount((int)wobj) : new WriteConcern.WMode((string)wobj);
            var builder = new MongoUrlBuilder(connectionString);
            var writeConcern = builder.GetWriteConcern(acknowledgedDefault);
            Assert.Equal(acknowledged, writeConcern.IsAcknowledged);
            Assert.Equal(w, writeConcern.W);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost")]
        [InlineData(500, "mongodb://localhost/?wtimeout=500ms")]
        public void TestGetWriteConcern_WTimeout(int? ms, string connectionString)
        {
            var wtimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var builder = new MongoUrlBuilder(connectionString);
            var writeConcern = builder.GetWriteConcern(true);
            Assert.Equal(wtimeout, writeConcern.WTimeout);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(500, "mongodb://localhost/?heartbeatInterval{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [InlineData(30000, "mongodb://localhost/?heartbeatInterval{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [InlineData(1800000, "mongodb://localhost/?heartbeatInterval{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [InlineData(3600000, "mongodb://localhost/?heartbeatInterval{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [InlineData(3723000, "mongodb://localhost/?heartbeatInterval{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestHeartbeatInterval(int? ms, string formatString, string[] values)
        {
            var heartbeatInterval = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (heartbeatInterval != null) { built.HeartbeatInterval = heartbeatInterval.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]).Replace("/?heartbeatInterval=10s", "");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(heartbeatInterval ?? ServerSettings.DefaultHeartbeatInterval, builder.HeartbeatInterval);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestHeartbeatInterval_Range()
        {
            var builder = new MongoUrlBuilder();
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.HeartbeatInterval = TimeSpan.FromMilliseconds(-1); });
            builder.HeartbeatInterval = TimeSpan.FromMilliseconds(0);
            builder.HeartbeatInterval = TimeSpan.FromMilliseconds(1);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(500, "mongodb://localhost/?heartbeatTimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [InlineData(30000, "mongodb://localhost/?heartbeatTimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [InlineData(1800000, "mongodb://localhost/?heartbeatTimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [InlineData(3600000, "mongodb://localhost/?heartbeatTimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [InlineData(3723000, "mongodb://localhost/?heartbeatTimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestHeartbeatTimeout(int? ms, string formatString, string[] values)
        {
            var heartbeatTimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (heartbeatTimeout != null) { built.HeartbeatTimeout = heartbeatTimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]).Replace("/?heartbeatTimeout=10s", "");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(heartbeatTimeout ?? ServerSettings.DefaultHeartbeatTimeout, builder.HeartbeatTimeout);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestHeartbeatTimeout_Range()
        {
            var builder = new MongoUrlBuilder();
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.HeartbeatTimeout = TimeSpan.FromMilliseconds(-2); });
            builder.HeartbeatTimeout = TimeSpan.FromMilliseconds(0);
            builder.HeartbeatTimeout = TimeSpan.FromMilliseconds(1);
            builder.HeartbeatTimeout = TimeSpan.FromMilliseconds(-1);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(false, "mongodb://localhost/?ipv6={0}", new[] { "false", "False" })]
        [InlineData(true, "mongodb://localhost/?ipv6={0}", new[] { "true", "True" })]
        public void TestIPv6(bool? ipv6, string formatString, string[] values)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (ipv6 != null) { built.IPv6 = ipv6.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]).Replace("/?ipv6=false", "");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(ipv6 ?? false, builder.IPv6);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" }, new[] { "" })]
        [InlineData(false, "mongodb://localhost/?{1}={0}", new[] { "false", "False" }, new[] { "journal", "j" })]
        [InlineData(true, "mongodb://localhost/?{1}={0}", new[] { "true", "True" }, new[] { "journal", "j" })]
        public void TestJournal(bool? journal, string formatString, string[] values, string[] journalAliases)
        {
            var built = new MongoUrlBuilder { Server = _localhost, Journal = journal };

            var canonicalConnectionString = string.Format(formatString, values[0], "journal");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values, journalAliases))
            {
                Assert.Equal(journal, builder.Journal);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }


        [Theory]
        [ParameterAttributeData]
        public void TestLoadBalanced([Values(false, true)] bool value)
        {
            var subject = new MongoUrlBuilder { LoadBalanced = value };

            subject.LoadBalanced.Should().Be(value);
            var expectedConnectionString = $"mongodb://localhost{(value ? $"/?loadBalanced={JsonConvert.ToString(value)}" : "")}";
            subject.ToString().Should().Be(expectedConnectionString);
        }

        [Theory]
        [ParameterAttributeData]
        public void TestMaxConnecting([Values(0, -1)] int incorrectMaxConnecting)
        {
            var value = 3;

            var builder = new MongoUrlBuilder { MaxConnecting = value };
            builder.MaxConnecting.Should().Be(value);

            Record.Exception(() => { builder.MaxConnecting = incorrectMaxConnecting; }).Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(500, "mongodb://localhost/?maxIdleTime{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [InlineData(30000, "mongodb://localhost/?maxIdleTime{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [InlineData(1800000, "mongodb://localhost/?maxIdleTime{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [InlineData(3600000, "mongodb://localhost/?maxIdleTime{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [InlineData(3723000, "mongodb://localhost/?maxIdleTime{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestMaxConnectionIdleTime(int? ms, string formatString, string[] values)
        {
            var maxConnectionIdleTime = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (maxConnectionIdleTime != null) { built.MaxConnectionIdleTime = maxConnectionIdleTime.Value; };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(maxConnectionIdleTime ?? MongoDefaults.MaxConnectionIdleTime, builder.MaxConnectionIdleTime);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestMaxConnectionIdleTime_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MaxConnectionIdleTime = TimeSpan.FromSeconds(-1); });
            builder.MaxConnectionIdleTime = TimeSpan.Zero;
            builder.MaxConnectionIdleTime = TimeSpan.FromSeconds(1);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(500, "mongodb://localhost/?maxLifeTime{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [InlineData(30000, "mongodb://localhost/?maxLifeTime{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [InlineData(1800000, "mongodb://localhost/?maxLifeTime{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [InlineData(3600000, "mongodb://localhost/?maxLifeTime{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [InlineData(3723000, "mongodb://localhost/?maxLifeTime{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestMaxConnectionLifeTime(int? ms, string formatString, string[] values)
        {
            var maxConnectionLifeTime = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (maxConnectionLifeTime != null) { built.MaxConnectionLifeTime = maxConnectionLifeTime.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]).Replace("/?maxLifeTime=30m", "");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(maxConnectionLifeTime ?? MongoDefaults.MaxConnectionLifeTime, builder.MaxConnectionLifeTime);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestMaxConnectionLifeTime_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MaxConnectionLifeTime = TimeSpan.FromSeconds(-1); });
            builder.MaxConnectionLifeTime = TimeSpan.Zero;
            builder.MaxConnectionLifeTime = TimeSpan.FromSeconds(1);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost")]
        [InlineData(123, "mongodb://localhost/?maxPoolSize=123")]
        public void TestMaxConnectionPoolSize(int? maxConnectionPoolSize, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (maxConnectionPoolSize != null) { built.MaxConnectionPoolSize = maxConnectionPoolSize.Value; }

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(maxConnectionPoolSize ?? MongoDefaults.MaxConnectionPoolSize, builder.MaxConnectionPoolSize);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestMaxConnectionPoolSize_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MaxConnectionPoolSize = -1; });
            builder.MaxConnectionPoolSize = 1;
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(500, "mongodb://localhost/?readPreference=secondary&maxStaleness{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "Seconds=0.5" })]
        [InlineData(20000, "mongodb://localhost/?readPreference=secondary&maxStaleness{0}", new[] { "=20s", "=20000ms", "=20", "=00:00:20", "Seconds=20.0" })]
        [InlineData(1800000, "mongodb://localhost/?readPreference=secondary&maxStaleness{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "Seconds=1800.0" })]
        [InlineData(3600000, "mongodb://localhost/?readPreference=secondary&maxStaleness{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "Seconds=3600.0" })]
        [InlineData(3723000, "mongodb://localhost/?readPreference=secondary&maxStaleness{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "Seconds=3723.0" })]
        public void TestMaxStaleness(int? ms, string formatString, string[] values)
        {
            var maxStaleness = ms.HasValue ? TimeSpan.FromMilliseconds(ms.Value) : (TimeSpan?)null;
            var readPreference = maxStaleness.HasValue ? new ReadPreference(ReadPreferenceMode.Secondary, maxStaleness: maxStaleness) : null;
            var built = new MongoUrlBuilder { Server = _localhost };
            if (readPreference != null) { built.ReadPreference = readPreference; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(readPreference, builder.ReadPreference);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData("mongodb://localhost/?readPreference=secondary")]
        [InlineData("mongodb://localhost/?readPreference=secondary&maxStalenessSeconds=-1")]
        [InlineData("mongodb://localhost/?readPreference=secondary&maxStaleness=-1")]
        [InlineData("mongodb://localhost/?readPreference=secondary&maxStaleness=-1s")]
        [InlineData("mongodb://localhost/?readPreference=secondary&maxStaleness=-1000ms")]
        public void TestNoMaxStaleness(string value)
        {
            var builder = new MongoUrlBuilder(value);

            builder.ReadPreference.MaxStaleness.Should().NotHaveValue();
            builder.ToString().Should().Be("mongodb://localhost/?readPreference=secondary");
        }

        [Theory]
        [InlineData(null, "mongodb://localhost")]
        [InlineData(123, "mongodb://localhost/?minPoolSize=123")]
        public void TestMinConnectionPoolSize(int? minConnectionPoolSize, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (minConnectionPoolSize != null) { built.MinConnectionPoolSize = minConnectionPoolSize.Value; }

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(minConnectionPoolSize ?? MongoDefaults.MinConnectionPoolSize, builder.MinConnectionPoolSize);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestMinConnectionPoolSize_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MinConnectionPoolSize = -1; });
            builder.MinConnectionPoolSize = 0;
            builder.MinConnectionPoolSize = 1;
        }

        [Theory]
        [InlineData("mongodb://localhost/?readConcernLevel=available", ReadConcernLevel.Available)]
        [InlineData("mongodb://localhost/?readConcernLevel=linearizable", ReadConcernLevel.Linearizable)]
        [InlineData("mongodb://localhost/?readConcernLevel=local", ReadConcernLevel.Local)]
        [InlineData("mongodb://localhost/?readConcernLevel=majority", ReadConcernLevel.Majority)]
        [InlineData("mongodb://localhost/?readConcernLevel=snapshot", ReadConcernLevel.Snapshot)]
        public void TestReadConcernLevel(string connectionString, ReadConcernLevel readConcernLevel)
        {
            var built = new MongoUrlBuilder { ReadConcernLevel = readConcernLevel };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(readConcernLevel, builder.ReadConcernLevel);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(ReadPreferenceMode.Primary, "mongodb://localhost/?readPreference={0}", new[] { "primary", "Primary" })]
        [InlineData(ReadPreferenceMode.PrimaryPreferred, "mongodb://localhost/?readPreference={0}", new[] { "primaryPreferred", "PrimaryPreferred" })]
        [InlineData(ReadPreferenceMode.Secondary, "mongodb://localhost/?readPreference={0}", new[] { "secondary", "Secondary" })]
        [InlineData(ReadPreferenceMode.SecondaryPreferred, "mongodb://localhost/?readPreference={0}", new[] { "secondaryPreferred", "SecondaryPreferred" })]
        [InlineData(ReadPreferenceMode.Nearest, "mongodb://localhost/?readPreference={0}", new[] { "nearest", "Nearest" })]
        public void TestReadPreference(ReadPreferenceMode? mode, string formatString, string[] values)
        {
            ReadPreference readPreference = null;
            if (mode != null) { readPreference = new ReadPreference(mode.Value); }
            var built = new MongoUrlBuilder { Server = _localhost, ReadPreference = readPreference };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(readPreference, builder.ReadPreference);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestReadPreference_NoReadPreferenceModeWithOneTagSet()
        {
            var connectionString = "mongodb://localhost/?readPreferenceTags=dc:ny,rack:1";

            Assert.Throws<MongoConfigurationException>(() => new MongoUrlBuilder(connectionString));
        }

        [Fact]
        public void TestReadPreference_SecondaryWithOneTagSet()
        {
            var tagSets = new TagSet[]
            {
                new TagSet(new [] { new Tag("dc", "ny"), new Tag("rack", "1") })
            };
            var readPreference = new ReadPreference(ReadPreferenceMode.Secondary, tagSets);
            var built = new MongoUrlBuilder { Server = _localhost, ReadPreference = readPreference };
            var connectionString = "mongodb://localhost/?readPreference=secondary&readPreferenceTags=dc:ny,rack:1";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(ReadPreferenceMode.Secondary, builder.ReadPreference.ReadPreferenceMode);
                var builderTagSets = builder.ReadPreference.TagSets.ToArray();
                Assert.Equal(1, builderTagSets.Length);
                var builderTagSet1Tags = builderTagSets[0].Tags.ToArray();
                Assert.Equal(2, builderTagSet1Tags.Length);
                Assert.Equal(new Tag("dc", "ny"), builderTagSet1Tags[0]);
                Assert.Equal(new Tag("rack", "1"), builderTagSet1Tags[1]);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestReadPreference_SecondaryWithTwoTagSets()
        {
            var tagSets = new TagSet[]
            {
                new TagSet(new [] { new Tag("dc", "ny"), new Tag("rack", "1") }),
                new TagSet(new [] { new Tag("dc", "sf") })
            };
            var readPreference = new ReadPreference(ReadPreferenceMode.Secondary, tagSets);
            var built = new MongoUrlBuilder { Server = _localhost, ReadPreference = readPreference };
            var connectionString = "mongodb://localhost/?readPreference=secondary&readPreferenceTags=dc:ny,rack:1&readPreferenceTags=dc:sf";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(ReadPreferenceMode.Secondary, builder.ReadPreference.ReadPreferenceMode);
                var builderTagSets = builder.ReadPreference.TagSets.ToArray();
                Assert.Equal(2, builderTagSets.Length);
                var builderTagSet1Tags = builderTagSets[0].Tags.ToArray();
                var builderTagSet2Tags = builderTagSets[1].Tags.ToArray();
                Assert.Equal(2, builderTagSet1Tags.Length);
                Assert.Equal(new Tag("dc", "ny"), builderTagSet1Tags[0]);
                Assert.Equal(new Tag("rack", "1"), builderTagSet1Tags[1]);
                Assert.Equal(1, builderTagSet2Tags.Length);
                Assert.Equal(new Tag("dc", "sf"), builderTagSet2Tags[0]);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData(null, "mongodb://localhost")]
        [InlineData("name", "mongodb://localhost/?replicaSet=name")]
        public void TestReplicaSetName(string name, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, ReplicaSetName = name };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(name, builder.ReplicaSetName);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData("mongodb://localhost/", null)]
        [InlineData("mongodb://localhost/?retryReads=true", true)]
        [InlineData("mongodb://localhost/?retryReads=false", false)]
        public void TestRetryReads(string url, bool? retryReads)
        {
            var builder = new MongoUrlBuilder(url);
            Assert.Equal(retryReads, builder.RetryReads);
        }

        [Theory]
        [InlineData("mongodb://localhost/", null)]
        [InlineData("mongodb://localhost/?retryWrites=true", true)]
        [InlineData("mongodb://localhost/?retryWrites=false", false)]
        public void TestRetryWrites(string url, bool? retryWrites)
        {
            var builder = new MongoUrlBuilder(url);
            Assert.Equal(retryWrites, builder.RetryWrites);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(0, "mongodb://localhost/?{0}", new[] { "w=0", "w=0&safe=false", "w=0&safe=False", "safe=false", "safe=False" })]
        [InlineData(0, "mongodb://localhost/?{0}", new[] { "w=0", "w=1&safe=false", "w=1&safe=False" })]
        [InlineData(0, "mongodb://localhost/?{0}", new[] { "w=0", "w=2&safe=false", "w=2&safe=False" })]
        [InlineData(0, "mongodb://localhost/?{0}", new[] { "w=0", "w=mode&safe=false", "w=mode&safe=False" })]
        [InlineData(1, "mongodb://localhost/?{0}", new[] { "w=1", "w=0&safe=true", "w=0&safe=True", "safe=true", "safe=True" })]
        [InlineData(1, "mongodb://localhost/?{0}", new[] { "w=1", "w=1&safe=true", "w=1&safe=True" })]
        [InlineData(2, "mongodb://localhost/?{0}", new[] { "w=2", "w=2&safe=true", "w=2&safe=True" })]
        [InlineData("mode", "mongodb://localhost/?{0}", new[] { "w=mode", "w=mode&safe=true", "w=mode&safe=True" })]
        public void TestSafe(object wobj, string formatString, string[] values)
        {
            var w = (wobj == null) ? null : (wobj is int) ? (WriteConcern.WValue)(int)wobj : (string)wobj;
            var built = new MongoUrlBuilder { Server = _localhost, W = w };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(w, builder.W);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(500, "mongodb://localhost/?localThreshold{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [InlineData(30000, "mongodb://localhost/?localThreshold{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [InlineData(1800000, "mongodb://localhost/?localThreshold{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [InlineData(3600000, "mongodb://localhost/?localThreshold{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [InlineData(3723000, "mongodb://localhost/?localThreshold{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestLocalThreshold(int? ms, string formatString, string[] values)
        {
            var localThreshold = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (localThreshold != null) { built.LocalThreshold = localThreshold.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(localThreshold ?? MongoDefaults.LocalThreshold, builder.LocalThreshold);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestLocalThreshold_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.LocalThreshold = TimeSpan.FromSeconds(-1); });
            builder.LocalThreshold = TimeSpan.Zero;
            builder.LocalThreshold = TimeSpan.FromSeconds(1);
        }

        [Theory]
        [InlineData("mongodb://host", ConnectionStringScheme.MongoDB)]
        [InlineData("mongodb+srv://host", ConnectionStringScheme.MongoDBPlusSrv)]
        public void TestScheme(string url, ConnectionStringScheme scheme)
        {
            var built = new MongoUrlBuilder(url);
            Assert.Equal(scheme, built.Scheme);
        }

        [Theory]
        [InlineData("host", null, "mongodb://host")]
        [InlineData("host", 27017, "mongodb://host")]
        [InlineData("host", 27018, "mongodb://host:27018")]
        public void TestServer(string host, int? port, string connectionString)
        {
            var server = (host == null) ? null : (port == null) ? new MongoServerAddress(host) : new MongoServerAddress(host, port.Value);
            var built = new MongoUrlBuilder { Server = server };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(server, builder.Server);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData(new string[] { "host" }, new object[] { null }, "mongodb://host")]
        [InlineData(new string[] { "host" }, new object[] { 27017 }, "mongodb://host")]
        [InlineData(new string[] { "host" }, new object[] { 27018 }, "mongodb://host:27018")]
        [InlineData(new string[] { "host1", "host2" }, new object[] { null, null }, "mongodb://host1,host2")]
        [InlineData(new string[] { "host1", "host2" }, new object[] { null, 27017 }, "mongodb://host1,host2")]
        [InlineData(new string[] { "host1", "host2" }, new object[] { null, 27018 }, "mongodb://host1,host2:27018")]
        [InlineData(new string[] { "host1", "host2" }, new object[] { 27017, null }, "mongodb://host1,host2")]
        [InlineData(new string[] { "host1", "host2" }, new object[] { 27017, 27017 }, "mongodb://host1,host2")]
        [InlineData(new string[] { "host1", "host2" }, new object[] { 27017, 27018 }, "mongodb://host1,host2:27018")]
        [InlineData(new string[] { "host1", "host2" }, new object[] { 27018, null }, "mongodb://host1:27018,host2")]
        [InlineData(new string[] { "host1", "host2" }, new object[] { 27018, 27017 }, "mongodb://host1:27018,host2")]
        [InlineData(new string[] { "host1", "host2" }, new object[] { 27018, 27018 }, "mongodb://host1:27018,host2:27018")]
        [InlineData(new string[] { "[::1]", "host2" }, new object[] { 27018, 27018 }, "mongodb://[::1]:27018,host2:27018")]
        public void TestServers(string[] hosts, object[] ports, string connectionString)
        {
            var servers = (hosts == null) ? null : new List<MongoServerAddress>();
            if (hosts != null)
            {
                Assert.Equal(hosts.Length, ports.Length);
                for (var i = 0; i < hosts.Length; i++)
                {
                    var server = (hosts[i] == null) ? null : (ports[i] == null) ? new MongoServerAddress(hosts[i]) : new MongoServerAddress(hosts[i], (int)ports[i]);
                    servers.Add(server);
                }
            }
            var built = new MongoUrlBuilder { Servers = servers };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(servers, builder.Servers);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(500, "mongodb://localhost/?serverSelectionTimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [InlineData(20000, "mongodb://localhost/?serverSelectionTimeout{0}", new[] { "=20s", "=20000ms", "=20", "=00:00:20", "MS=20000" })]
        [InlineData(1800000, "mongodb://localhost/?serverSelectionTimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [InlineData(3600000, "mongodb://localhost/?serverSelectionTimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [InlineData(3723000, "mongodb://localhost/?serverSelectionTimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestServerSelectionTimeout(int? ms, string formatString, string[] values)
        {
            var serverSelectionTimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (serverSelectionTimeout != null) { built.ServerSelectionTimeout = serverSelectionTimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(serverSelectionTimeout ?? MongoDefaults.ServerSelectionTimeout, builder.ServerSelectionTimeout);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestServerSelectionTimeout_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.ServerSelectionTimeout = TimeSpan.FromSeconds(-1); });
            builder.ServerSelectionTimeout = TimeSpan.Zero;
            builder.ServerSelectionTimeout = TimeSpan.FromSeconds(1);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(500, "mongodb://localhost/?socketTimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [InlineData(30000, "mongodb://localhost/?socketTimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [InlineData(1800000, "mongodb://localhost/?socketTimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [InlineData(3600000, "mongodb://localhost/?socketTimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [InlineData(3723000, "mongodb://localhost/?socketTimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestSocketTimeout(int? ms, string formatString, string[] values)
        {
            var socketTimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (socketTimeout != null) { built.SocketTimeout = socketTimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(socketTimeout ?? MongoDefaults.SocketTimeout, builder.SocketTimeout);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestSocketTimeout_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.SocketTimeout = TimeSpan.FromSeconds(-1); });
            builder.SocketTimeout = TimeSpan.Zero;
            builder.SocketTimeout = TimeSpan.FromSeconds(1);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(-1, "mongodb://localhost/?timeout{0}", new[] { "=0", "MS=0" })]
        [InlineData(500, "mongodb://localhost/?timeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [InlineData(30000, "mongodb://localhost/?timeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [InlineData(1800000, "mongodb://localhost/?timeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [InlineData(3600000, "mongodb://localhost/?timeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [InlineData(3723000, "mongodb://localhost/?timeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestTimeout(int? ms, string formatString, string[] values)
        {
            var timeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (timeout != null) { built.Timeout = timeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(timeout, builder.Timeout);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestSrvServiceName()
        {
            var srvServiceName = "customname";
            var canonicalConnectionString = $"mongodb+srv://localhost/?srvServiceName={srvServiceName}";

            var built = new MongoUrlBuilder { UseTls = true, Scheme = ConnectionStringScheme.MongoDBPlusSrv };

            Assert.Throws<ArgumentException>(() => built.SrvServiceName = "");

            built.SrvServiceName = srvServiceName;
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, canonicalConnectionString))
            {
                Assert.Equal(srvServiceName, builder.SrvServiceName);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestSrvServiceName_WithNonSRVScheme()
        {
            Assert.Throws<MongoConfigurationException>(() =>
            {
                _ = new MongoUrlBuilder("mongodb://localhost/?srvServiceName=customname");
            });
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(false, "mongodb://localhost/?tls={0}", new[] { "false", "False" })]
        [InlineData(true, "mongodb://localhost/?tls={0}", new[] { "true", "True" })]
        public void TestUseTls(bool? useTls, string formatString, string[] values)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (useTls != null) { built.UseTls = useTls.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]).Replace("/?tls=false", "");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(useTls ?? false, builder.UseTls);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Theory]
        [InlineData(false, false, 0, "mongodb://localhost/?w=0")]
        [InlineData(false, false, 0, "mongodb://localhost/?w=0")]
        [InlineData(false, true, 1, "mongodb://localhost/?w=1")]
        [InlineData(false, true, 2, "mongodb://localhost/?w=2")]
        [InlineData(false, true, "mode", "mongodb://localhost/?w=mode")]
        [InlineData(true, true, null, "mongodb://localhost")]
        [InlineData(true, false, 0, "mongodb://localhost/?w=0")]
        [InlineData(true, true, 1, "mongodb://localhost/?w=1")]
        [InlineData(true, true, 2, "mongodb://localhost/?w=2")]
        [InlineData(true, true, "mode", "mongodb://localhost/?w=mode")]
        public void TestW(bool acknowledgedDefault, bool acknowledged, object wobj, string connectionString)
        {
            var w = (wobj == null) ? null : (wobj is int) ? (WriteConcern.WValue)new WriteConcern.WCount((int)wobj) : new WriteConcern.WMode((string)wobj);
            var built = new MongoUrlBuilder { Server = _localhost, W = w };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                var writeConcern = builder.GetWriteConcern(acknowledgedDefault);
                Assert.Equal(acknowledged, writeConcern.IsAcknowledged);
                Assert.Equal(w, writeConcern.W);
                Assert.Equal(connectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestW_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            builder.W = null;
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.W = -1; });
            builder.W = 0; // magic zero
            builder.W = 1; // magic one
            builder.W = 2; // regular w value
            builder.W = "mode"; // a mode name
        }

        [Theory]
        [InlineData(null, "mongodb://localhost")]
        [InlineData(2.0, "mongodb://localhost/?waitQueueMultiple=2")]
        public void TestWaitQueueMultiple(double? multiple, string connectionString)
        {
#pragma warning disable 618
            var built = new MongoUrlBuilder { Server = _localhost };
            if (multiple != null) { built.WaitQueueMultiple = multiple.Value; }

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal(multiple ?? MongoDefaults.WaitQueueMultiple, builder.WaitQueueMultiple);
                Assert.Equal((multiple == null) ? MongoDefaults.WaitQueueSize : 0, builder.WaitQueueSize);
                Assert.Equal(connectionString, builder.ToString());
            }
#pragma warning restore 618
        }

        [Fact]
        public void TestWaitQueueMultiple_Range()
        {
#pragma warning disable 618
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueMultiple = -1.0; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueMultiple = 0.0; });
            builder.WaitQueueMultiple = 1.0;
#pragma warning restore 618
        }

        [Theory]
        [InlineData(null, "mongodb://localhost")]
        [InlineData(123, "mongodb://localhost/?waitQueueSize=123")]
        public void TestWaitQueueSize(int? size, string connectionString)
        {
#pragma warning disable 618
            var built = new MongoUrlBuilder { Server = _localhost };
            if (size != null) { built.WaitQueueSize = size.Value; }

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.Equal((size == null) ? MongoDefaults.WaitQueueMultiple : 0.0, builder.WaitQueueMultiple);
                Assert.Equal(size ?? MongoDefaults.WaitQueueSize, builder.WaitQueueSize);
                Assert.Equal(connectionString, builder.ToString());
            }
#pragma warning restore 618
        }

        [Fact]
        public void TestWaitQueueSize_Range()
        {
#pragma warning disable 618
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueSize = -1; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueSize = 0; });
            builder.WaitQueueSize = 1;
#pragma warning restore 618
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(500, "mongodb://localhost/?waitQueueTimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [InlineData(30000, "mongodb://localhost/?waitQueueTimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [InlineData(1800000, "mongodb://localhost/?waitQueueTimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [InlineData(3600000, "mongodb://localhost/?waitQueueTimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [InlineData(3723000, "mongodb://localhost/?waitQueueTimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestWaitQueueTimeout(int? ms, string formatString, string[] values)
        {
            var waitQueueTimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (waitQueueTimeout != null) { built.WaitQueueTimeout = waitQueueTimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(waitQueueTimeout ?? MongoDefaults.WaitQueueTimeout, builder.WaitQueueTimeout);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestWaitQueueTimeout_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueTimeout = TimeSpan.FromSeconds(-1); });
            builder.WaitQueueTimeout = TimeSpan.Zero;
            builder.WaitQueueTimeout = TimeSpan.FromSeconds(1);
        }

        [Theory]
        [InlineData(null, "mongodb://localhost", new[] { "" })]
        [InlineData(500, "mongodb://localhost/?wtimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [InlineData(30000, "mongodb://localhost/?wtimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [InlineData(1800000, "mongodb://localhost/?wtimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [InlineData(3600000, "mongodb://localhost/?wtimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [InlineData(3723000, "mongodb://localhost/?wtimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestWTimeout(int? ms, string formatString, string[] values)
        {
            var wtimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (wtimeout != null) { built.WTimeout = wtimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.Equal(wtimeout, builder.WTimeout);
                Assert.Equal(canonicalConnectionString, builder.ToString());
            }
        }

        [Fact]
        public void TestWTimeout_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WTimeout = TimeSpan.FromSeconds(-1); });
            builder.WTimeout = TimeSpan.Zero;
            builder.WTimeout = TimeSpan.FromSeconds(1);
        }

        [Theory]
        [InlineData("mongodb://localhost", "mongodb://localhost")]
        [InlineData("mongodb://localhost/?ssl=false", "mongodb://localhost")]
        [InlineData("mongodb://localhost/?ssl=true", "mongodb://localhost/?tls=true")]
        [InlineData("mongodb://localhost:27018", "mongodb://localhost:27018")]
        [InlineData("mongodb://localhost:27018/?ssl=false", "mongodb://localhost:27018")]
        [InlineData("mongodb://localhost:27018/?ssl=true", "mongodb://localhost:27018/?tls=true")]
        [InlineData("mongodb+srv://localhost", "mongodb+srv://localhost")]
        [InlineData("mongodb+srv://localhost/?ssl=false", "mongodb+srv://localhost/?tls=false")]
        [InlineData("mongodb+srv://localhost/?ssl=true", "mongodb+srv://localhost")]
        [InlineData("mongodb+srv://localhost/?srvMaxHosts=2", "mongodb+srv://localhost/?srvMaxHosts=2")]
        [InlineData("mongodb+srv://localhost/?srvServiceName=customname", "mongodb+srv://localhost/?srvServiceName=customname")]
        public void ToString_should_return_expected_result_for_scheme_port_and_ssl(string connectionString, string expectedResult)
        {
            var subject = new MongoUrlBuilder(connectionString);

            var result = subject.ToString();

            result.Should().Be(expectedResult);
        }

        // private methods
        private IEnumerable<MongoUrlBuilder> EnumerateBuiltAndParsedBuilders(
            MongoUrlBuilder built,
            string connectionString)
        {
            yield return built;
            yield return new MongoUrlBuilder(connectionString);
        }

        private IEnumerable<(MongoUrlBuilder Builder, string Value)> EnumerateBuiltAndParsedBuildersWithValue(
            MongoUrlBuilder built,
            string formatString,
            string[] values)
        {
            yield return (built, null);
            foreach (var parsed in EnumerateParsedBuilders(formatString, values))
            {
                yield return parsed;
            }
        }

        private IEnumerable<MongoUrlBuilder> EnumerateBuiltAndParsedBuilders(
            MongoUrlBuilder built,
            string formatString,
            string[] values)
        {
            return EnumerateBuiltAndParsedBuildersWithValue(built, formatString, values).Select(c => c.Builder);
        }

        private IEnumerable<MongoUrlBuilder> EnumerateBuiltAndParsedBuilders(
            MongoUrlBuilder built,
            string formatString,
            string[] values1,
            string[] values2)
        {
            yield return built;
            foreach (var parsed in EnumerateParsedBuilders(formatString, values1, values2))
            {
                yield return parsed;
            }
        }

        private IEnumerable<(MongoUrlBuilder Builder, string Value)> EnumerateParsedBuilders(
            string formatString,
            string[] values)
        {
            foreach (var v in values)
            {
                var connectionString = string.Format(formatString, v);
                yield return (new MongoUrlBuilder(connectionString), v);
            }
        }

        private IEnumerable<MongoUrlBuilder> EnumerateParsedBuilders(
            string formatString,
            string[] values1,
            string[] values2)
        {
            foreach (var v1 in values1)
            {
                foreach (var v2 in values2)
                {
                    var connectionString = string.Format(formatString, v1, v2);
                    yield return new MongoUrlBuilder(connectionString);
                }
            }
        }
    }
}
