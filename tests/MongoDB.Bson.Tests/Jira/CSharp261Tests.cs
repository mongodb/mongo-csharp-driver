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
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp261
{
    public class CSharp261Tests
    {
        [Fact]
        public void TestDate()
        {
            var json = "{ date : Date() }";
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.String, document["date"].BsonType);
            // not sure how to test the string since it will be localized
        }

        [Fact]
        public void TestNewDateZero()
        {
            var json = "{ date : new Date(0) }";
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.DateTime, document["date"].BsonType);
            Assert.Equal(BsonConstants.UnixEpoch, document["date"].ToUniversalTime());
        }

        [Fact]
        public void TestNewDateMillisecondsSinceEpoch()
        {
            var utcNow = DateTime.UtcNow;
            var millisecondsSinceEpoch = BsonUtils.ToMillisecondsSinceEpoch(utcNow);
            var json = string.Format("{{ date : new Date({0}) }}", millisecondsSinceEpoch);
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.DateTime, document["date"].BsonType);
            Assert.Equal(BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(millisecondsSinceEpoch), document["date"].ToUniversalTime());
        }

        [Fact]
        public void TestNewDateString()
        {
            var dateTimeString = "Thu, 07 Jul 2011 14:58:59 EDT";
            var json = string.Format("{{ date : new Date('{0}') }}", dateTimeString);
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.DateTime, document["date"].BsonType);
            Assert.Equal(new DateTime(2011, 7, 7, 18, 58, 59), document["date"].ToUniversalTime()); // note date is now in UTC
        }

        [Fact]
        public void TestNewDateYMD()
        {
            var json = "{ date : new Date(2011, 6, 7) }"; // July = 6 in JavaScript
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.DateTime, document["date"].BsonType);
            Assert.Equal(new DateTime(2011, 7, 7), document["date"].ToUniversalTime());
        }

        [Fact]
        public void TestNewDateYMDH()
        {
            var json = "{ date : new Date(2011, 6, 7, 1) }"; // July = 6 in JavaScript
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.DateTime, document["date"].BsonType);
            Assert.Equal(new DateTime(2011, 7, 7, 1, 0, 0), document["date"].ToUniversalTime());
        }

        [Fact]
        public void TestNewDateYMDHM()
        {
            var json = "{ date : new Date(2011, 6, 7, 1, 2) }"; // July = 6 in JavaScript
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.DateTime, document["date"].BsonType);
            Assert.Equal(new DateTime(2011, 7, 7, 1, 2, 0), document["date"].ToUniversalTime());
        }

        [Fact]
        public void TestNewDateYMDHMS()
        {
            var json = "{ date : new Date(2011, 6, 7, 1, 2, 33) }"; // July = 6 in JavaScript
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.DateTime, document["date"].BsonType);
            Assert.Equal(new DateTime(2011, 7, 7, 1, 2, 33), document["date"].ToUniversalTime());
        }

        [Fact]
        public void TestNewDateYMDHMSms()
        {
            var json = "{ date : new Date(2011, 6, 7, 1, 2, 33, 456) }"; // July = 6 in JavaScript
            var document = BsonDocument.Parse(json);
            Assert.Equal(BsonType.DateTime, document["date"].BsonType);
            Assert.Equal(new DateTime(2011, 7, 7, 1, 2, 33, 456), document["date"].ToUniversalTime());
        }
    }
}
