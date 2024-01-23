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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class IsMissingMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void Select_Exists_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Mql.Exists(x.S));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $ne : [{ $type : '$S' }, 'missing'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(false, true, true);
        }

        [Fact]
        public void Select_IsMissing_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Mql.IsMissing(x.S));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $eq : [{ $type : '$S' }, 'missing'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(true, false, false);
        }

        [Fact]
        public void Select_IsNullOrMissing_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => Mql.IsNullOrMissing(x.S));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $in : [{ $type : '$S' }, ['null', 'missing']] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(true, true, false);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                GetCollection<BsonDocument>("test"),
                BsonDocument.Parse("{ _id : 1 }"),
                BsonDocument.Parse("{ _id : 2, S : null }"),
                BsonDocument.Parse("{ _id : 3, S : 'abc' }"));
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public string S { get; set; }
        }
    }
}
