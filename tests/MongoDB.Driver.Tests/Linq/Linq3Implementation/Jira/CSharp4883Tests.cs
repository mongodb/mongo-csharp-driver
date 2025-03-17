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
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4883Tests : LinqIntegrationTest<CSharp4883Tests.ClassFixture>
    {
        public CSharp4883Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [ParameterAttributeData]
        public void SkipWhile_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().SkipWhile(x => x < 3).ToArray()) :
                collection.AsQueryable().Select(x => x.A.SkipWhile(x => x < 3).ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                """
                {
                    $project :
                    {
                        _v :
                        {
                            $let :
                            {
                                vars :
                                {
                                    "while" :
                                    {
                                        $reduce :
                                        {
                                             input : "$A",
                                             initialValue : { predicate : true, count : 0 },
                                             in :
                                             {
                                                 $switch :
                                                 {
                                                    branches :
                                                    [
                                                        { case : { $not : "$$value.predicate" }, then : "$$value" },
                                                        { case : { $lt : ["$$this", 3] }, then : { predicate : true, count : { $add : ["$$value.count", 1] } } },
                                                    ],
                                                    default : { predicate : false, count : "$$value.count" }
                                                 }
                                             }
                                         }
                                    }
                                },
                                in : { $slice : ["$A", "$$while.count", 2147483647] }
                            }
                        },
                        _id : 0
                    }
                }
                """);

            var result = queryable.Single();
            result.Should().Equal(3, 2, 1);
        }

        [Theory]
        [ParameterAttributeData]
        public void TakeWhile_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().TakeWhile(x => x < 3).ToArray()) :
                collection.AsQueryable().Select(x => x.A.TakeWhile(x => x < 3).ToArray());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                """
                {
                    $project :
                    {
                        _v :
                        {
                            $let :
                            {
                                vars :
                                {
                                    "while" :
                                    {
                                        $reduce :
                                        {
                                             input : "$A",
                                             initialValue : { predicate : true, count : 0 },
                                             in :
                                             {
                                                 $switch :
                                                 {
                                                    branches :
                                                    [
                                                        { case : { $not : "$$value.predicate" }, then : "$$value" },
                                                        { case : { $lt : ["$$this", 3] }, then : { predicate : true, count : { $add : ["$$value.count", 1] } } },
                                                    ],
                                                    default : { predicate : false, count : "$$value.count" }
                                                 }
                                             }
                                         }
                                    }
                                },
                                in : { $slice : ["$A", "$$while.count"] }
                            }
                        },
                        _id : 0
                    }
                }
                """);

            var result = queryable.Single();
            result.Should().Equal(1, 2);
        }

        public class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData { get; } =
            [
                new C { Id = 1, A = [1, 2, 3, 2, 1] }
            ];
        }
    }
}
