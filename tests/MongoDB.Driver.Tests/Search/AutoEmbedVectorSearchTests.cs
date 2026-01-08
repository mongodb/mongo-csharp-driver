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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Search;

[Trait("Category", "AtlasSearch")]
[Trait("Category", "Integration")]
public class AutoEmbedVectorSearchTests : LoggableTestClass
{
    private const int Timeout = 5 * 60 * 1000;

    private readonly IMongoCollection<Movie> _collection;
    private readonly IMongoClient _mongoClient;
    private readonly string _autoEmbedIndexName;

    public AutoEmbedVectorSearchTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        RequireEnvironment.Check().EnvironmentVariable("ATLAS_SEARCH_TESTS_ENABLED");

        _mongoClient = new MongoClient(CoreTestConfiguration.ConnectionString.ToString());
        _collection = _mongoClient.GetDatabase("dotnet-test").GetCollection<Movie>(GetRandomName());
        _autoEmbedIndexName = GetRandomName();

        _collection.InsertMany([
            new Movie { Title = "Tigers on the Moon", Plot = "Tigers escape from a moonbase and run amok in the lunar dust.", Runtime = 60, Year = 1986 },
            new Movie { Title = "Red Dawn", Plot = "A group of teenagers form a guerrilla army to fight off an invading force.", Runtime = 114, Year = 1984 },
            new Movie { Title = "Sands of Iwo Jima", Plot = "A tough sergeant leads his platoon of recruits through the battle of Iwo Jima.", Runtime = 100, Year = 1949 },
            new Movie { Title = "White Tiger", Plot = "A Russian tank commander searches for a mysterious German tank during WWII.", Runtime = 104, Year = 2012 },
            new Movie { Title = "P-51 Dragon Fighter", Plot = "Allied pilots in North Africa must fight off a swarm of Nazi dragons.", Runtime = 85, Year = 2014 },
            new Movie { Title = "When Trumpets Fade", Plot = "A soldier's struggle for survival during the Battle of Hurtgen Forest.", Runtime = 95, Year = 1998 },
            new Movie { Title = "The Great Escape", Plot = "Allied prisoners of war plan a massive escape from a German camp.", Runtime = 172, Year = 1963 },
            new Movie { Title = "Saving Private Ryan", Plot = "Soldiers go behind enemy lines to rescue a paratrooper whose brothers have been killed.", Runtime = 169, Year = 1998 },
            new Movie { Title = "Dunkirk", Plot = "Allied soldiers from Belgium, the British Empire, and France are surrounded by the German Army.", Runtime = 106, Year = 2017 },
            new Movie { Title = "Fury", Plot = "A battle-hardened sergeant and his tank crew fight their way across Germany.", Runtime = 134, Year = 2014 }
        ]);

        _collection.SearchIndexes.CreateOne(new CreateAutoEmbeddingVectorSearchIndexModel<Movie>(
            e => e.Plot, _autoEmbedIndexName, "voyage-4", filterFields: [e => e.Runtime, e => e.Year]));

        var foundIndex = TryGetIndex(_collection, _autoEmbedIndexName, out var indexDocument);
        Debug.Assert(foundIndex);

        if (indexDocument.TryGetElement("status", out _))
        {
            // If index is reporting status, then wait for it to be ready.
            while (TryGetIndex(_collection, _autoEmbedIndexName, out indexDocument)
                   && indexDocument["status"].AsString != "READY")
            {
                Thread.Sleep(5000);
            }
        }
        else
        {
            // If index has no state, then wait for 60 seconds and hope for the best!
            Thread.Sleep(60_000);
        }
    }

    private bool TryGetIndex<TDocument>(
        IMongoCollection<TDocument> collection, string indexName, out BsonDocument indexDefinition)
    {
        indexDefinition = collection.SearchIndexes.List().ToList()
            .SingleOrDefault(i => i["name"].AsString == indexName)?.AsBsonDocument;

        return indexDefinition != null;
    }

    protected override void DisposeInternal()
    {
        _collection.Database.DropCollection(_collection.CollectionNamespace.CollectionName);
        _mongoClient.Dispose();
    }

    [Fact(Timeout = Timeout)]
    public async Task VectorSearchAutoEmbed()
    {
        var expectedTitles = new[]
        {
            "Tigers on the Moon", "Dunkirk", "P-51 Dragon Fighter", "Red Dawn", "Fury"
        };

        var vectorText = "Brits and tigers";

        var options = new VectorSearchOptions<Movie> { IndexName = _autoEmbedIndexName };

        var query = _collection
            .Aggregate()
            .VectorSearch(m => m.Plot, vectorText, 5, options)
            .Project<Movie>(Builders<Movie>.Projection
                .Include(m => m.Title)
                .MetaVectorSearchScore(p => p.Score));

        var results = query.ToList();

        results.Select(m => m.Title).ShouldBeEquivalentTo(expectedTitles);
        results.Should().OnlyContain(m => m.Score > 0.5);

        results = await query.ToListAsync();

        results.Select(m => m.Title).ShouldBeEquivalentTo(expectedTitles);
        results.Should().OnlyContain(m => m.Score > 0.5);
    }

    [Fact(Timeout = Timeout)]
    public async Task VectorSearchAutoEmbed_Filters()
    {
        var expectedTitles = new[]
        {
            "Dunkirk", "P-51 Dragon Fighter", "White Tiger", "When Trumpets Fade"
        };

        var vectorText = "Brits and tigers";

        var options = new VectorSearchOptions<Movie>
        {
            IndexName = _autoEmbedIndexName,
            Filter = Builders<Movie>.Filter.Lt("runtime", 120) & Builders<Movie>.Filter.Gt("year", 1990),
            NumberOfCandidates = 256,
            Exact = false,
            AutoEmbeddingModelName = "voyage-4"
        };

        var query = _collection
            .Aggregate()
            .VectorSearch(m => m.Plot, vectorText, 5, options)
            .Project<Movie>(Builders<Movie>.Projection
                .Include(m => m.Title)
                .MetaVectorSearchScore(p => p.Score));

        var results = query.ToList();

        results.Select(m => m.Title).ShouldBeEquivalentTo(expectedTitles);
        results.Should().OnlyContain(m => m.Score > 0.5);

        results = await query.ToListAsync();

        results.Select(m => m.Title).ShouldBeEquivalentTo(expectedTitles);
        results.Should().OnlyContain(m => m.Score > 0.5);
    }

    [Fact(Timeout = Timeout)]
    public async Task VectorSearchAutoEmbed_Exact()
    {
        var expectedTitles = new[]
        {
            "Tigers on the Moon", "Dunkirk", "P-51 Dragon Fighter", "Red Dawn", "Fury"
        };

        var vectorText = "Brits and tigers";

        var options = new VectorSearchOptions<Movie> { IndexName = _autoEmbedIndexName, Exact = true };

        var query = _collection
            .Aggregate()
            .VectorSearch(m => m.Plot, vectorText, 5, options)
            .Project<Movie>(Builders<Movie>.Projection
                .Include(m => m.Title)
                .MetaVectorSearchScore(p => p.Score));

        var results = query.ToList();

        results.Select(m => m.Title).ShouldBeEquivalentTo(expectedTitles);
        results.Should().OnlyContain(m => m.Score > 0.5);

        results = await query.ToListAsync();

        results.Select(m => m.Title).ShouldBeEquivalentTo(expectedTitles);
        results.Should().OnlyContain(m => m.Score > 0.5);
    }

    private static string GetRandomName() => $"test_{Guid.NewGuid():N}";

    [BsonIgnoreExtraElements]
    public class Movie
    {
        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("plot")]
        public string Plot { get; set; }

        [BsonElement("year")]
        public int Year { get; set; }

        [BsonElement("runtime")]
        public int Runtime { get; set; }

        [BsonElement("score")]
        public double Score { get; set; }
    }
}
