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

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    public class ConvertExpressionToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void Should_translate_to_derived_class_on_method_call()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(p => new DerivedClass
                {
                    Id = p.Id,
                    A = ((DerivedClass)p).A.ToUpper()
                });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ '$project' : { _id : '$_id', A : { '$toUpper' : '$A' } } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
            result.A.Should().Be("ABC");
        }

        [Fact]
        public void Should_translate_to_derived_class_on_projection()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(p => new DerivedClass()
                {
                    Id = p.Id,
                    A = ((DerivedClass)p).A
                });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ '$project' : { _id : '$_id', A : '$A' } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
            result.A.Should().Be("abc");
        }

        [Fact]
        public void Project_using_convert_underlying_type_to_enum_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(p => new ProjectedModel
                {
                    Id = p.Id,
                    Enum = (Enum)p.EnumAsInt,
                    EnumComparisonResult = (Enum)p.EnumAsInt == Enum.Two,
                });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ '$project' : { _id : '$_id', Enum : '$EnumAsInt', EnumComparisonResult : { $eq : ['$EnumAsInt', 2] } } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
            result.Enum.Should().Be(Enum.Two);
        }

        [Fact]
        public void Project_using_convert_nullable_underlying_type_to_enum_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(p => new ProjectedModel
                {
                    Id = p.Id,
                    Enum = (Enum)p.EnumAsNullableInt,
                    EnumComparisonResult = (Enum)p.EnumAsNullableInt == Enum.Two,
                });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ '$project' : { _id : '$_id', Enum : '$EnumAsNullableInt', EnumComparisonResult : { $eq : ['$EnumAsNullableInt', 2] } } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
            result.Enum.Should().Be(Enum.Two);
        }

        [Fact]
        public void Project_using_convert_nullable_underlying_type_to_nullable_enum_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(p => new ProjectedModel
                {
                    Id = p.Id,
                    NullableEnum = (Enum?)p.EnumAsNullableInt
                });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ '$project' : { _id : '$_id', NullableEnum : '$EnumAsNullableInt' } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
            result.NullableEnum.Should().Be(Enum.Two);
        }

        [Fact]
        public void Project_using_convert_enum_to_underlying_type_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(p => new ProjectedModel
                {
                    Id = p.Id,
                    EnumAsInt = (int)p.Enum
                });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ '$project' : { _id : '$_id', EnumAsInt : '$Enum' } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
            result.EnumAsInt.Should().Be(2);
        }

        [Fact]
        public void Project_using_convert_nullable_enum_to_underlying_type_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(p => new ProjectedModel
                {
                    Id = p.Id,
                    EnumAsInt = (int)p.NullableEnum
                });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ '$project' : { _id : '$_id', EnumAsInt : '$NullableEnum' } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
            result.EnumAsInt.Should().Be(2);
        }

        [Fact]
        public void Project_using_convert_nullable_enum_to_nullable_underlying_type_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(p => new ProjectedModel
                {
                    Id = p.Id,
                    EnumAsNullableInt = (int)p.NullableEnum
                });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ '$project' : { _id : '$_id', EnumAsNullableInt : '$NullableEnum' } }");

            var result = queryable.Single();
            result.Id.Should().Be(1);
            result.EnumAsNullableInt.Should().Be(2);
        }



        private IMongoCollection<BaseClass> GetCollection()
        {
            var collection = GetCollection<BaseClass>("test");
            CreateCollection(collection, new DerivedClass()
            {
                Id = 1,
                A = "abc",
                Enum = Enum.Two,
                NullableEnum = Enum.Two,
                EnumAsInt = 2,
                EnumAsNullableInt = 2
            });
            return collection;
        }

        private class BaseClass
        {
            public int Id { get; set; }
            public Enum Enum { get; set; }
            public Enum? NullableEnum { get; set; }
            public int EnumAsInt { get; set; }
            public int? EnumAsNullableInt { get; set; }
        }

        private class DerivedClass : BaseClass
        {
            public string A { get; set; }
        }

        private class ProjectedModel
        {
            public int Id { get; set; }
            public Enum Enum { get; set; }
            public Enum? NullableEnum { get; set; }
            public int EnumAsInt { get; set; }
            public int? EnumAsNullableInt { get; set; }
            public bool EnumComparisonResult { get; set; }
        }

        private enum Enum
        {
            One = 1,
            Two = 2
        }
    }
}
