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
        public void Ctor_should_create_new_instance(Func<QueryVector> creator, BsonArray expectedArray)
        {
            var vector = creator();
            vector.Array.Should().Be(expectedArray);
        }

        [Theory]
        [MemberData(nameof(DataImplicitCast))]
        public void Implicit_conversion_should_return_new_instance(Func<QueryVector> conversion, BsonArray expectedArray)
        {
            var vector = conversion();
            vector.Array.Should().Be(expectedArray);
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
        public void Vector_should_be_serialized_correctly(Func<QueryVector> conversion, BsonArray expectedArray)
        {
            var vector = conversion();

            var objectActual = new { Array = vector.Array };
            var objectExpected = new { Array = expectedArray };

            var bsonActual = objectActual.ToBson();
            var bsonExpected = objectExpected.ToBson();
            bsonActual.ShouldAllBeEquivalentTo(bsonExpected);
        }

        public static IEnumerable<object[]> DataImplicitCast =>
            new[]
            {
                new object[] { () => (QueryVector)(new[] { 1.1, 2.2 }), ToBsonArray(new[] { 1.1, 2.2 }) },
                new object[] { () => (QueryVector)(new[] { 1.1f, 2.2f }), ToBsonArray(new[] { 1.1f, 2.2f }) },
                new object[] { () => (QueryVector)(new[] { 1, 2 }), ToBsonArray(new[] { 1.0, 2.0 }) },
                new object[] { () => (QueryVector)(new ReadOnlyMemory<double>(new[] { 1.1, 2.2 })), ToBsonArray(new[] { 1.1, 2.2 }) },
                new object[] { () => (QueryVector)(new ReadOnlyMemory<float>(new[] { 1.1f, 2.2f })), ToBsonArray(new[] { 1.1f, 2.2f }) },
                new object[] { () => (QueryVector)(new ReadOnlyMemory<int>(new[] { 1, 2 })), ToBsonArray(new[] { 1, 2 }) },
            };

        public static IEnumerable<object[]> DataCtor =>
            new[]
            {
                new object[] { () => new QueryVector(new[] { 1.1, 2.2 }), ToBsonArray(new[] { 1.1, 2.2 }) },
                new object[] { () => new QueryVector(new[] { 1.1f, 2.2f }), ToBsonArray(new[] { 1.1f, 2.2f }) },
                new object[] { () => new QueryVector(new[] { 1, 2 }), ToBsonArray(new[] { 1.0, 2.0 }) },
                new object[] { () => new QueryVector(new ReadOnlyMemory<double>(new[] { 1.1, 2.2 })), ToBsonArray(new[] { 1.1, 2.2 }) },
                new object[] { () => new QueryVector(new ReadOnlyMemory<float>(new[] { 1.1f, 2.2f })), ToBsonArray(new[] { 1.1f, 2.2f }) },
                new object[] { () => new QueryVector(new ReadOnlyMemory<int>(new[] { 1, 2 })), ToBsonArray(new[] { 1, 2 }) },
            };

        private static BsonArray ToBsonArray<T>(T[] array) where T : struct, IConvertible =>
            new BsonArray(array.Select(v => v.ToDouble(null)));
    }
}
