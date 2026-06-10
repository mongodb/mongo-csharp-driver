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
using System.Text.RegularExpressions;
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5532Tests : LinqIntegrationTest<CSharp5532Tests.ClassFixture>
{
    private static readonly ObjectId id1 = ObjectId.Parse("111111111111111111111111");
    private static readonly ObjectId id2 = ObjectId.Parse("222222222222222222222222");
    private static readonly ObjectId id3 = ObjectId.Parse("333333333333333333333333");

    public CSharp5532Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Filter_should_translate_correctly()
    {
        var collection = Fixture.Collection;
        List<string> jobIds = [id2.ToString()];

        var find = collection
            .Find(x => x.Parts.Any(a => a.Refs.Any(b => jobIds.Contains(b.id))));

        var filter = TranslateFindFilter(collection, find);

        filter.Should().Be("{ Parts : { $elemMatch : { Refs : { $elemMatch : { _id : { $in : [ObjectId('222222222222222222222222')] } } } } } }");
    }

    [Fact]
    public void Projection_should_translate_correctly()
    {
        var collection = Fixture.Collection;
        List<string> jobIds = [id2.ToString()];

        var find = collection
            .Find("{}")
            .Project(chain =>
                new
                {
                    chain.Parts
                        .First(p => p.Refs.Any(j => jobIds.Contains(j.id)))
                        .Refs.First(j => jobIds.Contains(j.id)).id
                });;

        var projectionTranslation = TranslateFindProjection(collection, find);

        var expectedTranslation =
            """
            {
                _id :
                {
                    $let :
                    {
                        vars :
                        {
                            this :
                            {
                                $arrayElemAt :
                                [
                                    {
                                        $filter :
                                        {
                                            input :
                                            {
                                                $let :
                                                {
                                                    vars :
                                                    {
                                                        this :
                                                        {
                                                            $arrayElemAt :
                                                            [
                                                                {
                                                                    $filter :
                                                                    {
                                                                        input : "$Parts",
                                                                        as : "p",
                                                                        cond :
                                                                        {
                                                                            $anyElementTrue :
                                                                            {
                                                                                $map :
                                                                                {
                                                                                    input : "$$p.Refs",
                                                                                    as : "j",
                                                                                    in : { $in : ["$$j._id", [{ "$oid" : "222222222222222222222222" }]] }
                                                                                }
                                                                            }
                                                                        },
                                                                        limit : 1
                                                                    }
                                                                },
                                                                0
                                                            ]
                                                        }
                                                    },
                                                    in : "$$this.Refs"
                                                }
                                            },
                                            as : "j",
                                            cond : { $in : ['$$j._id', [{ "$oid" : "222222222222222222222222" }]] },
                                            limit : 1
                                        }
                                    },
                                    0
                                ]
                            }
                        },
                        in : "$$this._id"
                    }
                }
            }
            """;
        if (!Feature.FilterLimit.IsSupported(CoreTestConfiguration.MaxWireVersion))
        {
            expectedTranslation = Regex.Replace(expectedTranslation, @",\s+limit : 1", "");
        }

        projectionTranslation.Should().Be(expectedTranslation);
    }

    public class Document
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }
    }

    public class Chain : Document
    {
        public ICollection<Unit> Parts { get; set; } = new List<Unit>();
    }

    public class Unit
    {
        public ICollection<Document> Refs { get; set; }

        public Unit()
        {
            Refs = new List<Document>();
        }
    }

    public sealed class ClassFixture : MongoCollectionFixture<Chain>
    {
        protected override IEnumerable<Chain> InitialData =>
        [
            new Chain
            {
                id = "0102030405060708090a0b0c",
                Parts = new List<Unit>()
                {
                    new()
                    {
                        Refs = new List<Document>()
                        {
                            new()
                            {
                                id = id1.ToString(),
                            },
                            new()
                            {
                                id = id2.ToString(),
                            },
                            new()
                            {
                                id = id3.ToString(),
                            },
                        }
                    }
                }
            }
        ];
    }
}
