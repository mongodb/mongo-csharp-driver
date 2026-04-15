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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class PickExpressionDefinitionBuilderIntegrationTests
        : IntegrationTest<PickExpressionDefinitionBuilderIntegrationTests.ClassFixture>
    {
        public PickExpressionDefinitionBuilderIntegrationTests(ClassFixture fixture)
            : base(fixture, s => s.Supports(Feature.PickAccumulatorsAsExpressionsNewIn83))
        {
        }
        
        [Fact]
        public void Top_in_set_stage_should_add_leader_field()
        {
            var collection = Fixture.Collection;
            var sortBy = Builders<Player>.Sort.Descending(p => p.Score);

            var results = collection.Aggregate()
                .Set(Builders<Tournament>.SetFields.Set(
                    new StringFieldDefinition<Tournament, Player>("Leader"),
                    PickExpressionDefinitionBuilder.Top<Tournament, Player>(x => x.Players, sortBy)))
                .SortBy(x => x.Id)
                .ToList();

            results[0].Leader.Name.Should().Be("Alice");
            results[0].Leader.Score.Should().Be(10);
            results[1].Leader.Name.Should().Be("Eve");
            results[1].Leader.Score.Should().Be(7);
        }

        [Fact]
        public void Bottom_in_set_stage_should_add_loser_field()
        {
            var collection = Fixture.Collection;
            var sortBy = Builders<Player>.Sort.Descending(p => p.Score);

            var results = collection.Aggregate()
                .Set(Builders<Tournament>.SetFields.Set(
                    new StringFieldDefinition<Tournament, Player>("Loser"),
                    PickExpressionDefinitionBuilder.Bottom<Tournament, Player>(x => x.Players, sortBy)))
                .SortBy(x => x.Id)
                .ToList();

            results[0].Loser.Name.Should().Be("Bob");
            results[0].Loser.Score.Should().Be(5);
            results[1].Loser.Name.Should().Be("Dave");
            results[1].Loser.Score.Should().Be(3);
        }

        [Fact]
        public void TopN_in_set_stage_should_add_top_players_field()
        {
            var collection = Fixture.Collection;
            var sortBy = Builders<Player>.Sort.Descending(p => p.Score);

            var results = collection.Aggregate()
                .Set(Builders<Tournament>.SetFields.Set(
                    new StringFieldDefinition<Tournament, IEnumerable<Player>>("TopPlayers"),
                    PickExpressionDefinitionBuilder.TopN<Tournament, Player>(x => x.Players, sortBy, n: 2)))
                .SortBy(x => x.Id)
                .ToList();

            results[0].TopPlayers.Select(p => p.Name).Should().Equal("Alice", "Charlie");
            results[1].TopPlayers.Select(p => p.Name).Should().Equal("Eve", "Dave");
        }

        [Fact]
        public void BottomN_in_set_stage_should_add_bottom_players_field()
        {
            var collection = Fixture.Collection;
            var sortBy = Builders<Player>.Sort.Descending(p => p.Score);

            var results = collection.Aggregate()
                .Set(Builders<Tournament>.SetFields.Set(
                    new StringFieldDefinition<Tournament, IEnumerable<Player>>("TopPlayers"),
                    PickExpressionDefinitionBuilder.BottomN<Tournament, Player>(x => x.Players, sortBy, n: 2)))
                .SortBy(x => x.Id)
                .ToList();

            results[0].TopPlayers.Select(p => p.Name).Should().Equal("Charlie", "Bob");
            results[1].TopPlayers.Select(p => p.Name).Should().Equal("Eve", "Dave");
        }

        [Fact]
        public void TopN_in_addFields_stage_should_add_top_players_field()
        {
            var collection = Fixture.Collection;
            var sortBy = Builders<Player>.Sort.Descending(p => p.Score);
            var pickExpr = PickExpressionDefinitionBuilder.TopN<Tournament, Player>(x => x.Players, sortBy, n: 2);

            var registry = BsonSerializer.SerializerRegistry;
            var serializer = registry.GetSerializer<Tournament>();
            var rendered = pickExpr.Render(new RenderArgs<Tournament>(serializer, registry));

            var addFieldsStage = new BsonDocumentPipelineStageDefinition<Tournament, Tournament>(
                new BsonDocument("$addFields", new BsonDocument("TopPlayers", rendered)));

            var results = collection.Aggregate()
                .AppendStage(addFieldsStage)
                .SortBy(x => x.Id)
                .ToList();

            results[0].TopPlayers.Select(p => p.Name).Should().Equal("Alice", "Charlie");
            results[1].TopPlayers.Select(p => p.Name).Should().Equal("Eve", "Dave");
        }

        [Fact]
        public void Top_in_project_stage_should_return_leader_field()
        {
            var collection = Fixture.Collection;
            var sortBy = Builders<Player>.Sort.Descending(p => p.Score);
            var pickExpr = PickExpressionDefinitionBuilder.Top<Tournament, Player>(x => x.Players, sortBy);

            var registry = BsonSerializer.SerializerRegistry;
            var serializer = registry.GetSerializer<Tournament>();
            var rendered = pickExpr.Render(new RenderArgs<Tournament>(serializer, registry));

            var projectStage = new BsonDocumentPipelineStageDefinition<Tournament, BsonDocument>(
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "Leader", rendered }
                }));

            var results = collection.Aggregate()
                .SortBy(x => x.Id)
                .AppendStage(projectStage)
                .ToList();

            results[0]["Leader"]["Name"].AsString.Should().Be("Alice");
            results[0]["Leader"]["Score"].AsInt32.Should().Be(10);
            results[1]["Leader"]["Name"].AsString.Should().Be("Eve");
        }

        [Fact]
        public void TopN_in_project_stage_should_return_top_players_field()
        {
            var collection = Fixture.Collection;
            var sortBy = Builders<Player>.Sort.Descending(p => p.Score);
            var pickExpr = PickExpressionDefinitionBuilder.TopN<Tournament, Player>(x => x.Players, sortBy, n: 2);

            var registry = BsonSerializer.SerializerRegistry;
            var serializer = registry.GetSerializer<Tournament>();
            var rendered = pickExpr.Render(new RenderArgs<Tournament>(serializer, registry));

            var projectStage = new BsonDocumentPipelineStageDefinition<Tournament, BsonDocument>(
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "TopPlayers", rendered }
                }));

            var results = collection.Aggregate()
                .SortBy(x => x.Id)
                .AppendStage(projectStage)
                .ToList();

            results[0]["TopPlayers"].AsBsonArray.Select(p => p["Name"].AsString).Should().Equal("Alice", "Charlie");
            results[1]["TopPlayers"].AsBsonArray.Select(p => p["Name"].AsString).Should().Equal("Eve", "Dave");
        }

        public class Tournament
        {
            [BsonId]
            public int Id { get; set; }

            [BsonElement("Name")]
            public string Name { get; set; }

            [BsonElement("Players")]
            public List<Player> Players { get; set; }

            [BsonElement("Leader")]
            public Player Leader { get; set; }

            [BsonElement("Loser")]
            public Player Loser { get; set; }

            [BsonElement("TopPlayers")]
            public List<Player> TopPlayers { get; set; }
        }

        public class Player
        {
            [BsonElement("Name")]
            public string Name { get; set; }

            [BsonElement("Score")]
            public int Score { get; set; }
        }

        public sealed class ClassFixture : MongoDatabaseFixture
        {
            public IMongoCollection<Tournament> Collection { get; private set; }

            protected override void InitializeFixture()
            {
                Collection = CreateCollection<Tournament>("pickExpressionTests");
                Collection.InsertMany([
                    new Tournament
                    {
                        Id = 1,
                        Name = "T1",
                        Players =
                        [
                            new() { Name = "Alice", Score = 10 },
                            new() { Name = "Bob", Score = 5 },
                            new() { Name = "Charlie", Score = 8 }
                        ]
                    },
                    new Tournament
                    {
                        Id = 2,
                        Name = "T2",
                        Players =
                        [
                            new() { Name = "Dave", Score = 3 },
                            new() { Name = "Eve", Score = 7 }
                        ]
                    }
                ]);
            }
        }
    }
}
