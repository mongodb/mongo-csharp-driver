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
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonClassMapSerializerTests
    {
        private static readonly BsonClassMap __classMap1;
        private static readonly BsonClassMap __classMap2;

        static BsonClassMapSerializerTests()
        {
            __classMap1 = new BsonClassMap(typeof(MyModel));
            __classMap1.AutoMap();
            __classMap1.Freeze();

            __classMap2 = new BsonClassMap(typeof(MyModel));
            __classMap2.AutoMap();
            __classMap2.MapProperty("Id").SetSerializer(new StringSerializer(BsonType.ObjectId));
            __classMap2.Freeze();
        }

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
                .Subject.Message.Should().Be($"No matching creator found for class {typeof(ModelWithCtor).FullName}.");
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new BsonClassMapSerializer<MyModel>(__classMap1);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new BsonClassMapSerializer<MyModel>(__classMap1);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new BsonClassMapSerializer<MyModel>(__classMap1);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new BsonClassMapSerializer<MyModel>(__classMap1);
            var y = new BsonClassMapSerializer<MyModel>(__classMap1);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new BsonClassMapSerializer<MyModel>(__classMap1);
            var y = new BsonClassMapSerializer<MyModel>(__classMap2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new BsonClassMapSerializer<MyModel>(__classMap1);

            var result = x.GetHashCode();

            result.Should().Be(0);
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
