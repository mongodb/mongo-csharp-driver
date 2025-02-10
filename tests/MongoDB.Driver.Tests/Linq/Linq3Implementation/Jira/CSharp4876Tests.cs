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
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4876Tests : LinqIntegrationTest<CSharp4876Tests.ClassFixture>
    {
        public CSharp4876Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [ParameterAttributeData]
        public void OfType_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().OfType<B2>().ToArray()) :
                collection.AsQueryable().Select(x => x.A.OfType<B2>().ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $filter : { input : '$A', as : 'item', cond : { $cond : { if : { $eq : [{ $type : '$$item._t' }, 'array'] }, then : { $in : ['B2', '$$item._t'] }, else : { $eq : ['$$item._t', 'B2'] } } } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(2);
        }

        public class C
        {
            public int Id { get; set; }
            public B[] A { get; set; }
        }

        [BsonDiscriminator(RootClass = true)]
        public class B
        {
            public int Id { get; set; }
        }

        public class B1 : B
        {
        }

        public class B2 : B
        {
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C
                {
                    Id = 1,
                    A =
                    [
                        new B1 { Id = 1 },
                        new B2 { Id = 2 }
                    ]
                }
            ];
        }
    }
}
