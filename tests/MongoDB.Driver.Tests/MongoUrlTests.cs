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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests
{
    [Trait("Category", "ConnectionString")]
    public class MongoUrlTests
    {
        [Fact]
        public void TestAll()
        {
            var readPreference = new ReadPreference(ReadPreferenceMode.Secondary, new[] { new TagSet(new[] { new Tag("dc", "1") }) }, TimeSpan.FromSeconds(11));
            var authMechanismProperties = new Dictionary<string, string>
            {
                { "SERVICE_NAME", "other" },
                { "CANONICALIZE_HOST_NAME", "true" }
            };
            var built = new MongoUrlBuilder()
            {
                ApplicationName = "app",
                AuthenticationMechanism = "GSSAPI",
                AuthenticationMechanismProperties = authMechanismProperties,
                AuthenticationSource = "db",
                ConnectionMode = ConnectionMode.ReplicaSet,
                ConnectTimeout = TimeSpan.FromSeconds(1),
                DatabaseName = "database",
                FSync = true,
                GuidRepresentation = GuidRepresentation.PythonLegacy,
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
                LocalThreshold = TimeSpan.FromSeconds(6),
                Server = new MongoServerAddress("host"),
                ServerSelectionTimeout = TimeSpan.FromSeconds(10),
                SocketTimeout = TimeSpan.FromSeconds(7),
                Username = "username",
                UseSsl = true,
                VerifySslCertificate = false,
                W = 2,
                WaitQueueSize = 123,
                WaitQueueTimeout = TimeSpan.FromSeconds(8),
                WTimeout = TimeSpan.FromSeconds(9)
            };

            var connectionString = "mongodb://username:password@host/database?" + string.Join(";", new[] {
                "authMechanism=GSSAPI",
                "authMechanismProperties=SERVICE_NAME:other,CANONICALIZE_HOST_NAME:true",
                "authSource=db",
                "appname=app",
                "ipv6=true",
                "ssl=true", // UseSsl
                "sslVerifyCertificate=false", // VerifySslCertificate
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
                "uuidRepresentation=pythonLegacy"
            });

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.Equal("app", url.ApplicationName);
                Assert.Equal("GSSAPI", url.AuthenticationMechanism);
                Assert.Equal(authMechanismProperties, url.AuthenticationMechanismProperties);
                Assert.Equal("db", url.AuthenticationSource);
                Assert.Equal(123, url.ComputedWaitQueueSize);
                Assert.Equal(ConnectionMode.ReplicaSet, url.ConnectionMode);
                Assert.Equal(TimeSpan.FromSeconds(1), url.ConnectTimeout);
                Assert.Equal("database", url.DatabaseName);
                Assert.Equal(true, url.FSync);
                Assert.Equal(GuidRepresentation.PythonLegacy, url.GuidRepresentation);
                Assert.Equal(TimeSpan.FromSeconds(11), url.HeartbeatInterval);
                Assert.Equal(TimeSpan.FromSeconds(12), url.HeartbeatTimeout);
                Assert.Equal(true, url.IPv6);
                Assert.Equal(true, url.Journal);
                Assert.Equal(TimeSpan.FromSeconds(2), url.MaxConnectionIdleTime);
                Assert.Equal(TimeSpan.FromSeconds(3), url.MaxConnectionLifeTime);
                Assert.Equal(4, url.MaxConnectionPoolSize);
                Assert.Equal(5, url.MinConnectionPoolSize);
                Assert.Equal("password", url.Password);
                Assert.Equal(ReadConcernLevel.Majority, url.ReadConcernLevel);
                Assert.Equal(readPreference, url.ReadPreference);
                Assert.Equal("name", url.ReplicaSetName);
                Assert.Equal(TimeSpan.FromSeconds(6), url.LocalThreshold);
                Assert.Equal(new MongoServerAddress("host", 27017), url.Server);
                Assert.Equal(TimeSpan.FromSeconds(10), url.ServerSelectionTimeout);
                Assert.Equal(TimeSpan.FromSeconds(7), url.SocketTimeout);
                Assert.Equal("username", url.Username);
                Assert.Equal(true, url.UseSsl);
                Assert.Equal(false, url.VerifySslCertificate);
                Assert.Equal(2, ((WriteConcern.WCount)url.W).Value);
                Assert.Equal(0.0, url.WaitQueueMultiple);
                Assert.Equal(123, url.WaitQueueSize);
                Assert.Equal(TimeSpan.FromSeconds(8), url.WaitQueueTimeout);
                Assert.Equal(TimeSpan.FromSeconds(9), url.WTimeout);
                Assert.Equal(connectionString, url.ToString());
            }
        }

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

        // private methods
        private IEnumerable<MongoUrl> EnumerateBuiltAndParsedUrls(
            MongoUrlBuilder built,
            string connectionString)
        {
            yield return built.ToMongoUrl();
            yield return new MongoUrl(connectionString);
        }
    }
}
