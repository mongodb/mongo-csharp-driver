/* Copyright 2016-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq3;
using MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators;
using MongoDB.Driver.Tests;
using Xunit;

namespace Tests.MongoDB.Driver.Linq3.Legacy
{
    public class MongoQueryableIntComparedToNullableIntWithStringRepresentationTests
    {
        private static readonly IMongoClient __client;
        private static readonly IMongoCollection<C> __collection;
        private static readonly IMongoDatabase __database;

        static MongoQueryableIntComparedToNullableIntWithStringRepresentationTests()
        {
            __client = DriverTestConfiguration.Client;
            __database = __client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            __collection = __database.GetCollection<C>(DriverTestConfiguration.CollectionNamespace.CollectionName);
        }

        public class C
        {
            [BsonRepresentation(BsonType.String)]
            public int I { get; set; }
        }

        [Theory]
        [InlineData(1, "{ \"I\" : \"1\" }")]
        [InlineData(null, "{ \"I\" : null }")]
        public void Where_operator_equal_should_render_correctly(int? value, string expectedFilter)
        {
            var subject = __collection.AsQueryable3();

            var queryable = subject.Where(x => x.I == value);

            AssertFilter(queryable, expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ \"I\" : { \"$gt\" : \"1\" } }")]
        [InlineData(null, "{ \"I\" : { \"$gt\" : null } }")]
        public void Where_operator_greater_than_should_render_correctly(int? value, string expectedFilter)
        {
            var subject = __collection.AsQueryable3();

            var queryable = subject.Where(x => x.I > value);

            AssertFilter(queryable, expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ \"I\" : { \"$gte\" : \"1\" } }")]
        [InlineData(null, "{ \"I\" : { \"$gte\" : null } }")]
        public void Where_operator_greater_than_or_equal_should_render_correctly(int? value, string expectedFilter)
        {
            var subject = __collection.AsQueryable3();

            var queryable = subject.Where(x => x.I >= value);

            AssertFilter(queryable, expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ \"I\" : { \"$lt\" : \"1\" } }")]
        [InlineData(null, "{ \"I\" : { \"$lt\" : null } }")]
        public void Where_operator_less_than_should_render_correctly(int? value, string expectedFilter)
        {
            var subject = __collection.AsQueryable3();

            var queryable = subject.Where(x => x.I < value);

            AssertFilter(queryable, expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ \"I\" : { \"$lte\" : \"1\" } }")]
        [InlineData(null, "{ \"I\" : { \"$lte\" : null } }")]
        public void Where_operator_less_than_or_equal_should_render_correctly(int? value, string expectedFilter)
        {
            var subject = __collection.AsQueryable3();

            var queryable = subject.Where(x => x.I <= value);

            AssertFilter(queryable, expectedFilter);
        }

        [Theory]
        [InlineData(1, "{ \"I\" : { \"$ne\" : \"1\" } }")]
        [InlineData(null, "{ \"I\" : { \"$ne\" : null } }")]
        public void Where_operator_not_equal_should_render_correctly(int? value, string expectedFilter)
        {
            var subject = __collection.AsQueryable3();

            var queryable = subject.Where(x => x.I != value);

            AssertFilter(queryable, expectedFilter);
        }

        // private methods
        private void AssertFilter<T>(IQueryable<T> queryable, string expectedFilter)
        {
            var stages = Translate(queryable);
            stages.Should().HaveCount(1);
            stages[0].Should().Be($"{{ \"$match\" : {expectedFilter} }}");
        }

        private BsonDocument[] Translate<T>(IQueryable<T> queryable)
        {
            var provider = (MongoQueryProvider<T>)queryable.Provider;
            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<T, T>(provider, queryable.Expression);
            return executableQuery.Pipeline.Stages.Select(s => (BsonDocument)s.Render()).ToArray();
        }
    }
}
