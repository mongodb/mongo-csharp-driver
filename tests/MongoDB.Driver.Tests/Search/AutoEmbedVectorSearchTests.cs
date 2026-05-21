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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Search;

[Trait("Category", "AtlasSearch")]
[Trait("Category", "Integration")]
[Collection(AtlasSearchCollection.Name)]
public class AutoEmbedVectorSearchTests : LoggableTestClass
{
    // Generous per-test budget: the fixture's auto-embed seeder may run on first access,
    // which involves Voyage-AI API round-trips for every seeded document.
    private const int Timeout = 15 * 60 * 1000;

    private readonly IMongoCollection<Movie> _collection;
    private readonly string _autoEmbedIndexName;

    public AutoEmbedVectorSearchTests(AtlasSearchFixture fixture, ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        RequireEnvironment.Check().EnvironmentVariable("ATLAS_SEARCH_TESTS_ENABLED");
        RequireEnvironment.Check().EnvironmentVariable("ATLAS_SEARCH_URI");
        // Auto-embedding needs a Voyage AI key provisioned via the Atlas UI (authenticates
        // against ai.mongodb.com, not api.voyageai.com) on the Atlas Local container.
        // Gated separately so contributors without that key can still run the rest of the suite.
        RequireEnvironment.Check().EnvironmentVariable("AUTO_EMBEDDING_TESTS_ENABLED");

        _collection = fixture.GetAutoEmbedMoviesCollection<Movie>();
        _autoEmbedIndexName = AtlasSearchFixture.AutoEmbedIndexName;

        // The fixture's EventCapturer is shared across every test class in the
        // collection. We don't assert on it here, but clear it so captured aggregate
        // events from AutoEmbedVectorSearchTests don't accumulate without bound.
        fixture.EventCapturer?.Clear();
    }

    [Fact(Timeout = Timeout)]
    public async Task VectorSearchAutoEmbed()
    {
        var vectorText = "Brits and tigers";

        var options = new VectorSearchOptions<Movie> { IndexName = _autoEmbedIndexName };

        var query = _collection
            .Aggregate()
            .VectorSearch(m => m.Plot, vectorText, 5, options)
            .Project<Movie>(Builders<Movie>.Projection
                .Include(m => m.Title)
                .MetaVectorSearchScore(p => p.Score));

        var results = query.ToList();

        // The seed has 4 movies; limit=5 returns all 4.
        results.Should().HaveCount(4);
        results.Select(m => m.Title).Should().BeEquivalentTo(
            new[] { "Tigers", "Dunkirk", "Old War", "Family Saga" });
        results.Should().BeInDescendingOrder(m => m.Score);

        results = await query.ToListAsync();

        results.Should().HaveCount(4);
        results.Select(m => m.Title).Should().BeEquivalentTo(
            new[] { "Tigers", "Dunkirk", "Old War", "Family Saga" });
        results.Should().BeInDescendingOrder(m => m.Score);
    }

    [Fact(Timeout = Timeout)]
    public async Task VectorSearchAutoEmbed_Filters()
    {
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

        // Only the two movies satisfying both filter clauses remain in the seed.
        results.Select(m => m.Title).Should().BeEquivalentTo(new[] { "Tigers", "Dunkirk" });
        results.Should().BeInDescendingOrder(m => m.Score);

        results = await query.ToListAsync();

        results.Select(m => m.Title).Should().BeEquivalentTo(new[] { "Tigers", "Dunkirk" });
        results.Should().BeInDescendingOrder(m => m.Score);
    }

    [Fact(Timeout = Timeout)]
    public async Task VectorSearchAutoEmbed_Exact()
    {
        var vectorText = "Brits and tigers";

        var options = new VectorSearchOptions<Movie> { IndexName = _autoEmbedIndexName, Exact = true };

        var query = _collection
            .Aggregate()
            .VectorSearch(m => m.Plot, vectorText, 5, options)
            .Project<Movie>(Builders<Movie>.Projection
                .Include(m => m.Title)
                .MetaVectorSearchScore(p => p.Score));

        var results = query.ToList();

        results.Should().HaveCount(4);
        results.Select(m => m.Title).Should().BeEquivalentTo(
            new[] { "Tigers", "Dunkirk", "Old War", "Family Saga" });
        results.Should().BeInDescendingOrder(m => m.Score);

        results = await query.ToListAsync();

        results.Should().HaveCount(4);
        results.Select(m => m.Title).Should().BeEquivalentTo(
            new[] { "Tigers", "Dunkirk", "Old War", "Family Saga" });
        results.Should().BeInDescendingOrder(m => m.Score);
    }

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
