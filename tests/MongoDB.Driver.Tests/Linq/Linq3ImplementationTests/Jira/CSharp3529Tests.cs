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
using System.Globalization;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp3529Tests : Linq3IntegrationTest
    {
        // $bottom examples are from: https://www.mongodb.com/docs/v6.0/reference/operator/aggregation/bottom/
        [Fact]
        public void Bottom_find_the_bottom_score_in_a_single_game_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            PlayerId = elements.Bottom(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score })
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $bottom : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' } } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(new { Id = "G1", PlayerId = new { P = "PlayerD", S = 1 } });
        }

        [Fact]
        public void Bottom_find_the_bottom_score_in_a_single_game_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            PlayerId = g.Bottom(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score })
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $bottom : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' } } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(new { Id = "G1", PlayerId = new { P = "PlayerD", S = 1 } });
        }

        [Fact]
        public void Bottom_find_the_bottom_score_across_multiple_games_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            PlayerId = elements.Bottom(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score })
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$GameId', __agg0 : { $bottom : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' } } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0' , _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().Equal(
                new { Id = "G1", PlayerId = new { P = "PlayerD", S = 1 } },
                new { Id = "G2", PlayerId = new { P = "PlayerA", S = 10 } });
        }

        [Fact]
        public void Bottom_find_the_bottom_score_across_multiple_games_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            PlayerId = g.Bottom(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score })
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$GameId', __agg0 : { $bottom : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' } } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0' , _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().Equal(
                new { Id = "G1", PlayerId = new { P = "PlayerD", S = 1 } },
                new { Id = "G2", PlayerId = new { P = "PlayerA", S = 10 } });
        }

        [Fact]
        public void Bottom_without_GroupBy_should_have_helpful_error_message()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateDocumentsWithArrayCollection();
            var queryable = collection
                .AsQueryable()
                .Select(x => x.A.Bottom(Builders<ArrayElement>.Sort.Ascending(e => e.X), e => e.X));

            var exception = Record.Exception(() => Translate(collection, queryable));
            var notSupportedException = exception.Should().BeOfType<ExpressionNotSupportedException>().Subject;
            notSupportedException.Message.Should().Contain("Bottom can only be used as an accumulator with GroupBy");
        }

        // $bottomN examples are from: https://www.mongodb.com/docs/v6.0/reference/operator/aggregation/bottomN/
        [Fact]
        public void BottomN_find_the_three_lowest_scores_in_a_single_game_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            PlayerId = elements.BottomN(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $bottomN : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("G1");
            results[0].PlayerId.Should().Equal(new { P = "PlayerB", S = 33 }, new { P = "PlayerA", S = 31 }, new { P = "PlayerD", S = 1 });
        }

        [Fact]
        public void BottomN_find_the_three_lowest_scores_in_a_single_game_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            PlayerId = g.BottomN(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $bottomN : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("G1");
            results[0].PlayerId.Should().Equal(new { P = "PlayerB", S = 33 }, new { P = "PlayerA", S = 31 }, new { P = "PlayerD", S = 1 });
        }

        [Fact]
        public void BottomN_find_the_three_lowest_scores_across_multiple_games_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            PlayerId = elements.BottomN(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$GameId', __agg0 : { $bottomN : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("G1");
            results[0].PlayerId.Should().Equal(new { P = "PlayerB", S = 33 }, new { P = "PlayerA", S = 31 }, new { P = "PlayerD", S = 1 });
            results[1].Id.Should().Be("G2");
            results[1].PlayerId.Should().Equal(new { P = "PlayerC", S = 66 }, new { P = "PlayerB", S = 14 }, new { P = "PlayerA", S = 10 });
        }

        [Fact]
        public void BottomN_find_the_three_lowest_scores_across_multiple_games_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            PlayerId = g.BottomN(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$GameId', __agg0 : { $bottomN : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("G1");
            results[0].PlayerId.Should().Equal(new { P = "PlayerB", S = 33 }, new { P = "PlayerA", S = 31 }, new { P = "PlayerD", S = 1 });
            results[1].Id.Should().Be("G2");
            results[1].PlayerId.Should().Equal(new { P = "PlayerC", S = 66 }, new { P = "PlayerB", S = 14 }, new { P = "PlayerA", S = 10 });
        }

        [Fact]
        public void BottomN_computing_n_based_on_the_group_key_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => new { GameId = x.GameId },
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            GameScores = elements.BottomN(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => e.Score,
                                key,
                                key => key.GameId == "G2" ? 1 : 3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : { GameId : '$GameId' }, __agg0 : { $bottomN : { sortBy : { Score : -1 }, output : '$Score', n : { $cond : { if : { $eq : ['$GameId', 'G2'] }, then : 1, else : 3 } } } } } }",
                "{ $project : { Id : '$_id', GameScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id.GameId).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(new { GameId = "G1" });
            results[0].GameScores.Should().Equal(33, 31, 1);
            results[1].Id.Should().Be( new { GameId = "G2" });
            results[1].GameScores.Should().Equal(10);
        }

        [Fact]
        public void BottomN_computing_n_based_on_the_group_key_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => new { GameId = x.GameId })
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            GameScores = g.BottomN(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => e.Score,
                                g.Key,
                                key => key.GameId == "G2" ? 1 : 3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : { GameId : '$GameId' }, __agg0 : { $bottomN : { sortBy : { Score : -1 }, output : '$Score', n : { $cond : { if : { $eq : ['$GameId', 'G2'] }, then : 1, else : 3 } } } } } }",
                "{ $project : { Id : '$_id', GameScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id.GameId).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(new { GameId = "G1" });
            results[0].GameScores.Should().Equal(33, 31, 1);
            results[1].Id.Should().Be(new { GameId = "G2" });
            results[1].GameScores.Should().Equal(10);
        }

        [Fact]
        public void BottomN_without_GroupBy_should_have_helpful_error_message()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateDocumentsWithArrayCollection();
            var queryable = collection
                .AsQueryable()
                .Select(x => x.A.BottomN(Builders<ArrayElement>.Sort.Ascending(e => e.X), e => e.X, 2));

            var exception = Record.Exception(() => Translate(collection, queryable));
            var notSupportedException = exception.Should().BeOfType<ExpressionNotSupportedException>().Subject;
            notSupportedException.Message.Should().Contain("BottomN can only be used as an accumulator with GroupBy");
        }

        // $first examples are from: https://www.mongodb.com/docs/v6.0/reference/operator/aggregation/first/
        [Fact]
        public void First_use_in_group_stage_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateSalesCollection();
            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.Item).ThenBy(x => x.Date)
                .GroupBy(
                    x => x.Item,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            FirstSale = elements.Select(e => e.Date).First()
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { Item : 1, Date : 1 } }",
                "{ $group : { _id : '$Item', __agg0 : { $first : '$Date' } } }",
                "{ $project : { Id : '$_id', FirstSale : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id);
            results.Should().Equal(
                new { Id = "abc", FirstSale = DateTime.Parse("2014-01-01T08:00:00.000Z", null, DateTimeStyles.AdjustToUniversal) },
                new { Id = "jkl", FirstSale = DateTime.Parse("2014-02-03T09:00:00.000Z", null, DateTimeStyles.AdjustToUniversal) },
                new { Id = "xyz", FirstSale = DateTime.Parse("2014-02-03T09:05:00.000Z", null, DateTimeStyles.AdjustToUniversal) });
        }

        [Fact]
        public void First_use_in_group_stage_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateSalesCollection();
            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.Item).ThenBy(x => x.Date)
                .GroupBy(x => x.Item)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            FirstSale = g.Select(e => e.Date).First()
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { Item : 1, Date : 1 } }",
                "{ $group : { _id : '$Item', __agg0 : { $first : '$Date' } } }",
                "{ $project : { Id : '$_id', FirstSale : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id);
            results.Should().Equal(
                new { Id = "abc", FirstSale = DateTime.Parse("2014-01-01T08:00:00.000Z", null, DateTimeStyles.AdjustToUniversal) },
                new { Id = "jkl", FirstSale = DateTime.Parse("2014-02-03T09:00:00.000Z", null, DateTimeStyles.AdjustToUniversal) },
                new { Id = "xyz", FirstSale = DateTime.Parse("2014-02-03T09:05:00.000Z", null, DateTimeStyles.AdjustToUniversal) });
        }

        // $firstN examples are from: https://www.mongodb.com/docs/upcoming/reference/operator/aggregation/firstN/
        [Fact]
        public void FirstN_find_the_first_three_player_scores_for_a_single_game_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            FirstThreeScores = elements.FirstN(
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $firstN : { input : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', FirstThreeScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("G1");
            results[0].FirstThreeScores.Should().Equal(new { P = "PlayerA", S = 31 }, new { P = "PlayerB", S = 33 }, new { P = "PlayerC", S = 99 });
        }

        [Fact]
        public void FirstN_find_the_first_three_player_scores_for_a_single_game_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            FirstThreeScores = g.FirstN(
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $firstN : { input : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', FirstThreeScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("G1");
            results[0].FirstThreeScores.Should().Equal(new { P = "PlayerA", S = 31 }, new { P = "PlayerB", S = 33 }, new { P = "PlayerC", S = 99 });
        }

        [Fact]
        public void FirstN_using_sort_with_firstN_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.Score)
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            PlayerId = elements.FirstN(
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { Score : -1 } }",
                "{ $group : { _id : '$GameId', __agg0 : { $firstN : { input : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("G1");
            results[0].PlayerId.Should().Equal(new { P = "PlayerC", S = 99 }, new { P = "PlayerB", S = 33 }, new { P = "PlayerA", S = 31 });
            results[1].Id.Should().Be("G2");
            results[1].PlayerId.Should().Equal(new { P = "PlayerD", S = 80 }, new { P = "PlayerC", S = 66 }, new { P = "PlayerB", S = 14 });
        }

        [Fact]
        public void FirstN_using_sort_with_firstN_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.Score)
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            PlayerId = g.FirstN(
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { Score : -1 } }",
                "{ $group : { _id : '$GameId', __agg0 : { $firstN : { input : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("G1");
            results[0].PlayerId.Should().Equal(new { P = "PlayerC", S = 99 }, new { P = "PlayerB", S = 33 }, new { P = "PlayerA", S = 31 });
            results[1].Id.Should().Be("G2");
            results[1].PlayerId.Should().Equal(new { P = "PlayerD", S = 80 }, new { P = "PlayerC", S = 66 }, new { P = "PlayerB", S = 14 });
        }

        [Fact]
        public void FirstN_computing_n_based_on_the_group_key_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => new { GameId = x.GameId },
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            GameScores = elements.FirstN(
                                e => e.Score,
                                key,
                                key => key.GameId == "G2" ? 1 : 3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : { GameId : '$GameId' }, __agg0 : { $firstN : { input : '$Score', n : { $cond : { if : { $eq : ['$GameId', 'G2'] }, then : 1, else : 3 } } } } } }",
                "{ $project : { Id : '$_id', GameScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id.GameId).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(new { GameId = "G1" });
            results[0].GameScores.Should().Equal(31, 33, 99);
            results[1].Id.Should().Be(new { GameId = "G2" });
            results[1].GameScores.Should().Equal(10);
        }

        [Fact]
        public void FirstN_computing_n_based_on_the_group_key_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => new { GameId = x.GameId })
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            GameScores = g.FirstN(
                                e => e.Score,
                                g.Key,
                                key => key.GameId == "G2" ? 1 : 3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : { GameId : '$GameId' }, __agg0 : { $firstN : { input : '$Score', n : { $cond : { if : { $eq : ['$GameId', 'G2'] }, then : 1, else : 3 } } } } } }",
                "{ $project : { Id : '$_id', GameScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id.GameId).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(new { GameId = "G1" });
            results[0].GameScores.Should().Equal(31, 33, 99);
            results[1].Id.Should().Be(new { GameId = "G2" });
            results[1].GameScores.Should().Equal(10);
        }

        [Fact]
        public void FirstN_array_operator_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateDocumentsWithArrayCollection();
            var queryable = collection
                .AsQueryable()
                .Select(x => x.A.FirstN(e => e.X, 2));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $firstN : { input : '$A.X', n : 2  } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Should().Equal(1, 2);
        }

        // $last examples are from: https://www.mongodb.com/docs/v6.0/reference/operator/aggregation/last/
        [Fact]
        public void Last_use_in_group_stage_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateSalesCollection();
            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.Item).ThenBy(x => x.Date)
                .GroupBy(
                    x => x.Item,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            LastSalesDate = elements.Select(e => e.Date).Last()
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { Item : 1, Date : 1 } }",
                "{ $group : { _id : '$Item', __agg0 : { $last : '$Date' } } }",
                "{ $project : { Id : '$_id', LastSalesDate : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id);
            results.Should().Equal(
                new { Id = "abc", LastSalesDate = DateTime.Parse("2014-02-15T08:00:00Z", null, DateTimeStyles.AdjustToUniversal) },
                new { Id = "jkl", LastSalesDate = DateTime.Parse("2014-02-03T09:00:00Z", null, DateTimeStyles.AdjustToUniversal) },
                new { Id = "xyz", LastSalesDate = DateTime.Parse("2014-02-15T14:12:12Z", null, DateTimeStyles.AdjustToUniversal) });
        }

        [Fact]
        public void Last_use_in_group_stage_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateSalesCollection();
            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.Item).ThenBy(x => x.Date)
                .GroupBy(x => x.Item)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            LastSalesDate = g.Select(e => e.Date).Last()
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { Item : 1, Date : 1 } }",
                "{ $group : { _id : '$Item', __agg0 : { $last : '$Date' } } }",
                "{ $project : { Id : '$_id', LastSalesDate : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id);
            results.Should().Equal(
                new { Id = "abc", LastSalesDate = DateTime.Parse("2014-02-15T08:00:00Z", null, DateTimeStyles.AdjustToUniversal) },
                new { Id = "jkl", LastSalesDate = DateTime.Parse("2014-02-03T09:00:00Z", null, DateTimeStyles.AdjustToUniversal) },
                new { Id = "xyz", LastSalesDate = DateTime.Parse("2014-02-15T14:12:12Z", null, DateTimeStyles.AdjustToUniversal) });
        }

        // $lastN examples are from: https://www.mongodb.com/docs/v6.0/reference/operator/aggregation/lastN/
        [Fact]
        public void LastN_find_the_last_three_player_scores_for_a_single_game_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            LastThreeScores = elements.LastN(
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $lastN : { input : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', LastThreeScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("G1");
            results[0].LastThreeScores.Should().Equal(new { P = "PlayerB", S = 33 }, new { P = "PlayerC", S = 99 }, new { P = "PlayerD", S = 1 });
        }

        [Fact]
        public void LastN_find_the_last_three_player_scores_for_a_single_game_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            LastThreeScores = g.LastN(
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $lastN : { input : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', LastThreeScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("G1");
            results[0].LastThreeScores.Should().Equal(new { P = "PlayerB", S = 33 }, new { P = "PlayerC", S = 99 }, new { P = "PlayerD", S = 1 });
        }

        [Fact]
        public void LastN_using_sort_with_lastN_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.Score)
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            PlayerId = elements.LastN(
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { Score : -1 } }",
                "{ $group : { _id : '$GameId', __agg0 : { $lastN : { input : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("G1");
            results[0].PlayerId.Should().Equal(new { P = "PlayerB", S = 33 }, new { P = "PlayerA", S = 31 }, new { P = "PlayerD", S = 1 });
            results[1].Id.Should().Be("G2");
            results[1].PlayerId.Should().Equal(new { P = "PlayerC", S = 66 }, new { P = "PlayerB", S = 14 }, new { P = "PlayerA", S = 10 });
        }

        [Fact]
        public void LastN_using_sort_with_lastN_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.Score)
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            PlayerId = g.LastN(
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { Score : -1 } }",
                "{ $group : { _id : '$GameId', __agg0 : { $lastN : { input : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("G1");
            results[0].PlayerId.Should().Equal(new { P = "PlayerB", S = 33 }, new { P = "PlayerA", S = 31 }, new { P = "PlayerD", S = 1 });
            results[1].Id.Should().Be("G2");
            results[1].PlayerId.Should().Equal(new { P = "PlayerC", S = 66 }, new { P = "PlayerB", S = 14 }, new { P = "PlayerA", S = 10 });
        }

        [Fact]
        public void LastN_computing_n_based_on_the_group_key_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => new { GameId = x.GameId },                
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            GameScores = elements.LastN(
                                e => e.Score,
                                key,
                                key => key.GameId == "G2" ? 1 : 3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : { GameId : '$GameId' }, __agg0 : { $lastN : { input : '$Score', n : { $cond : { if : { $eq : ['$GameId', 'G2'] }, then : 1, else : 3 } } } } } }",
                "{ $project : { Id : '$_id', GameScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id.GameId).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(new { GameId = "G1" });
            results[0].GameScores.Should().Equal(33, 99, 1);
            results[1].Id.Should().Be(new { GameId = "G2" });
            results[1].GameScores.Should().Equal(80);
        }

        [Fact]
        public void LastN_computing_n_based_on_the_group_key_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => new { GameId = x.GameId })
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            GameScores = g.LastN(
                                e => e.Score,
                                g.Key,
                                key => key.GameId == "G2" ? 1 : 3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : { GameId : '$GameId' }, __agg0 : { $lastN : { input : '$Score', n : { $cond : { if : { $eq : ['$GameId', 'G2'] }, then : 1, else : 3 } } } } } }",
                "{ $project : { Id : '$_id', GameScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id.GameId).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(new { GameId = "G1" });
            results[0].GameScores.Should().Equal(33, 99, 1);
            results[1].Id.Should().Be(new { GameId = "G2" });
            results[1].GameScores.Should().Equal(80);
        }

        [Fact]
        public void LastN_array_operator_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateDocumentsWithArrayCollection();
            var queryable = collection
                .AsQueryable()
                .Select(x => x.A.LastN(e => e.X, 2));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $lastN : { input : '$A.X', n : 2  } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Should().Equal(2, 3);
        }

        // $max examples are from: https://www.mongodb.com/docs/v6.0/reference/operator/aggregation/max/
        [Fact]
        public void Max_use_in_group_stage_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateSalesCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => x.Item,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            MaxTotalAmount = elements.Max(e => e.Price * e.Quantity),
                            MaxQuantity = elements.Max(e => e.Quantity)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$Item', __agg0 : { $max : { $multiply : ['$Price', '$Quantity'] } }, __agg1 : { $max : '$Quantity' } } }",
                "{ $project : { Id : '$_id', MaxTotalAmount : '$__agg0', MaxQuantity : '$__agg1', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id);
            results.Should().Equal(
                new { Id = "abc", MaxTotalAmount = 100.0, MaxQuantity = 10 },
                new { Id = "jkl", MaxTotalAmount = 20.0, MaxQuantity = 1 },
                new { Id = "xyz", MaxTotalAmount = 50.0, MaxQuantity = 10 });
        }

        [Fact]
        public void Max_use_in_group_stage_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateSalesCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => x.Item)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            MaxTotalAmount = g.Max(e => e.Price * e.Quantity),
                            MaxQuantity = g.Max(e => e.Quantity)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$Item', __agg0 : { $max : { $multiply : ['$Price', '$Quantity'] } }, __agg1 : { $max : '$Quantity' } } }",
                "{ $project : { Id : '$_id', MaxTotalAmount : '$__agg0', MaxQuantity : '$__agg1', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id);
            results.Should().Equal(
                new { Id = "abc", MaxTotalAmount = 100.0, MaxQuantity = 10 },
                new { Id = "jkl", MaxTotalAmount = 20.0, MaxQuantity = 1 },
                new { Id = "xyz", MaxTotalAmount = 50.0, MaxQuantity = 10 });
        }

        // $maxN examples are from: https://www.mongodb.com/docs/v6.0/reference/operator/aggregation/maxN/
        [Fact]
        public void MaxN_find_the_maximum_three_scores_for_a_single_game_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            MaxThreeScores = elements.MaxN(
                                e => new { S = e.Score, P = e.PlayerId },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $maxN : { input : { S : '$Score', P : '$PlayerId' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', MaxThreeScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("G1");
            results[0].MaxThreeScores.Should().Equal(new { S = 99, P = "PlayerC" }, new { S = 33, P = "PlayerB" }, new { S = 31, P = "PlayerA" });
        }

        [Fact]
        public void MaxN_find_the_maximum_three_scores_for_a_single_game_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            MaxThreeScores = g.MaxN(
                                e => new { S = e.Score, P = e.PlayerId },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $maxN : { input : { S : '$Score', P : '$PlayerId' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', MaxThreeScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("G1");
            results[0].MaxThreeScores.Should().Equal(new { S = 99, P = "PlayerC" }, new { S = 33, P = "PlayerB" }, new { S = 31, P = "PlayerA" });
        }

        [Fact]
        public void MaxN_find_the_maximum_three_scores_across_multiple_games_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            MaxScores = elements.MaxN(
                                e => new { S = e.Score, P = e.PlayerId },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$GameId', __agg0 : { $maxN : { input : { S : '$Score', P : '$PlayerId' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', MaxScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("G1");
            results[0].MaxScores.Should().Equal(new { S = 99, P = "PlayerC" }, new { S = 33, P = "PlayerB" }, new { S = 31, P = "PlayerA" });
            results[1].Id.Should().Be("G2");
            results[1].MaxScores.Should().Equal(new { S = 80, P = "PlayerD" }, new { S = 66, P = "PlayerC" }, new { S = 14, P = "PlayerB" });
        }

        [Fact]
        public void MaxN_find_the_maximum_three_scores_across_multiple_games_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            MaxScores = g.MaxN(
                                e => new { S = e.Score, P = e.PlayerId },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$GameId', __agg0 : { $maxN : { input : { S : '$Score', P : '$PlayerId' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', MaxScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("G1");
            results[0].MaxScores.Should().Equal(new { S = 99, P = "PlayerC" }, new { S = 33, P = "PlayerB" }, new { S = 31, P = "PlayerA" });
            results[1].Id.Should().Be("G2");
            results[1].MaxScores.Should().Equal(new { S = 80, P = "PlayerD" }, new { S = 66, P = "PlayerC" }, new { S = 14, P = "PlayerB" });
        }

        [Fact]
        public void MaxN_computing_n_based_on_the_group_key_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => new { GameId = x.GameId },
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            MaxScores = elements.MaxN(
                                e => new { S = e.Score, P = e.PlayerId },
                                key,
                                key => key.GameId == "G2" ? 1 : 3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : { GameId : '$GameId' }, __agg0 : { $maxN : { input : { S : '$Score', P : '$PlayerId' }, n : { $cond : { if : { $eq : ['$GameId', 'G2'] }, then : 1, else : 3 } } } } } }",
                "{ $project : { Id : '$_id', MaxScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id.GameId).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(new { GameId = "G1" });
            results[0].MaxScores.Should().Equal(new { S = 99, P = "PlayerC" }, new { S = 33, P = "PlayerB" }, new { S = 31, P = "PlayerA" });
            results[1].Id.Should().Be(new { GameId = "G2" });
            results[1].MaxScores.Should().Equal(new { S = 80, P = "PlayerD" });
        }

        [Fact]
        public void MaxN_computing_n_based_on_the_group_key_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => new { GameId = x.GameId })
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            MaxScores = g.MaxN(
                                e => new { S = e.Score, P = e.PlayerId },
                                g.Key,
                                key => key.GameId == "G2" ? 1 : 3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : { GameId : '$GameId' }, __agg0 : { $maxN : { input : { S : '$Score', P : '$PlayerId' }, n : { $cond : { if : { $eq : ['$GameId', 'G2'] }, then : 1, else : 3 } } } } } }",
                "{ $project : { Id : '$_id', MaxScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id.GameId).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(new { GameId = "G1" });
            results[0].MaxScores.Should().Equal(new { S = 99, P = "PlayerC" }, new { S = 33, P = "PlayerB" }, new { S = 31, P = "PlayerA" });
            results[1].Id.Should().Be(new { GameId = "G2" });
            results[1].MaxScores.Should().Equal(new { S = 80, P = "PlayerD" });
        }

        [Fact]
        public void MaxN_array_operator_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateDocumentsWithArrayCollection();
            var queryable = collection
                .AsQueryable()
                .Select(x => x.A.MaxN(e => e.X, 2));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $maxN : { input : '$A.X', n : 2  } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Should().Equal(3, 2);
        }

        // $min examples are from: https://www.mongodb.com/docs/v6.0/reference/operator/aggregation/min/
        [Fact]
        public void Min_use_in_group_stage_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateSalesCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => x.Item,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            MinQuantity = elements.Min(e => e.Quantity)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$Item', __agg0 : { $min : '$Quantity' } } }",
                "{ $project : { Id : '$_id', MinQuantity : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id);
            results.Should().Equal(
                new { Id = "abc", MinQuantity = 2 },
                new { Id = "jkl", MinQuantity = 1 },
                new { Id = "xyz", MinQuantity = 5 });
        }

        [Fact]
        public void Min_use_in_group_stage_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateSalesCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => x.Item)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            MinQuantity = g.Min(e => e.Quantity)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$Item', __agg0 : { $min : '$Quantity' } } }",
                "{ $project : { Id : '$_id', MinQuantity : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id);
            results.Should().Equal(
                new { Id = "abc", MinQuantity = 2 },
                new { Id = "jkl", MinQuantity = 1 },
                new { Id = "xyz", MinQuantity = 5 });
        }

        // $minN examples are from: https://www.mongodb.com/docs/v6.0/reference/operator/aggregation/minN/
        [Fact]
        public void MinN_find_the_minimum_three_scores_for_a_single_game_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            MinScores = elements.MinN(
                                e => new { S = e.Score, P = e.PlayerId },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $minN : { input : { S : '$Score', P : '$PlayerId' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', MinScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("G1");
            results[0].MinScores.Should().Equal(new { S = 1, P = "PlayerD" }, new { S = 31, P = "PlayerA" }, new { S = 33, P = "PlayerB" });
        }

        [Fact]
        public void MinN_find_the_minimum_three_scores_for_a_single_game_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            MinScores = g.MinN(
                                e => new { S = e.Score, P = e.PlayerId },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $minN : { input : { S : '$Score', P : '$PlayerId' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', MinScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("G1");
            results[0].MinScores.Should().Equal(new { S = 1, P = "PlayerD" }, new { S = 31, P = "PlayerA" }, new { S = 33, P = "PlayerB" });
        }

        [Fact]
        public void MinN_find_the_minimum_three_scores_across_multiple_games_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            MinScores = elements.MinN(
                                e => new { S = e.Score, P = e.PlayerId },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$GameId', __agg0 : { $minN : { input : { S : '$Score', P : '$PlayerId' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', MinScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("G1");
            results[0].MinScores.Should().Equal(new { S = 1, P = "PlayerD" }, new { S = 31, P = "PlayerA" }, new { S = 33, P = "PlayerB" });
            results[1].Id.Should().Be("G2");
            results[1].MinScores.Should().Equal(new { S = 10, P = "PlayerA" }, new { S = 14, P = "PlayerB" }, new { S = 66, P = "PlayerC" });
        }

        [Fact]
        public void MinN_find_the_minimum_three_scores_across_multiple_games_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            MinScores = g.MinN(
                                e => new { S = e.Score, P = e.PlayerId },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$GameId', __agg0 : { $minN : { input : { S : '$Score', P : '$PlayerId' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', MinScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("G1");
            results[0].MinScores.Should().Equal(new { S = 1, P = "PlayerD" }, new { S = 31, P = "PlayerA" }, new { S = 33, P = "PlayerB" });
            results[1].Id.Should().Be("G2");
            results[1].MinScores.Should().Equal(new { S = 10, P = "PlayerA" }, new { S = 14, P = "PlayerB" }, new { S = 66, P = "PlayerC" });
        }

        [Fact]
        public void MinN_computing_n_based_on_the_group_key_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => new { GameId = x.GameId },
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            MinScores = elements.MinN(
                                e => new { S = e.Score, P = e.PlayerId },
                                key,
                                key => key.GameId == "G2" ? 1 : 3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : { GameId : '$GameId' }, __agg0 : { $minN : { input : { S : '$Score', P : '$PlayerId' }, n : { $cond : { if : { $eq : ['$GameId', 'G2'] }, then : 1, else : 3 } } } } } }",
                "{ $project : { Id : '$_id', MinScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id.GameId).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(new { GameId = "G1" });
            results[0].MinScores.Should().Equal(new { S = 1, P = "PlayerD" }, new { S = 31, P = "PlayerA" }, new { S = 33, P = "PlayerB" });
            results[1].Id.Should().Be(new { GameId = "G2" });
            results[1].MinScores.Should().Equal(new { S = 10, P = "PlayerA" });
        }

        [Fact]
        public void MinN_computing_n_based_on_the_group_key_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => new { GameId = x.GameId })
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            MinScores = g.MinN(
                                e => new { S = e.Score, P = e.PlayerId },
                                g.Key,
                                key => key.GameId == "G2" ? 1 : 3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : { GameId : '$GameId' }, __agg0 : { $minN : { input : { S : '$Score', P : '$PlayerId' }, n : { $cond : { if : { $eq : ['$GameId', 'G2'] }, then : 1, else : 3 } } } } } }",
                "{ $project : { Id : '$_id', MinScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id.GameId).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(new { GameId = "G1" });
            results[0].MinScores.Should().Equal(new { S = 1, P = "PlayerD" }, new { S = 31, P = "PlayerA" }, new { S = 33, P = "PlayerB" });
            results[1].Id.Should().Be(new { GameId = "G2" });
            results[1].MinScores.Should().Equal(new { S = 10, P = "PlayerA" });
        }

        [Fact]
        public void MinN_array_operator_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateDocumentsWithArrayCollection();
            var queryable = collection
                .AsQueryable()
                .Select(x => x.A.MinN(e => e.X, 2));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $minN : { input : '$A.X', n : 2  } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Should().Equal(1, 2);
        }

        // $top examples are from: https://www.mongodb.com/docs/v6.0/reference/operator/aggregation/top/
        [Fact]
        public void Top_find_the_top_score_in_a_single_game_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            PlayerId = elements.Top(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score })
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $top : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' } } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(new { Id = "G1", PlayerId = new { P = "PlayerC", S = 99 } });
        }

        [Fact]
        public void Top_find_the_top_score_in_a_single_game_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            PlayerId = g.Top(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score })
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $top : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' } } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(new { Id = "G1", PlayerId = new { P = "PlayerC", S = 99 } });
        }

        [Fact]
        public void Top_find_the_top_score_across_multiple_games_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            PlayerId = elements.Top(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score })
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$GameId', __agg0 : { $top : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' } } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0' , _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().Equal(
                new { Id = "G1", PlayerId = new { P = "PlayerC", S = 99 } },
                new { Id = "G2", PlayerId = new { P = "PlayerD", S = 80 } });
        }

        [Fact]
        public void Top_find_the_top_score_across_multiple_games_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            PlayerId = g.Top(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score })
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$GameId', __agg0 : { $top : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' } } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0' , _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().Equal(
                new { Id = "G1", PlayerId = new { P = "PlayerC", S = 99 } },
                new { Id = "G2", PlayerId = new { P = "PlayerD", S = 80 } });
        }

        [Fact]
        public void Top_without_GroupBy_should_have_helpful_error_message()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateDocumentsWithArrayCollection();
            var queryable = collection
                .AsQueryable()
                .Select(x => x.A.Top(Builders<ArrayElement>.Sort.Ascending(e => e.X), e => e.X));

            var exception = Record.Exception(() => Translate(collection, queryable));
            var notSupportedException = exception.Should().BeOfType<ExpressionNotSupportedException>().Subject;
            notSupportedException.Message.Should().Contain("Top can only be used as an accumulator with GroupBy");
        }

        // $topN examples are from: https://www.mongodb.com/docs/v6.0/reference/operator/aggregation/topN/
        [Fact]
        public void TopN_find_the_three_highest_scores_in_a_single_game_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            PlayerId = elements.TopN(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $topN : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("G1");
            results[0].PlayerId.Should().Equal(new { P = "PlayerC", S = 99 }, new { P = "PlayerB", S = 33 }, new { P = "PlayerA", S = 31 });
        }

        [Fact]
        public void TopN_find_the_three_highest_scores_in_a_single_game_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .Where(x => x.GameId == "G1")
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            PlayerId = g.TopN(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { GameId : 'G1' } }",
                "{ $group : { _id : '$GameId', __agg0 : { $topN : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("G1");
            results[0].PlayerId.Should().Equal(new { P = "PlayerC", S = 99 }, new { P = "PlayerB", S = 33 }, new { P = "PlayerA", S = 31 });
        }

        [Fact]
        public void TopN_find_the_three_highest_scores_across_multiple_games_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => x.GameId,
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            PlayerId = elements.TopN(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$GameId', __agg0 : { $topN : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("G1");
            results[0].PlayerId.Should().Equal(new { P = "PlayerC", S = 99 }, new { P = "PlayerB", S = 33 }, new { P = "PlayerA", S = 31 });
            results[1].Id.Should().Be("G2");
            results[1].PlayerId.Should().Equal(new { P = "PlayerD", S = 80 }, new { P = "PlayerC", S = 66 }, new { P = "PlayerB", S = 14 });
        }

        [Fact]
        public void TopN_find_the_three_highest_scores_across_multiple_games_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => x.GameId)
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            PlayerId = g.TopN(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => new { P = e.PlayerId, S = e.Score },
                                3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : '$GameId', __agg0 : { $topN : { sortBy : { Score : -1 }, output : { P : '$PlayerId', S : '$Score' }, n : 3 } } } }",
                "{ $project : { Id : '$_id', PlayerId : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("G1");
            results[0].PlayerId.Should().Equal(new { P = "PlayerC", S = 99 }, new { P = "PlayerB", S = 33 }, new { P = "PlayerA", S = 31 });
            results[1].Id.Should().Be("G2");
            results[1].PlayerId.Should().Equal(new { P = "PlayerD", S = 80 }, new { P = "PlayerC", S = 66 }, new { P = "PlayerB", S = 14 });
        }

        [Fact]
        public void TopN_computing_n_based_on_the_group_key_example_using_GroupBy_with_result_selector_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(
                    x => new { GameId = x.GameId },
                    (key, elements) =>
                        new
                        {
                            Id = key,
                            GameScores = elements.TopN(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => e.Score,
                                key,
                                key => key.GameId == "G2" ? 1 : 3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : { GameId : '$GameId' }, __agg0 : { $topN : { sortBy : { Score : -1 }, output : '$Score', n : { $cond : { if : { $eq : ['$GameId', 'G2'] }, then : 1, else : 3 } } } } } }",
                "{ $project : { Id : '$_id', GameScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id.GameId).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(new { GameId = "G1" });
            results[0].GameScores.Should().Equal(99, 33, 31);
            results[1].Id.Should().Be(new { GameId = "G2" });
            results[1].GameScores.Should().Equal(80);
        }

        [Fact]
        public void TopN_computing_n_based_on_the_group_key_example_using_GroupBy_and_Select_should_work()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateGameScoresCollection();
            var queryable = collection
                .AsQueryable()
                .GroupBy(x => new { GameId = x.GameId })
                .Select(
                    g =>
                        new
                        {
                            Id = g.Key,
                            GameScores = g.TopN(
                                Builders<GameScore>.Sort.Descending(g => g.Score),
                                e => e.Score,
                                g.Key,
                                key => key.GameId == "G2" ? 1 : 3)
                        });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : { GameId : '$GameId' }, __agg0 : { $topN : { sortBy : { Score : -1 }, output : '$Score', n : { $cond : { if : { $eq : ['$GameId', 'G2'] }, then : 1, else : 3 } } } } } }",
                "{ $project : { Id : '$_id', GameScores : '$__agg0', _id : 0 } }");

            var results = queryable.ToList().OrderBy(x => x.Id.GameId).ToList();
            results.Should().HaveCount(2);
            results[0].Id.Should().Be(new { GameId = "G1" });
            results[0].GameScores.Should().Equal(99, 33, 31);
            results[1].Id.Should().Be(new { GameId = "G2" });
            results[1].GameScores.Should().Equal(80);
        }

        [Fact]
        public void TopN_without_GroupBy_should_have_helpful_error_message()
        {
            RequireServer.Check().Supports(Feature.PickAccumulatorsNewIn52);
            var collection = CreateDocumentsWithArrayCollection();
            var queryable = collection
                .AsQueryable()
                .Select(x => x.A.TopN(Builders<ArrayElement>.Sort.Ascending(e => e.X), e => e.X, 2));

            var exception = Record.Exception(() => Translate(collection, queryable));
            var notSupportedException = exception.Should().BeOfType<ExpressionNotSupportedException>().Subject;
            notSupportedException.Message.Should().Contain("TopN can only be used as an accumulator with GroupBy");
        }

        private IMongoCollection<DocumentWithArray> CreateDocumentsWithArrayCollection()
        {
            var collection = GetCollection<DocumentWithArray>();

            CreateCollection(
                collection,
                new DocumentWithArray { Id = 1, A = new[] { new ArrayElement { X = 1 }, new ArrayElement { X = 2 }, new ArrayElement { X = 3 } } });

            return collection;
        }

        private IMongoCollection<GameScore> CreateGameScoresCollection()
        {
            var collection = GetCollection<GameScore>();

            CreateCollection(
                collection,
                new GameScore { Id = 1, PlayerId = "PlayerA", GameId = "G1", Score = 31 },
                new GameScore { Id = 2, PlayerId = "PlayerB", GameId = "G1", Score = 33 },
                new GameScore { Id = 3, PlayerId = "PlayerC", GameId = "G1", Score = 99 },
                new GameScore { Id = 4, PlayerId = "PlayerD", GameId = "G1", Score = 1 },
                new GameScore { Id = 5, PlayerId = "PlayerA", GameId = "G2", Score = 10 },
                new GameScore { Id = 6, PlayerId = "PlayerB", GameId = "G2", Score = 14 },
                new GameScore { Id = 7, PlayerId = "PlayerC", GameId = "G2", Score = 66 },
                new GameScore { Id = 8, PlayerId = "PlayerD", GameId = "G2", Score = 80 });

            return collection;
        }

        private IMongoCollection<Sale> CreateSalesCollection()
        {
            var collection = GetCollection<Sale>();

            CreateCollection(
                collection,
                new Sale { Id = 1, Item = "abc", Price = 10.00, Quantity = 2, Date = DateTime.Parse("2014-01-01T08:00:00Z", null, DateTimeStyles.AdjustToUniversal) },
                new Sale { Id = 2, Item = "jkl", Price = 20.00, Quantity = 1, Date = DateTime.Parse("2014-02-03T09:00:00Z", null, DateTimeStyles.AdjustToUniversal) },
                new Sale { Id = 3, Item = "xyz", Price = 5.00, Quantity = 5, Date = DateTime.Parse("2014-02-03T09:05:00Z", null, DateTimeStyles.AdjustToUniversal) },
                new Sale { Id = 4, Item = "abc", Price = 10.00, Quantity = 10, Date = DateTime.Parse("2014-02-15T08:00:00Z", null, DateTimeStyles.AdjustToUniversal) },
                new Sale { Id = 5, Item = "xyz", Price = 5.00, Quantity = 10, Date = DateTime.Parse("2014-02-15T09:05:00Z", null, DateTimeStyles.AdjustToUniversal) },
                new Sale { Id = 6, Item = "xyz", Price = 5.00, Quantity = 5, Date = DateTime.Parse("2014-02-15T12:05:10Z", null, DateTimeStyles.AdjustToUniversal) },
                new Sale { Id = 7, Item = "xyz", Price = 5.00, Quantity = 10, Date = DateTime.Parse("2014-02-15T14:12:12Z", null, DateTimeStyles.AdjustToUniversal) });

            return collection;
        }

        public class GameScore
        {
            public int Id { get; set; }
            public string PlayerId { get; set; }
            public string GameId { get; set; }
            public int Score { get; set; }
        }

        public class Sale
        {
            public int Id { get; set; }
            public string Item { get; set; }
            public double Price { get; set; }
            public int Quantity { get; set; }
            public DateTime Date { get; set; }
        }

        public class DocumentWithArray
        {
            public int Id { get; set; }
            public ArrayElement[] A { get; set; }
        }

        public class ArrayElement
        {
            public int X { get; set; }
        }
    }
}
