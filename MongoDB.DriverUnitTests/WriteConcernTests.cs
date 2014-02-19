/* Copyright 2010-2014 MongoDB Inc.
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

using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class WriteConcernTests
    {
        private static readonly string __noWriteConcernConnectionUri = "mongodb://localhost";
        private static readonly string __w1WriteConcernConnectionUri = "mongodb://localhost/?w=1";

        [Test]
        public void TestAcknowledgedWriteConcern()
        {
            var acknowledgedWriteConcern = WriteConcern.Acknowledged;
            Assert.IsTrue(acknowledgedWriteConcern.IsFrozen);
            Assert.IsTrue(acknowledgedWriteConcern.Enabled);
            Assert.IsNull(acknowledgedWriteConcern.FSync);
            Assert.IsNull(acknowledgedWriteConcern.Journal);
            Assert.IsNull(acknowledgedWriteConcern.W);
            Assert.IsNull(acknowledgedWriteConcern.WTimeout);
        }

        [Test]
        public void TestW1WriteConcern()
        {
            var acknowledgedWriteConcern = WriteConcern.W1;
            Assert.IsTrue(acknowledgedWriteConcern.IsFrozen);
            Assert.IsTrue(acknowledgedWriteConcern.Enabled);
            Assert.IsNull(acknowledgedWriteConcern.FSync);
            Assert.IsNull(acknowledgedWriteConcern.Journal);
            Assert.IsTrue(1 == acknowledgedWriteConcern.W);
            Assert.IsNull(acknowledgedWriteConcern.WTimeout);
        }

        [Test]
        public void TestMongoClientDefaultWriteConcern()
        {
            var client = new MongoClient();
            Assert.AreEqual(WriteConcern.Acknowledged, client.Settings.WriteConcern);
        }

        [Test]
        public void TestMongoServerDefaultWriteConcern()
        {
#pragma warning disable 618 // about obsolete MongoServer.Create
            var server = MongoServer.Create();
#pragma warning restore 618 
            Assert.IsFalse(server.Settings.WriteConcern.Enabled);
        }

        [Test]
        public void TestMongoClient_ConnectionString_No_W()
        {
            var client = new MongoClient(__noWriteConcernConnectionUri);
            Assert.AreEqual(WriteConcern.Acknowledged, client.Settings.WriteConcern);
        }

        [Test]
        public void TestMongoServer_ConnectionString_No_W()
        {
#pragma warning disable 618 // about obsolete MongoServer.Create
            var server = MongoServer.Create(__noWriteConcernConnectionUri);
#pragma warning restore 618
            Assert.IsFalse(server.Settings.WriteConcern.Enabled);
        }

        [Test]
        public void TestMongoClient_ConnectionString_W1()
        {
            var client = new MongoClient(__w1WriteConcernConnectionUri);
            Assert.AreEqual(WriteConcern.W1, client.Settings.WriteConcern);
        }

        [Test]
        public void TestMongoServer_ConnectionString_W1()
        {
#pragma warning disable 618 // about obsolete MongoServer.Create
            var server = MongoServer.Create(__w1WriteConcernConnectionUri);
#pragma warning restore 618
            Assert.IsTrue(TestWriteConcernIsW1Only(server.Settings.WriteConcern));
        }

        private bool TestWriteConcernIsW1Only(WriteConcern writeConcern)
        {
            if (writeConcern == null) { return false; }
            return
                writeConcern.IsFrozen &&
                writeConcern.Enabled &&
                (writeConcern.FSync == null) &&
                (writeConcern.Journal == null) &&
                (writeConcern.WTimeout == null) &&
                (writeConcern.W == 1);
        }
    }
}
