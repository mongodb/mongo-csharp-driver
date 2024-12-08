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
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4876Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void OfType_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = GetCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().OfType<B2>().ToArray()) :
                collection.AsQueryable().Select(x => x.A.OfType<B2>().ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $filter : { input : '$A', as : 'this', cond : { $cond : { if : { $eq : [{ $type : '$$this._t' }, 'array'] }, then : { $in : ['B2', '$$this._t'] }, else : { $eq : ['$$this._t', 'B2'] } } } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Id).Should().Equal(2);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C
                {
                    Id = 1, A =
                        [
                            new B1 { Id = 1 },
                            new B2 { Id = 2 }
                        ]
                });
            return collection;
        }

        private class C
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
    }
}
