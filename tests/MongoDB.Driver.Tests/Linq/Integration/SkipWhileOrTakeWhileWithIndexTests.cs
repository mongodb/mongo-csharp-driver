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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Integration;

public class SkipWhileOrTakeWhileWithIndexTests : LinqIntegrationTest<SkipWhileOrTakeWhileWithIndexTests.ClassFixture>
{
    public SkipWhileOrTakeWhileWithIndexTests(ClassFixture fixture)
        : base(fixture, server => server.Supports(Feature.ReduceArrayIndexAs))
    {
    }

    [Theory]
    [ParameterAttributeData]
    public void SkipWhile_with_index_should_work(
        [Values(false, true)] bool withNestedAsQueryable)
    {
        var collection = Fixture.Collection;

        var queryable = withNestedAsQueryable ?
            collection.AsQueryable().Select(x => x.A.AsQueryable().SkipWhile((item, i) => i < 3).ToArray()) :
            collection.AsQueryable().Select(x => x.A.SkipWhile((item, i) => i < 3).ToArray());

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
                                                    { case : { $lt : ["$$__i", 3] }, then : { predicate : true, count : { $add : ["$$value.count", 1] } } },
                                                ],
                                                default : { predicate : false, count : "$$value.count" }
                                             }
                                         },
                                         arrayIndexAs : "__i"
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
        result.Should().Equal(4, 5);
    }

    [Theory]
    [ParameterAttributeData]
    public void TakeWhile_with_index_should_work(
        [Values(false, true)] bool withNestedAsQueryable)
    {
        var collection = Fixture.Collection;

        var queryable = withNestedAsQueryable ?
            collection.AsQueryable().Select(x => x.A.AsQueryable().TakeWhile((item, i) => i < 3).ToArray()) :
            collection.AsQueryable().Select(x => x.A.TakeWhile((item, i) => i < 3).ToArray());

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
                                                    { case : { $lt : ["$$__i", 3] }, then : { predicate : true, count : { $add : ["$$value.count", 1] } } },
                                                ],
                                                default : { predicate : false, count : "$$value.count" }
                                             }
                                         },
                                         arrayIndexAs : "__i"
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
        result.Should().Equal(1, 2, 3);
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
            new() { Id = 1, A = [1, 2, 3, 4, 5] }
        ];
    }
}
