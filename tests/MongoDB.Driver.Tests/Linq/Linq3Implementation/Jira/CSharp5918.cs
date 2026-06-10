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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5918 : LinqIntegrationTest<CSharp5918.ClassFixture>
{
    public CSharp5918(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Pipeline_update_with_ToList_should_work()
    {
        var collection = Fixture.Collection;
        var filter = Builders<C>.Filter.Empty;

        var pipeline = new EmptyPipelineDefinition<C>()
            .Set(x => new C
            {
                List1 = x.List1.Concat(x.List2).ToList()
            });

        var update = Builders<C>.Update.Pipeline(pipeline);

        var updateStages =
            update.Render(new(collection.DocumentSerializer, BsonSerializer.SerializerRegistry))
                .AsBsonArray
                .Cast<BsonDocument>();
        AssertStages(
            updateStages,
            "{ $set : { List1 : { $concatArrays : ['$List1', '$List2'] } } }");

        collection.UpdateMany(filter, update, new UpdateOptions { IsUpsert = true });

        var items = collection.AsQueryable().ToList();
        items.Single().List1.Should().BeEquivalentTo("a", "b", "c", "d");
    }

    [Fact]
    public void Pipeline_update_with_ToList_to_other_model_should_work()
    {
        var collection = Fixture.Collection;
        var filter = Builders<C>.Filter.Empty;

        var pipeline = new EmptyPipelineDefinition<C>()
            .Set(x => new Other
            {
                List1 = x.List1.Concat(x.List2).ToList()
            });

        var update = Builders<C>.Update.Pipeline(pipeline);

        var updateStages =
            update.Render(new(collection.DocumentSerializer, BsonSerializer.SerializerRegistry))
                .AsBsonArray
                .Cast<BsonDocument>();
        AssertStages(
            updateStages,
            "{ $set : { List1 : { $concatArrays : ['$List1', '$List2'] } } }");

        collection.UpdateMany(filter, update, new UpdateOptions { IsUpsert = true });

        var items = collection.AsQueryable().ToList();
        items.Single().List1.Should().BeEquivalentTo("a", "b", "c", "d");
    }

    public class C
    {
        public int Id { get; set; }
        public List<string> List1 { get; set; }

        public List<string> List2 { get; set; }
    }

    public class Other
    {
        public List<string> List1 { get; set; }

        public List<string> List2 { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        public override bool InitializeDataBeforeEachTestCase => true;

        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, List1 = ["a", "b"], List2 = ["c", "d"]}
        ];
    }
}
