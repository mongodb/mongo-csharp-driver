/* Copyright 2020-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AggregateGraphLookupEnumerableFromOrToTests
    {
        // public methods
        [SkippableFact]
        public void GraphLookup_with_many_to_one_parameters_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateGraphLookupStage);
            var database = GetDatabase();
            var collectionName = "collectionManyToOne";
            EnsureTestDataManyToOne(database, collectionName);
            var expectedResult = new ManyToOneResult[]
            {
                new ManyToOneResult
                {
                    Id = 1,
                    From = new int[] { 2, 3 },
                    To = 1,
                    Matches = new List<ManyToOne> { new ManyToOne { Id = 2, From = new[] { 3, 4 }, To = 2 } }
                },
                new ManyToOneResult
                {
                    Id = 2,
                    From = new[] { 3, 4 },
                    To = 2,
                    Matches = new List<ManyToOne>()
                }
            };
            var collection = database.GetCollection<ManyToOne>(collectionName);

            var result = collection
                .Aggregate()
                .GraphLookup(
                    from: collection,
                    connectFromField: x => x.From,
                    connectToField: x => x.To,
                    startWith: x => x.From,
                    @as: (ManyToOneResult x) => x.Matches)
                .ToList();

            result.Count.Should().Be(2);
            result[0].ToBsonDocument().Should().Be(expectedResult[0].ToBsonDocument());
            result[1].ToBsonDocument().Should().Be(expectedResult[1].ToBsonDocument());
        }

        [SkippableFact]
        public void GraphLookup_with_one_to_many_parameters_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateGraphLookupStage);
            var database = GetDatabase();
            var collectionName = "collectionOneToMany";
            EnsureTestDataOneToMany(database, collectionName);
            var expectedResult = new OneToManyResult[]
            {
                new OneToManyResult
                {
                    Id = 1,
                    From = 1,
                    To = new[] { 2, 3 },
                    Matches = new List<OneToMany>()
                },
                new OneToManyResult
                {
                    Id = 2,
                    From = 2,
                    To = new[] { 3, 4 },
                    Matches = new List<OneToMany> { new OneToMany { Id = 1, From = 1, To = new[] { 2, 3 } } }
                }
            };
            var collection = database.GetCollection<OneToMany>(collectionName);

            var result = collection
                .Aggregate()
                .GraphLookup(
                    from: collection,
                    connectFromField: x => x.From,
                    connectToField: x => x.To,
                    startWith: x => x.From,
                    @as: (OneToManyResult x) => x.Matches)
                .ToList();

            result.Count.Should().Be(2);
            result[0].ToBsonDocument().Should().Be(expectedResult[0].ToBsonDocument());
            result[1].ToBsonDocument().Should().Be(expectedResult[1].ToBsonDocument());
        }

        [SkippableFact]
        public void GraphLookup_with_one_to_one_parameters_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateGraphLookupStage);
            var database = GetDatabase();
            var collectionName = "collectionOneToOne";
            EnsureTestDataOneToOne(database, collectionName);
            var expectedResult = new OneToOneResult[]
            {
                new OneToOneResult
                {
                    Id = 1,
                    From = 1,
                    To = 2,
                    Matches = new List<OneToOne>()
                },
                new OneToOneResult
                {
                    Id = 2,
                    From = 2,
                    To = 3,
                    Matches = new List<OneToOne> { new OneToOne { Id = 1, From = 1, To = 2 } }
                }
            };
            var collection = database.GetCollection<OneToOne>(collectionName);

            var result = collection
                .Aggregate()
                .GraphLookup(
                    from: collection,
                    connectFromField: x => x.From,
                    connectToField: x => x.To,
                    startWith: x => x.From,
                    @as: (OneToOneResult x) => x.Matches)
                .ToList();

            result.Count.Should().Be(2);
            result[0].ToBsonDocument().Should().Be(expectedResult[0].ToBsonDocument());
            result[1].ToBsonDocument().Should().Be(expectedResult[1].ToBsonDocument());
        }

        // private methods
        private void EnsureTestDataOneToOne(IMongoDatabase database, string collectionName)
        {
            database.DropCollection(collectionName);
            var collection = database.GetCollection<OneToOne>(collectionName);
            var documents = new OneToOne[]
            {
                new OneToOne { Id = 1, From = 1, To = 2 },
                new OneToOne { Id = 2, From = 2, To = 3 },
            };
            collection.InsertMany(documents);
        }

        private void EnsureTestDataOneToMany(IMongoDatabase database, string collectionName)
        {
            database.DropCollection(collectionName);
            var collection = database.GetCollection<OneToMany>(collectionName);
            var documents = new OneToMany[]
            {
                new OneToMany { Id = 1, From = 1, To = new[] { 2, 3 } },
                new OneToMany { Id = 2, From = 2, To = new[] { 3, 4 } },
            };
            collection.InsertMany(documents);
        }

        private void EnsureTestDataManyToOne(IMongoDatabase database, string collectionName)
        {
            database.DropCollection(collectionName);
            var collection = database.GetCollection<ManyToOne>(collectionName);
            var documents = new ManyToOne[]
            {
                new ManyToOne { Id = 1, From = new[] { 2, 3 }, To = 1 },
                new ManyToOne { Id = 2, From = new[] { 3, 4 }, To = 2 },
            };
            collection.InsertMany(documents);
        }

        private IMongoDatabase GetDatabase()
        {
            var client = DriverTestConfiguration.Client;
            var databaseName = CoreTestConfiguration.DatabaseNamespace.DatabaseName;
            return client.GetDatabase(databaseName);
        }

        // nested types
        private class ManyToOne
        {
            public int Id { get; set; }
            public IEnumerable<int> From { get; set; }
            public int To { get; set; }
        }

        private class ManyToOneResult
        {
            public int Id { get; set; }
            public IEnumerable<int> From { get; set; }
            public int To { get; set; }
            public List<ManyToOne> Matches { get; set; }
        }

        private class OneToMany
        {
            public int Id { get; set; }
            public int From { get; set; }
            public IEnumerable<int> To { get; set; }
        }

        private class OneToManyResult
        {
            public int Id { get; set; }
            public int From { get; set; }
            public IEnumerable<int> To { get; set; }
            public List<OneToMany> Matches { get; set; }
        }

        private class OneToOne
        {
            public int Id { get; set; }
            public int From { get; set; }
            public int To { get; set; }
        }

        private class OneToOneResult
        {
            public int Id { get; set; }
            public int From { get; set; }
            public int To { get; set; }
            public List<OneToOne> Matches { get; set; }
        }
    }
}
