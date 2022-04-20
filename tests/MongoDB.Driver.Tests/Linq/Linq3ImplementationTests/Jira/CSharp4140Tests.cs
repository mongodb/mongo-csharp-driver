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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4140Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData("abc")]
        [InlineData("123")]
        [InlineData("12$")]
        [InlineData("!@#")]
        public void Should_not_use_getField(string fieldName)
        {
            var collection = GetCollection<BsonDocument>();
            CreateCollection(collection, new BsonDocument(fieldName, 123));
            var queryable = collection.AsQueryable()
                .Select(x => x[fieldName]);

            var stages = Translate(collection, queryable);
            AssertStages(stages, $"{{ $project : {{ _v : '${fieldName}', _id : 0 }} }}");

            var results = queryable.ToList();
            results.Should().Equal(123);
        }

        [Theory]
        [InlineData("")]
        [InlineData(".")]
        [InlineData(".a")]
        [InlineData("a.")]
        public void Should_use_getField(string fieldName)
        {
            RequireServer.Check().Supports(Feature.GetField);

            var collection = GetCollection<BsonDocument>();
            CreateCollection(collection, new BsonDocument(fieldName, 123));
            var queryable = collection.AsQueryable()
                .Select(x => x[fieldName]);

            var stages = Translate(collection, queryable);
            AssertStages(stages, $"{{ $project : {{ _v : {{ $getField : {{ field : '{fieldName}', input : '$$ROOT' }} }}, _id : 0 }} }}");

            var results = queryable.ToList();
            results.Should().Equal(123);
        }

        [Theory]
        [InlineData("$")]
        [InlineData("$a")]
        [InlineData("$$a")]
        [InlineData("$a$")]
        public void Should_use_getField_with_literal(string fieldName)
        {
            RequireServer.Check().Supports(Feature.GetField);

            var collection = GetCollection<BsonDocument>();
            CreateCollection(collection, new BsonDocument(fieldName, 123));
            var queryable = collection.AsQueryable()
                .Select(x => x[fieldName]);

            var stages = Translate(collection, queryable);
            AssertStages(stages, $"{{ $project : {{ _v : {{ $getField : {{ field : {{ $literal : '{fieldName}' }}, input : '$$ROOT' }} }}, _id : 0 }} }}");

            var results = queryable.ToList();
            results.Should().Equal(123);
        }
    }
}
