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

using System;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira.CSharp261
{
    [TestFixture]
    public class CSharp261Tests
    {
        [Test]
        public void TestDate()
        {
            var json = "{ date : Date() }";
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.String, document["date"].BsonType);
            // not sure how to test the string since it will be localized
        }

        [Test]
        public void TestNewDateZero()
        {
            var json = "{ date : new Date(0) }";
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.DateTime, document["date"].BsonType);
            Assert.AreEqual(BsonConstants.UnixEpoch, document["date"].ToUniversalTime());
        }

        [Test]
        public void TestNewDateMillisecondsSinceEpoch()
        {
            var utcNow = DateTime.UtcNow;
            var millisecondsSinceEpoch = BsonUtils.ToMillisecondsSinceEpoch(utcNow);
            var json = string.Format("{{ date : new Date({0}) }}", millisecondsSinceEpoch);
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.DateTime, document["date"].BsonType);
            Assert.AreEqual(BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(millisecondsSinceEpoch), document["date"].ToUniversalTime());
        }

        [Test]
        public void TestNewDateString()
        {
            var dateTimeString = "Thu, 07 Jul 2011 14:58:59 EDT";
            var json = string.Format("{{ date : new Date('{0}') }}", dateTimeString);
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.DateTime, document["date"].BsonType);
            Assert.AreEqual(new DateTime(2011, 7, 7, 18, 58, 59), document["date"].ToUniversalTime()); // note date is now in UTC
        }

        [Test]
        public void TestNewDateYMD()
        {
            var json = "{ date : new Date(2011, 6, 7) }"; // July = 6 in JavaScript
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.DateTime, document["date"].BsonType);
            Assert.AreEqual(new DateTime(2011, 7, 7), document["date"].ToUniversalTime());
        }

        [Test]
        public void TestNewDateYMDH()
        {
            var json = "{ date : new Date(2011, 6, 7, 1) }"; // July = 6 in JavaScript
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.DateTime, document["date"].BsonType);
            Assert.AreEqual(new DateTime(2011, 7, 7, 1, 0, 0), document["date"].ToUniversalTime());
        }

        [Test]
        public void TestNewDateYMDHM()
        {
            var json = "{ date : new Date(2011, 6, 7, 1, 2) }"; // July = 6 in JavaScript
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.DateTime, document["date"].BsonType);
            Assert.AreEqual(new DateTime(2011, 7, 7, 1, 2, 0), document["date"].ToUniversalTime());
        }

        [Test]
        public void TestNewDateYMDHMS()
        {
            var json = "{ date : new Date(2011, 6, 7, 1, 2, 33) }"; // July = 6 in JavaScript
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.DateTime, document["date"].BsonType);
            Assert.AreEqual(new DateTime(2011, 7, 7, 1, 2, 33), document["date"].ToUniversalTime());
        }

        [Test]
        public void TestNewDateYMDHMSms()
        {
            var json = "{ date : new Date(2011, 6, 7, 1, 2, 33, 456) }"; // July = 6 in JavaScript
            var document = BsonDocument.Parse(json);
            Assert.AreEqual(BsonType.DateTime, document["date"].BsonType);
            Assert.AreEqual(new DateTime(2011, 7, 7, 1, 2, 33, 456), document["date"].ToUniversalTime());
        }
    }
}
