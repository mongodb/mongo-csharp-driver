/* Copyright 2010-2016 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
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

        [Fact]
        public void TestDuplicateElementInDerivedClass()
        {
            var ex = Record.Exception(() => BsonClassMap.LookupClassMap(typeof(D)));

            var expectedMessage = "The property 'N' of type 'MongoDB.Bson.Tests.Jira.CSharp293Tests+D' cannot use element name 'N' because it is already being used by field 'N' of type 'MongoDB.Bson.Tests.Jira.CSharp293Tests+C'.";
            Assert.IsType<BsonSerializationException>(ex);
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void TestDuplicateElementInSameClass()
        {
            var ex = Record.Exception(() => BsonClassMap.LookupClassMap(typeof(E)));

            var expectedMessage = "The property 'N2' of type 'MongoDB.Bson.Tests.Jira.CSharp293Tests+E' cannot use element name 'n' because it is already being used by field 'N1'.";
            Assert.IsType<BsonSerializationException>(ex);
            Assert.Equal(expectedMessage, ex.Message);
        }
    }
}
