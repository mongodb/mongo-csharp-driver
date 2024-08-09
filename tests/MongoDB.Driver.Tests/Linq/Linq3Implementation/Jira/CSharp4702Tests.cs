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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp702Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Query1_using_list_should_work()
        {
            var collection = GetCollection();
            var lookingFor = new List<string> { "value1", "value2" };

            var queryable = collection.AsQueryable()
                .Where(model => lookingFor.Any(value => model.List.Contains(value)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { List : { $in : ['value1', 'value2'] } } }");

            var results = queryable.ToList();
            results.Select(model => model.Id).Should().BeEquivalentTo(4, 5);
        }

        [Fact]
        public void Query2_using_list_should_work()
        {
            var collection = GetCollection();
            var lookingFor = new List<string> { "value1", "value2" };

            var queryable = collection.AsQueryable()
                .Where(model => model.List.Any(value => lookingFor.Contains(value)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { List : { $in : ['value1', 'value2'] } } }");

            var results = queryable.ToList();
            results.Select(model => model.Id).Should().BeEquivalentTo(4, 5);
        }

        [Fact]
        public void Query1_using_hashset_should_work()
        {
            var collection = GetCollection();
            var lookingFor = new List<string> { "value1", "value2" };

            var queryable = collection.AsQueryable()
                .Where(model => lookingFor.Any(value => model.HashSet.Contains(value)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { HashSet : { $in : ['value1', 'value2'] } } }");

            var results = queryable.ToList();
            results.Select(model => model.Id).Should().BeEquivalentTo(4, 5);
        }

        [Fact]
        public void Query2_using_hashset_should_work()
        {
            var collection = GetCollection();
            var lookingFor = new List<string> { "value1", "value2" };

            var queryable = collection.AsQueryable()
                .Where(model => model.HashSet.Any(value => lookingFor.Contains(value)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { HashSet : { $in : ['value1', 'value2'] } } }");

            var results = queryable.ToList();
            results.Select(model => model.Id).Should().BeEquivalentTo(4, 5);
        }

        private IMongoCollection<Model> GetCollection()
        {
            var collection = GetCollection<Model>("test");
            var documentsCollection = GetCollection<BsonDocument>("test");
            CreateCollection(
                documentsCollection,
                BsonDocument.Parse("{ _id : 1 }"),
                BsonDocument.Parse("{ _id : 2, List : null, HashSet : null }"),
                BsonDocument.Parse("{ _id : 3, List : [], HashSet : [] }"),
                BsonDocument.Parse("{ _id : 4, List : ['value1'], HashSet : ['value1'] }"),
                BsonDocument.Parse("{ _id : 5, List : ['value1', 'value2'], HashSet : ['value1', 'value2'] }"),
                BsonDocument.Parse("{ _id : 6, List : ['value3'], HashSet : ['value3'] }"),
                BsonDocument.Parse("{ _id : 7, List : ['value3', 'value4'], HashSet : ['value3', 'value4'] }"));
            return collection;
        }

        private class Model
        {
            public int Id { get; set; }
            public List<string> List { get; set; }
            public HashSet<string> HashSet { get; set; }
        }
    }
}
