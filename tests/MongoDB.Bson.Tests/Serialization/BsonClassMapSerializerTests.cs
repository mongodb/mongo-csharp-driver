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

using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonClassMapSerializerTests
    {
        // public methods
        [Fact]
        public void Deserialize_should_throw_invalidOperationException_when_creator_returns_null()
        {
            var bsonClassMap = new BsonClassMap<MyModel>();
            bsonClassMap.SetCreator(() => null);
            bsonClassMap.Freeze();

            var subject = new BsonClassMapSerializer<MyModel>(bsonClassMap);

            using var reader = new JsonReader("{ \"_id\": \"just_an_id\" }");
            var context = BsonDeserializationContext.CreateRoot(reader);

            var exception = Record.Exception(() => subject.Deserialize(context));
            exception.Should().BeOfType<BsonSerializationException>();
        }

        [Fact]
        public void Deserialize_should_throw_when_no_creators_found()
        {
            var bsonClassMap = new BsonClassMap<ModelWithCtor>();
            bsonClassMap.AutoMap();
            bsonClassMap.Freeze();

            var subject = new BsonClassMapSerializer<ModelWithCtor>(bsonClassMap);

            using var reader = new JsonReader("{ \"_id\": \"just_an_id\" }");
            var context = BsonDeserializationContext.CreateRoot(reader);

            var exception = Record.Exception(() => subject.Deserialize(context));
            exception.Should().BeOfType<BsonSerializationException>()
                .Subject.Message.Should().Be($"No matching creator found for class {typeof(ModelWithCtor).FullName}");
        }

        // nested classes
        private class MyModel
        {
            public string Id { get; set; }
        }

        private class ModelWithCtor
        {
            private readonly string _myId;
            private readonly int _i;

            public ModelWithCtor(string id, int i)
            {
                _myId = id;
                _i = i;
            }

            public string Id => _myId;
            public int I => _i;
        }
    }
}
