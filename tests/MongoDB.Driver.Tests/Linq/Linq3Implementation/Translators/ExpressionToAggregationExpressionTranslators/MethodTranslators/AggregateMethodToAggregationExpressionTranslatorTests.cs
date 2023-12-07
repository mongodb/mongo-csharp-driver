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
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class AggregateMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Aggregate_with_func_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => x.A.Aggregate((x, y) => x * y));

            var stages = Translate(collection, queryable);
            var results = queryable.ToList();

            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $reduce : { input : '$A', initialValue : 0, in : { $multiply : ['$$value', '$$this'] } } }, _id : 0 } }");
                results.Should().Equal(0, 0, 0, 0); // LINQ2 results are wrong
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $let : { vars : { seed : { $arrayElemAt : ['$A', 0] }, rest : { $slice : ['$A', 1, 2147483647] } }, in : { $cond : { if : { $eq : [{ $size : '$$rest' }, 0] }, then : '$$seed', else : { $reduce : { input : '$$rest', initialValue : '$$seed', in : { $multiply : ['$$value', '$$this'] } } } } } } }, _id : 0 } }");
                results.Should().Equal(0, 1, 2, 6); // C# throws exception on empty sequence but MQL returns 0
            }
        }

        private IMongoCollection<C> CreateCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                GetCollection<BsonDocument>("test"),
                BsonDocument.Parse("{ _id : 0, A : [] }"),
                BsonDocument.Parse("{ _id : 1, A : [1] }"),
                BsonDocument.Parse("{ _id : 2, A : [1, 2] }"),
                BsonDocument.Parse("{ _id : 3, A : [1, 2, 3] }"));
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
        }
    }
}
