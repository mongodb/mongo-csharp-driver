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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    [Category("ConnectionString")]
    public class MongoUrlBuilderTests
    {
        private MongoServerAddress _localhost = new MongoServerAddress("localhost");

        [Test]
        public void TestAll()
        {
            var readPreference = new ReadPreference(ReadPreferenceMode.Secondary, new[] { new TagSet(new[] { new Tag("dc", "1") }) });
            var authMechanismProperties = new Dictionary<string, string>
            {
                { "SERVICE_NAME", "other" },
                { "CANONICALIZE_HOST_NAME", "true" }
            };
            var built = new MongoUrlBuilder()
            {
                AuthenticationMechanism = "GSSAPI",
                AuthenticationMechanismProperties = authMechanismProperties,
                AuthenticationSource = "db",
                ConnectionMode = ConnectionMode.ReplicaSet,
                ConnectTimeout = TimeSpan.FromSeconds(1),
                DatabaseName = "database",
                FSync = true,
                GuidRepresentation = GuidRepresentation.PythonLegacy,
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
                "ipv6=true",
                "ssl=true", // UseSsl
                "sslVerifyCertificate=false", // VerifySslCertificate
                "connect=replicaSet",
                "replicaSet=name",
                "readConcernLevel=majority",
                "readPreference=secondary;readPreferenceTags=dc:1",
                "fsync=true",
                "journal=true",
                "w=2",
                "wtimeout=9s",
                "connectTimeout=1s",
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

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual("GSSAPI", builder.AuthenticationMechanism);
                CollectionAssert.AreEqual(authMechanismProperties, builder.AuthenticationMechanismProperties);
                Assert.AreEqual("db", builder.AuthenticationSource);
                Assert.AreEqual(123, builder.ComputedWaitQueueSize);
                Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
                Assert.AreEqual(TimeSpan.FromSeconds(1), builder.ConnectTimeout);
                Assert.AreEqual("database", builder.DatabaseName);
                Assert.AreEqual(true, builder.FSync);
                Assert.AreEqual(GuidRepresentation.PythonLegacy, builder.GuidRepresentation);
                Assert.AreEqual(true, builder.IPv6);
                Assert.AreEqual(true, builder.Journal);
                Assert.AreEqual(TimeSpan.FromSeconds(2), builder.MaxConnectionIdleTime);
                Assert.AreEqual(TimeSpan.FromSeconds(3), builder.MaxConnectionLifeTime);
                Assert.AreEqual(4, builder.MaxConnectionPoolSize);
                Assert.AreEqual(5, builder.MinConnectionPoolSize);
                Assert.AreEqual("password", builder.Password);
                Assert.AreEqual(ReadConcernLevel.Majority, builder.ReadConcernLevel);
                Assert.AreEqual(readPreference, builder.ReadPreference);
                Assert.AreEqual("name", builder.ReplicaSetName);
                Assert.AreEqual(TimeSpan.FromSeconds(6), builder.LocalThreshold);
                Assert.AreEqual(new MongoServerAddress("host", 27017), builder.Server);
                Assert.AreEqual(TimeSpan.FromSeconds(10), builder.ServerSelectionTimeout);
                Assert.AreEqual(TimeSpan.FromSeconds(7), builder.SocketTimeout);
                Assert.AreEqual("username", builder.Username);
                Assert.AreEqual(true, builder.UseSsl);
                Assert.AreEqual(false, builder.VerifySslCertificate);
                Assert.AreEqual(2, ((WriteConcern.WCount)builder.W).Value);
                Assert.AreEqual(0.0, builder.WaitQueueMultiple);
                Assert.AreEqual(123, builder.WaitQueueSize);
                Assert.AreEqual(TimeSpan.FromSeconds(8), builder.WaitQueueTimeout);
                Assert.AreEqual(TimeSpan.FromSeconds(9), builder.WTimeout);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase("MONGODB-CR", "mongodb://localhost/?authMechanism=MONGODB-CR")]
        [TestCase("SCRAM-SHA-1", "mongodb://localhost/?authMechanism=SCRAM-SHA-1")]
        [TestCase("MONGODB-X509", "mongodb://localhost/?authMechanism=MONGODB-X509")]
        [TestCase("GSSAPI", "mongodb://localhost/?authMechanism=GSSAPI")]
        public void TestAuthMechanism(string mechanism, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, AuthenticationMechanism = mechanism };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(mechanism, builder.AuthenticationMechanism);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase("db", "mongodb://localhost/?authSource=db")]
        public void TestAuthSource(string authSource, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, AuthenticationSource = authSource };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(authSource, builder.AuthenticationSource);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestComputedWaitQueueSize_UsingMultiple()
        {
            var built = new MongoUrlBuilder { Server = _localhost, MaxConnectionPoolSize = 123, WaitQueueMultiple = 2.0 };
            var connectionString = "mongodb://localhost/?maxPoolSize=123;waitQueueMultiple=2";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(123, builder.MaxConnectionPoolSize);
                Assert.AreEqual(2.0, builder.WaitQueueMultiple);
                Assert.AreEqual(0, builder.WaitQueueSize);
                Assert.AreEqual(246, builder.ComputedWaitQueueSize);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestComputedWaitQueueSize_UsingSize()
        {
            var built = new MongoUrlBuilder { Server = _localhost, WaitQueueSize = 123 };
            var connectionString = "mongodb://localhost/?waitQueueSize=123";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(0.0, builder.WaitQueueMultiple);
                Assert.AreEqual(123, builder.WaitQueueSize);
                Assert.AreEqual(123, builder.ComputedWaitQueueSize);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(ConnectionMode.Automatic, "mongodb://localhost{0}", new[] { "", "/?connect=automatic", "/?connect=Automatic" })]
        [TestCase(ConnectionMode.Direct, "mongodb://localhost/?connect={0}", new[] { "direct", "Direct" })]
        [TestCase(ConnectionMode.ReplicaSet, "mongodb://localhost/?connect={0}", new[] { "replicaSet", "ReplicaSet" })]
        [TestCase(ConnectionMode.ShardRouter, "mongodb://localhost/?connect={0}", new[] { "shardRouter", "ShardRouter" })]
        public void TestConnectionMode(ConnectionMode? connectionMode, string formatString, string[] values)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (connectionMode != null) { built.ConnectionMode = connectionMode.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(connectionMode ?? ConnectionMode.Automatic, builder.ConnectionMode);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(500, "mongodb://localhost/?connectTimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "mongodb://localhost/?connectTimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "mongodb://localhost/?connectTimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "mongodb://localhost/?connectTimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "mongodb://localhost/?connectTimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestConnectTimeout(int? ms, string formatString, string[] values)
        {
            var connectTimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (connectTimeout != null) { built.ConnectTimeout = connectTimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]).Replace("/?connectTimeout=30s", "");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(connectTimeout ?? MongoDefaults.ConnectTimeout, builder.ConnectTimeout);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestConnectTimeout_Range()
        {
            var builder = new MongoUrlBuilder();
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.ConnectTimeout = TimeSpan.FromMilliseconds(-1); });
            builder.ConnectTimeout = TimeSpan.FromMilliseconds(0);
            builder.ConnectTimeout = TimeSpan.FromMilliseconds(1);
        }

        [Test]
        [TestCase(null, null, "mongodb://localhost")]
        [TestCase("username@domain.com", "password", "mongodb://username%40domain.com:password@localhost")]
        [TestCase("username", "password", "mongodb://username:password@localhost")]
        [TestCase("usern;me", "p;ssword", "mongodb://usern%3Bme:p%3Bssword@localhost")]
        [TestCase("usern;me", null, "mongodb://usern%3Bme@localhost")]
        [TestCase("usern;me", "", "mongodb://usern%3Bme:@localhost")]
        public void TestCredential(string username, string password, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, Username = username, Password = password };

            foreach (var url in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(password, url.Password);
                Assert.AreEqual(username, url.Username);
                Assert.AreEqual(connectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase("database", "mongodb://localhost/database")]
        public void TestDatabaseName(string databaseName, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, DatabaseName = databaseName };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(databaseName, builder.DatabaseName);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestDefaults()
        {
            var built = new MongoUrlBuilder();
            var connectionString = "mongodb://localhost";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(null, builder.AuthenticationMechanism);
                Assert.AreEqual(0, builder.AuthenticationMechanismProperties.Count());
                Assert.AreEqual(null, builder.AuthenticationSource);
                Assert.AreEqual(MongoDefaults.ComputedWaitQueueSize, builder.ComputedWaitQueueSize);
                Assert.AreEqual(ConnectionMode.Automatic, builder.ConnectionMode);
                Assert.AreEqual(MongoDefaults.ConnectTimeout, builder.ConnectTimeout);
                Assert.AreEqual(null, builder.DatabaseName);
                Assert.AreEqual(null, builder.FSync);
                Assert.AreEqual(MongoDefaults.GuidRepresentation, builder.GuidRepresentation);
                Assert.AreEqual(false, builder.IPv6);
                Assert.AreEqual(null, builder.Journal);
                Assert.AreEqual(MongoDefaults.MaxConnectionIdleTime, builder.MaxConnectionIdleTime);
                Assert.AreEqual(MongoDefaults.MaxConnectionLifeTime, builder.MaxConnectionLifeTime);
                Assert.AreEqual(MongoDefaults.MaxConnectionPoolSize, builder.MaxConnectionPoolSize);
                Assert.AreEqual(MongoDefaults.MinConnectionPoolSize, builder.MinConnectionPoolSize);
                Assert.AreEqual(null, builder.Password);
                Assert.AreEqual(null, builder.ReadPreference);
                Assert.AreEqual(null, builder.ReplicaSetName);
                Assert.AreEqual(MongoDefaults.LocalThreshold, builder.LocalThreshold);
                Assert.AreEqual(MongoDefaults.ServerSelectionTimeout, builder.ServerSelectionTimeout);
                Assert.AreEqual(MongoDefaults.SocketTimeout, builder.SocketTimeout);
                Assert.AreEqual(null, builder.Username);
                Assert.AreEqual(false, builder.UseSsl);
                Assert.AreEqual(true, builder.VerifySslCertificate);
                Assert.AreEqual(null, builder.W);
                Assert.AreEqual(MongoDefaults.WaitQueueMultiple, builder.WaitQueueMultiple);
                Assert.AreEqual(MongoDefaults.WaitQueueSize, builder.WaitQueueSize);
                Assert.AreEqual(MongoDefaults.WaitQueueTimeout, builder.WaitQueueTimeout);
                Assert.AreEqual(null, builder.WTimeout);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(false, "mongodb://localhost/?fsync={0}", new[] { "false", "False" })]
        [TestCase(true, "mongodb://localhost/?fsync={0}", new[] { "true", "True" })]
        public void TestFSync(bool? fsync, string formatString, string[] values)
        {
            var built = new MongoUrlBuilder { Server = _localhost, FSync = fsync };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(fsync, builder.FSync);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(false, false, "mongodb://localhost")]
        [TestCase(false, false, "mongodb://localhost/?safe=false")]
        [TestCase(false, true, "mongodb://localhost/?safe=true")]
        [TestCase(false, false, "mongodb://localhost/?w=0")]
        [TestCase(false, true, "mongodb://localhost/?w=1")]
        [TestCase(false, true, "mongodb://localhost/?w=2")]
        [TestCase(false, true, "mongodb://localhost/?w=mode")]
        [TestCase(true, true, "mongodb://localhost")]
        [TestCase(true, false, "mongodb://localhost/?safe=false")]
        [TestCase(true, true, "mongodb://localhost/?safe=true")]
        [TestCase(true, false, "mongodb://localhost/?w=0")]
        [TestCase(true, true, "mongodb://localhost/?w=1")]
        [TestCase(true, true, "mongodb://localhost/?w=2")]
        [TestCase(true, true, "mongodb://localhost/?w=mode")]
        public void TestGetWriteConcern_IsAcknowledged(bool acknowledgedDefault, bool acknowledged, string connectionString)
        {
            var builder = new MongoUrlBuilder(connectionString);
            var writeConcern = builder.GetWriteConcern(acknowledgedDefault);
            Assert.AreEqual(acknowledged, writeConcern.IsAcknowledged);
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase(false, "mongodb://localhost/?fsync=false")]
        [TestCase(true, "mongodb://localhost/?fsync=true")]
        public void TestGetWriteConcern_FSync(bool? fsync, string connectionString)
        {
            var builder = new MongoUrlBuilder(connectionString);
            var writeConcern = builder.GetWriteConcern(true);
            Assert.AreEqual(fsync, writeConcern.FSync);
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" }, new[] { "" })]
        [TestCase(false, "mongodb://localhost/?{1}={0}", new[] { "false", "False" }, new[] { "journal", "j" })]
        [TestCase(true, "mongodb://localhost/?{1}={0}", new[] { "true", "True" }, new[] { "journal", "j" })]
        public void TestGetWriteConcern_Journal(bool? journal, string formatString, string[] values, string[] journalAliases)
        {
            var canonicalConnectionString = string.Format(formatString, values[0], "journal");
            foreach (var builder in EnumerateParsedBuilders(formatString, values, journalAliases))
            {
                var writeConcern = builder.GetWriteConcern(true);
                Assert.AreEqual(journal, writeConcern.Journal);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(false, false, 0, "mongodb://localhost")]
        [TestCase(false, false, 0, "mongodb://localhost/?w=0")]
        [TestCase(false, true, 1, "mongodb://localhost/?w=1")]
        [TestCase(false, true, 2, "mongodb://localhost/?w=2")]
        [TestCase(false, true, "mode", "mongodb://localhost/?w=mode")]
        [TestCase(true, true, null, "mongodb://localhost")]
        [TestCase(true, false, 0, "mongodb://localhost/?w=0")]
        [TestCase(true, true, 1, "mongodb://localhost/?w=1")]
        [TestCase(true, true, 2, "mongodb://localhost/?w=2")]
        [TestCase(true, true, "mode", "mongodb://localhost/?w=mode")]
        public void TestGetWriteConcern_W(bool acknowledgedDefault, bool acknowledged, object wobj, string connectionString)
        {
            var w = (wobj == null) ? null : (wobj is int) ? (WriteConcern.WValue)new WriteConcern.WCount((int)wobj) : new WriteConcern.WMode((string)wobj);
            var builder = new MongoUrlBuilder(connectionString);
            var writeConcern = builder.GetWriteConcern(acknowledgedDefault);
            Assert.AreEqual(acknowledged, writeConcern.IsAcknowledged);
            Assert.AreEqual(w, writeConcern.W);
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase(500, "mongodb://localhost/?wtimeout=500ms")]
        public void TestGetWriteConcern_WTimeout(int? ms, string connectionString)
        {
            var wtimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var builder = new MongoUrlBuilder(connectionString);
            var writeConcern = builder.GetWriteConcern(true);
            Assert.AreEqual(wtimeout, writeConcern.WTimeout);
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" }, new[] { "" })]
        [TestCase(GuidRepresentation.CSharpLegacy, "mongodb://localhost/?{1}={0}", new[] { "csharpLegacy", "CSharpLegacy" }, new[] { "uuidRepresentation", "guids" })]
        [TestCase(GuidRepresentation.JavaLegacy, "mongodb://localhost/?{1}={0}", new[] { "javaLegacy", "JavaLegacy" }, new[] { "uuidRepresentation", "guids" })]
        [TestCase(GuidRepresentation.PythonLegacy, "mongodb://localhost/?{1}={0}", new[] { "pythonLegacy", "PythonLegacy" }, new[] { "uuidRepresentation", "guids" })]
        [TestCase(GuidRepresentation.Standard, "mongodb://localhost/?{1}={0}", new[] { "standard", "Standard" }, new[] { "uuidRepresentation", "guids" })]
        [TestCase(GuidRepresentation.Unspecified, "mongodb://localhost/?{1}={0}", new[] { "unspecified", "Unspecified" }, new[] { "uuidRepresentation", "guids" })]
        public void TestGuidRepresentation(GuidRepresentation? guidRepresentation, string formatString, string[] values, string[] uuidAliases)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (guidRepresentation != null) { built.GuidRepresentation = guidRepresentation.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0], "uuidRepresentation").Replace("/?uuidRepresentation=csharpLegacy", "");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values, uuidAliases))
            {
                Assert.AreEqual(guidRepresentation ?? MongoDefaults.GuidRepresentation, builder.GuidRepresentation);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(false, "mongodb://localhost/?ipv6={0}", new[] { "false", "False" })]
        [TestCase(true, "mongodb://localhost/?ipv6={0}", new[] { "true", "True" })]
        public void TestIPv6(bool? ipv6, string formatString, string[] values)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (ipv6 != null) { built.IPv6 = ipv6.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]).Replace("/?ipv6=false", "");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(ipv6 ?? false, builder.IPv6);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" }, new[] { "" })]
        [TestCase(false, "mongodb://localhost/?{1}={0}", new[] { "false", "False" }, new[] { "journal", "j" })]
        [TestCase(true, "mongodb://localhost/?{1}={0}", new[] { "true", "True" }, new[] { "journal", "j" })]
        public void TestJournal(bool? journal, string formatString, string[] values, string[] journalAliases)
        {
            var built = new MongoUrlBuilder { Server = _localhost, Journal = journal };

            var canonicalConnectionString = string.Format(formatString, values[0], "journal");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values, journalAliases))
            {
                Assert.AreEqual(journal, builder.Journal);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(500, "mongodb://localhost/?maxIdleTime{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "mongodb://localhost/?maxIdleTime{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "mongodb://localhost/?maxIdleTime{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "mongodb://localhost/?maxIdleTime{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "mongodb://localhost/?maxIdleTime{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestMaxConnectionIdleTime(int? ms, string formatString, string[] values)
        {
            var maxConnectionIdleTime = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (maxConnectionIdleTime != null) { built.MaxConnectionIdleTime = maxConnectionIdleTime.Value; };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(maxConnectionIdleTime ?? MongoDefaults.MaxConnectionIdleTime, builder.MaxConnectionIdleTime);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestMaxConnectionIdleTime_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MaxConnectionIdleTime = TimeSpan.FromSeconds(-1); });
            builder.MaxConnectionIdleTime = TimeSpan.Zero;
            builder.MaxConnectionIdleTime = TimeSpan.FromSeconds(1);
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(500, "mongodb://localhost/?maxLifeTime{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "mongodb://localhost/?maxLifeTime{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "mongodb://localhost/?maxLifeTime{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "mongodb://localhost/?maxLifeTime{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "mongodb://localhost/?maxLifeTime{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestMaxConnectionLifeTime(int? ms, string formatString, string[] values)
        {
            var maxConnectionLifeTime = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (maxConnectionLifeTime != null) { built.MaxConnectionLifeTime = maxConnectionLifeTime.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]).Replace("/?maxLifeTime=30m", "");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(maxConnectionLifeTime ?? MongoDefaults.MaxConnectionLifeTime, builder.MaxConnectionLifeTime);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestMaxConnectionLifeTime_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MaxConnectionLifeTime = TimeSpan.FromSeconds(-1); });
            builder.MaxConnectionIdleTime = TimeSpan.Zero;
            builder.MaxConnectionIdleTime = TimeSpan.FromSeconds(1);
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase(123, "mongodb://localhost/?maxPoolSize=123")]
        public void TestMaxConnectionPoolSize(int? maxConnectionPoolSize, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (maxConnectionPoolSize != null) { built.MaxConnectionPoolSize = maxConnectionPoolSize.Value; }

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(maxConnectionPoolSize ?? MongoDefaults.MaxConnectionPoolSize, builder.MaxConnectionPoolSize);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestMaxConnectionPoolSize_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MaxConnectionPoolSize = -1; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MaxConnectionPoolSize = 0; });
            builder.MaxConnectionPoolSize = 1;
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase(123, "mongodb://localhost/?minPoolSize=123")]
        public void TestMinConnectionPoolSize(int? minConnectionPoolSize, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (minConnectionPoolSize != null) { built.MinConnectionPoolSize = minConnectionPoolSize.Value; }

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(minConnectionPoolSize ?? MongoDefaults.MinConnectionPoolSize, builder.MinConnectionPoolSize);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestMinConnectionPoolSize_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MinConnectionPoolSize = -1; });
            builder.MinConnectionPoolSize = 0;
            builder.MinConnectionPoolSize = 1;
        }

        [Test]
        [TestCase("mongodb://localhost/?readConcernLevel=local", ReadConcernLevel.Local)]
        [TestCase("mongodb://localhost/?readConcernLevel=majority", ReadConcernLevel.Majority)]
        public void TestReadConcernLevel(string connectionString, ReadConcernLevel readConcernLevel)
        {
            var built = new MongoUrlBuilder { ReadConcernLevel = readConcernLevel };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(readConcernLevel, builder.ReadConcernLevel);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(ReadPreferenceMode.Primary, "mongodb://localhost/?readPreference={0}", new[] { "primary", "Primary" })]
        [TestCase(ReadPreferenceMode.PrimaryPreferred, "mongodb://localhost/?readPreference={0}", new[] { "primaryPreferred", "PrimaryPreferred" })]
        [TestCase(ReadPreferenceMode.Secondary, "mongodb://localhost/?readPreference={0}", new[] { "secondary", "Secondary" })]
        [TestCase(ReadPreferenceMode.SecondaryPreferred, "mongodb://localhost/?readPreference={0}", new[] { "secondaryPreferred", "SecondaryPreferred" })]
        [TestCase(ReadPreferenceMode.Nearest, "mongodb://localhost/?readPreference={0}", new[] { "nearest", "Nearest" })]
        public void TestReadPreference(ReadPreferenceMode? mode, string formatString, string[] values)
        {
            ReadPreference readPreference = null;
            if (mode != null) { readPreference = new ReadPreference(mode.Value); }
            var built = new MongoUrlBuilder { Server = _localhost, ReadPreference = readPreference };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(readPreference, builder.ReadPreference);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestReadPreference_NoReadPreferenceModeWithOneTagSet()
        {
            var connectionString = "mongodb://localhost/?readPreferenceTags=dc:ny,rack:1";

            Assert.Throws<MongoConfigurationException>(() => new MongoUrlBuilder(connectionString));
        }

        [Test]
        public void TestReadPreference_SecondaryWithOneTagSet()
        {
            var tagSets = new TagSet[]
            {
                new TagSet(new [] { new Tag("dc", "ny"), new Tag("rack", "1") })
            };
            var readPreference = new ReadPreference(ReadPreferenceMode.Secondary, tagSets);
            var built = new MongoUrlBuilder { Server = _localhost, ReadPreference = readPreference };
            var connectionString = "mongodb://localhost/?readPreference=secondary;readPreferenceTags=dc:ny,rack:1";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(ReadPreferenceMode.Secondary, builder.ReadPreference.ReadPreferenceMode);
                var builderTagSets = builder.ReadPreference.TagSets.ToArray();
                Assert.AreEqual(1, builderTagSets.Length);
                var builderTagSet1Tags = builderTagSets[0].Tags.ToArray();
                Assert.AreEqual(2, builderTagSet1Tags.Length);
                Assert.AreEqual(new Tag("dc", "ny"), builderTagSet1Tags[0]);
                Assert.AreEqual(new Tag("rack", "1"), builderTagSet1Tags[1]);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestReadPreference_SecondaryWithTwoTagSets()
        {
            var tagSets = new TagSet[]
            {
                new TagSet(new [] { new Tag("dc", "ny"), new Tag("rack", "1") }),
                new TagSet(new [] { new Tag("dc", "sf") })
            };
            var readPreference = new ReadPreference(ReadPreferenceMode.Secondary, tagSets);
            var built = new MongoUrlBuilder { Server = _localhost, ReadPreference = readPreference };
            var connectionString = "mongodb://localhost/?readPreference=secondary;readPreferenceTags=dc:ny,rack:1;readPreferenceTags=dc:sf";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(ReadPreferenceMode.Secondary, builder.ReadPreference.ReadPreferenceMode);
                var builderTagSets = builder.ReadPreference.TagSets.ToArray();
                Assert.AreEqual(2, builderTagSets.Length);
                var builderTagSet1Tags = builderTagSets[0].Tags.ToArray();
                var builderTagSet2Tags = builderTagSets[1].Tags.ToArray();
                Assert.AreEqual(2, builderTagSet1Tags.Length);
                Assert.AreEqual(new Tag("dc", "ny"), builderTagSet1Tags[0]);
                Assert.AreEqual(new Tag("rack", "1"), builderTagSet1Tags[1]);
                Assert.AreEqual(1, builderTagSet2Tags.Length);
                Assert.AreEqual(new Tag("dc", "sf"), builderTagSet2Tags[0]);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase("name", "mongodb://localhost/?replicaSet=name")]
        public void TestReplicaSetName(string name, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, ReplicaSetName = name };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(name, builder.ReplicaSetName);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(0, "mongodb://localhost/?{0}", new[] { "w=0", "w=0;safe=false", "w=0;safe=False", "safe=false", "safe=False" })]
        [TestCase(0, "mongodb://localhost/?{0}", new[] { "w=0", "w=1;safe=false", "w=1;safe=False" })]
        [TestCase(0, "mongodb://localhost/?{0}", new[] { "w=0", "w=2;safe=false", "w=2;safe=False" })]
        [TestCase(0, "mongodb://localhost/?{0}", new[] { "w=0", "w=mode;safe=false", "w=mode;safe=False" })]
        [TestCase(1, "mongodb://localhost/?{0}", new[] { "w=1", "w=0;safe=true", "w=0;safe=True", "safe=true", "safe=True" })]
        [TestCase(1, "mongodb://localhost/?{0}", new[] { "w=1", "w=1;safe=true", "w=1;safe=True" })]
        [TestCase(2, "mongodb://localhost/?{0}", new[] { "w=2", "w=2;safe=true", "w=2;safe=True" })]
        [TestCase("mode", "mongodb://localhost/?{0}", new[] { "w=mode", "w=mode;safe=true", "w=mode;safe=True" })]
        public void TestSafe(object wobj, string formatString, string[] values)
        {
            var w = (wobj == null) ? null : (wobj is int) ? (WriteConcern.WValue)(int)wobj : (string)wobj;
            var built = new MongoUrlBuilder { Server = _localhost, W = w };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(w, builder.W);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(500, "mongodb://localhost/?localThreshold{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "mongodb://localhost/?localThreshold{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "mongodb://localhost/?localThreshold{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "mongodb://localhost/?localThreshold{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "mongodb://localhost/?localThreshold{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestLocalThreshold(int? ms, string formatString, string[] values)
        {
            var localThreshold = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (localThreshold != null) { built.LocalThreshold = localThreshold.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(localThreshold ?? MongoDefaults.LocalThreshold, builder.LocalThreshold);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestLocalThreshold_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.LocalThreshold = TimeSpan.FromSeconds(-1); });
            builder.LocalThreshold = TimeSpan.Zero;
            builder.LocalThreshold = TimeSpan.FromSeconds(1);
        }

        [Test]
        [TestCase("host", null, "mongodb://host")]
        [TestCase("host", 27017, "mongodb://host")]
        [TestCase("host", 27018, "mongodb://host:27018")]
        public void TestServer(string host, int? port, string connectionString)
        {
            var server = (host == null) ? null : (port == null) ? new MongoServerAddress(host) : new MongoServerAddress(host, port.Value);
            var built = new MongoUrlBuilder { Server = server };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(server, builder.Server);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(new string[] { "host" }, new object[] { null }, "mongodb://host")]
        [TestCase(new string[] { "host" }, new object[] { 27017 }, "mongodb://host")]
        [TestCase(new string[] { "host" }, new object[] { 27018 }, "mongodb://host:27018")]
        [TestCase(new string[] { "host1", "host2" }, new object[] { null, null }, "mongodb://host1,host2")]
        [TestCase(new string[] { "host1", "host2" }, new object[] { null, 27017 }, "mongodb://host1,host2")]
        [TestCase(new string[] { "host1", "host2" }, new object[] { null, 27018 }, "mongodb://host1,host2:27018")]
        [TestCase(new string[] { "host1", "host2" }, new object[] { 27017, null }, "mongodb://host1,host2")]
        [TestCase(new string[] { "host1", "host2" }, new object[] { 27017, 27017 }, "mongodb://host1,host2")]
        [TestCase(new string[] { "host1", "host2" }, new object[] { 27017, 27018 }, "mongodb://host1,host2:27018")]
        [TestCase(new string[] { "host1", "host2" }, new object[] { 27018, null }, "mongodb://host1:27018,host2")]
        [TestCase(new string[] { "host1", "host2" }, new object[] { 27018, 27017 }, "mongodb://host1:27018,host2")]
        [TestCase(new string[] { "host1", "host2" }, new object[] { 27018, 27018 }, "mongodb://host1:27018,host2:27018")]
        [TestCase(new string[] { "[::1]", "host2" }, new object[] { 27018, 27018 }, "mongodb://[::1]:27018,host2:27018")]
        public void TestServers(string[] hosts, object[] ports, string connectionString)
        {
            var servers = (hosts == null) ? null : new List<MongoServerAddress>();
            if (hosts != null)
            {
                Assert.AreEqual(hosts.Length, ports.Length);
                for (var i = 0; i < hosts.Length; i++)
                {
                    var server = (hosts[i] == null) ? null : (ports[i] == null) ? new MongoServerAddress(hosts[i]) : new MongoServerAddress(hosts[i], (int)ports[i]);
                    servers.Add(server);
                }
            }
            var built = new MongoUrlBuilder { Servers = servers };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(servers, builder.Servers);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase("mongodb://localhost/?slaveOk=true", ReadPreferenceMode.SecondaryPreferred)]
        [TestCase("mongodb://localhost/?slaveOk=false", ReadPreferenceMode.Primary)]
        public void TestSlaveOk(string url, ReadPreferenceMode mode)
        {
            var builder = new MongoUrlBuilder(url);
            Assert.AreEqual(mode, builder.ReadPreference.ReadPreferenceMode);
        }

        [Test]
        public void TestSlaveOk_AfterReadPreference()
        {
            Assert.Throws<MongoConfigurationException>(() => new MongoUrlBuilder("mongodb://localhost/?readPreference=primary&slaveOk=true"));
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(500, "mongodb://localhost/?serverSelectionTimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(20000, "mongodb://localhost/?serverSelectionTimeout{0}", new[] { "=20s", "=20000ms", "=20", "=00:00:20", "MS=20000" })]
        [TestCase(1800000, "mongodb://localhost/?serverSelectionTimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "mongodb://localhost/?serverSelectionTimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "mongodb://localhost/?serverSelectionTimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestServerSelectionTimeout(int? ms, string formatString, string[] values)
        {
            var serverSelectionTimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (serverSelectionTimeout != null) { built.ServerSelectionTimeout = serverSelectionTimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(serverSelectionTimeout ?? MongoDefaults.ServerSelectionTimeout, builder.ServerSelectionTimeout);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestServerSelectionTimeout_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.ServerSelectionTimeout = TimeSpan.FromSeconds(-1); });
            builder.ServerSelectionTimeout = TimeSpan.Zero;
            builder.ServerSelectionTimeout = TimeSpan.FromSeconds(1);
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(500, "mongodb://localhost/?socketTimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "mongodb://localhost/?socketTimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "mongodb://localhost/?socketTimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "mongodb://localhost/?socketTimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "mongodb://localhost/?socketTimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestSocketTimeout(int? ms, string formatString, string[] values)
        {
            var socketTimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (socketTimeout != null) { built.SocketTimeout = socketTimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(socketTimeout ?? MongoDefaults.SocketTimeout, builder.SocketTimeout);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestSocketTimeout_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.SocketTimeout = TimeSpan.FromSeconds(-1); });
            builder.SocketTimeout = TimeSpan.Zero;
            builder.SocketTimeout = TimeSpan.FromSeconds(1);
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(false, "mongodb://localhost/?ssl={0}", new[] { "false", "False" })]
        [TestCase(true, "mongodb://localhost/?ssl={0}", new[] { "true", "True" })]
        public void TestUseSsl(bool? useSsl, string formatString, string[] values)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (useSsl != null) { built.UseSsl = useSsl.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]).Replace("/?ssl=false", "");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(useSsl ?? false, builder.UseSsl);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(false, "mongodb://localhost/?sslVerifyCertificate={0}", new[] { "false", "False" })]
        [TestCase(true, "mongodb://localhost/?sslVerifyCertificate={0}", new[] { "true", "True" })]
        public void TestVerifySslCertificate(bool? verifySslCertificate, string formatString, string[] values)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (verifySslCertificate != null) { built.VerifySslCertificate = verifySslCertificate.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]).Replace("/?sslVerifyCertificate=true", "");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(verifySslCertificate ?? true, builder.VerifySslCertificate);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(false, false, 0, "mongodb://localhost/?w=0")]
        [TestCase(false, false, 0, "mongodb://localhost/?w=0")]
        [TestCase(false, true, 1, "mongodb://localhost/?w=1")]
        [TestCase(false, true, 2, "mongodb://localhost/?w=2")]
        [TestCase(false, true, "mode", "mongodb://localhost/?w=mode")]
        [TestCase(true, true, null, "mongodb://localhost")]
        [TestCase(true, false, 0, "mongodb://localhost/?w=0")]
        [TestCase(true, true, 1, "mongodb://localhost/?w=1")]
        [TestCase(true, true, 2, "mongodb://localhost/?w=2")]
        [TestCase(true, true, "mode", "mongodb://localhost/?w=mode")]
        public void TestW(bool acknowledgedDefault, bool acknowledged, object wobj, string connectionString)
        {
            var w = (wobj == null) ? null : (wobj is int) ? (WriteConcern.WValue)new WriteConcern.WCount((int)wobj) : new WriteConcern.WMode((string)wobj);
            var built = new MongoUrlBuilder { Server = _localhost, W = w };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                var writeConcern = builder.GetWriteConcern(acknowledgedDefault);
                Assert.AreEqual(acknowledged, writeConcern.IsAcknowledged);
                Assert.AreEqual(w, writeConcern.W);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
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

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase(2.0, "mongodb://localhost/?waitQueueMultiple=2")]
        public void TestWaitQueueMultiple(double? multiple, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (multiple != null) { built.WaitQueueMultiple = multiple.Value; }

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(multiple ?? MongoDefaults.WaitQueueMultiple, builder.WaitQueueMultiple);
                Assert.AreEqual((multiple == null) ? MongoDefaults.WaitQueueSize : 0, builder.WaitQueueSize);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestWaitQueueMultiple_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueMultiple = -1.0; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueMultiple = 0.0; });
            builder.WaitQueueMultiple = 1.0;
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase(123, "mongodb://localhost/?waitQueueSize=123")]
        public void TestWaitQueueSize(int? size, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (size != null) { built.WaitQueueSize = size.Value; }

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual((size == null) ? MongoDefaults.WaitQueueMultiple : 0.0, builder.WaitQueueMultiple);
                Assert.AreEqual(size ?? MongoDefaults.WaitQueueSize, builder.WaitQueueSize);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestWaitQueueSize_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueSize = -1; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueSize = 0; });
            builder.WaitQueueSize = 1;
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(500, "mongodb://localhost/?waitQueueTimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "mongodb://localhost/?waitQueueTimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "mongodb://localhost/?waitQueueTimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "mongodb://localhost/?waitQueueTimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "mongodb://localhost/?waitQueueTimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestWaitQueueTimeout(int? ms, string formatString, string[] values)
        {
            var waitQueueTimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (waitQueueTimeout != null) { built.WaitQueueTimeout = waitQueueTimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(waitQueueTimeout ?? MongoDefaults.WaitQueueTimeout, builder.WaitQueueTimeout);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestWaitQueueTimeout_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueTimeout = TimeSpan.FromSeconds(-1); });
            builder.WaitQueueTimeout = TimeSpan.Zero;
            builder.WaitQueueTimeout = TimeSpan.FromSeconds(1);
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(500, "mongodb://localhost/?wtimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "mongodb://localhost/?wtimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "mongodb://localhost/?wtimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "mongodb://localhost/?wtimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "mongodb://localhost/?wtimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestWTimeout(int? ms, string formatString, string[] values)
        {
            var wtimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (wtimeout != null) { built.WTimeout = wtimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(wtimeout, builder.WTimeout);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestWTimeout_Range()
        {
            var builder = new MongoUrlBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WTimeout = TimeSpan.FromSeconds(-1); });
            builder.WTimeout = TimeSpan.Zero;
            builder.WTimeout = TimeSpan.FromSeconds(1);
        }

        // private methods
        private IEnumerable<MongoUrlBuilder> EnumerateBuiltAndParsedBuilders(
            MongoUrlBuilder built,
            string connectionString)
        {
            yield return built;
            yield return new MongoUrlBuilder(connectionString);
        }

        private IEnumerable<MongoUrlBuilder> EnumerateBuiltAndParsedBuilders(
            MongoUrlBuilder built,
            string formatString,
            string[] values)
        {
            yield return built;
            foreach (var parsed in EnumerateParsedBuilders(formatString, values))
            {
                yield return parsed;
            }
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

        private IEnumerable<MongoUrlBuilder> EnumerateParsedBuilders(
            string formatString,
            string[] values)
        {
            foreach (var v in values)
            {
                var connectionString = string.Format(formatString, v);
                yield return new MongoUrlBuilder(connectionString);
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
