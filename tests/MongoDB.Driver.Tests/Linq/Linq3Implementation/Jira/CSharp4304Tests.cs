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
    public class CSharp4304Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Example_should_work()
        {
            var collection = GetCollection<Parent>();
            var database = collection.Database;

            var queryable = GetChildrenQueryable<Child>(database, "Child");

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _outer : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'Child', localField : '_outer.ChildId', foreignField : '_id', as : '_inner' } }",
                "{ $unwind : '$_inner' }",
                "{ $project : { _v : '$_inner', _id : 0 } }");

            CreateParentCollection();
            CreateChildCollection();
            var results = queryable.ToList();
            results.OrderBy(r => r.Id).Select(r => r.Id).Should().Equal("11", "22");
        }

        private IQueryable<TChild> GetChildrenQueryable<TChild>(IMongoDatabase db, string childCollectionName) where TChild : IEntity
        {
            var parentCollection = db.GetCollection<Parent>("Parent");
            var childCollection = db.GetCollection<TChild>(childCollectionName).AsQueryable();

            return parentCollection
                .AsQueryable()
                .Join(
                    childCollection,
                    p => p.ChildId,
                    c => c.Id,
                    (_, c) => c);
        }

        private void CreateParentCollection()
        {
            var parentCollection = GetCollection<Parent>("Parent");

            CreateCollection(
                parentCollection,
                new Parent { Id = "1", ChildId = "11" },
                new Parent { Id = "2", ChildId = "22" });
        }

        private void CreateChildCollection()
        {
            var childCollection = GetCollection<Child>("Child");

            CreateCollection(
                childCollection,
                new Child { Id = "11" },
                new Child { Id = "22" });
        }

        public interface IEntity
        {
            string Id { get; set; }
        }

        public class Parent : IEntity
        {
            public string Id { get; set; }
            public string ChildId { get; set; }
        }

        public class Child : IEntity
        {
            public string Id { get; set; }
        }
    }
}
