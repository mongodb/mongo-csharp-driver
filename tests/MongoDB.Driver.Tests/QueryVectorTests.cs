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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class QueryVectorTests
    {
        [Theory]
        [MemberData(nameof(DataCtor))]
        public void Ctor_should_create_new_instance(Func<QueryVector> creator, BsonValue expectedBsonValue)
        {
            var vector = creator();
            vector.Vector.Should().Be(expectedBsonValue);
        }

        [Fact]
        public void Ctor_should_throw_on_null_binaryData()
        {
            BsonBinaryData bsonBinaryData = null;
            var exception = Record.Exception(() => new QueryVector(bsonBinaryData));
            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("bsonBinaryData");
        }

        [Fact]
        public void Ctor_should_throw_on_invalid_binaryData_subtype()
        {
            var bsonBinaryData = new BsonBinaryData([1], BsonBinarySubType.Binary);
            var exception = Record.Exception(() => new QueryVector(bsonBinaryData));
            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be(nameof(bsonBinaryData.SubType));
        }

        [Theory]
        [MemberData(nameof(DataImplicitCast))]
        public void Implicit_conversion_should_return_new_instance(Func<QueryVector> conversion, BsonValue expectedBsonValue)
        {
            var vector = conversion();
            vector.Vector.Should().Be(expectedBsonValue);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new double[0])]
        public void Implicit_conversion_should_throw_on_empty_array(double[] array)
        {
            var exception = Record.Exception(() => (QueryVector)array);
            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("array");
        }

        [Fact]
        public void QueryVectorBsonArray_should_use_correct_serializer()
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<QueryVectorBsonArray<float>>();
            serializer.Should().BeOfType<QueryVectorArraySerializer<float>>();
        }

        [Theory]
        [MemberData(nameof(DataImplicitCast))]
        public void Vector_should_be_serialized_correctly(Func<QueryVector> conversion, BsonValue expectedBsonValue)
        {
            var vector = conversion();

            var objectActual = new { Vector = vector.Vector };
            var objectExpected = new { Vector = expectedBsonValue };

            var bsonActual = objectActual.ToBson();
            var bsonExpected = objectExpected.ToBson();
            bsonActual.ShouldAllBeEquivalentTo(bsonExpected);
        }

        public static IEnumerable<object[]> DataImplicitCast =>
            [
                [() => (QueryVector)(new[] { 1.1, 2.2 }), ToBsonArray(new[] { 1.1, 2.2 })],
                [() => (QueryVector)(new[] { 1.1f, 2.2f }), ToBsonArray(new[] { 1.1f, 2.2f })],
                [() => (QueryVector)(new[] { 1, 2 }), ToBsonArray(new[] { 1.0, 2.0 })],
                [() => (QueryVector)new ReadOnlyMemory<double>([1.1, 2.2]), ToBsonArray(new[] { 1.1, 2.2 })],
                [() => (QueryVector)new ReadOnlyMemory<float>([1.1f, 2.2f]), ToBsonArray(new[] { 1.1f, 2.2f })],
                [() => (QueryVector)new ReadOnlyMemory<int>([1, 2]), ToBsonArray(new[] { 1, 2 })],
                [() => (QueryVector)new BinaryVectorInt8(new sbyte[] { 1, 2 }), new BinaryVectorInt8(new sbyte[] { 1, 2 }).ToBsonBinaryData()],
                [() => (QueryVector)new BinaryVectorFloat32(new float[] { 1.1f, 2.2f }), new BinaryVectorFloat32(new float[] { 1.1f, 2.2f }).ToBsonBinaryData()],
                [() => (QueryVector)new BinaryVectorPackedBit(new byte[] { 1, 2 }, 0), new BinaryVectorPackedBit(new byte[] { 1, 2 }, 0).ToBsonBinaryData()]
            ];

        public static IEnumerable<object[]> DataCtor =>
            [
                [() => new QueryVector(new[] { 1.1, 2.2 }), ToBsonArray(new[] { 1.1, 2.2 })],
                [() => new QueryVector(new[] { 1.1f, 2.2f }), ToBsonArray(new[] { 1.1f, 2.2f })],
                [() => new QueryVector(new[] { 1, 2 }), ToBsonArray(new[] { 1.0, 2.0 })],
                [() => new QueryVector(new ReadOnlyMemory<double>([1.1, 2.2])), ToBsonArray(new[] { 1.1, 2.2 })],
                [() => new QueryVector(new ReadOnlyMemory<float>([1.1f, 2.2f])), ToBsonArray(new[] { 1.1f, 2.2f })],
                [() => new QueryVector(new ReadOnlyMemory<int>([1, 2])), ToBsonArray(new[] { 1, 2 })],
                [() => new QueryVector(new BinaryVectorInt8(new sbyte[] { 1, 2 }).ToBsonBinaryData()), new BinaryVectorInt8(new sbyte[] { 1, 2 }).ToBsonBinaryData()]
            ];

        private static BsonArray ToBsonArray<T>(T[] array) where T : struct, IConvertible =>
            new(array.Select(v => v.ToDouble(null)));
    }
}
