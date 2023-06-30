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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class RoundMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void Math_round_double_should_work()
        {
            RequireServer.Check().Supports(Feature.Round);

            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(i => Math.Round(i.Double));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $round : '$Double' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(10.0, 10.0, 9.0);
        }

        [Fact]
        public void Math_round_double_with_digits_should_work()
        {
            RequireServer.Check().Supports(Feature.Round);

            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(i => Math.Round(i.Double, 1));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $round : ['$Double', 1] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(10.2, 9.7, 9.2);
        }

        [Fact]
        public void Math_round_decimal_should_work()
        {
            RequireServer.Check().Supports(Feature.Round);

            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(i => Math.Round(i.Decimal));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $round : '$Decimal' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(10.0m, 10.0m, 9.0m);
        }

        [Fact]
        public void Math_round_decimal_with_decimals_should_work()
        {
            RequireServer.Check().Supports(Feature.Round);

            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(i => Math.Round(i.Decimal, 1));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $round : ['$Decimal', 1] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(10.2m, 9.7m, 9.2m);
        }

        private IMongoCollection<Data> CreateCollection()
        {
            var collection = GetCollection<Data>("test");
            CreateCollection(
                collection,
                new Data { Double = 10.234, Decimal = 10.234m },
                new Data { Double = 9.66, Decimal = 9.66m },
                new Data { Double = 9.2, Decimal = 9.2m });
            return collection;
        }

        private class Data
        {
            public double Double { get; set; }
            [BsonRepresentation(BsonType.Decimal128)]
            public decimal Decimal { get; set; }
        }
    }
}
