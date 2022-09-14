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

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4317Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Projection_of_ArrayOfDocuments_dictionary_keys_and_values_should_work()
        {
            var collection = CreateCollection();
            var projectStage = "{ $project : { Keys : '$Data.k', Values : '$Data.v', _id : 0 } }";

            var queryable = collection
                .AsQueryable()
                .Select(x => new {
                    Keys = x.Data.Select(y => y.Key),
                    Values = x.Data.Select(y => y.Value)
                });

            var stages = Translate(collection, queryable);
            AssertStages(stages, projectStage);

            var result = queryable.First();
            result.Keys.Should().Equal(1, 2);
            result.Values.Should().Equal("v1", "v2");
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("C");

            CreateCollection(
                collection,
                new C { Id = 1, Data = new Dictionary<int, string> { { 1, "v1" }, { 2, "v2" } } });

            return collection;
        }

        private class C
        {
            public int Id { get; set; }

            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
            public Dictionary<int, string> Data { get; set; }
        }
    }
}
