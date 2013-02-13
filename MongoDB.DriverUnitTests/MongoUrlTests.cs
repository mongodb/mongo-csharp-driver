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
                SecondaryAcceptableLatency = TimeSpan.FromSeconds(6),
                TagSets = new[] { new ReplicaSetTagSet { { "dc", "1" } } }
            };
            var built = new MongoUrlBuilder()
            {
                AuthenticationMechanism = "GSSAPI",
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
                ReadPreference = readPreference,
                ReplicaSetName = "name",
                Server = new MongoServerAddress("host"),
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
                "authSource=db",
                "ipv6=true",
                "ssl=true", // UseSsl
                "sslVerifyCertificate=false", // VerifySslCertificate
                "connect=replicaSet",
                "replicaSet=name",
                "readPreference=secondary;secondaryAcceptableLatency=6s;readPreferenceTags=dc:1",
                "fsync=true",
                "journal=true",
                "w=2",
                "wtimeout=9s",
                "connectTimeout=1s",
                "maxIdleTime=2s",
                "maxLifeTime=3s",
                "maxPoolSize=4",
                "minPoolSize=5",
                "socketTimeout=7s",
                "waitQueueSize=123",
                "waitQueueTimeout=8s",
                "uuidRepresentation=pythonLegacy"
            });

            foreach (var url in EnumerateBuiltAndParsedUrls(built, connectionString))
            {
                Assert.AreEqual("GSSAPI", url.AuthenticationMechanism);
                Assert.AreEqual("db", url.AuthenticationSource);
                Assert.AreEqual(123, url.ComputedWaitQueueSize);
                Assert.AreEqual(ConnectionMode.ReplicaSet, url.ConnectionMode);
                Assert.AreEqual(TimeSpan.FromSeconds(1), url.ConnectTimeout);
                Assert.AreEqual("database", url.DatabaseName);
                Assert.AreEqual(true, url.FSync);
                Assert.AreEqual(GuidRepresentation.PythonLegacy, url.GuidRepresentation);
                Assert.AreEqual(true, url.IPv6);
                Assert.AreEqual(true, url.Journal);
                Assert.AreEqual(TimeSpan.FromSeconds(2), url.MaxConnectionIdleTime);
                Assert.AreEqual(TimeSpan.FromSeconds(3), url.MaxConnectionLifeTime);
                Assert.AreEqual(4, url.MaxConnectionPoolSize);
                Assert.AreEqual(5, url.MinConnectionPoolSize);
                Assert.AreEqual("password", url.Password);
                Assert.AreEqual(readPreference, url.ReadPreference);
                Assert.AreEqual("name", url.ReplicaSetName);
#pragma warning disable 618
                Assert.AreEqual(new SafeMode(true) { FSync = true, Journal = true, W = 2, WTimeout = TimeSpan.FromSeconds(9) }, url.SafeMode);
#pragma warning restore
                Assert.AreEqual(new MongoServerAddress("host", 27017), url.Server);
#pragma warning disable 618
                Assert.AreEqual(true, url.SlaveOk);
#pragma warning restore
                Assert.AreEqual(TimeSpan.FromSeconds(7), url.SocketTimeout);
                Assert.AreEqual("username", url.Username);
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
