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

using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira.CSharp151
{
    [TestFixture]
    public class CSharp151Tests
    {
        public class Doc
        {
            public decimal Value { get; set; }
        }

        [Test]
        public void TestDeserializeDouble()
        {
            var json = "{ 'Value' : 1.23 }".Replace("'", "\"");
            var doc = BsonSerializer.Deserialize<Doc>(json);
            Assert.AreEqual(1.23m, doc.Value);
        }

        [Test]
        public void TestDeserializeInt32()
        {
            var json = "{ 'Value' : 123 }".Replace("'", "\"");
            var doc = BsonSerializer.Deserialize<Doc>(json);
            Assert.AreEqual(123m, doc.Value);
        }

        [Test]
        public void TestDeserializeInt64()
        {
            var json = "{ 'Value' : 12345678900 }".Replace("'", "\"");
            var doc = BsonSerializer.Deserialize<Doc>(json);
            Assert.AreEqual(12345678900m, doc.Value);
        }
    }
}
