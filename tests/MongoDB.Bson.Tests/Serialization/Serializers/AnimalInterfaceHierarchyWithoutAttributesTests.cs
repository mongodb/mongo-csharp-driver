/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class AnimalInterfaceHierarchyWithoutAttributesTests
    {
        public interface IAnimal
        {
            public ObjectId Id { get; set; }
            public int Age { get; set; }
            public string Name { get; set; }
        }

        public class Bear : IAnimal
        {
            public ObjectId Id { get; set; }
            public int Age { get; set; }
            public string Name { get; set; }
        }

        public interface  ICat : IAnimal
        {
        }

        public class Tiger : ICat
        {
            public ObjectId Id { get; set; }
            public int Age { get; set; }
            public string Name { get; set; }
        }

        public class Lion : ICat
        {
            public ObjectId Id { get; set; }
            public int Age { get; set; }
            public string Name { get; set; }
        }

        static AnimalInterfaceHierarchyWithoutAttributesTests()
        {
            BsonSerializer.RegisterSerializer(new DiscriminatedInterfaceSerializer<IAnimal>(new InterfaceDiscriminatorConvention<IAnimal>("_t")));
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
                { "_t", new BsonArray { "IAnimal", "Bear" } },
                { "Age", 123 },
                { "Name", "Panda Bear" }
            };

            var bson = document.ToBson();
            var rehydrated = (Bear)BsonSerializer.Deserialize<IAnimal>(bson);
            Assert.IsType<Bear>(rehydrated);

            var json = rehydrated.ToJson<IAnimal>(args: new BsonSerializationArgs { SerializeIdFirst = true });
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), '_t' : ['IAnimal', 'Bear'], 'Age' : 123, 'Name' : 'Panda Bear' }".Replace("'", "\"");
            Assert.Equal(expected, json);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<IAnimal>(args: new BsonSerializationArgs { SerializeIdFirst = true })));
        }

        [Fact]
        public void TestDeserializeTiger()
        {
            var document = new BsonDocument
            {
                { "_id", ObjectId.Empty },
                { "_t", new BsonArray { "IAnimal", "ICat", "Tiger" } },
                { "Age", 234 },
                { "Name", "Striped Tiger" }
            };

            var bson = document.ToBson();
            var rehydrated = (Tiger)BsonSerializer.Deserialize<IAnimal>(bson);
            Assert.IsType<Tiger>(rehydrated);

            var json = rehydrated.ToJson<IAnimal>(args: new BsonSerializationArgs { SerializeIdFirst = true });
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), '_t' : ['IAnimal', 'ICat', 'Tiger'], 'Age' : 234, 'Name' : 'Striped Tiger' }".Replace("'", "\"");
            Assert.Equal(expected, json);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<IAnimal>(args: new BsonSerializationArgs { SerializeIdFirst = true })));
        }

        [Fact]
        public void TestDeserializeLion()
        {
            var document = new BsonDocument
            {
                { "_id", ObjectId.Empty },
                { "_t", new BsonArray { "IAnimal", "ICat", "Lion" } },
                { "Age", 234 },
                { "Name", "King Lion" }
            };

            var bson = document.ToBson();
            var rehydrated = (Lion)BsonSerializer.Deserialize<IAnimal>(bson);
            Assert.IsType<Lion>(rehydrated);

            var json = rehydrated.ToJson<IAnimal>(args: new BsonSerializationArgs { SerializeIdFirst = true });
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), '_t' : ['IAnimal', 'ICat', 'Lion'], 'Age' : 234, 'Name' : 'King Lion' }".Replace("'", "\"");
            Assert.Equal(expected, json);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<IAnimal>(args: new BsonSerializationArgs { SerializeIdFirst = true })));
        }
    }
}
