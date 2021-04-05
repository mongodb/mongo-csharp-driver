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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq3;
using MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators;
using MongoDB.Driver.Tests;
using Xunit;

namespace Tests.MongoDB.Driver.Linq3.Legacy.Jira
{
    public class CSharp3524Tests
    {
        [Fact]
        public void SelectMany_should_translate_correctly()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<Item>("test");
            var queryable = collection.AsQueryable3()
                .SelectMany(
                    x => x.Meta,
                    (item, meta) =>
                        new ProjectedItem
                        {
                            ItemId = item.Id.ToString(),
                            Meta = meta,
                            Property = item.Properties.First(p => p.Id == meta.PropertyId)
                        });
            var provider = (MongoQueryProvider<Item>)queryable.Provider;

            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<Item, ProjectedItem>(provider, queryable.Expression);

            var stages = executableQuery.Stages;
            var expectedStages = new string[]
            {
                @"
                { $project : {
                    _v : { $map : {
                        input : '$Meta',
                        as : 'meta',
                        in : {
                            ItemId : { $toString : '$_id' },
                            Meta : '$$meta',
                            Property : {
                                $arrayElemAt : [
                                    { $filter : { input : '$Properties', as : 'p', cond : { $eq : ['$$p._id', '$$meta.PropertyId'] } } },
                                    0
                                ]
                            }
                        } } },
                    _id : 0
                } }",
                @"{ $unwind : '$_v' }"
            };
            stages.Should().Equal(expectedStages.Select(s => BsonDocument.Parse(s)));
        }

        // nested types
        private class Item
        {
            public ObjectId Id { get; set; }
            public List<ItemMeta> Meta { get; set; }
            public List<ItemProperty> Properties { get; set; }
        }

        private class ProjectedItem
        {
            public String ItemId { get; set; }
            public ItemMeta Meta { get; set; }
            public ItemProperty Property { get; set; }
        }

        private class ItemMeta
        {
            public String Meta { get; set; }
            public Int32 PropertyId { get; set; }
        }

        private class ItemProperty
        {
            public Int32 Id { get; set; }
            public String Text { get; set; }
        }
    }
}
