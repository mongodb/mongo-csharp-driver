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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class AnimalHierarchyWithAttributesTests
    {
        [BsonDiscriminator(RootClass = true)]
        [BsonKnownTypes(typeof(Bear), typeof(Cat))]
        public abstract class Animal
        {
            public ObjectId Id { get; set; }
            public int Age { get; set; }
            public string Name { get; set; }
        }

        public class Bear : Animal
        {
        }

        [BsonKnownTypes(typeof(Tiger), typeof(Lion))]
        public abstract class Cat : Animal
        {
        }

        public class Tiger : Cat
        {
        }

        public class Lion : Cat
        {
        }

        [Test]
        public void TestDeserializeBear()
        {
            var document = new BsonDocument
            {
                { "_id", ObjectId.Empty },
                { "_t", new BsonArray { "Animal", "Bear" } },
                { "Age", 123 },
                { "Name", "Panda Bear" }
            };

            var bson = document.ToBson();
            var rehydrated = (Bear)BsonSerializer.Deserialize<Animal>(bson);
            Assert.IsInstanceOf<Bear>(rehydrated);

            var json = rehydrated.ToJson<Animal>(DocumentSerializationOptions.SerializeIdFirstInstance);
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), '_t' : ['Animal', 'Bear'], 'Age' : 123, 'Name' : 'Panda Bear' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson<Animal>(DocumentSerializationOptions.SerializeIdFirstInstance)));
        }

        [Test]
        public void TestDeserializeTiger()
        {
            var document = new BsonDocument
            {
                { "_id", ObjectId.Empty },
                { "_t", new BsonArray { "Animal", "Cat", "Tiger" } },
                { "Age", 234 },
                { "Name", "Striped Tiger" }
            };

            var bson = document.ToBson();
            var rehydrated = (Tiger)BsonSerializer.Deserialize<Animal>(bson);
            Assert.IsInstanceOf<Tiger>(rehydrated);

            var json = rehydrated.ToJson<Animal>(DocumentSerializationOptions.SerializeIdFirstInstance);
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), '_t' : ['Animal', 'Cat', 'Tiger'], 'Age' : 234, 'Name' : 'Striped Tiger' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson<Animal>(DocumentSerializationOptions.SerializeIdFirstInstance)));
        }

        [Test]
        public void TestDeserializeLion()
        {
            var document = new BsonDocument
            {
                { "_id", ObjectId.Empty },
                { "_t", new BsonArray { "Animal", "Cat", "Lion" } },
                { "Age", 234 },
                { "Name", "King Lion" }
            };

            var bson = document.ToBson();
            var rehydrated = (Lion)BsonSerializer.Deserialize<Animal>(bson);
            Assert.IsInstanceOf<Lion>(rehydrated);

            var json = rehydrated.ToJson<Animal>(DocumentSerializationOptions.SerializeIdFirstInstance);
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), '_t' : ['Animal', 'Cat', 'Lion'], 'Age' : 234, 'Name' : 'King Lion' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson<Animal>(DocumentSerializationOptions.SerializeIdFirstInstance)));
        }
    }
}
