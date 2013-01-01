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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoUrlTests
    {
        private MongoServerAddress _localhost = new MongoServerAddress("localhost");

        [Test]
        public void TestAll()
        {
            var readPreference = new ReadPreference
            {
                ReadPreferenceMode = ReadPreferenceMode.Secondary,
                TagSets = new[] { new ReplicaSetTagSet { { "dc", "1" } } }
            };
            var built = new MongoUrlBuilder()
            {
                ConnectionMode = ConnectionMode.ReplicaSet,
                ConnectTimeout = TimeSpan.FromSeconds(1),
                DatabaseName = "database",
                DefaultCredentials = new MongoCredentials("username", "password"),
                FSync = true,
                GuidRepresentation = GuidRepresentation.PythonLegacy,
                IPv6 = true,
                Journal = true,
                MaxConnectionIdleTime = TimeSpan.FromSeconds(2),
                MaxConnectionLifeTime = TimeSpan.FromSeconds(3),
                MaxConnectionPoolSize = 4,
                MinConnectionPoolSize = 5,
                ReadPreference = readPreference,
                ReplicaSetName = "name",
                SecondaryAcceptableLatency = TimeSpan.FromSeconds(6),
                Server = new MongoServerAddress("host"),
                SocketTimeout = TimeSpan.FromSeconds(7),
                UseSsl = true,
                VerifySslCertificate = false,
                W = 2,
                WaitQueueSize = 123,
                WaitQueueTimeout = TimeSpan.FromSeconds(8),
                WTimeout = TimeSpan.FromSeconds(9)
            };

            var connectionString = "mongodb://username:password@host/database?" + string.Join(";", new[] {
                "ipv6=true",
                "ssl=true", // UseSsl
                "sslVerifyCertificate=false", // VerifySslCertificate
                "connect=replicaSet",
                "replicaSet=name",
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
                "secondaryAcceptableLatency=6s",
                "socketTimeout=7s",
                "waitQueueSize=123",
                "waitQueueTimeout=8s",
                "uuidRepresentation=pythonLegacy"
            });

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(123, url.ComputedWaitQueueSize);
                Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
                Assert.AreEqual(TimeSpan.FromSeconds(1), url.ConnectTimeout);
                Assert.AreEqual("database", url.DatabaseName);
                Assert.AreEqual(new MongoCredentials("username", "password"), url.DefaultCredentials);
                Assert.AreEqual(true, url.FSync);
                Assert.AreEqual(GuidRepresentation.PythonLegacy, url.GuidRepresentation);
                Assert.AreEqual(true, url.IPv6);
                Assert.AreEqual(true, url.Journal);
                Assert.AreEqual(TimeSpan.FromSeconds(2), url.MaxConnectionIdleTime);
                Assert.AreEqual(TimeSpan.FromSeconds(3), url.MaxConnectionLifeTime);
                Assert.AreEqual(4, url.MaxConnectionPoolSize);
                Assert.AreEqual(5, url.MinConnectionPoolSize);
                Assert.AreEqual(readPreference, url.ReadPreference);
                Assert.AreEqual("name", url.ReplicaSetName);
#pragma warning disable 618
                Assert.AreEqual(new SafeMode(true) { FSync = true, Journal = true, W = 2, WTimeout = TimeSpan.FromSeconds(9) }, url.SafeMode);
#pragma warning restore
                Assert.AreEqual(TimeSpan.FromSeconds(6), url.SecondaryAcceptableLatency);
                Assert.AreEqual(new MongoServerAddress("host", 27017), url.Server);
#pragma warning disable 618
                Assert.AreEqual(true, url.SlaveOk);
#pragma warning restore
                Assert.AreEqual(TimeSpan.FromSeconds(7), url.SocketTimeout);
                Assert.AreEqual(true, url.UseSsl);
                Assert.AreEqual(false, url.VerifySslCertificate);
                Assert.AreEqual(2, ((WriteConcern.WCount)url.W).Value);
                Assert.AreEqual(0.0, url.WaitQueueMultiple);
                Assert.AreEqual(123, url.WaitQueueSize);
                Assert.AreEqual(TimeSpan.FromSeconds(8), url.WaitQueueTimeout);
                Assert.AreEqual(TimeSpan.FromSeconds(9), url.WTimeout);
                Assert.AreEqual(connectionString, url.ToString());
            }
        }

        [Test]
        public void TestComputedWaitQueueSize_UsingMultiple()
        {
            var built = new MongoUrlBuilder { Server = _localhost, MaxConnectionPoolSize = 123, WaitQueueMultiple = 2.0 };
            var connectionString = "mongodb://localhost/?maxPoolSize=123;waitQueueMultiple=2";

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(123, url.MaxConnectionPoolSize);
                Assert.AreEqual(2.0, url.WaitQueueMultiple);
                Assert.AreEqual(0, url.WaitQueueSize);
                Assert.AreEqual(246, url.ComputedWaitQueueSize);
                Assert.AreEqual(connectionString, url.ToString());
            }
        }

        [Test]
        public void TestComputedWaitQueueSize_UsingSize()
        {
            var built = new MongoUrlBuilder { Server = _localhost, WaitQueueSize = 123 };
            var connectionString = "mongodb://localhost/?waitQueueSize=123";

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(0.0, url.WaitQueueMultiple);
                Assert.AreEqual(123, url.WaitQueueSize);
                Assert.AreEqual(123, url.ComputedWaitQueueSize);
                Assert.AreEqual(connectionString, url.ToString());
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(connectionMode ?? ConnectionMode.Automatic, url.ConnectionMode);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(connectTimeout ?? MongoDefaults.ConnectTimeout, url.ConnectTimeout);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase("database", "mongodb://localhost/database")]
        public void TestDatabaseName(string databaseName, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, DatabaseName = databaseName };

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(databaseName, url.DatabaseName);
                Assert.AreEqual(connectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(null, null, "mongodb://localhost")]
        [TestCase("username", "password", "mongodb://username:password@localhost")]
        [TestCase("usern;me", "p;ssword", "mongodb://usern%3Bme:p%3Bssword@localhost")]
        public void TestDefaultCredentials(string username, string password, string connectionString)
        {
            var defaultCredentials = (username == null) ? null : new MongoCredentials(username, password);
            var built = new MongoUrlBuilder { Server = _localhost, DefaultCredentials = defaultCredentials };

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(defaultCredentials, url.DefaultCredentials);
                Assert.AreEqual(connectionString, url.ToString());
            }
        }

        [Test]
        public void TestDefaults()
        {
            var built = new MongoUrlBuilder();
            var connectionString = "mongodb://";

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(MongoDefaults.ComputedWaitQueueSize, url.ComputedWaitQueueSize);
                Assert.AreEqual(ConnectionMode.Automatic, url.ConnectionMode);
                Assert.AreEqual(MongoDefaults.ConnectTimeout, url.ConnectTimeout);
                Assert.AreEqual(null, url.DatabaseName);
                Assert.AreEqual(null, url.DefaultCredentials);
                Assert.AreEqual(null, url.FSync);
                Assert.AreEqual(MongoDefaults.GuidRepresentation, url.GuidRepresentation);
                Assert.AreEqual(false, url.IPv6);
                Assert.AreEqual(null, url.Journal);
                Assert.AreEqual(MongoDefaults.MaxConnectionIdleTime, url.MaxConnectionIdleTime);
                Assert.AreEqual(MongoDefaults.MaxConnectionLifeTime, url.MaxConnectionLifeTime);
                Assert.AreEqual(MongoDefaults.MaxConnectionPoolSize, url.MaxConnectionPoolSize);
                Assert.AreEqual(MongoDefaults.MinConnectionPoolSize, url.MinConnectionPoolSize);
                Assert.AreEqual(null, url.ReadPreference);
                Assert.AreEqual(null, url.ReplicaSetName);
#pragma warning disable 618
                Assert.AreEqual(null, url.SafeMode);
#pragma warning restore
                Assert.AreEqual(MongoDefaults.SecondaryAcceptableLatency, url.SecondaryAcceptableLatency);
                Assert.AreEqual(null, url.Server);
                Assert.AreEqual(null, url.Servers);
#pragma warning disable 618
                Assert.AreEqual(false, url.SlaveOk);
#pragma warning restore
                Assert.AreEqual(MongoDefaults.SocketTimeout, url.SocketTimeout);
                Assert.AreEqual(false, url.UseSsl);
                Assert.AreEqual(true, url.VerifySslCertificate);
                Assert.AreEqual(null, url.W);
                Assert.AreEqual(MongoDefaults.WaitQueueMultiple, url.WaitQueueMultiple);
                Assert.AreEqual(MongoDefaults.WaitQueueSize, url.WaitQueueSize);
                Assert.AreEqual(MongoDefaults.WaitQueueTimeout, url.WaitQueueTimeout);
                Assert.AreEqual(null, url.WTimeout);
                Assert.AreEqual(connectionString, url.ToString());
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(fsync, url.FSync);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
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
        public void TestGetWriteConcern_Enabled(bool enabledDefault, bool enabled, string connectionString)
        {
            var url = new MongoUrl(connectionString);
            var writeConcern = url.GetWriteConcern(enabledDefault || enabled);
            Assert.AreEqual(enabled, writeConcern.Enabled);
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase(false, "mongodb://localhost/?fsync=false")]
        [TestCase(true, "mongodb://localhost/?fsync=true")]
        public void TestGetWriteConcern_FSync(bool? fsync, string connectionString)
        {
            var url = new MongoUrl(connectionString);
            var writeConcern = url.GetWriteConcern(true);
            Assert.AreEqual(fsync, writeConcern.FSync);
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" }, new[] { "" })]
        [TestCase(false, "mongodb://localhost/?{1}={0}", new[] { "false", "False" }, new[] { "journal", "j" })]
        [TestCase(true, "mongodb://localhost/?{1}={0}", new[] { "true", "True" }, new[] { "journal", "j" })]
        public void TestGetWriteConcern_Journal(bool? journal, string formatString, string[] values, string[] journalAliases)
        {
            var canonicalConnectionString = string.Format(formatString, values[0], "journal");
            foreach (var url in EnumerateParsedUrls(formatString, values, journalAliases))
            {
                var writeConcern = url.GetWriteConcern(true);
                Assert.AreEqual(journal, writeConcern.Journal);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(false, false, null, "mongodb://localhost")]
        [TestCase(false, false, 0, "mongodb://localhost/?w=0")]
        [TestCase(false, true, 1, "mongodb://localhost/?w=1")]
        [TestCase(false, true, 2, "mongodb://localhost/?w=2")]
        [TestCase(false, true, "mode", "mongodb://localhost/?w=mode")]
        [TestCase(true, true, null, "mongodb://localhost")]
        [TestCase(true, false, 0, "mongodb://localhost/?w=0")]
        [TestCase(true, true, 1, "mongodb://localhost/?w=1")]
        [TestCase(true, true, 2, "mongodb://localhost/?w=2")]
        [TestCase(true, true, "mode", "mongodb://localhost/?w=mode")]
        public void TestGetWriteConcern_W(bool enabledDefault, bool enabled, object wobj, string connectionString)
        {
            var w = (wobj == null) ? null : (wobj is int) ? (WriteConcern.WValue)new WriteConcern.WCount((int)wobj) : new WriteConcern.WMode((string)wobj);
            var url = new MongoUrl(connectionString);
            var writeConcern = url.GetWriteConcern(enabledDefault);
            Assert.AreEqual(enabled, writeConcern.Enabled);
            Assert.AreEqual(w, writeConcern.W);
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase(500, "mongodb://localhost/?wtimeout=500ms")]
        public void TestGetWriteConcern_WTimeout(int? ms, string connectionString)
        {
            var wtimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var url = new MongoUrl(connectionString);
            var writeConcern = url.GetWriteConcern(true);
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values, uuidAliases))
            {
                Assert.AreEqual(guidRepresentation ?? MongoDefaults.GuidRepresentation, url.GuidRepresentation);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(ipv6 ?? false, url.IPv6);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values, journalAliases))
            {
                Assert.AreEqual(journal, url.Journal);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(maxConnectionIdleTime ?? MongoDefaults.MaxConnectionIdleTime, url.MaxConnectionIdleTime);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(maxConnectionLifeTime ?? MongoDefaults.MaxConnectionLifeTime, url.MaxConnectionLifeTime);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase(123, "mongodb://localhost/?maxPoolSize=123")]
        public void TestMaxConnectionPoolSize(int? maxConnectionPoolSize, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (maxConnectionPoolSize != null) { built.MaxConnectionPoolSize = maxConnectionPoolSize.Value; }

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(maxConnectionPoolSize ?? MongoDefaults.MaxConnectionPoolSize, url.MaxConnectionPoolSize);
                Assert.AreEqual(connectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase(123, "mongodb://localhost/?minPoolSize=123")]
        public void TestMinConnectionPoolSize(int? minConnectionPoolSize, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (minConnectionPoolSize != null) { built.MinConnectionPoolSize = minConnectionPoolSize.Value; }

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(minConnectionPoolSize ?? MongoDefaults.MinConnectionPoolSize, url.MinConnectionPoolSize);
                Assert.AreEqual(connectionString, url.ToString());
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
            if (mode != null) { readPreference = new ReadPreference { ReadPreferenceMode = mode.Value }; }
            var built = new MongoUrlBuilder { Server = _localhost, ReadPreference = readPreference };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(readPreference, url.ReadPreference);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
        }

        [Test]
        public void TestReadPreference_SecondaryWithOneTagSet()
        {
            var tagSets = new ReplicaSetTagSet[]
            {
                new ReplicaSetTagSet { { "dc", "ny" }, { "rack", "1" } }
            };
            var readPreference = new ReadPreference { ReadPreferenceMode = ReadPreferenceMode.Secondary, TagSets = tagSets };
            var built = new MongoUrlBuilder { Server = _localhost, ReadPreference = readPreference };
            var connectionString = "mongodb://localhost/?readPreference=secondary;readPreferenceTags=dc:ny,rack:1";

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(ReadPreferenceMode.Secondary, url.ReadPreference.ReadPreferenceMode);
                var builderTagSets = url.ReadPreference.TagSets.ToArray();
                Assert.AreEqual(1, builderTagSets.Length);
                var builderTagSet1Tags = builderTagSets[0].Tags.ToArray();
                Assert.AreEqual(2, builderTagSet1Tags.Length);
                Assert.AreEqual(new ReplicaSetTag("dc", "ny"), builderTagSet1Tags[0]);
                Assert.AreEqual(new ReplicaSetTag("rack", "1"), builderTagSet1Tags[1]);
                Assert.AreEqual(connectionString, url.ToString());
            }
        }

        [Test]
        public void TestReadPreference_SecondaryWithTwoTagSets()
        {
            var tagSets = new ReplicaSetTagSet[]
            {
                new ReplicaSetTagSet { { "dc", "ny" }, { "rack", "1" } },
                new ReplicaSetTagSet { { "dc", "sf" } }
            };
            var readPreference = new ReadPreference { ReadPreferenceMode = ReadPreferenceMode.Secondary, TagSets = tagSets };
            var built = new MongoUrlBuilder { Server = _localhost, ReadPreference = readPreference };
            var connectionString = "mongodb://localhost/?readPreference=secondary;readPreferenceTags=dc:ny,rack:1;readPreferenceTags=dc:sf";

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(ReadPreferenceMode.Secondary, url.ReadPreference.ReadPreferenceMode);
                var builderTagSets = url.ReadPreference.TagSets.ToArray();
                Assert.AreEqual(2, builderTagSets.Length);
                var builderTagSet1Tags = builderTagSets[0].Tags.ToArray();
                var builderTagSet2Tags = builderTagSets[1].Tags.ToArray();
                Assert.AreEqual(2, builderTagSet1Tags.Length);
                Assert.AreEqual(new ReplicaSetTag("dc", "ny"), builderTagSet1Tags[0]);
                Assert.AreEqual(new ReplicaSetTag("rack", "1"), builderTagSet1Tags[1]);
                Assert.AreEqual(1, builderTagSet2Tags.Length);
                Assert.AreEqual(new ReplicaSetTag("dc", "sf"), builderTagSet2Tags[0]);
                Assert.AreEqual(connectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase("name", "mongodb://localhost/?replicaSet=name")]
        public void TestReplicaSetName(string name, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost, ReplicaSetName = name };

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(name, url.ReplicaSetName);
                Assert.AreEqual(connectionString, url.ToString());
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(w, url.W);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(false, false, "mongodb://localhost/?fsync={0};{1}={0};w=2;wtimeout=30s", new[] { "false", "False" }, new[] { "journal", "j" })]
        [TestCase(false, true, "mongodb://localhost/?fsync={0};{1}={0};w=2;wtimeout=30s", new[] { "true", "True" }, new[] { "journal", "j" })]
        [TestCase(true, false, "mongodb://localhost/?fsync={0};{1}={0};w=2;wtimeout=30s", new[] { "false", "False" }, new[] { "journal", "j" })]
        [TestCase(true, true, "mongodb://localhost/?fsync={0};{1}={0};w=2;wtimeout=30s", new[] { "true", "True" }, new[] { "journal", "j" })]
        public void TestSafeMode_All(bool enabledDefault, bool trueOrFalse, string formatString, string[] values, string[] journalAliases)
        {
#pragma warning disable 618
            var safeMode = new SafeMode(enabledDefault) { FSync = trueOrFalse, Journal = trueOrFalse, W = 2, WTimeout = TimeSpan.FromSeconds(30) };
            var built = new MongoUrlBuilder { Server = _localhost, SafeMode = safeMode };

            var canonicalConnectionString = string.Format(formatString, values[0], "journal");
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values, journalAliases))
            {
                Assert.AreEqual(true, url.SafeMode.Enabled);
                Assert.AreEqual(trueOrFalse, url.SafeMode.FSync);
                Assert.AreEqual(trueOrFalse, url.SafeMode.Journal);
                Assert.AreEqual(2, url.SafeMode.W);
                Assert.AreEqual(TimeSpan.FromSeconds(30), url.SafeMode.WTimeout);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
#pragma warning restore
        }

        [Test]
        [TestCase(false, null, "mongodb://localhost/?w=1", new[] { "" })]
        [TestCase(false, false, "mongodb://localhost/?fsync={0};w=1", new[] { "false", "False" })]
        [TestCase(false, true, "mongodb://localhost/?fsync={0};w=1", new[] { "true", "True" })]
        [TestCase(true, null, "mongodb://localhost/?w=1", new[] { "" })]
        [TestCase(true, false, "mongodb://localhost/?fsync={0};w=1", new[] { "false", "False" })]
        [TestCase(true, true, "mongodb://localhost/?fsync={0};w=1", new[] { "true", "True" })]
        public void TestSafeMode_FSync(bool enabledDefault, bool? fsync, string formatString, string[] values)
        {
#pragma warning disable 618
            var safeMode = new SafeMode(enabledDefault) { W = 1 };
            if (fsync != null) { safeMode.FSync = fsync.Value; }
            var built = new MongoUrlBuilder { Server = _localhost, SafeMode = safeMode };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(safeMode, url.SafeMode);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
#pragma warning restore
        }

        [Test]
        [TestCase(false, null, "mongodb://localhost/?w=1", new[] { "" }, new[] { "" })]
        [TestCase(false, false, "mongodb://localhost/?{1}={0};w=1", new[] { "false", "False" }, new[] { "journal", "j" })]
        [TestCase(false, true, "mongodb://localhost/?{1}={0};w=1", new[] { "true", "True" }, new[] { "journal", "j" })]
        [TestCase(true, null, "mongodb://localhost/?w=1", new[] { "" }, new[] { "" })]
        [TestCase(true, false, "mongodb://localhost/?{1}={0};w=1", new[] { "false", "False" }, new[] { "journal", "j" })]
        [TestCase(true, true, "mongodb://localhost/?{1}={0};w=1", new[] { "true", "True" }, new[] { "journal", "j" })]
        public void TestSafeMode_Journal(bool enabledDefault, bool? journal, string formatString, string[] values, string[] journalAliases)
        {
#pragma warning disable 618
            var safeMode = new SafeMode(enabledDefault) { W = 1 };
            if (journal != null) { safeMode.Journal = journal.Value; }
            var built = new MongoUrlBuilder { Server = _localhost, SafeMode = safeMode };

            var canonicalConnectionString = string.Format(formatString, values[0], "journal");
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values, journalAliases))
            {
                Assert.AreEqual(safeMode, url.SafeMode);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
#pragma warning restore
        }

        [Test]
        [TestCase(false, null, 0, "mongodb://localhost/?w=0")]
        [TestCase(false, false, 0, "mongodb://localhost/?w=0")]
        [TestCase(false, true, 1, "mongodb://localhost/?w=1")]
        [TestCase(true, null, 1, "mongodb://localhost/?w=1")]
        [TestCase(true, false, 0, "mongodb://localhost/?w=0")]
        [TestCase(true, true, 1, "mongodb://localhost/?w=1")]
        public void TestSafeMode_Enabled(bool enabledDefault, bool? enabled, int w, string connectionString)
        {
#pragma warning disable 618
            var safeMode = new SafeMode(enabledDefault);
            if (enabled != null) { safeMode.Enabled = enabled.Value; }
            var built = new MongoUrlBuilder { Server = _localhost, SafeMode = safeMode };

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual((WriteConcern.WValue)w, url.W);
                Assert.AreEqual(connectionString, url.ToString());
            }
#pragma warning restore
        }

        [Test]
        [TestCase(false, false, 0, "mongodb://localhost/?w=0")]
        [TestCase(false, true, 1, "mongodb://localhost/?w=1")]
        [TestCase(false, true, 2, "mongodb://localhost/?w=2")]
        [TestCase(true, false, 0, "mongodb://localhost/?w=0")]
        [TestCase(true, true, 1, "mongodb://localhost/?w=1")]
        [TestCase(true, true, 2, "mongodb://localhost/?w=2")]
        public void TestSafeMode_W(bool enabledDefault, bool enabled, int w, string connectionString)
        {
#pragma warning disable 618
            var safeMode = new SafeMode(enabledDefault) { W = w };
            var built = new MongoUrlBuilder { Server = _localhost, SafeMode = safeMode };

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(enabled, url.SafeMode.Enabled);
                Assert.AreEqual(w, url.SafeMode.W);
                Assert.AreEqual(connectionString, url.ToString());
            }
#pragma warning restore
        }

        [Test]
        [TestCase(false, false, null, "mongodb://localhost/?w=0")]
        [TestCase(false, true, "mode", "mongodb://localhost/?w=mode")]
        [TestCase(true, true, null, "mongodb://localhost/?w=1")]
        [TestCase(true, true, "mode", "mongodb://localhost/?w=mode")]
        public void TestSafeMode_WMode(bool enabledDefault, bool enabled, string wmode, string connectionString)
        {
#pragma warning disable 618
            var built = new MongoUrlBuilder { Server = _localhost, SafeMode = new SafeMode(enabledDefault) { WMode = wmode } };

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(enabled, url.SafeMode.Enabled);
                Assert.AreEqual(wmode, url.SafeMode.WMode);
                Assert.AreEqual(connectionString, url.ToString());
            }
#pragma warning restore
        }

        [Test]
        [TestCase(false, null, "mongodb://localhost/?w=2", new[] { "" })]
        [TestCase(false, 500, "mongodb://localhost/?w=2;wtimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(false, 30000, "mongodb://localhost/?w=2;wtimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(false, 1800000, "mongodb://localhost/?w=2;wtimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(false, 3600000, "mongodb://localhost/?w=2;wtimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(false, 3723000, "mongodb://localhost/?w=2;wtimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        [TestCase(true, null, "mongodb://localhost/?w=2", new[] { "" })]
        [TestCase(true, 500, "mongodb://localhost/?w=2;wtimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(true, 30000, "mongodb://localhost/?w=2;wtimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(true, 1800000, "mongodb://localhost/?w=2;wtimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(true, 3600000, "mongodb://localhost/?w=2;wtimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(true, 3723000, "mongodb://localhost/?w=2;wtimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestSafeMode_WTimeout(bool enabledDefault, int? ms, string formatString, string[] values)
        {
#pragma warning disable 618
            var wtimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var safeMode = new SafeMode(enabledDefault) { W = 2 };
            if (wtimeout != null) { safeMode.WTimeout = wtimeout.Value; }
            var built = new MongoUrlBuilder { Server = _localhost, SafeMode = safeMode };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(true, url.SafeMode.Enabled);
                Assert.AreEqual(2, url.SafeMode.W);
                Assert.AreEqual(wtimeout ?? TimeSpan.Zero, url.SafeMode.WTimeout);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
#pragma warning restore
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(500, "mongodb://localhost/?secondaryAcceptableLatency{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "mongodb://localhost/?secondaryAcceptableLatency{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "mongodb://localhost/?secondaryAcceptableLatency{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "mongodb://localhost/?secondaryAcceptableLatency{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "mongodb://localhost/?secondaryAcceptableLatency{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestSecondaryAcceptableLatency(int? ms, string formatString, string[] values)
        {
            var secondaryAcceptableLatency = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoUrlBuilder { Server = _localhost };
            if (secondaryAcceptableLatency != null) { built.SecondaryAcceptableLatency = secondaryAcceptableLatency.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(secondaryAcceptableLatency ?? MongoDefaults.SecondaryAcceptableLatency, url.SecondaryAcceptableLatency);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(null, null, "mongodb://")]
        [TestCase("host", null, "mongodb://host")]
        [TestCase("host", 27017, "mongodb://host")]
        [TestCase("host", 27018, "mongodb://host:27018")]
        public void TestServer(string host, int? port, string connectionString)
        {
            var server = (host == null) ? null : (port == null) ? new MongoServerAddress(host) : new MongoServerAddress(host, port.Value);
            var built = new MongoUrlBuilder { Server = server };

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(server, url.Server);
                Assert.AreEqual(connectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(null, null, "mongodb://")]
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

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(servers, url.Servers);
                Assert.AreEqual(connectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost", new[] { "" })]
        [TestCase(false, "mongodb://localhost/?slaveOk={0}", new[] { "false", "False" })]
        [TestCase(true, "mongodb://localhost/?slaveOk={0}", new[] { "true", "True" })]
        public void TestSlaveOk(bool? slaveOk, string formatString, string[] values)
        {
#pragma warning disable 618
            var built = new MongoUrlBuilder { Server = _localhost };
            if (slaveOk != null) { built.SlaveOk = slaveOk.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(slaveOk ?? false, url.SlaveOk);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
#pragma warning restore
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(socketTimeout ?? MongoDefaults.SocketTimeout, url.SocketTimeout);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(useSsl ?? false, url.UseSsl);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(verifySslCertificate ?? true, url.VerifySslCertificate);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(false, false, null, "mongodb://localhost")]
        [TestCase(false, false, 0, "mongodb://localhost/?w=0")]
        [TestCase(false, true, 1, "mongodb://localhost/?w=1")]
        [TestCase(false, true, 2, "mongodb://localhost/?w=2")]
        [TestCase(false, true, "mode", "mongodb://localhost/?w=mode")]
        [TestCase(true, true, null, "mongodb://localhost")]
        [TestCase(true, false, 0, "mongodb://localhost/?w=0")]
        [TestCase(true, true, 1, "mongodb://localhost/?w=1")]
        [TestCase(true, true, 2, "mongodb://localhost/?w=2")]
        [TestCase(true, true, "mode", "mongodb://localhost/?w=mode")]
        public void TestW(bool enabledDefault, bool enabled, object wobj, string connectionString)
        {
            var w = (wobj == null) ? null : (wobj is int) ? (WriteConcern.WValue)new WriteConcern.WCount((int)wobj) : new WriteConcern.WMode((string)wobj);
            var built = new MongoUrlBuilder { Server = _localhost, W = w };

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                var writeConcern = url.GetWriteConcern(enabledDefault);
                Assert.AreEqual(enabled, writeConcern.Enabled);
                Assert.AreEqual(w, writeConcern.W);
                Assert.AreEqual(connectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase(2.0, "mongodb://localhost/?waitQueueMultiple=2")]
        public void TestWaitQueueMultiple(double? multiple, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (multiple != null) { built.WaitQueueMultiple = multiple.Value; }

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual(multiple ?? MongoDefaults.WaitQueueMultiple, url.WaitQueueMultiple);
                Assert.AreEqual((multiple == null) ? MongoDefaults.WaitQueueSize : 0, url.WaitQueueSize);
                Assert.AreEqual(connectionString, url.ToString());
            }
        }

        [Test]
        [TestCase(null, "mongodb://localhost")]
        [TestCase(123, "mongodb://localhost/?waitQueueSize=123")]
        public void TestWaitQueueSize(int? size, string connectionString)
        {
            var built = new MongoUrlBuilder { Server = _localhost };
            if (size != null) { built.WaitQueueSize = size.Value; }

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual((size == null) ? MongoDefaults.WaitQueueMultiple : 0.0, url.WaitQueueMultiple);
                Assert.AreEqual(size ?? MongoDefaults.WaitQueueSize, url.WaitQueueSize);
                Assert.AreEqual(connectionString, url.ToString());
            }
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(waitQueueTimeout ?? MongoDefaults.WaitQueueTimeout, url.WaitQueueTimeout);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
            }
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
            foreach (var url in EnumerateBuiltAndParsedUrls(built, formatString, values))
            {
                Assert.AreEqual(wtimeout, url.WTimeout);
                Assert.AreEqual(canonicalConnectionString, url.ToString());
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

        private IEnumerable<MongoUrl> EnumerateBuiltAndParsedUrls(
            MongoUrlBuilder built,
            string formatString,
            string[] values)
        {
            yield return built.ToMongoUrl();
            foreach (var parsed in EnumerateParsedUrls(formatString, values))
            {
                yield return parsed;
            }
        }

        private IEnumerable<MongoUrl> EnumerateBuiltAndParsedUrls(
            MongoUrlBuilder built,
            string formatString,
            string[] values1,
            string[] values2)
        {
            yield return built.ToMongoUrl();
            foreach (var parsed in EnumerateParsedUrls(formatString, values1, values2))
            {
                yield return parsed;
            }
        }

        private IEnumerable<MongoUrl> EnumerateParsedUrls(
            string formatString,
            string[] values)
        {
            foreach (var v in values)
            {
                var connectionString = string.Format(formatString, v);
                yield return new MongoUrl(connectionString);
            }
        }

        private IEnumerable<MongoUrl> EnumerateParsedUrls(
            string formatString,
            string[] values1,
            string[] values2)
        {
            foreach (var v1 in values1)
            {
                foreach (var v2 in values2)
                {
                    var connectionString = string.Format(formatString, v1, v2);
                    yield return new MongoUrl(connectionString);
                }
            }
        }
    }
}
