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
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Tests.Search
{
    /// <summary>
    /// Shared XUnit fixture for all Atlas Search test classes. Owns the MongoClient and seeds
    /// every collection / index needed by the suite into a single ephemeral
    /// <c>atlas_search_&lt;guid&gt;</c> database, so individual tests no longer depend on a
    /// pre-configured Atlas deployment. Bound to test classes via <see cref="AtlasSearchCollection"/>.
    /// </summary>
    public sealed class AtlasSearchFixture : IDisposable
    {
        private const int IndexesPollPeriod = 10_000;
        private const int IndexReadyTimeoutMs = 5 * 60 * 1000;

        // Collection names used inside the fixture-owned database.
        public const string HistoricalDocumentsName = "historical_documents";
        public const string MoviesName = "movies";
        public const string EmbeddedMoviesName = "embedded_movies";
        public const string AirbnbListingsName = "airbnb_listings";
        public const string TestClassesName = "test_classes";
        public const string BinaryVectorItemsName = "binary_vector_items";
        public const string ReturnScopeDirectorsName = "directors";
        public const string TransportSynonymsName = "synonyms_transport";
        public const string AttireSynonymsName = "synonyms_attire";
        public const string AutoEmbedMoviesName = "auto_embed_movies";
        public const string AutoEmbedIndexName = "auto_embed_movies_index";

        // Auto-embedding indexes go through the Voyage AI API, which is rate-limited and slow.
        // Allow more time than the regular search-index wait.
        private const int AutoEmbedIndexReadyTimeoutMs = 15 * 60 * 1000;

        private readonly object _initLock = new();

        // Per-collection one-time-init guards
        private bool _historicalInitialized;
        private bool _moviesInitialized;
        private bool _embeddedMoviesInitialized;
        private bool _airbnbInitialized;
        private bool _testClassesInitialized;
        private bool _binaryVectorInitialized;
        private bool _autoEmbedInitialized;
        private bool _returnScopeInitialized;
        private bool? _isRerankSupported;

        public AtlasSearchFixture()
        {
            // The fixture must be tolerant of missing env vars so xUnit can skip individual
            // tests via RequireEnvironment.Check() rather than failing the whole collection setup.
            var atlasSearchUri = Environment.GetEnvironmentVariable("ATLAS_SEARCH_URI");
            if (string.IsNullOrEmpty(atlasSearchUri))
            {
                return;
            }

            var settings = MongoClientSettings.FromConnectionString(atlasSearchUri);
            settings.ClusterSource = DisposingClusterSource.Instance;

            EventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "aggregate");
            settings.ClusterConfigurator = b => b.Subscribe(EventCapturer);

            Client = new MongoClient(settings);
            DatabaseName = "atlas_search_" + Guid.NewGuid().ToString("N");
        }

        public IMongoClient Client { get; }

        public EventCapturer EventCapturer { get; }

        public string DatabaseName { get; }

        /// <summary>
        /// Lazily probes for $rerank support on first access. Tests guard themselves on this so the
        /// suite continues to work on Atlas deployments that don't yet ship the rerank model.
        /// </summary>
        public bool IsRerankSupported
        {
            get
            {
                EnsureMoviesInitialized();
                if (_isRerankSupported is bool cached)
                {
                    return cached;
                }
                lock (_initLock)
                {
                    if (_isRerankSupported is bool again)
                    {
                        return again;
                    }
                    _isRerankSupported = ProbeRerankSupport();
                    // ProbeRerankSupport fires a real $search+$rerank aggregate; clear so
                    // tests asserting on captured aggregate events don't see the probe.
                    EventCapturer?.Clear();
                    return _isRerankSupported.Value;
                }
            }
        }

        // ---- Typed collection accessors ----

        public IMongoCollection<T> GetHistoricalDocumentsCollection<T>()
        {
            EnsureHistoricalInitialized();
            return Database.GetCollection<T>(HistoricalDocumentsName);
        }

        public IMongoCollection<T> GetMoviesCollection<T>()
        {
            EnsureMoviesInitialized();
            return Database.GetCollection<T>(MoviesName);
        }

        public IMongoCollection<T> GetEmbeddedMoviesCollection<T>()
        {
            EnsureEmbeddedMoviesInitialized();
            return Database.GetCollection<T>(EmbeddedMoviesName);
        }

        public IMongoCollection<T> GetAirbnbListingsCollection<T>()
        {
            EnsureAirbnbInitialized();
            return Database.GetCollection<T>(AirbnbListingsName);
        }

        public IMongoCollection<T> GetTestClassesCollection<T>()
        {
            EnsureTestClassesInitialized();
            return Database.GetCollection<T>(TestClassesName);
        }

        public IMongoCollection<T> GetBinaryVectorItemsCollection<T>()
        {
            EnsureBinaryVectorInitialized();
            return Database.GetCollection<T>(BinaryVectorItemsName);
        }

        public IMongoCollection<T> GetAutoEmbedMoviesCollection<T>()
        {
            EnsureAutoEmbedInitialized();
            return Database.GetCollection<T>(AutoEmbedMoviesName);
        }

        public IMongoCollection<T> GetReturnScopeDirectorsCollection<T>()
        {
            EnsureReturnScopeInitialized();
            return Database.GetCollection<T>(ReturnScopeDirectorsName);
        }

        public IMongoCollection<BsonDocument> GetReturnScopeDirectorsCollection() =>
            GetReturnScopeDirectorsCollection<BsonDocument>();

        public void Dispose()
        {
            if (Client == null)
            {
                return;
            }

            try
            {
                Client.DropDatabase(DatabaseName);
            }
            catch (Exception ex)
            {
                // best-effort cleanup; never block tear-down, but surface the failure so
                // operators can spot leaked test databases on the cluster.
                Console.Error.WriteLine(
                    $"AtlasSearchFixture: failed to drop database '{DatabaseName}': {ex}");
            }

            Client.Dispose();
        }

        // ---- Seeders ----

        private IMongoDatabase Database
        {
            get
            {
                if (Client == null)
                {
                    throw new InvalidOperationException(
                        "AtlasSearchFixture has no client; ATLAS_SEARCH_URI must be set to access seeded collections.");
                }
                return Client.GetDatabase(DatabaseName);
            }
        }

        private void EnsureHistoricalInitialized()
        {
            if (_historicalInitialized) return;
            lock (_initLock)
            {
                if (_historicalInitialized) return;

                var collection = Database.GetCollection<BsonDocument>(HistoricalDocumentsName);
                if (!collection.AsQueryable().Any())
                {
                    collection.InsertMany(AtlasSearchFixtureSeedData.HistoricalDocuments);
                }

                CreateSearchIndexes(collection, new[]
                {
                    new CreateSearchIndexModel("default", BsonDocument.Parse(
                        """
                        {
                          "storedSource": true,
                          "mappings": {
                            "dynamic": true,
                            "fields": {
                              "body": {
                                "type": "string",
                                "multi": {
                                  "english": { "type": "string", "analyzer": "lucene.english" }
                                }
                              },
                              "title":    [{ "type": "string" }, { "type": "token" }, { "type": "autocomplete" }],
                              "author":   [{ "type": "string" }, { "type": "stringFacet" }],
                              "index":    [{ "type": "number" }, { "type": "numberFacet", "representation": "int64" }],
                              "date":     [{ "type": "date" },   { "type": "dateFacet" }],
                              "comments": { "type": "embeddedDocuments", "dynamic": true }
                            }
                          }
                        }
                        """))
                });

                _historicalInitialized = true;
                EventCapturer?.Clear();
            }
        }

        private void EnsureMoviesInitialized()
        {
            if (_moviesInitialized) return;
            lock (_initLock)
            {
                if (_moviesInitialized) return;

                // Synonyms are sourced from two separate small collections; the synonyms-tests
                // index references them by collection name.
                SeedSynonymCollection(TransportSynonymsName, new[]
                {
                    BuildEquivalentSynonym("automobile", "vehicle", "car"),
                    BuildEquivalentSynonym("boat", "ship"),
                });
                SeedSynonymCollection(AttireSynonymsName, new[]
                {
                    BuildEquivalentSynonym("attire", "dress"),
                });

                var collection = Database.GetCollection<BsonDocument>(MoviesName);
                if (!collection.AsQueryable().Any())
                {
                    collection.InsertMany(AtlasSearchFixtureSeedData.Movies);
                }

                CreateSearchIndexes(collection, new[]
                {
                    new CreateSearchIndexModel("default", BsonDocument.Parse(
                        """
                        {
                          "mappings": {
                            "dynamic": true,
                            "fields": {
                              "title":   [{ "type": "string" }, { "type": "token", "normalizer": "lowercase" }],
                              "plot":    { "type": "string" },
                              "genres":  [{ "type": "string" }, { "type": "token", "normalizer": "lowercase" }],
                              "year":    { "type": "number" },
                              "runtime": { "type": "number" }
                            }
                          }
                        }
                        """)),
                    new CreateSearchIndexModel("synonyms-tests", BsonDocument.Parse(
                        $$"""
                        {
                          "analyzer": "lucene.english",
                          "mappings": {
                            "dynamic": false,
                            "fields": {
                              "title": [{ "type": "string", "analyzer": "lucene.english" }],
                              "plot":  [{ "type": "string", "analyzer": "lucene.english" }]
                            }
                          },
                          "synonyms": [
                            { "name": "transportSynonyms", "source": { "collection": "{{TransportSynonymsName}}" }, "analyzer": "lucene.english" },
                            { "name": "attireSynonyms",    "source": { "collection": "{{AttireSynonymsName}}"    }, "analyzer": "lucene.english" }
                          ]
                        }
                        """))
                });

                _moviesInitialized = true;
                EventCapturer?.Clear();
            }
        }

        private void EnsureEmbeddedMoviesInitialized()
        {
            if (_embeddedMoviesInitialized) return;
            lock (_initLock)
            {
                if (_embeddedMoviesInitialized) return;

                var collection = Database.GetCollection<BsonDocument>(EmbeddedMoviesName);
                if (!collection.AsQueryable().Any())
                {
                    collection.InsertMany(AtlasSearchFixtureSeedData.EmbeddedMovies);
                }

                // Three indexes, named to match the existing test assertions. They share the
                // same field shape; the only difference is the test-side reference.
                var searchWithVectorDef = BsonDocument.Parse(
                    """
                    {
                      "mappings": {
                        "dynamic": false,
                        "fields": {
                          "title":    [{ "type": "string" }],
                          "fullplot": [{ "type": "string" }],
                          "year":     [{ "type": "number" }],
                          "runtime":  [{ "type": "number" }],
                          "plot_embedding": [{
                              "type": "knnVector",
                              "dimensions": 1536,
                              "similarity": "cosine"
                          }]
                        }
                      }
                    }
                    """);

                var vectorOnlyDef = BsonDocument.Parse(
                    """
                    {
                      "fields": [
                        { "type": "vector", "path": "plot_embedding", "numDimensions": 1536, "similarity": "cosine" }
                      ]
                    }
                    """);

                CreateSearchIndexes(collection, new[]
                {
                    new CreateSearchIndexModel("search-with-vector", searchWithVectorDef),
                    new CreateSearchIndexModel("sample_mflix__embedded_movies", searchWithVectorDef),
                    new CreateSearchIndexModel("vector_search_embedded_movies", SearchIndexType.VectorSearch, vectorOnlyDef)
                });

                _embeddedMoviesInitialized = true;
                EventCapturer?.Clear();
            }
        }

        private void EnsureAirbnbInitialized()
        {
            if (_airbnbInitialized) return;
            lock (_initLock)
            {
                if (_airbnbInitialized) return;

                var collection = Database.GetCollection<BsonDocument>(AirbnbListingsName);
                if (!collection.AsQueryable().Any())
                {
                    collection.InsertMany(AtlasSearchFixtureSeedData.AirbnbListings);
                }

                CreateSearchIndexes(collection, new[]
                {
                    new CreateSearchIndexModel("default", BsonDocument.Parse(
                        """
                        {
                          "mappings": {
                            "dynamic": false,
                            "fields": {
                              "name":     [{ "type": "string" }, { "type": "token", "normalizer": "lowercase" }],
                              "description": { "type": "string" },
                              "space":       { "type": "string" },
                              "bedrooms": { "type": "number" },
                              "beds":     { "type": "number" },
                              "address": {
                                "type": "document",
                                "fields": {
                                  "location": { "type": "geo", "indexShapes": true },
                                  "street":   { "type": "string" }
                                }
                              }
                            }
                          }
                        }
                        """))
                });

                _airbnbInitialized = true;
                EventCapturer?.Clear();
            }
        }

        private void EnsureTestClassesInitialized()
        {
            if (_testClassesInitialized) return;
            lock (_initLock)
            {
                if (_testClassesInitialized) return;

                var collection = Database.GetCollection<BsonDocument>(TestClassesName);
                if (!collection.AsQueryable().Any())
                {
                    collection.InsertMany(BuildTestClassesSeed());
                }

                CreateSearchIndexes(collection, new[]
                {
                    new CreateSearchIndexModel("default", BsonDocument.Parse(
                        """
                        {
                          "mappings": {
                            "dynamic": true
                          }
                        }
                        """))
                });

                _testClassesInitialized = true;
                EventCapturer?.Clear();
            }
        }

        private void EnsureBinaryVectorInitialized()
        {
            if (_binaryVectorInitialized) return;
            lock (_initLock)
            {
                if (_binaryVectorInitialized) return;

                var collection = Database.GetCollection<BsonDocument>(BinaryVectorItemsName);
                if (!collection.AsQueryable().Any())
                {
                    collection.InsertMany(BuildBinaryVectorSeed());
                }

                CreateSearchIndexes(collection, new[]
                {
                    new CreateSearchIndexModel("vector_search_index", SearchIndexType.VectorSearch, BsonDocument.Parse(
                        """
                        {
                          "fields": [
                            { "type": "vector", "path": "int8Vector",    "numDimensions": 5, "similarity": "cosine" },
                            { "type": "vector", "path": "float32Vector", "numDimensions": 5, "similarity": "cosine" }
                          ]
                        }
                        """))
                });

                _binaryVectorInitialized = true;
                EventCapturer?.Clear();
            }
        }

        private void EnsureAutoEmbedInitialized()
        {
            if (_autoEmbedInitialized) return;

            // Building the auto-embed index can block for up to AutoEmbedIndexReadyTimeoutMs
            // (~15 minutes) on a real Voyage AI round-trip. Refuse to enter the seeder
            // unless the caller has explicitly opted in via AUTO_EMBEDDING_TESTS_ENABLED so
            // that mis-routed tests fail fast instead of timing out the test runner.
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AUTO_EMBEDDING_TESTS_ENABLED")))
            {
                throw new InvalidOperationException(
                    "AtlasSearchFixture.EnsureAutoEmbedInitialized was called without " +
                    "AUTO_EMBEDDING_TESTS_ENABLED set. Auto-embedding requires a Voyage AI " +
                    "key provisioned on the Atlas Local container; gate the calling test " +
                    "with RequireEnvironment.Check().EnvironmentVariable(\"AUTO_EMBEDDING_TESTS_ENABLED\").");
            }
            lock (_initLock)
            {
                if (_autoEmbedInitialized) return;

                var collection = Database.GetCollection<BsonDocument>(AutoEmbedMoviesName);
                if (!collection.AsQueryable().Any())
                {
                    // Minimum corpus for AutoEmbedVectorSearchTests: 2 docs that pass
                    // (runtime < 120 ∧ year > 1990) and 2 that don't, so filter-on and
                    // filter-off branches both have selection pressure. Each doc costs
                    // one Voyage-AI embedding call at index-build time.
                    collection.InsertMany(new[]
                    {
                        BuildAutoEmbedMovie("Tigers", "Tigers escape and run amok.", runtime: 60, year: 1995),
                        BuildAutoEmbedMovie("Dunkirk", "Allied soldiers evacuate from a beach.", runtime: 100, year: 2000),
                        BuildAutoEmbedMovie("Old War", "Soldiers fight in muddy trenches.", runtime: 150, year: 1980),
                        BuildAutoEmbedMovie("Family Saga", "A family struggles across decades.", runtime: 200, year: 2010)
                    });
                }

                if (!HasIndex(collection, AutoEmbedIndexName))
                {
                    collection.SearchIndexes.CreateOne(
                        new CreateAutoEmbeddingVectorSearchIndexModel<BsonDocument>(
                            "plot", AutoEmbedIndexName, "voyage-4", "runtime", "year"));
                }

                WaitForAutoEmbedIndexReady(collection, AutoEmbedIndexName);

                _autoEmbedInitialized = true;
                EventCapturer?.Clear();
            }
        }

        private static BsonDocument BuildAutoEmbedMovie(string title, string plot, int runtime, int year) =>
            new BsonDocument
            {
                { "title", title },
                { "plot", plot },
                { "runtime", runtime },
                { "year", year }
            };

        private static bool HasIndex(IMongoCollection<BsonDocument> collection, string indexName) =>
            collection.SearchIndexes.List().ToList()
                .Any(d => d.TryGetValue("name", out var n) && n.AsString == indexName);

        private static void WaitForAutoEmbedIndexReady(IMongoCollection<BsonDocument> collection, string indexName)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(AutoEmbedIndexReadyTimeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                var idx = collection.SearchIndexes.List().ToList()
                    .FirstOrDefault(d => d.TryGetValue("name", out var n) && n.AsString == indexName);

                if (idx != null && idx.TryGetValue("status", out var statusValue))
                {
                    var status = statusValue.AsString;
                    if (status == "READY")
                    {
                        return;
                    }
                    if (status == "FAILED")
                    {
                        throw new InvalidOperationException(
                            $"Auto-embedding index '{indexName}' on {collection.CollectionNamespace} failed to build: {idx}");
                    }
                }

                Thread.Sleep(IndexesPollPeriod);
            }

            throw new TimeoutException(
                $"Timed out after {AutoEmbedIndexReadyTimeoutMs / 1000}s waiting for auto-embedding " +
                $"index '{indexName}' on {collection.CollectionNamespace} to reach READY. " +
                "Voyage AI API quota or connectivity may be exhausted; verify the Atlas Local container has VOYAGE_API_KEY set.");
        }

        private void EnsureReturnScopeInitialized()
        {
            if (_returnScopeInitialized) return;
            lock (_initLock)
            {
                if (_returnScopeInitialized) return;

                var directors =
                    """
                    [
                    {
                        _id: 0,
                        director: "Peyton Reed",
                        birthDate: "1964-07-03",
                        age: 35,
                        movies: [
                          {
                            title: "Ant-Man",
                            release: 2015,
                            genre: "Action",
                            reviews: [
                              { rating: 8.5, text: "Funny and thrilling" },
                              { rating: 8.0, text: "Great cast performances" }
                            ]
                          },
                          {
                            title: "Ant-Man-2",
                            release: 2017,
                            genre: "Action",
                            reviews: [
                              { rating: 8.5, text: "Funny and thrilling" },
                              { rating: 8.0, text: "Great cast performances" }
                            ]
                          },
                          {
                            title: "Yes Man",
                            release: 2008,
                            genre: "Comedy",
                            reviews: [
                              { rating: 7.0, text: "Hilarious and uplifting" },
                              { rating: 6.5, text: "Feel-good comedy flick" }
                            ]
                          }
                        ]
                      },
                      {
                        _id: 1,
                        director: "M. Night Shyamalan",
                        birthDate: "1970-08-06",
                        age: 25,
                        movies: [
                          {
                            title: "The Sixth Sense",
                            releaseYear: 1999,
                            genre: "Thriller",
                            reviews: [
                              { rating: 9.0, text: "Mind-blowing and suspenseful" },
                              { rating: 8.5, text: "Incredible plot twist" },
                              { rating: 5.5, text: "Amazing plot twist" }
                            ]
                          },
                          {
                            title: "Split",
                            releaseYear: 2016,
                            genre: "Thriller",
                            reviews: [
                              { rating: 8.0, text: "Intense psychological thriller" },
                              { rating: 7.5, text: "Amazing lead performance" }
                            ]
                          }
                        ]
                      }
                    ]
                    """;

                var collection = Database.GetCollection<BsonDocument>(ReturnScopeDirectorsName);
                if (!collection.AsQueryable().Any())
                {
                    collection.InsertMany(
                        BsonSerializer.Deserialize<BsonArray>(directors).Select(e => e.AsBsonDocument));
                }

                CreateSearchIndexes(collection, new[]
                {
                    new CreateSearchIndexModel("returnScopeIndex1", BsonDocument.Parse(
                        """
                        {
                          "mappings": {
                            "dynamic": true,
                            "fields": {
                              "movies": {
                                  "type": "embeddedDocuments",
                                  "dynamic": true,
                                  "storedSource": { "exclude": ["title"] }
                              }
                            }
                          }
                        }
                        """)),
                    new CreateSearchIndexModel("returnScopeIndex2", BsonDocument.Parse(
                        """
                        {
                          "mappings": {
                            "dynamic": true,
                            "fields": {
                              "movies": {
                                  "type": "embeddedDocuments",
                                  "dynamic": true,
                                  "storedSource": { "exclude": ["title"] },
                                  "fields": {
                                    "reviews": {
                                      "type": "embeddedDocuments",
                                      "storedSource": { "exclude": ["author", "creation_time"] }
                                    }
                                  }
                              }
                            }
                          }
                        }
                        """)),
                    new CreateSearchIndexModel("returnScopeIndex3", BsonDocument.Parse(
                        """
                        {
                          "mappings": {
                            "fields": {
                              "movies": {
                                "type": "embeddedDocuments",
                                "fields": {
                                  "reviews": {
                                    "type": "embeddedDocuments",
                                    "storedSource": { "exclude": ["author", "creation_time"] }
                                  }
                                }
                              }
                            }
                          }
                        }
                        """))
                });

                _returnScopeInitialized = true;
                EventCapturer?.Clear();
            }
        }

        // ---- Helpers ----

        private void SeedSynonymCollection(string name, IEnumerable<BsonDocument> docs)
        {
            var collection = Database.GetCollection<BsonDocument>(name);
            if (!collection.AsQueryable().Any())
            {
                collection.InsertMany(docs);
            }
        }

        private static BsonDocument BuildEquivalentSynonym(params string[] synonyms) =>
            new BsonDocument
            {
                { "mappingType", "equivalent" },
                { "synonyms", new BsonArray(synonyms) }
            };

        private static IEnumerable<BsonDocument> BuildTestClassesSeed()
        {
            // Tests pin specific Guids; surrounding docs provide background for In/Equals queries.
            var test6Guid = Guid.Parse("b52af144-bc97-454f-a578-418a64fa95bf");
            var test7Guid = Guid.Parse("84da5d44-bc97-454f-a578-418a64fa937a");

            yield return BuildTestClass("test1", "alpha", Guid.NewGuid());
            yield return BuildTestClass("test2", "beta", Guid.NewGuid());
            yield return BuildTestClass("test3", "gamma", Guid.NewGuid());
            yield return BuildTestClass("test4", "delta", Guid.NewGuid());
            yield return BuildTestClass("test5", "epsilon", Guid.NewGuid());
            yield return BuildTestClass("test6", "zeta", test6Guid);
            yield return BuildTestClass("test7", "eta", test7Guid);
            yield return BuildTestClass("testNull", null, Guid.NewGuid());
        }

        private static BsonDocument BuildTestClass(string name, string testString, Guid testGuid) =>
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() },
                { "name", name },
                { "testString", testString == null ? BsonNull.Value : (BsonValue)testString },
                { "testGuid", new BsonBinaryData(testGuid, GuidRepresentation.Standard) }
            };

        private static IEnumerable<BsonDocument> BuildBinaryVectorSeed()
        {
            // Doc 0 is the perfect cosine-1 match for both vector tests.
            yield return BuildBinaryVectorItem(
                new sbyte[] { 0, 1, 2, 3, 4 },
                new[] { 0.0001f, 1.12345f, 2.23456f, 3.34567f, 4.45678f },
                year: 2024);

            // Four background docs with vectors that produce strictly-decreasing cosine to both queries.
            yield return BuildBinaryVectorItem(
                new sbyte[] { 1, 2, 3, 4, 5 },
                new[] { 0.001f, 1.1f, 2.2f, 3.3f, 4.4f },
                year: 2023);
            yield return BuildBinaryVectorItem(
                new sbyte[] { 2, 3, 4, 5, 6 },
                new[] { 0.01f, 1.0f, 2.0f, 3.0f, 4.0f },
                year: 2022);
            yield return BuildBinaryVectorItem(
                new sbyte[] { 3, 4, 5, 6, 7 },
                new[] { 0.1f, 0.9f, 1.8f, 2.7f, 3.6f },
                year: 2021);
            yield return BuildBinaryVectorItem(
                new sbyte[] { 5, 6, 7, 8, 9 },
                new[] { 1.0f, 0.5f, 0.5f, 0.5f, 0.5f },
                year: 2020);
        }

        private static BsonDocument BuildBinaryVectorItem(sbyte[] int8, float[] float32, int year) =>
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() },
                { "int8Vector", new BinaryVectorInt8(int8).ToBsonBinaryData() },
                { "float32Vector", new BinaryVectorFloat32(float32).ToBsonBinaryData() },
                { "year", year }
            };

        private void CreateSearchIndexes(IMongoCollection<BsonDocument> collection, CreateSearchIndexModel[] indexes)
        {
            // Don't recreate indexes that already exist; speeds up reruns when a previous test
            // session left the collection populated (e.g., when reusing DatabaseName during debugging).
            var existing = collection.SearchIndexes.List().ToList()
                .Select(d => d["name"].AsString)
                .ToHashSet();

            var toCreate = indexes.Where(i => !existing.Contains(i.Name)).ToArray();
            if (toCreate.Length > 0)
            {
                collection.SearchIndexes.CreateMany(toCreate);
            }

            WaitForIndexesReady(collection, indexes.Select(i => i.Name).ToArray());
        }

        private static void WaitForIndexesReady(IMongoCollection<BsonDocument> collection, string[] indexNames)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(IndexReadyTimeoutMs);
            while (true)
            {
                var indexes = collection.SearchIndexes.List().ToList();
                var matches = indexes
                    .Where(d => d.TryGetValue("name", out var n) && indexNames.Contains(n.AsString))
                    .ToList();

                if (matches.Count == indexNames.Length &&
                    matches.All(i => i.TryGetValue("status", out var s) && s.AsString == "READY"))
                {
                    return;
                }

                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException(
                        $"Timed out waiting for search indexes [{string.Join(", ", indexNames)}] on " +
                        $"{collection.CollectionNamespace} to reach READY status.");
                }

                Thread.Sleep(IndexesPollPeriod);
            }
        }

        private bool ProbeRerankSupport()
        {
            try
            {
                var moviesCollection = Database.GetCollection<BsonDocument>(MoviesName);
                var pipeline = new[]
                {
                    BsonDocument.Parse("""{ "$search": { "text": { "query": "x", "path": "title" } } }"""),
                    BsonDocument.Parse("""{ "$rerank": { "query": { "text": "probe" }, "path": "title", "limit": 1, "model": "rerank-2.5-lite" } }"""),
                    BsonDocument.Parse("""{ "$limit": 1 }""")
                };
                moviesCollection.Aggregate<BsonDocument>(pipeline).ToList();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
