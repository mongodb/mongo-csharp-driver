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

using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp102Tests
    {
        private class Test
        {
            public ObjectId Id { get; set; }
            public string Normal { get; set; }
            public string this[string item]
            {
                get { return "ignored"; }
                set { throw new InvalidOperationException(); }
            }
        }

        [Test]
        public void TestClassMap()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(Test));
            Assert.AreEqual(2, classMap.AllMemberMaps.Count());
            Assert.IsTrue(classMap.AllMemberMaps.Any(m => m.MemberName == "Id"));
            Assert.IsTrue(classMap.AllMemberMaps.Any(m => m.MemberName == "Normal"));
            Assert.AreEqual("Id", classMap.IdMemberMap.MemberName);

            var test = new Test { Normal = "normal" };
            var json = test.ToJson();
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), 'Normal' : 'normal' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = test.ToBson();
            var rehydrated = BsonSerializer.Deserialize<Test>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
