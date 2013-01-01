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

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp70Tests
    {
        private class TestClass
        {
            public string PrivateSetter { get; private set; }
            public string PrivateGetter { private get; set; }
        }

        [Test]
        public void TestThatPrivateSettersAreValid()
        {
            var classMap = new BsonClassMap<TestClass>(c => c.AutoMap());

            var setter = classMap.GetMemberMap(x => x.PrivateSetter).Setter;
        }

        [Test]
        public void TestThatPrivateGettersAreValid()
        {
            var classMap = new BsonClassMap<TestClass>(c => c.AutoMap());

            var getter = classMap.GetMemberMap("PrivateGetter").Getter;
        }
    }
}
