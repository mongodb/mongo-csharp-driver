/* Copyright 2010 10gen Inc.
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
using System.Text;
using NUnit.Framework;

using MongoDB.MongoDBClient;

namespace MongoDB.MongoDBClient.Tests {
    [TestFixture]
    public class MongoConnectionStringBuilderTests {
        [Test]
        public void TestL() {
            string connectionString = "mongodb://localhost";
            MongoConnectionStringBuilder csb = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(1, csb.Servers.Count);
            Assert.AreEqual("localhost", csb.Servers[0].Host);
            Assert.AreEqual(27017, csb.Servers[0].Port);
            Assert.IsNull(csb.Database);
            Assert.IsNull(csb.Username);
            Assert.IsNull(csb.Password);
            Assert.AreEqual(connectionString, csb.ToString());
        }

        [Test]
        public void TestH() {
            string connectionString = "mongodb://mongo.xyz.com";
            MongoConnectionStringBuilder csb = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(1, csb.Servers.Count);
            Assert.AreEqual("mongo.xyz.com", csb.Servers[0].Host);
            Assert.AreEqual(27017, csb.Servers[0].Port);
            Assert.IsNull(csb.Database);
            Assert.IsNull(csb.Username);
            Assert.IsNull(csb.Password);
            Assert.AreEqual(connectionString, csb.ToString());
        }

        [Test]
        public void TestHP() {
            string connectionString = "mongodb://mongo.xyz.com:12345";
            MongoConnectionStringBuilder csb = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(1, csb.Servers.Count);
            Assert.AreEqual("mongo.xyz.com", csb.Servers[0].Host);
            Assert.AreEqual(12345, csb.Servers[0].Port);
            Assert.IsNull(csb.Database);
            Assert.IsNull(csb.Username);
            Assert.IsNull(csb.Password);
            Assert.AreEqual(connectionString, csb.ToString());
        }

        [Test]
        public void TestH1H2() {
            string connectionString = "mongodb://mongo1.xyz.com,mongo2.xyz.com";
            MongoConnectionStringBuilder csb = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(2, csb.Servers.Count);
            Assert.AreEqual("mongo1.xyz.com", csb.Servers[0].Host);
            Assert.AreEqual(27017, csb.Servers[0].Port);
            Assert.AreEqual("mongo2.xyz.com", csb.Servers[1].Host);
            Assert.AreEqual(27017, csb.Servers[1].Port);
            Assert.IsNull(csb.Database);
            Assert.IsNull(csb.Username);
            Assert.IsNull(csb.Password);
            Assert.AreEqual(connectionString, csb.ToString());
        }

        [Test]
        public void TestH1P1H2P2() {
            string connectionString = "mongodb://mongo1.xyz.com:12345,mongo2.xyz.com:23456";
            MongoConnectionStringBuilder csb = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(2, csb.Servers.Count);
            Assert.AreEqual("mongo1.xyz.com", csb.Servers[0].Host);
            Assert.AreEqual(12345, csb.Servers[0].Port);
            Assert.AreEqual("mongo2.xyz.com", csb.Servers[1].Host);
            Assert.AreEqual(23456, csb.Servers[1].Port);
            Assert.IsNull(csb.Database);
            Assert.IsNull(csb.Username);
            Assert.IsNull(csb.Password);
            Assert.AreEqual(connectionString, csb.ToString());
        }

        [Test]
        public void TestUPLD() {
            string connectionString = "mongodb://userx:pwd@localhost/dbname";
            MongoConnectionStringBuilder csb = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(1, csb.Servers.Count);
            Assert.AreEqual("localhost", csb.Servers[0].Host);
            Assert.AreEqual(27017, csb.Servers[0].Port);
            Assert.AreEqual("dbname", csb.Database);
            Assert.AreEqual("userx", csb.Username);
            Assert.AreEqual("pwd", csb.Password);
            Assert.AreEqual(connectionString, csb.ToString());
        }

        [Test]
        public void TestUPH1H2D() {
            string connectionString = "mongodb://userx:pwd@mongo1.xyz.com,mongo2.xyz.com/dbname";
            MongoConnectionStringBuilder csb = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(2, csb.Servers.Count);
            Assert.AreEqual("mongo1.xyz.com", csb.Servers[0].Host);
            Assert.AreEqual(27017, csb.Servers[0].Port);
            Assert.AreEqual("mongo2.xyz.com", csb.Servers[1].Host);
            Assert.AreEqual(27017, csb.Servers[1].Port);
            Assert.AreEqual("dbname", csb.Database);
            Assert.AreEqual("userx", csb.Username);
            Assert.AreEqual("pwd", csb.Password);
            Assert.AreEqual(connectionString, csb.ToString());
        }

        [Test]
        public void TestUPH1P1H2P2D() {
            string connectionString = "mongodb://userx:pwd@mongo1.xyz.com:12345,mongo2.xyz.com:23456/dbname";
            MongoConnectionStringBuilder csb = new MongoConnectionStringBuilder(connectionString);
            Assert.AreEqual(2, csb.Servers.Count);
            Assert.AreEqual("mongo1.xyz.com", csb.Servers[0].Host);
            Assert.AreEqual(12345, csb.Servers[0].Port);
            Assert.AreEqual("mongo2.xyz.com", csb.Servers[1].Host);
            Assert.AreEqual(23456, csb.Servers[1].Port);
            Assert.AreEqual("dbname", csb.Database);
            Assert.AreEqual("userx", csb.Username);
            Assert.AreEqual("pwd", csb.Password);
            Assert.AreEqual(connectionString, csb.ToString());
        }
    }
}
