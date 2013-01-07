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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp104Tests
    {
        private static bool __firstTime = true;

#pragma warning disable 169 // never used
        private class Test
        {
            static Test()
            {
            }

            private const string _literal = "constant";
            private readonly string _readOnly;
            private string _getOnly;
            private string _setOnly;

            public Test(string value)
            {
                _getOnly = value;
            }

            public ObjectId Id { get; set; }
            public string GetOnly { get { return _getOnly; } }
            public string SetOnly { set { _setOnly = value; } }
        }
#pragma warning restore

        [Test]
        public void TestClassMap()
        {
            // this test passes normally when the Test class is automapped
            // uncomment all or parts of the class map initialization code to test
            // the exceptions thrown for each non-compliant field or property

            if (__firstTime)
            {
                BsonClassMap.RegisterClassMap<Test>(cm =>
                {
                    cm.AutoMap();
                    // cm.MapField("literal");
                    // cm.MapField("readOnly");
                    // cm.MapField("notfound");
                    // cm.MapProperty("GetOnly");
                    // cm.MapProperty("SetOnly");
                    // cm.MapProperty("notfound");
                    // cm.MapMember(null);
                });
                __firstTime = false;
            }

            var test = new Test("x") { SetOnly = "y" };
            var json = test.ToJson();
            var expected = "{ '_id' : ObjectId('000000000000000000000000') }".Replace("'", "\"");
            // Assert.AreEqual(expected, json);

            var bson = test.ToBson();
            var rehydrated = BsonSerializer.Deserialize<Test>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
