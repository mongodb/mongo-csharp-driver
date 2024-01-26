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

using System;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class ZipMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Enumerable_Zip_should_work(
            [Values(false, true)] bool withNestedAsQueryableSource2,
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = withNestedAsQueryableSource2 ?
                collection.AsQueryable().Select(x => x.A.Zip(x.B.AsQueryable(), (x, y) => x * y)) :
                collection.AsQueryable().Select(x => x.A.Zip(x.B, (x, y) => x * y));

            Exception exception = null;
            if (linqProvider == LinqProvider.V2)
            {
                if (withNestedAsQueryableSource2)
                {
                    exception = Record.Exception(() => Translate(collection, queryable));
                    exception.Should().BeOfType<NotSupportedException>();
                }
                else
                {
                    var stages = Translate(collection, queryable);
                    AssertStages(stages, "{ $project : { __fld0 : { $map : { input: { $zip : { inputs : ['$A', '$B'] } }, as : 'x_y', in : { $multiply : [{ $arrayElemAt : ['$$x_y', 0] }, { $arrayElemAt : ['$$x_y', 1] }] } } }, _id : 0 } }");
                }
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : { $map : { input : { $zip : { inputs : ['$A', '$B'] } }, as : 'pair', in : { $let : { vars : { x : { $arrayElemAt : ['$$pair', 0] }, y : { $arrayElemAt : ['$$pair', 1] } }, in : { $multiply : ['$$x', '$$y'] } } } } }, _id : 0 } }");
            }

            if (exception == null)
            {
                var results = queryable.ToList();
                results.Should().HaveCount(4);
                results[0].Should().Equal();
                results[1].Should().Equal();
                results[2].Should().Equal();
                results[3].Should().Equal(3, 8);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Queryable_Zip_should_work(
            [Values(false, true)] bool withNestedAsQueryableSource2,
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = withNestedAsQueryableSource2 ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().Zip(x.B.AsQueryable(), (x, y) => x * y)) :
                collection.AsQueryable().Select(x => x.A.AsQueryable().Zip(x.B, (x, y) => x * y));

            Exception exception = null;
            if (linqProvider == LinqProvider.V2)
            {
                exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().NotBeNull(); // the two cases throw different exceptions
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { _v : { $map : { input : { $zip : { inputs : ['$A', '$B'] } }, as : 'pair', in : { $let : { vars : { x : { $arrayElemAt : ['$$pair', 0] }, y : { $arrayElemAt : ['$$pair', 1] } }, in : { $multiply : ['$$x', '$$y'] } } } } }, _id : 0 } }");

                var results = queryable.ToList();
                results.Should().HaveCount(4);
                results[0].Should().Equal();
                results[1].Should().Equal();
                results[2].Should().Equal();
                results[3].Should().Equal(3, 8);
            }
        }

        private IMongoCollection<C> CreateCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 0, A = new int[0], B = new int[0] },
                new C { Id = 1, A = new int[0], B = new int[] { 1 } },
                new C { Id = 2, A = new int[] { 1 }, B = new int[0] },
                new C { Id = 3, A = new int[] { 1, 2 }, B = new int[] { 3, 4, 5 } });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
            public int[] B { get; set; }
        }
    }
}
