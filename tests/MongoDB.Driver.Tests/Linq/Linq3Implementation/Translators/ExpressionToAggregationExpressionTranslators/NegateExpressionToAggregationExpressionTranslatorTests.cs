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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    public class NegateExpressionToAggregationExpressionTranslatorTests: Linq3IntegrationTest
    {
        [Fact]
        public void Negate_int_should_work()
        {
            var collection = CreateCollection();
            var queryable = Queryable.Select(collection.AsQueryable(), i => -i.Int);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $subtract : [0, '$Int'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(10, -5, 0, int.MaxValue, -int.MaxValue);
        }

        [Fact]
        public void Negate_long_should_work()
        {
            var collection = CreateCollection();
            var queryable = Queryable.Select(collection.AsQueryable(), i => -i.Long);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $subtract : [0, '$Long'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(10L, -5L, 0L, long.MaxValue, -long.MaxValue);
        }

        [Fact]
        public void Negate_single_should_work()
        {
            var collection = CreateCollection();
            var queryable = Queryable.Select(collection.AsQueryable(), i => -i.Single);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $subtract : [0, '$Single'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(10F, -5F, 0F, float.MaxValue, -float.MaxValue);
        }

        [Fact]
        public void Negate_double_should_work()
        {
            var collection = CreateCollection();
            var queryable = Queryable.Select(collection.AsQueryable(), i => -i.Double);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $subtract : [0, '$Double'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(10.0, -5.0, 0.0, double.MaxValue, -double.MaxValue);
        }

        [Fact]
        public void Negate_decimal128_should_work()
        {
            var collection = CreateCollection();
            var queryable = Queryable.Select(collection.AsQueryable(), i => -i.DecimalAsDecimal128);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $subtract : [0, '$DecimalAsDecimal128'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(10.0m, -5.0m, 0.0m, decimal.MaxValue, -decimal.MaxValue);
        }

        [Theory]
        [ParameterAttributeData]
        public void Negate_decimal_as_string_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            var collection = CreateCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = Queryable.Select(collection.AsQueryable(translationOptions), i => -i.DecimalAsString);

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().Equal(10M, -5M, 0M, decimal.MaxValue, -decimal.MaxValue);
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
            }
        }

        private IMongoCollection<Data> CreateCollection()
        {
            var collection = GetCollection<Data>("test");
            CreateCollection(
                collection,
                new Data { Id = 1, Int = -10, Double = -10, Single = -10, Long = -10, DecimalAsString = -10, DecimalAsDecimal128 = -10},
                new Data { Id = 2, Int = 5, Double = 5, Single = 5, Long = 5, DecimalAsString = 5, DecimalAsDecimal128 = 5},
                new Data { Id = 3, Int = 0, Double = 0, Single = 0, Long = 0, DecimalAsString = 0, DecimalAsDecimal128 = 0},
                new Data { Id = 4, Int = -int.MaxValue, Double = -double.MaxValue, Single = -float.MaxValue, Long = -long.MaxValue, DecimalAsString = -decimal.MaxValue, DecimalAsDecimal128 = -decimal.MaxValue},
                new Data { Id = 5, Int = int.MaxValue, Double = double.MaxValue, Single = float.MaxValue, Long = long.MaxValue, DecimalAsString = decimal.MaxValue, DecimalAsDecimal128 = decimal.MaxValue});
            return collection;
        }

        private class Data
        {
            public int Id { get; set; }
            public int Int { get; set; }
            public long Long { get; set; }
            public float Single { get; set; }
            public double Double { get; set; }
            [BsonRepresentation(BsonType.String)]
            public decimal DecimalAsString { get; set; }
            [BsonRepresentation(BsonType.Decimal128)]
            public decimal DecimalAsDecimal128 { get; set; }
        }
    }
}
