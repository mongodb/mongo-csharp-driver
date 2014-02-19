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

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp313Tests
    {
        private static object[] __scalarValues = new object[]
        {
            new BsonBinaryData(new byte[] { 1, 2, 3 }),
            true,
            DateTime.UtcNow,
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

        [Test]
        public void TestStringToBson()
        {
            foreach (var scalarValue in __scalarValues)
            {
                Assert.Throws<InvalidOperationException>(() => { var bson = scalarValue.ToBson(); });
                Assert.Throws<InvalidOperationException>(() => { var bson = scalarValue.ToBsonDocument(); });
            }
        }
    }
}
