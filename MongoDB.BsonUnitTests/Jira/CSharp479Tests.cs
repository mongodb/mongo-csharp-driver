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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp479Tests
    {
        public class Test
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public string[] OtherIds { get; set;}
        }

        [Test]
        public void TestRoundTripping()
        {
            var id1 = ObjectId.GenerateNewId().ToString();
            var id2 = ObjectId.GenerateNewId().ToString();

            var test = new Test();
            test.OtherIds = new string[] { id1, id2 };

            var bson = test.ToBson();
            var rehydrated = BsonSerializer.Deserialize<Test>(bson);

            Assert.AreEqual(id1, test.OtherIds[0]);
            Assert.AreEqual(id2, test.OtherIds[1]);
        }
    }
}
