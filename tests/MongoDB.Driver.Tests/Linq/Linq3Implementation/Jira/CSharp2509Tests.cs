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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp2509Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Where_ContainsValue_should_work_when_representation_is_Dictionary()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.D1.ContainsValue(1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { $expr : { $reduce : { input : { $objectToArray : '$D1' }, initialValue : false, in : { $cond : { if : '$$value', then : true, else : { $eq : ['$$this.v', 1] } } } } } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void Where_ContainsValue_should_work_when_representation_is_ArrayOfArrays()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.D2.ContainsValue(1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { $expr : { $reduce : { input : '$D2', initialValue : false, in : { $cond : { if : '$$value', then : true, else : { $eq : [{ $arrayElemAt : ['$$this', 1] }, 1] } } } } } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void Where_ContainsValue_should_work_when_representation_is_ArrayOfDocuments()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.D3.ContainsValue(1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { D3 : { $elemMatch : { v : 1 } } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void Select_ContainsValue_should_work_when_representation_is_Dictionary()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.D1.ContainsValue(1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $reduce : { input : { $objectToArray : '$D1' }, initialValue : false, in : { $cond : { if : '$$value', then : true, else : { $eq : ['$$this.v', 1] } } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(true, true, false);
        }

        [Fact]
        public void Select_ContainsValue_should_work_when_representation_is_ArrayOfArrays()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.D2.ContainsValue(1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $reduce : { input : '$D2', initialValue : false, in : { $cond : { if : '$$value', then : true, else : { $eq : [{ $arrayElemAt : ['$$this', 1] }, 1] } } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(true, true, false);
        }

        [Fact]
        public void Select_ContainsValue_should_work_when_representation_is_ArrayOfDocuments()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.D3.ContainsValue(1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $reduce : { input : '$D3', initialValue : false, in : { $cond : { if : '$$value', then : true, else : { $eq : ['$$this.v', 1] } } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(true, true, false);
        }

        private IMongoCollection<User> GetCollection()
        {
            var collection = GetCollection<User>("test");
            CreateCollection(
                collection,
                new User
                {
                    Id = 1,
                    D1 = new() { { "A", 1 }, { "B", 2 } },
                    D2 = new() { { "A", 1 }, { "B", 2 } },
                    D3 = new() { { "A", 1 }, { "B", 2 } }
                },
                new User
                {
                    Id = 2,
                    D1 = new() { { "A", 2 }, { "B", 1 } },
                    D2 = new() { { "A", 2 }, { "B", 1 } },
                    D3 = new() { { "A", 2 }, { "B", 1 } }
                },
                new User
                {
                    Id = 3,
                    D1 = new() { { "A", 2 }, { "B", 3 } },
                    D2 = new() { { "A", 2 }, { "B", 3 } },
                    D3 = new() { { "A", 2 }, { "B", 3 } }
                });
            return collection;
        }

        private class User
        {
            public int Id { get; set; }
            [BsonDictionaryOptions(DictionaryRepresentation.Document)]
            public Dictionary<string, int> D1 { get; set; }
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
            public Dictionary<string, int> D2 { get; set; }
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
            public Dictionary<string, int> D3 { get; set; }
        }
    }
}
