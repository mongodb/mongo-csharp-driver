/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class BsonValueSerializerBaseTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);
            var y = new DerivedFromConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);
            var y = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = (BsonValueSerializerBase<BsonInt32>)new ConcreteBsonValueSerializerBase<BsonInt32>(BsonType.Int32);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class ConcreteBsonValueSerializerBase<TBsonValue> : BsonValueSerializerBase<TBsonValue>
            where TBsonValue : BsonValue
        {
            public ConcreteBsonValueSerializerBase(BsonType representation) : base(representation) { }

            protected override TBsonValue DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args) => throw new System.NotImplementedException();
            protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, TBsonValue value) => throw new System.NotImplementedException();
        }

        public class DerivedFromConcreteBsonValueSerializerBase<TBsonValue> : ConcreteBsonValueSerializerBase<TBsonValue>
            where TBsonValue : BsonValue
        {
            public DerivedFromConcreteBsonValueSerializerBase(BsonType representation) : base(representation) { }
        }
    }

    public static class BsonValueSerializerBaseReflector
    {
        public static BsonType? _bsonType<TBsonValue>(this BsonValueSerializerBase<TBsonValue> obj) where TBsonValue : BsonValue
            => (BsonType?)Reflector.GetFieldValue(obj, nameof(_bsonType));
    }
}
