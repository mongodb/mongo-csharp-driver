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
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4880Tests : LinqIntegrationTest<CSharp4880Tests.ClassFixture>
    {
        public CSharp4880Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_SequenceEqual_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().SequenceEqual(x.B)) :
                collection.AsQueryable().Select(x => x.A.SequenceEqual(x.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $and : [{ $eq : [{ $type : '$A' }, 'array'] }, { $eq : [{ $type : '$B' }, 'array'] }, { $eq : [{ $size : '$A' }, { $size : '$B' }] }, { $allElementsTrue : { $map : { input : { $zip : { inputs : ['$A', '$B'] } }, as : 'pair', in: { $eq : [{ $arrayElemAt : ['$$pair', 0] }, { $arrayElemAt : ['$$pair', 1] }] } } } }] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(false, false, false, false, true, false, false);
        }

        public class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
            public int[] B { get; set; }
        }

        public sealed class ClassFixture :  MongoCollectionFixture<C, BsonDocument>
        {
            protected override IEnumerable<BsonDocument> InitialData =>
            [
                BsonDocument.Parse("{ _id : 1, A : null, B : null }"),
                BsonDocument.Parse("{ _id : 2, A : null, B : [1, 2, 3] }"),
                BsonDocument.Parse("{ _id : 3, A : [1, 2, 3], B : null }"),
                BsonDocument.Parse("{ _id : 4, A : [1, 2, 3], B : [1, 2] }"),
                BsonDocument.Parse("{ _id : 5, A : [1, 2, 3], B : [1, 2, 3] }"),
                BsonDocument.Parse("{ _id : 6, A : [1, 2, 3], B : [4, 5, 6] }"),
                BsonDocument.Parse("{ _id : 7, A : [1, 2, 3], B : [1, 2, 3, 4] }")
            ];
        }
    }
}
