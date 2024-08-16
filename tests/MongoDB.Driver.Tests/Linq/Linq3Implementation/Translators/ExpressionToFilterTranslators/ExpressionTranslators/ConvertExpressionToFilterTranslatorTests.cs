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
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    public class ConvertExpressionToFilterTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void Filter_using_convert_underlying_type_to_enum_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Where(p => (Enum)p.EnumAsInt == Enum.One);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { EnumAsInt : 1 } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
        }

        [Fact]
        public void Filter_using_convert_nullable_underlying_type_to_enum_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Where(p => (Enum)p.EnumAsNullableInt == Enum.One);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { EnumAsNullableInt : 1 } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
        }

        [Fact]
        public void Filter_using_convert_enum_to_underlying_type_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Where(p => (int)p.Enum == 2);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { Enum : 2 } }");

            var result = queryable.Single();
            result.Id.Should().Be(2);
        }

        [Fact]
        public void Filter_using_convert_nullable_enum_to_underlying_type_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Where(p => (int)p.NullableEnum == 2);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { NullableEnum : 2 } }");

            var result = queryable.Single();
            result.Id.Should().Be(2);
        }

        private IMongoCollection<Data> GetCollection()
        {
            var collection = GetCollection<Data>("test");
            CreateCollection(
                collection,
                new Data { Id = 1, Enum = Enum.One, NullableEnum = Enum.One, EnumAsInt = 1, EnumAsNullableInt = 1 },
                new Data { Id = 2, Enum = Enum.Two, NullableEnum = Enum.Two, EnumAsInt = 2, EnumAsNullableInt = 2 });
            return collection;
        }

        private class Data
        {
            public int Id { get; set; }
            public Enum Enum { get; set; }
            public Enum? NullableEnum { get; set; }
            public int EnumAsInt { get; set; }
            public int? EnumAsNullableInt { get; set; }
        }

        private enum Enum
        {
            One = 1,
            Two = 2
        }
    }
}
