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
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp313Tests
    {
        private static object[] __scalarValues = new object[]
        {
            new BsonBinaryData(new byte[] { 1, 2, 3 }),
            true,
            new DateTime(2013, 8, 17, 23, 30, 0, DateTimeKind.Utc),
            1.2,
            1,
            1L,
            new BsonJavaScript("this.x == 1"),
            new BsonJavaScriptWithScope("this.x == y", new BsonDocument("y", 1)),
            BsonMaxKey.Value,
            BsonMinKey.Value,
            BsonNull.Value,
            ObjectId.GenerateNewId(),
            new BsonRegularExpression("abc"),
            new BsonArray { 1, 2 },
            "abc",
            BsonSymbolTable.Lookup("abc"),
            new BsonTimestamp(1234L),
            BsonUndefined.Value
        };

        public class C
        {
            public object ScalarValue { get; set; }
        }

        [Fact]
        public void TestStringToBson()
        {
            // these scalar values used to fail to be serialized
            // but the new ObjectSerializer can round trip them either at the top level or as properties
            foreach (var scalarValue in __scalarValues)
            {
                var json = scalarValue.ToJson();
                var rehydrated = BsonSerializer.Deserialize<object>(json);
                Assert.Equal(scalarValue, rehydrated);

                var bson = scalarValue.ToBson();
                rehydrated = BsonSerializer.Deserialize<object>(bson);
                Assert.Equal(scalarValue, rehydrated);

                var document = scalarValue.ToBsonDocument();
                rehydrated = BsonSerializer.Deserialize<object>(document);
                Assert.Equal(scalarValue, rehydrated);

                json = new C { ScalarValue = scalarValue }.ToJson();
                var c = BsonSerializer.Deserialize<C>(json);
                Assert.Equal(scalarValue, c.ScalarValue);

                bson = new C { ScalarValue = scalarValue }.ToBson();
                c = BsonSerializer.Deserialize<C>(bson);
                Assert.Equal(scalarValue, c.ScalarValue);

                document = new C { ScalarValue = scalarValue }.ToBsonDocument();
                c = BsonSerializer.Deserialize<C>(document);
                Assert.Equal(scalarValue, c.ScalarValue);
            }
        }
    }
}
