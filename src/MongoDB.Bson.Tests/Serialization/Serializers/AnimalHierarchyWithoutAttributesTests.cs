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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class AnimalHierarchyWithoutAttributesTests
    {
        public abstract class Animal
        {
            public ObjectId Id { get; set; }
            public int Age { get; set; }
            public string Name { get; set; }
        }

        public class Bear : Animal
        {
        }

        public abstract class Cat : Animal
        {
        }

        public class Tiger : Cat
        {
        }

        public class Lion : Cat
        {
        }

        static AnimalHierarchyWithoutAttributesTests()
        {
            BsonClassMap.RegisterClassMap<Animal>(cm => { cm.AutoMap(); cm.SetIsRootClass(true); });
            BsonClassMap.RegisterClassMap<Bear>();
            BsonClassMap.RegisterClassMap<Tiger>();
            BsonClassMap.RegisterClassMap<Lion>();
        }

        [Fact]
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
            Assert.IsType<Bear>(rehydrated);

            var json = rehydrated.ToJson<Animal>(args: new BsonSerializationArgs { SerializeIdFirst = true });
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), '_t' : ['Animal', 'Bear'], 'Age' : 123, 'Name' : 'Panda Bear' }".Replace("'", "\"");
            Assert.Equal(expected, json);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<Animal>(args: new BsonSerializationArgs { SerializeIdFirst = true })));
        }

        [Fact]
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
            Assert.IsType<Tiger>(rehydrated);

            var json = rehydrated.ToJson<Animal>(args: new BsonSerializationArgs { SerializeIdFirst = true });
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), '_t' : ['Animal', 'Cat', 'Tiger'], 'Age' : 234, 'Name' : 'Striped Tiger' }".Replace("'", "\"");
            Assert.Equal(expected, json);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<Animal>(args: new BsonSerializationArgs { SerializeIdFirst = true })));
        }

        [Fact]
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
            Assert.IsType<Lion>(rehydrated);

            var json = rehydrated.ToJson<Animal>(args: new BsonSerializationArgs { SerializeIdFirst = true });
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), '_t' : ['Animal', 'Cat', 'Lion'], 'Age' : 234, 'Name' : 'King Lion' }".Replace("'", "\"");
            Assert.Equal(expected, json);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<Animal>(args: new BsonSerializationArgs { SerializeIdFirst = true })));
        }
    }
}
