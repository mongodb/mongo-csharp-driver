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
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4062Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Project_anonymous_class_constant_should_work()
        {
            var collection = GetCollection<BsonDocument>();
            var aggregate = collection.Aggregate()
                .Project(x => new { R = true });

            var stages = Translate(collection, aggregate);

            AssertStages(stages, "{ $project : { _v : { R : true }, _id : 0 } }");
        }

        [Fact]
        public void Project_non_anonymous_class_constant_should_work()
        {
            var collection = GetCollection<BsonDocument>();
            var aggregate = collection.Aggregate()
                .Project(x => new C { R = true });

            var stages = Translate(collection, aggregate);

            AssertStages(stages, "{ $project : { _v : { R : true }, _id : 0 } }");
        }

        [Fact]
        public void Project_boolean_constant_should_work()
        {
            var collection = GetCollection<BsonDocument>();
            var aggregate = collection.Aggregate()
                .Project(x => true);

            var stages = Translate(collection, aggregate);

            AssertStages(stages, "{ $project : { _v : true, _id : 0 } }");
        }

        [Fact]
        public void Project_int_constant_should_work()
        {
            var collection = GetCollection<BsonDocument>();
            var aggregate = collection.Aggregate()
                .Project(x => 1);

            var stages = Translate(collection, aggregate);

            AssertStages(stages, "{ $project : { _v : 1, _id : 0 } }");
        }

        [Fact]
        public void Select_anonymous_class_constant_should_work()
        {
            var collection = GetCollection<BsonDocument>();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = true });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : { R : true }, _id : 0 } }");
        }

        [Fact]
        public void Select_non_anonymous_class_constant_should_work()
        {
            var collection = GetCollection<BsonDocument>();
            var queryable = collection.AsQueryable()
                .Select(x => new C { R = true });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : { R : true }, _id : 0 } }");
        }

        [Fact]
        public void Select_boolean_constant_should_work()
        {
            var collection = GetCollection<BsonDocument>();
            var queryable = collection.AsQueryable()
                .Select(x => true);

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : true, _id : 0 } }");
        }

        [Fact]
        public void Select_int_constant_should_work()
        {
            var collection = GetCollection<BsonDocument>();
            var queryable = collection.AsQueryable()
                .Select(x => 1);

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : 1, _id : 0 } }");
        }

        private class C
        {
            public bool R { get; set; }
        }
    }
}
