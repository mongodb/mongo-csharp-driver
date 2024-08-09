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
using System.Globalization;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class DateFromStringMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData(1, "2023-12-26T12:34:56Z")]
        [InlineData(2, "throws:FormatException")]
        [InlineData(3, "throws:FormatException")]
        [InlineData(4, "throws:MongoCommandException")]
        public void DateTime_Parse_should_work(int id, string expectedResult)
        {
            var collection = GetCollection();

            // technically this Parse method is not an Mql method but this test is to confirm that Parse and DateFromString behave the same
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => DateTime.Parse(x.S));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    "{ $project : { _v : { $dateFromString : { dateString : '$S' } }, _id : 0 } }"
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(1, "2023-12-26T12:34:56Z")]
        [InlineData(2, "throws:FormatException")]
        [InlineData(3, "throws:FormatException")]
        [InlineData(4, "throws:MongoCommandException")]
        public void MongoDBFunctions_DateFromString_should_work(int id, string expectedResult)
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.DateFromString(x.S));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    "{ $project : { _v : { $dateFromString : { dateString : '$S' } }, _id : 0 } }"
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(1, "2023-12-26T12:34:56Z")]
        [InlineData(2, "throws:FormatException")]
        [InlineData(3, "throws:FormatException")]
        [InlineData(4, "throws:MongoCommandException")]
        public void MongoDBFunctions_DateFromString_with_format_should_work(int id, string expectedResult)
        {
            RequireServer.Check().Supports(Feature.DateFromStringFormatArgument);
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.DateFromString(x.S, x.F));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    "{ $project : { _v : { $dateFromString : { dateString : '$S', format : '$F' } }, _id : 0 } }"
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(1, "2023-12-26T12:34:56Z")]
        [InlineData(2, "throws:FormatException")]
        [InlineData(3, "throws:FormatException")]
        [InlineData(4, "throws:MongoCommandException")]
        public void MongoDBFunctions_DateFromString_with_format_and_timezone_should_work(int id, string expectedResult)
        {
            RequireServer.Check().Supports(Feature.DateFromStringFormatArgument);
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.DateFromString(x.S, x.F, x.TZ));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    "{ $project : { _v : { $dateFromString : { dateString : '$S', format : '$F', timezone : '$TZ' } }, _id : 0 } }"
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(1, "2023-12-26T12:34:56Z")]
        [InlineData(2, "0001-01-01T00:00:00Z")]
        [InlineData(3, "0001-01-01T00:00:00Z")]
        [InlineData(4, "1111-11-11T11:11:11Z")]
        [InlineData(5, "default")]
        [InlineData(6, "default")]
        [InlineData(7, "1111-11-11T11:11:11Z")]
        public void MongoDBFunctions_DateFromString_with_format_and_timezone_and_onError_and_onNull_should_work(int id, string expectedResult)
        {
            RequireServer.Check().Supports(Feature.DateFromStringFormatArgument);
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.DateFromString(x.S, x.F, x.TZ, x.OnError, x.OnNull));

            var expectedStages = 
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    "{ $project : { _v : { $dateFromString : { dateString : '$S', format : '$F', timezone : '$TZ', onError : '$OnError', onNull : '$OnNull' } }, _id : 0 } }"
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        private void AssertOutcome(
            IMongoCollection<C> collection,
            IMongoQueryable<DateTime> queryable,
            string[] expectedStages,
            string expectedResult)
        {
            AssertOutcome(collection, queryable, expectedStages, expectedResult, AssertDateTimeResult);
        }

        private void AssertOutcome(
            IMongoCollection<C> collection,
            IMongoQueryable<DateTime?> queryable,
            string[] expectedStages,
            string expectedResult)
        {
            AssertOutcome(collection, queryable, expectedStages, expectedResult, AssertNullableDateTimeResult);
        }

        private void AssertOutcome<TResult>(
            IMongoCollection<C> collection,
            IMongoQueryable<TResult> queryable,
            string[] expectedStages,
            string expectedResult,
            Action<TResult, string> assertResult)
        {
            List<BsonDocument> stages = null;
            TResult result = default;
            var exception = Record.Exception(() => stages = Translate(collection, queryable));
            if (exception == null)
            {
                AssertStages(stages, expectedStages);
                exception = Record.Exception(() => result = queryable.Single());
            }

            if (expectedResult.StartsWith("throws:"))
            {
                var expectedExceptionType = expectedResult.Substring(7);
                exception.GetType().Name.Should().Be(expectedExceptionType);
            }
            else
            {
                exception.Should().BeNull();
                assertResult(result, expectedResult);
            }
        }

        private void AssertDateTimeResult(DateTime result, string expectedResult)
        {
            result.Should().Be(DateTime.Parse(expectedResult, null, DateTimeStyles.AdjustToUniversal));
        }

        private void AssertNullableDateTimeResult(DateTime? result, string expectedResult)
        {
            result.Should().Be(expectedResult == "default" ? (DateTime?)default : DateTime.Parse(expectedResult, null, DateTimeStyles.AdjustToUniversal));
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection.Database.GetCollection<BsonDocument>("test"),
                BsonDocument.Parse("{ _id : 1, S : '2023-12-26T12:34:56', F : '%Y-%m-%dT%H:%M:%S', TZ : 'UTC', OnError : { $date : '0001-01-01T00:00:00' }, OnNull : { $date : '0001-01-01T00:00:00' } }"),
                BsonDocument.Parse("{ _id : 2, F : '%Y-%m-%dT%H:%M:%S', TZ : 'UTC', OnError : { $date : '0001-01-01T00:00:00' }, OnNull : { $date : '0001-01-01T00:00:00' } }"),
                BsonDocument.Parse("{ _id : 3, S : null, F : '%Y-%m-%dT%H:%M:%S', TZ : 'UTC', OnError : { $date : '0001-01-01T00:00:00' }, OnNull : { $date : '0001-01-01T00:00:00' } }"),
                BsonDocument.Parse("{ _id : 4, S : 'error', F : '%Y-%m-%dT%H:%M:%S', TZ : 'UTC', OnError : { $date : '1111-11-11T11:11:11' }, OnNull : { $date : '0001-01-01T00:00:00' } }"),
                BsonDocument.Parse("{ _id : 5, F : '%Y-%m-%dT%H:%M:%S', TZ : 'UTC', OnError : { $date : '0001-01-01T00:00:00' }, OnNull : null }"),
                BsonDocument.Parse("{ _id : 6, S : null, F : '%Y-%m-%dT%H:%M:%S', TZ : 'UTC', OnError : { $date : '0001-01-01T00:00:00' }, OnNull : null }"),
                BsonDocument.Parse("{ _id : 7, S : 'error', F : '%Y-%m-%dT%H:%M:%S', TZ : 'UTC', OnError : { $date : '1111-11-11T11:11:11' }, OnNull : null }"));
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public string S { get; set; }
            public string F { get; set; }
            public string TZ { get; set; }
            public DateTime? OnError { get; set; }
            public DateTime? OnNull { get; set; }
        }
    }
}
