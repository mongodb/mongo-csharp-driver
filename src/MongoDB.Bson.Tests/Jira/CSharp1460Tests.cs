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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.Jira
{
    [TestFixture]
    public class CSharp1460Tests
    {
        [Test]
        public void TestJsonWriterLocalDateTimeSetting()
        {
            var testDateTime = DateTime.ParseExact("2015-10-28T00:00:00Z", "yyyy-MM-ddTHH:mm:ss.FFFZ", System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime();
            var document = new BsonDocument();
            document.Add("DateTimeField", testDateTime);
            var json = document.ToJson(new Bson.IO.JsonWriterSettings() { UseLocalTime = true });
            var expected = ("{ 'DateTimeField' : ISODate('" + testDateTime.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFzzz") + "') }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = document.ToBson();
            var rehydrated = BsonDocument.Parse(json);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));

            //test without settings, should work as before
            json = document.ToJson();
            expected = ("{ 'DateTimeField' : ISODate('" + testDateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ") + "') }").Replace("'", "\"");
            Assert.AreEqual(expected, json);
            bson = document.ToBson();
            rehydrated = BsonDocument.Parse(json);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));

            //test with parameter, with no setting specified, should work as before
            json = document.ToJson(new Bson.IO.JsonWriterSettings());
            expected = ("{ 'DateTimeField' : ISODate('" + testDateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ") + "') }").Replace("'", "\"");
            Assert.AreEqual(expected, json);
            bson = document.ToBson();
            rehydrated = BsonDocument.Parse(json);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
