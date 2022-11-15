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
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp1326Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Projection_of_ArrayOfDocuments_dictionary_keys_and_values_should_work()
        {
            var collection = CreateCollection();
            var parentIds = new int[] { 1, 2, 3 };
            var childrenFilter =
                Builders<Child>.Filter.In(c => c.ParentId, parentIds) &
                Builders<Child>.Filter.Eq(c => c.Gender, Gender.Male);

            var aggregate = collection
                .Aggregate()
                .Match(childrenFilter)
                .Group(c => c.ParentId, g => new KeyValuePair<int, List<Child>>(g.Key, new List<Child>(g)));

            var stages = Translate(collection, aggregate);
            AssertStages(
                stages,
                "{ $match : { ParentId : { $in : [1, 2, 3] }, Gender : 'Male' } }",
                "{ $group : { _id : '$ParentId', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { Key : '$_id', Value : '$_elements', _id : 0 } }");

            var results = aggregate.ToList().OrderBy(x => x.Key).ToList();
            results[0].Key.Should().Be(1);
            results[0].Value.Select(x => x.Id).Should().BeEquivalentTo(1, 2);
            results[1].Key.Should().Be(2);
            results[1].Value.Select(x => x.Id).Should().BeEquivalentTo(4);
        }

        private IMongoCollection<Child> CreateCollection()
        {
            var collection = GetCollection<Child>("Children");

            CreateCollection(
                collection,
                new Child { Id = 1, ParentId = 1, Gender = Gender.Male },
                new Child { Id = 2, ParentId = 1, Gender = Gender.Male },
                new Child { Id = 3, ParentId = 1, Gender = Gender.Female },
                new Child { Id = 4, ParentId = 2, Gender = Gender.Male },
                new Child { Id = 5, ParentId = 4, Gender = Gender.Male });

            return collection;
        }

        public class Parent
        {
            public int Id { get; set; }
        }

        public class Child
        {
            public int Id { get; set; }

            public int ParentId { get; set; }

            [BsonRepresentation(BsonType.String)]
            public Gender Gender { get; set; }
        }

        public enum Gender { Male, Female };
    }
}
