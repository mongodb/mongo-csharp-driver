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
using MongoDB.Bson.Serialization.Options;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.Serializers
{
    [TestFixture(Description = "Derived classes should put timestamp in the second position for server to automatically fill in the value.")]
    public class ObjectMitTimestampSerializerTests
    {
        public void FixtureSetUp() {}

        class C1
        {
            public BsonTimestamp Timestamp;
            public int Id;
        }

        class D1 : C1
        {
            public int X1;
            public int X2;
        }

        [Test]
        public void TimestampInTheFirstPosition()
        {
            AssertJson(
                new D1 { Timestamp = new BsonTimestamp(0) }, 
                "{ '_id' : 0, 'Timestamp' : { '$timestamp' : NumberLong(0) }, '_t' : 'D1', 'X1' : 0, 'X2' : 0 }");
        }

        class C2
        {
            public int Id;
            public BsonTimestamp Timestamp;
        }

        class D2 : C2
        {
            public int X1;
            public int X2;
        }

        [Test]
        public void TimestampInTheSecondPosition()
        {
            AssertJson(
                new D2 { Timestamp = new BsonTimestamp(0) }, 
                "{ '_id' : 0, 'Timestamp' : { '$timestamp' : NumberLong(0) }, '_t' : 'D2', 'X1' : 0, 'X2' : 0 }");
        }

        [Test]
        public void TimestampInTheSecondPosition_DoNotSerializeIdFirst()
        {
            AssertJson(
                new D2 { Timestamp = new BsonTimestamp(0) }, 
                "{ '_t' : 'D2', '_id' : 0, 'Timestamp' : { '$timestamp' : NumberLong(0) }, 'X1' : 0, 'X2' : 0 }",
                DocumentSerializationOptions.Defaults);
        }

        class C3
        {
            public int Id;
            public int X;
            public BsonTimestamp Timestamp;
        }

        class D3 : C3 { }

        [Test]
        public void TimestampInTheThirdPosition()
        {
            AssertJson(
                new D3 { Timestamp = new BsonTimestamp(0) }, 
                "{ '_id' : 0, '_t' : 'D3', 'X' : 0, 'Timestamp' : { '$timestamp' : NumberLong(0) } }");
        }

        class C4
        {
            public int X;
            public BsonTimestamp Timestamp;
            public int Id;
        }

        class D4 : C4 { }

        [Test]
        public void TimestampInTheSecondPositionNotAfterId()
        {
            AssertJson(
                new D4 { Timestamp = new BsonTimestamp(0) }, 
                "{ '_id' : 0, '_t' : 'D4', 'X' : 0, 'Timestamp' : { '$timestamp' : NumberLong(0) } }");
        }

        private class C5
        {
            public int X1;
            public int X2;
        }

        private class D5 : C5
        {
            public int Id;
            public BsonTimestamp Timestamp;
        }

        [Test]
        public void TimestampInInDerivedClass()
        {
            AssertJson(
                new D5 { Timestamp = new BsonTimestamp(0) }, 
                "{ '_id' : 0, '_t' : 'D5', 'X1' : 0, 'X2' : 0, 'Timestamp' : { '$timestamp' : NumberLong(0) } }");
        }

        private void AssertJson(object o, string exptectedJson)
        {
            AssertJson(o, exptectedJson, DocumentSerializationOptions.SerializeIdFirstInstance);
        }

        private void AssertJson(object o, string exptectedJson, IBsonSerializationOptions options) 
        {
            var actual = o.ToJson(o.GetType().BaseType, options);
            Assert.That(actual, Is.EqualTo(exptectedJson.Replace("'", "\"")));
        }
    }
}
