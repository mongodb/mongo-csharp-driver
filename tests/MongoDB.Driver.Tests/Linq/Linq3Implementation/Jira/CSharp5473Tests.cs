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

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5473Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Translate_queryable_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => x.X + 1);
            var provider = (IMongoQueryProvider)queryable.Provider;

            var stages = provider.Translate(queryable, out var outputSerializer);
            AssertStages(stages, "{ $project : { _v : { $add : ['$X', 1] }, _id : 0 } }");

            var pipeline = new BsonDocumentStagePipelineDefinition<C, int>(stages, outputSerializer);
            var result = collection.Aggregate(pipeline).Single();
            result.Should().Be(2);
        }

        [Fact]
        public void Translate_expression_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => x.X + 1);
            var expression = queryable.Expression; // collection was just used as an easy way to create the Expression

            // this is an example of how to translate an Expression using a dummyQueryable
            var client = DriverTestConfiguration.Client;
            var dummyDatabase = client.GetDatabase("dummy");
            var dummyQueryable = dummyDatabase.AsQueryable().Provider.CreateQuery<C>(expression);
            var provider = (IMongoQueryProvider)dummyQueryable.Provider;
            var stages = provider.Translate(queryable, out var outputSerializer);
            AssertStages(stages, "{ $project : { _v : { $add : ['$X', 1] }, _id : 0 } }");

            var pipeline = new BsonDocumentStagePipelineDefinition<C, int>(stages, outputSerializer);
            var result = collection.Aggregate(pipeline).Single();
            result.Should().Be(2);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, X = 1 });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
