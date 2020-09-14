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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using Xunit;

namespace MongoDB.Driver.Tests
{
    [Trait("Category", "ConnectionString")]
    public class MongoUrlTests
    {
        [Theory]
        [InlineData("mongodb://localhost", true)]
        [InlineData("mongodb+srv://localhost", false)]
        public void constructor_with_string_should_set_isResolved_to_expected_value(string connectionString, bool expectedResult)
        {
            var subject = new MongoUrl(connectionString);

            var result = subject.IsResolved;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("mongodb://localhost", true)]
        [InlineData("mongodb+srv://localhost", false)]
        [InlineData("mongodb+srv://localhost", true)]
        public void constructor_with_string_and_bool_should_initialize_instance(string url, bool isResolved)
        {
            var result = new MongoUrl(url, isResolved);

            result.IsResolved.Should().Be(isResolved);
        }

        [Theory]
        [InlineData("mongodb://localhost")]
        public void constructor_with_string_and_bool_should_throw_when_false_is_invalid(string url)
        {
            var exception = Record.Exception(() => new MongoUrl(url, false));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("isResolved");
        }

        [Theory]
        [InlineData("mongodb+srv://test5.test.build.10gen.cc", "localhost.test.build.10gen.cc:27017", false)]
        [InlineData("mongodb+srv://test5.test.build.10gen.cc", "localhost.test.build.10gen.cc:27017", true)]
        public void Resolve_should_return_expected_result(string url, string expectedServer, bool async)
        {
            var subject = new MongoUrl(url);

            MongoUrl result;
            if (async)
            {
                result = subject.Resolve();
            }
            else
            {
                result = subject.ResolveAsync().GetAwaiter().GetResult();
            }

            var expectedServers = new[] { MongoServerAddress.Parse(expectedServer) };
            result.Servers.Should().Equal(expectedServers);
        }

        [Theory]
        [InlineData("mongodb+srv://test5.test.build.10gen.cc", false, "test5.test.build.10gen.cc:53", false)]
        [InlineData("mongodb+srv://test5.test.build.10gen.cc", false, "test5.test.build.10gen.cc:53", true)]
        [InlineData("mongodb+srv://test5.test.build.10gen.cc", true, "localhost.test.build.10gen.cc:27017", false)]
        [InlineData("mongodb+srv://test5.test.build.10gen.cc", true, "localhost.test.build.10gen.cc:27017", true)]
        public void Resolve_with_resolveHosts_should_return_expected_result(string url, bool resolveHosts, string expectedServer, bool async)
        {
            var subject = new MongoUrl(url);

            MongoUrl result;
            if (async)
            {
                result = subject.Resolve(resolveHosts);
            }
            else
            {
                result = subject.ResolveAsync(resolveHosts).GetAwaiter().GetResult();
            }

            var expectedServers = new[] { MongoServerAddress.Parse(expectedServer) };
            result.Servers.Should().Equal(expectedServers);
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
#pragma warning disable CS0618 // Type or member is obsolete
                ConnectionMode = ConnectionMode.ReplicaSet,
#pragma warning restore CS0618 // Type or member is obsolete
                ConnectTimeout = TimeSpan.FromSeconds(1),
                DatabaseName = "database",
                FSync = true,
                HeartbeatInterval = TimeSpan.FromSeconds(11),
                HeartbeatTimeout = TimeSpan.FromSeconds(12),
                IPv6 = true,
                Journal = true,
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
                ServerSelectionTimeout = TimeSpan.FromSeconds(10),
                SocketTimeout = TimeSpan.FromSeconds(7),
                Username = "username",
                UseTls = true,
                W = 2,
#pragma warning disable 618
                WaitQueueSize = 123,
#pragma warning restore 618
                WaitQueueTimeout = TimeSpan.FromSeconds(8),
                WTimeout = TimeSpan.FromSeconds(9)
            };
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                built.GuidRepresentation = GuidRepresentation.PythonLegacy;
            }
#pragma warning restore 618

            var connectionString = "mongodb://username:password@host/database?" + string.Join(";", new[] {
                "authMechanism=GSSAPI",
                "authMechanismProperties=SERVICE_NAME:other,CANONICALIZE_HOST_NAME:true",
                "authSource=db",
                "appname=app",
                "ipv6=true",
                "tls=true", // UseTls
                "tlsInsecure=true",
                "compressors=zlib",
                "zlibCompressionLevel=4",
                "connect=replicaSet",
                "replicaSet=name",
                "readConcernLevel=majority",
                "readPreference=secondary;readPreferenceTags=dc:1;maxStaleness=11s",
                "fsync=true",
                "journal=true",
                "w=2",
                "wtimeout=9s",
                "connectTimeout=1s",
                "heartbeatInterval=11s",
                "heartbeatTimeout=12s",
                "maxIdleTime=2s",
                "maxLifeTime=3s",
                "maxPoolSize=4",
                "minPoolSize=5",
                "localThreshold=6s",
                "serverSelectionTimeout=10s",
                "socketTimeout=7s",
                "waitQueueSize=123",
                "waitQueueTimeout=8s",
                "retryReads=false",
                "retryWrites=true"
            });
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                var index = connectionString.IndexOf("retryReads=false;");
                connectionString = connectionString.Insert(index, "uuidRepresentation=pythonLegacy;");
            }
#pragma warning restore 618

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.Equal(true, url.AllowInsecureTls);
                Assert.Equal("app", url.ApplicationName);
                Assert.Equal("GSSAPI", url.AuthenticationMechanism);
                Assert.Equal(authMechanismProperties, url.AuthenticationMechanismProperties);
                Assert.Equal("db", url.AuthenticationSource);
#pragma warning disable CS0618
                Assert.Equal(ConnectionModeSwitch.UseConnectionMode, url.ConnectionModeSwitch);
#pragma warning restore CS0618
                Assert.Contains(url.Compressors, x => x.Type == CompressorType.Zlib);
#pragma warning disable 618
                Assert.Equal(123, url.ComputedWaitQueueSize);
                Assert.Equal(ConnectionMode.ReplicaSet, url.ConnectionMode);
#pragma warning restore 618
                Assert.Equal(TimeSpan.FromSeconds(1), url.ConnectTimeout);
                Assert.Equal("database", url.DatabaseName);
                Assert.Equal(true, url.FSync);
#pragma warning disable 618
                if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
                {
                    Assert.Equal(GuidRepresentation.PythonLegacy, url.GuidRepresentation);
                }
#pragma warning restore 618
                Assert.Equal(TimeSpan.FromSeconds(11), url.HeartbeatInterval);
                Assert.Equal(TimeSpan.FromSeconds(12), url.HeartbeatTimeout);
                Assert.Equal(true, url.IPv6);
                Assert.Equal(true, url.IsResolved);
                Assert.Equal(true, url.Journal);
                Assert.Equal(TimeSpan.FromSeconds(2), url.MaxConnectionIdleTime);
                Assert.Equal(TimeSpan.FromSeconds(3), url.MaxConnectionLifeTime);
                Assert.Equal(4, url.MaxConnectionPoolSize);
                Assert.Equal(5, url.MinConnectionPoolSize);
                Assert.Equal("password", url.Password);
                Assert.Equal(ReadConcernLevel.Majority, url.ReadConcernLevel);
                Assert.Equal(readPreference, url.ReadPreference);
                Assert.Equal("name", url.ReplicaSetName);
                Assert.Equal(false, url.RetryReads);
                Assert.Equal(true, url.RetryWrites);
                Assert.Equal(TimeSpan.FromSeconds(6), url.LocalThreshold);
                Assert.Equal(ConnectionStringScheme.MongoDB, url.Scheme);
                Assert.Equal(new MongoServerAddress("host", 27017), url.Server);
                Assert.Equal(TimeSpan.FromSeconds(10), url.ServerSelectionTimeout);
                Assert.Equal(TimeSpan.FromSeconds(7), url.SocketTimeout);
                Assert.Equal(true, url.TlsDisableCertificateRevocationCheck);
                Assert.Equal("username", url.Username);
#pragma warning disable 618
                Assert.Equal(true, url.UseSsl);
#pragma warning restore 618
                Assert.Equal(true, url.UseTls);
#pragma warning disable 618
                Assert.Equal(false, url.VerifySslCertificate);
#pragma warning restore 618
                Assert.Equal(2, ((WriteConcern.WCount)url.W).Value);
#pragma warning disable 618
                Assert.Equal(0.0, url.WaitQueueMultiple);
                Assert.Equal(123, url.WaitQueueSize);
#pragma warning restore 618
                Assert.Equal(TimeSpan.FromSeconds(8), url.WaitQueueTimeout);
                Assert.Equal(TimeSpan.FromSeconds(9), url.WTimeout);
                var expectedConnectionString = connectionString;
#pragma warning disable 618
                if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
                {
                    var defaultGuidRepresentation = BsonDefaults.GuidRepresentation;
                    if (url.GuidRepresentation == defaultGuidRepresentation)
                    {
                        expectedConnectionString = expectedConnectionString.Replace("uuidRepresentation=pythonLegacy;", "");
                    }
                }
#pragma warning restore 618
                Assert.Equal(expectedConnectionString, url.ToString());
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void TestDirectConnection([Values(false, true, null)] bool? directConnection)
        {
            var directConnectionString = directConnection.HasValue ? $"?directConnection={directConnection.Value}" : string.Empty;
            var connectionString = $"mongodb://localhost/{directConnectionString}";
            var url = new MongoUrl(connectionString);

            url.DirectConnection.Should().Be(directConnection);
#pragma warning disable CS0618 // Type or member is obsolete
            url.ConnectionModeSwitch.Should().Be(directConnectionString != string.Empty ? ConnectionModeSwitch.UseDirectConnection : ConnectionModeSwitch.NotSet);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Theory]
#pragma warning disable CS0618 // Type or member is obsolete
        [InlineData(ConnectionModeSwitch.NotSet, "directConnection", false)]
        [InlineData(ConnectionModeSwitch.NotSet, "connect", false)]
        [InlineData(ConnectionModeSwitch.UseConnectionMode, "directConnection", true)]
        [InlineData(ConnectionModeSwitch.UseDirectConnection, "connect", true)]
        public void TestThatNotExpectedPropertyCallThrow(ConnectionModeSwitch connectionModeSwitch, string property, bool shouldFail)
        {
            var connectionString = $"mongodb://localhost";
            var url = new MongoUrl(connectionString);
            url._connectionModeSwitch(connectionModeSwitch);
            Exception exception;
            switch (property)
            {
                case "connect": exception = Record.Exception(() => url.ConnectionMode); break;
                case "directConnection": exception = Record.Exception(() => url.DirectConnection); break;
                default: throw new Exception($"Unexpected property {property}.");
            }

            if (shouldFail)
            {
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                exception.Should().BeNull();
            }
        }
#pragma warning restore CS0618 // Type or member is obsolete

        [Theory]
        [InlineData("mongodb://localhost/?readPreference=secondary")]
        [InlineData("mongodb://localhost/?readPreference=secondary;maxStalenessSeconds=-1")]
        [InlineData("mongodb://localhost/?readPreference=secondary;maxStaleness=-1")]
        [InlineData("mongodb://localhost/?readPreference=secondary;maxStaleness=-1s")]
        [InlineData("mongodb://localhost/?readPreference=secondary;maxStaleness=-1000ms")]
        public void TestNoMaxStaleness(string value)
        {
            var url = new MongoUrl(value);

            url.ReadPreference.MaxStaleness.Should().NotHaveValue();
            url.ToString().Should().Be("mongodb://localhost/?readPreference=secondary");
        }

        [Fact]
        public void TestResolveWithANativeUrl()
        {
            var connectionString = "mongodb://localhost";

            var subject = new MongoUrl(connectionString);

            var resolved = subject.Resolve();

            resolved.Should().BeSameAs(subject);
        }

        [Fact]
        public void TestResolveWithASrvUrl()
        {
            // NOTE: this requires SRV and TXT records in DNS as specified here:
            // https://github.com/mongodb/specifications/tree/master/source/initial-dns-seedlist-discovery

            var connectionString = "mongodb+srv://user%40GSSAPI.COM:password@test5.test.build.10gen.cc/funny?replicaSet=rs0";

            var subject = new MongoUrl(connectionString);

            var resolved = subject.Resolve();

            Assert.Equal("mongodb://user%40GSSAPI.COM:password@localhost.test.build.10gen.cc/funny?authSource=thisDB;tls=true;replicaSet=rs0", resolved.ToString());
        }

        [Fact]
        public async Task TestResolveAsyncWithASrvUrl()
        {
            // NOTE: this requires SRV and TXT records in DNS as specified here:
            // https://github.com/mongodb/specifications/tree/master/source/initial-dns-seedlist-discovery

            var connectionString = "mongodb+srv://user%40GSSAPI.COM:password@test5.test.build.10gen.cc/funny?replicaSet=rs0";

            var subject = new MongoUrl(connectionString);

            var resolved = await subject.ResolveAsync();

            Assert.Equal("mongodb://user%40GSSAPI.COM:password@localhost.test.build.10gen.cc/funny?authSource=thisDB;tls=true;replicaSet=rs0", resolved.ToString());
        }

        [Fact]
        public void TestTlsDisableCertificateRevocationCheck()
        {
            var built = new MongoUrlBuilder { TlsDisableCertificateRevocationCheck = true };
            var connectionString = "mongodb://aincrad/?tlsDisableCertificateRevocationCheck=true";

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                url.TlsDisableCertificateRevocationCheck.Should().Be(true);
            }
        }

        // private methods
        private IEnumerable<MongoUrl> EnumerateBuiltAndParsedUrls(
            MongoUrlBuilder built,
            string connectionString)
        {
            yield return built.ToMongoUrl();
            yield return new MongoUrl(connectionString);
        }
    }

    internal static class MongoUrlReflector
    {
#pragma warning disable CS0618 // Type or member is obsolete
        public static void _connectionModeSwitch(this MongoUrl url, ConnectionModeSwitch connectionModeSwitch)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            Reflector.SetFieldValue(url, nameof(_connectionModeSwitch), connectionModeSwitch);
        }
    }
}
