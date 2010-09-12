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

using MongoDB.CSharpDriver;

namespace MongoDB.CSharpDriver.Tests {
    [TestFixture]
    public class MongoUrlTests {
        [Test]
        public void TestL() {
            string urlString = "mongodb://localhost";
            MongoUrl url = new MongoUrl(urlString);
            Assert.AreEqual(1, url.SeedList.Count());
            Assert.AreEqual("localhost", url.SeedList.Single().Host);
            Assert.AreEqual(27017, url.SeedList.Single().Port);
            Assert.IsNull(url.DatabaseName);
            Assert.IsNull(url.Username);
            Assert.IsNull(url.Password);
            Assert.AreEqual(urlString, url.ToString());
        }

        [Test]
        public void TestH() {
            string urlString = "mongodb://mongo.xyz.com";
            MongoUrl url = new MongoUrl(urlString);
            Assert.AreEqual(1, url.SeedList.Count());
            Assert.AreEqual("mongo.xyz.com", url.SeedList.Single().Host);
            Assert.AreEqual(27017, url.SeedList.Single().Port);
            Assert.IsNull(url.DatabaseName);
            Assert.IsNull(url.Username);
            Assert.IsNull(url.Password);
            Assert.AreEqual(urlString, url.ToString());
        }

        [Test]
        public void TestHP() {
            string urlString = "mongodb://mongo.xyz.com:12345";
            MongoUrl url = new MongoUrl(urlString);
            Assert.AreEqual(1, url.SeedList.Count());
            Assert.AreEqual("mongo.xyz.com", url.SeedList.Single().Host);
            Assert.AreEqual(12345, url.SeedList.Single().Port);
            Assert.IsNull(url.DatabaseName);
            Assert.IsNull(url.Username);
            Assert.IsNull(url.Password);
            Assert.AreEqual(urlString, url.ToString());
        }

        [Test]
        public void TestH1H2() {
            string urlString = "mongodb://mongo1.xyz.com,mongo2.xyz.com";
            MongoUrl url = new MongoUrl(urlString);
            Assert.AreEqual(2, url.SeedList.Count());
            Assert.AreEqual("mongo1.xyz.com", url.SeedList.First().Host);
            Assert.AreEqual(27017, url.SeedList.First().Port);
            Assert.AreEqual("mongo2.xyz.com", url.SeedList.Skip(1).Single().Host);
            Assert.AreEqual(27017, url.SeedList.Skip(1).Single().Port);
            Assert.IsNull(url.DatabaseName);
            Assert.IsNull(url.Username);
            Assert.IsNull(url.Password);
            Assert.AreEqual(urlString, url.ToString());
        }

        [Test]
        public void TestH1P1H2P2() {
            string urlString = "mongodb://mongo1.xyz.com:12345,mongo2.xyz.com:23456";
            MongoUrl url = new MongoUrl(urlString);
            Assert.AreEqual(2, url.SeedList.Count());
            Assert.AreEqual("mongo1.xyz.com", url.SeedList.First().Host);
            Assert.AreEqual(12345, url.SeedList.First().Port);
            Assert.AreEqual("mongo2.xyz.com", url.SeedList.Skip(1).Single().Host);
            Assert.AreEqual(23456, url.SeedList.Skip(1).Single().Port);
            Assert.IsNull(url.DatabaseName);
            Assert.IsNull(url.Username);
            Assert.IsNull(url.Password);
            Assert.AreEqual(urlString, url.ToString());
        }

        [Test]
        public void TestUPLD() {
            string urlString = "mongodb://userx:pwd@localhost/dbname";
            MongoUrl url = new MongoUrl(urlString);
            Assert.AreEqual(1, url.SeedList.Count());
            Assert.AreEqual("localhost", url.SeedList.Single().Host);
            Assert.AreEqual(27017, url.SeedList.Single().Port);
            Assert.AreEqual("dbname", url.DatabaseName);
            Assert.AreEqual("userx", url.Username);
            Assert.AreEqual("pwd", url.Password);
            Assert.AreEqual(urlString, url.ToString());
        }

        [Test]
        public void TestUPH1H2D() {
            string urlString = "mongodb://userx:pwd@mongo1.xyz.com,mongo2.xyz.com/dbname";
            MongoUrl url = new MongoUrl(urlString);
            Assert.AreEqual(2, url.SeedList.Count());
            Assert.AreEqual("mongo1.xyz.com", url.SeedList.First().Host);
            Assert.AreEqual(27017, url.SeedList.First().Port);
            Assert.AreEqual("mongo2.xyz.com", url.SeedList.Skip(1).Single().Host);
            Assert.AreEqual(27017, url.SeedList.Skip(1).Single().Port);
            Assert.AreEqual("dbname", url.DatabaseName);
            Assert.AreEqual("userx", url.Username);
            Assert.AreEqual("pwd", url.Password);
            Assert.AreEqual(urlString, url.ToString());
        }

        [Test]
        public void TestUPH1P1H2P2D() {
            string urlString = "mongodb://userx:pwd@mongo1.xyz.com:12345,mongo2.xyz.com:23456/dbname";
            MongoUrl url = new MongoUrl(urlString);
            Assert.AreEqual(2, url.SeedList.Count());
            Assert.AreEqual("mongo1.xyz.com", url.SeedList.First().Host);
            Assert.AreEqual(12345, url.SeedList.First().Port);
            Assert.AreEqual("mongo2.xyz.com", url.SeedList.Skip(1).Single().Host);
            Assert.AreEqual(23456, url.SeedList.Skip(1).Single().Port);
            Assert.AreEqual("dbname", url.DatabaseName);
            Assert.AreEqual("userx", url.Username);
            Assert.AreEqual("pwd", url.Password);
            Assert.AreEqual(urlString, url.ToString());
        }
    }
}
