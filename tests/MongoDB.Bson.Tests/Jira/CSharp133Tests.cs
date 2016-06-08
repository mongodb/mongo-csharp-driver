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
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp133
{
    public class C
    {
        public string S;
        [BsonIgnoreIfNull]
        public string I;
#pragma warning disable 618 // SerializeDefaultValue is obsolete
        [BsonDefaultValue(null, SerializeDefaultValue = false)] // works the same as [BsonIgnoreIfNull]
        public string D;
#pragma warning restore 618
        [BsonIgnoreIfDefault]
        public DateTime I2;
    }

    public class CSharp133Tests
    {
        [Fact]
        public void TestNull()
        {
            var c = new C { S = null, I = null, D = null };
            var json = c.ToJson();
            var expected = "{ 'S' : null }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsType<C>(rehydrated);
            Assert.Null(rehydrated.S);
            Assert.Null(rehydrated.I);
            Assert.Equal(DateTime.MinValue, rehydrated.I2);
            Assert.Null(rehydrated.D);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNotNull()
        {
            var date = new DateTime(1980, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var c = new C { S = "xyz", I = "xyz", I2 = date, D = "xyz" };
            var json = c.ToJson();
            var expected = ("{ 'S' : 'xyz', 'I' : 'xyz', 'D' : 'xyz', 'I2' : ISODate('" + date.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ") + "') }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsType<C>(rehydrated);
            Assert.Equal("xyz", rehydrated.S);
            Assert.Equal("xyz", rehydrated.I);
            Assert.Equal(date, rehydrated.I2);
            Assert.Equal("xyz", rehydrated.D);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
