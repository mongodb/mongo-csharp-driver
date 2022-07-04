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
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp1555Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Queryable_should_work()
        {
            var collection = CreatePeopleCollection();
            var queryable = collection.AsQueryable();

            var stages = Translate(collection, queryable);
            AssertStages(stages, new string[0]);

            var result = queryable.ToList().Single();
            result.ShouldBeEquivalentTo(new Person { Id = 1, Name = "A" });
        }

        [Fact]
        public void Select_new_Person_should_work()
        {
            var collection = CreatePeopleCollection();
            var queryable = collection.AsQueryable()
                .Select(p => new Person { Id = p.Id, Name = p.Name });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _id : '$_id', Name : '$Name' } }");

            var result = queryable.ToList().Single();
            result.ShouldBeEquivalentTo(new Person { Id = 1, Name = "A" });
        }

        [Fact]
        public void Select_new_Person_without_Name_should_work()
        {
            var collection = CreatePeopleCollection();
            var queryable = collection.AsQueryable()
                .Select(p => new Person { Id = p.Id });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _id : '$_id' } }");

            var result = queryable.ToList().Single();
            result.ShouldBeEquivalentTo(new Person { Id = 1, Name = null });
        }

        [Fact]
        public void Select_new_Person_without_Id_should_work()
        {
            var collection = CreatePeopleCollection();
            var queryable = collection.AsQueryable()
                .Select(p => new Person { Name = p.Name });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Name : '$Name', _id : 0 } }");

            var result = queryable.ToList().Single();
            result.ShouldBeEquivalentTo(new Person { Id = 0, Name = "A" });
        }

        private IMongoCollection<Person> CreatePeopleCollection()
        {
            var collection = GetCollection<Person>();

            var documents = new[]
            {
                new Person { Id = 1, Name = "A" }
            };
            CreateCollection(collection, documents);

            return collection;
        }

        private class Person
        {
            [BsonIgnoreIfNull]
            public int Id { get; set; }
            [BsonIgnoreIfNull]
            public string Name { get; set; }
        }
    }
}
