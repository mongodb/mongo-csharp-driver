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
    public class CSharp293Tests
    {
        public class C
        {
            public ObjectId Id;
            public int N;
        }

        public class D : C
        {
            public new int N { get; set; }
        }

        public class E
        {
            public ObjectId Id;
            [BsonElement("n")]
            public int N1;
            [BsonElement("n")]
            public int N2 { get; set; }
        }

        [Test]
        public void TestDuplicateElementInDerivedClass()
        {
            var message = "The property 'N' of class 'MongoDB.BsonUnitTests.Jira.D' cannot use element name 'N' because it already being used by field 'N' of class 'MongoDB.BsonUnitTests.Jira.C'.";
            Assert.Throws<BsonSerializationException>(() => BsonClassMap.LookupClassMap(typeof(D)), message);
        }

        [Test]
        public void TestDuplicateElementInSameClass()
        {
            var message = "The property 'N2' of class 'MongoDB.BsonUnitTests.Jira.E' cannot use element name 'n' because it already being used by field 'N1'.";
            Assert.Throws<BsonSerializationException>(() => BsonClassMap.LookupClassMap(typeof(E)), message);
        }
    }
}
