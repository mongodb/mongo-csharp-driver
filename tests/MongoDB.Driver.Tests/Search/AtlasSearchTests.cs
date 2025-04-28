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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.Search;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;
using Builders = MongoDB.Driver.Builders<MongoDB.Driver.Tests.Search.AtlasSearchTests.HistoricalDocument>;
using GeoBuilders = MongoDB.Driver.Builders<MongoDB.Driver.Tests.Search.AtlasSearchTests.AirbnbListing>;

namespace MongoDB.Driver.Tests.Search
{
    [Trait("Category", "AtlasSearch")]
    public class AtlasSearchTests : LoggableTestClass
    {
        #region static

        private static readonly GeoJsonPolygon<GeoJson2DGeographicCoordinates> __testPolygon =
            new(new(new(new GeoJson2DGeographicCoordinates[]
            {
                new(-8.6131, 41.14),
                new(-8.6131, 41.145),
                new(-8.60308, 41.145),
                new(-8.60308, 41.14),
                new(-8.6131, 41.14),
            })));

        private static readonly GeoWithinBox<GeoJson2DGeographicCoordinates> __testBox =
            new(new(new(-8.6131, 41.14)), new(new(-8.60308, 41.145)));

        private static readonly GeoWithinCircle<GeoJson2DGeographicCoordinates> __testCircle =
            new(new(new(-8.61308, 41.1413)), 273);

        #endregion

        private readonly IMongoClient _mongoClient;

        public AtlasSearchTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            RequireEnvironment.Check().EnvironmentVariable("ATLAS_SEARCH_TESTS_ENABLED");

            var atlasSearchUri = Environment.GetEnvironmentVariable("ATLAS_SEARCH");
            Ensure.IsNotNullOrEmpty(atlasSearchUri, nameof(atlasSearchUri));

            var mongoClientSettings = MongoClientSettings.FromConnectionString(atlasSearchUri);
            mongoClientSettings.ClusterSource = DisposingClusterSource.Instance;

            _mongoClient = new MongoClient(atlasSearchUri);
        }

        protected override void DisposeInternal() => _mongoClient.Dispose();

        [Fact]
        public void Autocomplete()
        {
            var result = SearchSingle(Builders.Search.Autocomplete(x => x.Title, "Declaration of Ind"));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Compound()
        {
            const int score = 42;
            var searchDefinition = Builders.Search.Compound(Builders.SearchScore.Constant(score))
                .Must(Builders.Search.Text(x => x.Body, "life"), Builders.Search.Text(x => x.Body, "liberty"))
                .MustNot(Builders.Search.Text(x => x.Body, "property"))
                .Must(Builders.Search.Text(x => x.Body, "pursuit of happiness"));

            var projectionDefinition = Builders.Projection
                .Include(x => x.Body)
                .Include(x => x.Title)
                .MetaSearchScore(x => x.Score);

            var result = SearchSingle(searchDefinition, projectionDefinition);
            result.Title.Should().Be("Declaration of Independence");
            result.Score.Should().Be(score);
        }

        [Fact]
        public void Count_total()
        {
            var results = GetTestCollection().Aggregate()
                .Search(
                    Builders.Search.Phrase(x => x.Body, "life, liberty, and the pursuit of happiness"),
                    count: new SearchCountOptions()
                    {
                        Type = SearchCountType.Total
                    })
                .Project<HistoricalDocument>(Builders.Projection.SearchMeta(x => x.MetaResult))
                .Limit(1)
                .ToList();
            results.Should().ContainSingle().Which.MetaResult.Count.Total.Should().Be(108);
        }

        [Fact]
        public void EmbeddedDocument()
        {
            var builderHistoricalDocument = Builders<HistoricalDocumentWithCommentsOnly>.Search;
            var builderComments = Builders<Comment>.Search;

            var result = GetTestCollection< HistoricalDocumentWithCommentsOnly>()
                .Aggregate()
                .Search(builderHistoricalDocument.EmbeddedDocument(
                    p => p.Comments,
                    builderComments.Text(p => p.Author, "Corliss Zuk")))
                .Limit(10)
                .ToList();

            foreach (var document in result)
            {
                document.Comments.Should().Contain(c => c.Author == "Corliss Zuk");
            }
        }

        [Fact]
        public void EqualsGuid()
        {
            var testGuid = Guid.Parse("b52af144-bc97-454f-a578-418a64fa95bf");

            var result = GetExtraTestsCollection().Aggregate()
                .Search(Builders<TestClass>.Search.Equals(c => c.TestGuid,  testGuid))
                .Single();

            result.Name.Should().Be("test6");
        }

        [Fact]
        public void EqualsNull()
        {
            var result = GetExtraTestsCollection().Aggregate()
                .Search(Builders<TestClass>.Search.Equals(c => c.TestString,  null))
                .Single();

            result.Name.Should().Be("testNull");
        }
        
        [Fact]
        public void EqualsArrayField()
        {
            var results = GetSynonymTestCollection().Aggregate()
                .Search(Builders<Movie>.Search.Equals(p => p.Genres, "family"))
                .Limit(3)
                .ToList();
            
            results.Should().HaveCount(3);
            foreach (var result in results)
            {
                result.Genres.Should().Contain("Family");
            }
            
            results[0].Title.Should().Be("The Poor Little Rich Girl");
            results[1].Title.Should().Be("Robin Hood");
            results[2].Title.Should().Be("Peter Pan");
        }
        
        [Fact]
        public void EqualsStringField()
        {
            var results = GetSynonymTestCollection().Aggregate()
                .Search(Builders<Movie>.Search.Equals(p => p.Title, "a corner in wheat"))
                .ToList();
            
            results.Should().ContainSingle().Which.Title.Should().Be("A Corner in Wheat");
        }

        [Fact]
        public void Exists()
        {
            var result = SearchSingle(
                Builders.Search.Compound().Must(
                    Builders.Search.Text(x => x.Body, "life, liberty, and the pursuit of happiness"),
                    Builders.Search.Exists(x => x.Title)));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Filter()
        {
            var result = SearchSingle(
                Builders.Search.Compound().Filter(
                    Builders.Search.Phrase(x => x.Body, "life, liberty"),
                    Builders.Search.Wildcard(x => x.Body, "happ*", true)));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Theory]
        [InlineData("add")]
        [InlineData("constant")]
        [InlineData("gauss")]
        [InlineData("log")]
        [InlineData("log1p")]
        [InlineData("multiply")]
        [InlineData("path")]
        [InlineData("relevance")]
        public void FunctionScore(string functionScoreType)
        {
            var scoreFunction = functionScoreType switch
            {
                "add" => Builders.SearchScoreFunction.Add(Constant(1), Constant(2)),
                "constant" => Constant(1),
                "gauss" => Builders.SearchScoreFunction.Gauss(x => x.Score, 100, 1, 0.1, 1),
                "log" => Builders.SearchScoreFunction.Log(Constant(1)),
                "log1p" => Builders.SearchScoreFunction.Log1p(Constant(1)),
                "multiply" => Builders.SearchScoreFunction.Multiply(Constant(1), Constant(2)),
                "path" => Builders.SearchScoreFunction.Path(x => x.Score, 1),
                "relevance" => Builders.SearchScoreFunction.Relevance(),
                _ => throw new ArgumentOutOfRangeException(nameof(functionScoreType), functionScoreType, "Invalid score function")
            };

            var result = SearchSingle(Builders.Search.Phrase(
                x => x.Body,
                "life, liberty, and the pursuit of happiness",
                score: Builders.SearchScore.Function(scoreFunction)));

            result.Title.Should().Be("Declaration of Independence");

            SearchScoreFunction<HistoricalDocument> Constant(double value) =>
                Builders.SearchScoreFunction.Constant(value);
        }

        [Fact]
        public void GeoShape()
        {
            var results = GeoSearch(
                GeoBuilders.Search.GeoShape(
                    x => x.Address.Location,
                    GeoShapeRelation.Intersects,
                    __testPolygon));

            results.Count.Should().Be(25);
            results.First().Name.Should().Be("Ribeira Charming Duplex");
        }

        [Theory]
        [InlineData("box")]
        [InlineData("circle")]
        [InlineData("polygon")]
        public void GeoWithin(string geometryType)
        {
            GeoWithinArea<GeoJson2DGeographicCoordinates> geoArea = geometryType switch
            {
                "box" => __testBox,
                "circle" => __testCircle,
                "polygon" => new GeoWithinGeometry<GeoJson2DGeographicCoordinates>(__testPolygon),
                _ => throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType, "Invalid geometry type")
            };

            var results = GeoSearch(GeoBuilders.Search.GeoWithin(x => x.Address.Location, geoArea));

            results.Count.Should().Be(25);
            results.First().Name.Should().Be("Ribeira Charming Duplex");
        }

        [Fact]
        public void In()
        {
            var results = GetSynonymTestCollection()
                .Aggregate()
                .Search(
                    Builders<Movie>.Search.In(x => x.Runtime, new[] { 31, 231 }),
                    new() { Sort = Builders<Movie>.Sort.Descending(x => x.Runtime)})
                .Limit(10)
                .ToList();

            results.Count.Should().Be(2);
            results[0].Runtime.Should().Be(231);
            results[1].Runtime.Should().Be(31);
        }

        [Fact]
        public void InGuid()
        {
            var testGuids = new[]
            {
                Guid.Parse("b52af144-bc97-454f-a578-418a64fa95bf"), Guid.Parse("84da5d44-bc97-454f-a578-418a64fa937a")
            };

            var result = GetExtraTestsCollection().Aggregate()
                .Search(Builders<TestClass>.Search.In(c => c.TestGuid,  testGuids))
                .Limit(10)
                .ToList();

            result.Should().HaveCount(2);
            result.Select(s => s.Name).Should().BeEquivalentTo(["test6", "test7"]);
        }

        [Fact]
        public void MoreLikeThis()
        {
            var likeThisDocument = new HistoricalDocument
            {
                Title = "Declaration of Independence",
                Body = "We hold these truths to be self-evident that all men are created equal..."
            };
            var result = SearchSingle(Builders.Search.MoreLikeThis(likeThisDocument));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Must()
        {
            var result = SearchSingle(
                Builders.Search.Compound().Must(
                    Builders.Search.Phrase(x => x.Body, "life, liberty"),
                    Builders.Search.Wildcard(x => x.Body, "happ*", true)));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void MustNot()
        {
            var result = SearchSingle(
                Builders.Search.Compound().MustNot(
                    Builders.Search.Phrase(x => x.Body, "life, liberty")),
                sort: Builders.Sort.Descending(x => x.Title));
            result.Title.Should().Be("US Constitution");
        }

        [Fact]
        public void Near()
        {
            var results = GetGeoTestCollection().Aggregate()
                .Search(GeoBuilders.Search.Near(x => x.Address.Location, __testCircle.Center, 1000))
                .Limit(1)
                .ToList();

            results.Should().ContainSingle().Which.Name.Should().Be("Ribeira Charming Duplex");
        }

        [Fact]
        public void Phrase()
        {
            // This test case exercises the indexName and returnStoredSource arguments. The
            // remaining test cases omit them.
            var results = GetTestCollection().Aggregate()
                .Search(Builders.Search.Phrase(x => x.Body, "life, liberty, and the pursuit of happiness"),
                    new SearchHighlightOptions<HistoricalDocument>(x => x.Body),
                    indexName: "default",
                    returnStoredSource: true,
                    scoreDetails: true)
                .Limit(1)
                .Project<HistoricalDocument>(Builders.Projection
                    .Include(x => x.Title)
                    .Include(x => x.Body)
                    .MetaSearchScore(x => x.Score)
                    .MetaSearchHighlights(x => x.Highlights)
                    .MetaSearchScoreDetails(x => x.ScoreDetails))
                .ToList();

            var result = results.Should().ContainSingle().Subject;
            result.Title.Should().Be("Declaration of Independence");
            result.Score.Should().NotBe(0);

            var highlightTexts = result.Highlights.Should().ContainSingle().Subject.Texts;
            highlightTexts.Should().HaveCount(15);

            foreach (var highlight in highlightTexts)
            {
                var expectedType = char.IsLetter(highlight.Value[0]) ? HighlightTextType.Hit : HighlightTextType.Text;
                highlight.Type.Should().Be(expectedType);
            }

            var highlightRangeStr = string.Join(string.Empty, highlightTexts.Skip(1).Select(x => x.Value));
            highlightRangeStr.Should().Be("Life, Liberty and the pursuit of Happiness.");

            result.ScoreDetails.Description.Should().Contain("life liberty and the pursuit of happiness");
            result.ScoreDetails.Value.Should().NotBe(0);

            var scoreDetail = result.ScoreDetails.Details.Should().ContainSingle().Subject;
            scoreDetail.Description.Should().NotBeNullOrEmpty();
            scoreDetail.Value.Should().NotBe(0);
            scoreDetail.Details.Should().NotBeEmpty();
        }

        [Fact]
        public void PhraseMultiPath()
        {
            var result = SearchSingle(
                Builders.Search.Phrase(
                    Builders.SearchPath.Multi(x => x.Title, x => x.Body),
                    "life, liberty, and the pursuit of happiness"));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void PhraseAnalyzerPath()
        {
            var result = SearchSingle(
                Builders.Search.Phrase(
                    Builders.SearchPath.Analyzer(x => x.Body, "english"),
                    "life, liberty, and the pursuit of happiness"));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void PhraseSynonym()
        {
            var result =
                GetSynonymTestCollection().Aggregate()
                    .Search(
                        Builders<Movie>.Search.Phrase("plot", "automobile race", new SearchPhraseOptions<Movie> { Synonyms = "transportSynonyms" }),
                        indexName: "synonyms-tests")
                    .Project<Movie>(Builders<Movie>.Projection.Include("Title").Exclude("_id"))
                    .Limit(5)
                    .ToList();

            result.Count.Should().Be(5);
            result[0].Title.Should().Be("The Great Race");
            result[1].Title.Should().Be("The Cannonball Run");
            result[2].Title.Should().Be("National Mechanics");
            result[3].Title.Should().Be("Genevieve");
            result[4].Title.Should().Be("Speedway Junky");
        }

        [Fact]
        public void PhraseWildcardPath()
        {
            var result = SearchSingle(
                Builders.Search.Phrase(
                    Builders.SearchPath.Wildcard("b*"),
                    "life, liberty, and the pursuit of happiness"));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void QueryString()
        {
            var result = SearchSingle(Builders.Search.QueryString(x => x.Body, "life, liberty, and the pursuit of happiness"));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Range()
        {
            var results = GeoSearch(
                GeoBuilders.Search.Compound().Must(
                    GeoBuilders.Search.Range(x => x.Bedrooms, SearchRangeV2Builder.Gt(2).Lt(4)),
                    GeoBuilders.Search.Range(x => x.Beds, SearchRangeV2Builder.Gte(14).Lte(14))));

            results.Should().ContainSingle().Which.Name.Should().Be("House close to station & direct to opera house....");
        }
        
        [Fact]
        public void RangeString()
        {
            var results = GetSynonymTestCollection().Aggregate()
                .Search(Builders<Movie>.Search.Range(p => p.Title, SearchRangeV2Builder.Gt("city").Lt("country")))
                .Limit(5)
                .Project<Movie>(Builders<Movie>.Projection.Include(p => p.Title))
                .ToList();
            
            results[0].Title.Should().Be("Civilization");
            results[1].Title.Should().Be("Clash of the Wolves");
            results[2].Title.Should().Be("City Lights");
            results[3].Title.Should().Be("Comradeship");
            results[4].Title.Should().Be("Come and Get It");
        }

        // TODO: Once we have an Atlas cluster running server 8.1, update this test to retrieve actual results from the server instead of merely validating the syntax.
        [Fact]
        public void RankFusion()
        {
            const int limit = 5;

            var vector = new[] { 1.0, 2.0, 3.0 };
            var vectorOptions = new VectorSearchOptions<EmbeddedMovie>()
            {
                IndexName = "vector_search_embedded_movies"
            };
            var vectorPipeline = new EmptyPipelineDefinition<EmbeddedMovie>().VectorSearch(m => m.Embedding, vector, limit, vectorOptions);

            var searchDefinition = Builders<EmbeddedMovie>.Search.Text(new[]{"fullplot", "title"}, "ape");
            var searchPipeline = new EmptyPipelineDefinition<EmbeddedMovie>().Search(searchDefinition, indexName: "search_embedded_movies").Limit(limit);

            var result = GetTestCollection<EmbeddedMovie>()
                .Aggregate()
                .RankFusion(new Dictionary<string, PipelineDefinition<EmbeddedMovie, EmbeddedMovie>>()
                {
                    { "vector", vectorPipeline },
                    { "search", searchPipeline }
                });

            result.Stages.Count.Should().Be(1);

            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var inputSerializer = serializerRegistry.GetSerializer<EmbeddedMovie>();
            var renderedStage = result.Stages[0].Render(inputSerializer, serializerRegistry);
            renderedStage.Document.Should().Be(
                """
                {
                    $rankFusion: {
                        input: {
                            pipelines: {
                                vector: [{
                                    $vectorSearch: {
                                        queryVector: [1.0, 2.0, 3.0],
                                        path: "plot_embedding",
                                        limit: 5,
                                        numCandidates: 50,
                                        index: "vector_search_embedded_movies"
                                    }
                                }],
                                search: [{
                                    $search: {
                                        text: {query: "ape", path: ["fullplot", "title"]},
                                        index: "search_embedded_movies"
                                    }
                                },
                                { $limit: 5 }
                                ]
                            }
                        }}
                }
                """);
        }

        [Fact]
        public void SearchSequenceToken()
        {
            const int limitVal = 10;
            var titles = new[]
            {
                "Equinox Flower",
                "Flower Drum Song",
                "Cactus Flower",
                "The Flower of My Secret",
            };

            var searchDefinition = Builders<Movie>.Search.Text(t => t.Title, "flower");
            var searchOptions = new SearchOptions<Movie>
            {
                IndexName = "default",
                Sort = Builders<Movie>.Sort.Ascending("year")
            };
            var projection = Builders<Movie>.Projection
                .Include(x => x.Title)
                .MetaSearchSequenceToken(x => x.PaginationToken);

            // Base search
            var baseSearchResults = GetSynonymTestCollection()
                .Aggregate()
                .Search(searchDefinition, searchOptions)
                .Project<Movie>(projection)
                .Limit(limitVal)
                .ToList();

            baseSearchResults.Count.Should().Be(limitVal);
            baseSearchResults.ForEach( m => m.PaginationToken.Should().NotBeNullOrEmpty());
            baseSearchResults[0].Title.Should().Be(titles[0]);
            baseSearchResults[1].Title.Should().Be(titles[1]);
            baseSearchResults[2].Title.Should().Be(titles[2]);
            baseSearchResults[3].Title.Should().Be(titles[3]);

            // Testing SearchAfter
            // We're searching after the 2nd result of the base search
            searchOptions.SearchAfter = baseSearchResults[1].PaginationToken;
            var searchAfterResults = GetSynonymTestCollection()
                .Aggregate()
                .Search(searchDefinition, searchOptions)
                .Project<Movie>(projection)
                .Limit(limitVal)
                .ToList();

            searchAfterResults.Count.Should().Be(limitVal);
            searchAfterResults.ForEach( m => m.PaginationToken.Should().NotBeNullOrEmpty());
            searchAfterResults[0].Title.Should().Be(titles[2]);
            searchAfterResults[1].Title.Should().Be(titles[3]);

            // Testing SearchBefore
            // We're searching before the 4th result of the base search
            searchOptions.SearchAfter = null;
            searchOptions.SearchBefore = baseSearchResults[3].PaginationToken;
            var searchBeforeResults = GetSynonymTestCollection()
                .Aggregate()
                .Search(searchDefinition, searchOptions)
                .Project<Movie>(projection)
                .Limit(limitVal)
                .ToList();

            // We only get the first 3 elements of the base search
            searchBeforeResults.Count.Should().Be(3);
            searchBeforeResults.ForEach( m => m.PaginationToken.Should().NotBeNullOrEmpty());
            // With searchBefore the results are reversed
            searchBeforeResults[0].Title.Should().Be(titles[2]);
            searchBeforeResults[1].Title.Should().Be(titles[1]);
            searchBeforeResults[2].Title.Should().Be(titles[0]);
        }

        [Fact]
        public void Search_count_lowerBound()
        {
            var results = GetTestCollection().Aggregate()
                .Search(
                    Builders.Search.Phrase(x => x.Body, "life, liberty, and the pursuit of happiness"),
                    count: new SearchCountOptions()
                    {
                        Type = SearchCountType.LowerBound,
                        Threshold = 128
                    })
                .Project<HistoricalDocument>(Builders.Projection.SearchMeta(x => x.MetaResult))
                .Limit(1)
                .ToList();
            results.Should().ContainSingle().Which.MetaResult.Count.LowerBound.Should().Be(108);
        }

        [Fact]
        public void SearchMeta_count()
        {
            var result = GetTestCollection().Aggregate()
                .SearchMeta(
                    Builders.Search.Phrase(x => x.Body, "life, liberty, and the pursuit of happiness"),
                    "default",
                    new SearchCountOptions() { Type = SearchCountType.Total })
                .Single();

            result.Should().NotBeNull();
            result.Count.Should().NotBeNull();
            result.Count.Total.Should().Be(108);
        }

        [Fact]
        public void SearchMeta_facet()
        {
            var result = GetTestCollection().Aggregate()
                .SearchMeta(Builders.Search.Facet(
                    Builders.Search.Phrase(x => x.Body, "life, liberty, and the pursuit of happiness"),
                    Builders.SearchFacet.String("string", x => x.Author, 100),
                    Builders.SearchFacet.Number("number", x => x.Index, 0, 100),
                    Builders.SearchFacet.Date("date", x => x.Date, DateTime.MinValue, DateTime.MaxValue)))
                .Single();

            result.Should().NotBeNull();

            var bucket = result.Facet["string"].Buckets.Should().NotBeNull().And.ContainSingle().Subject;
            bucket.Id.Should().Be((BsonString)"machine");
            bucket.Count.Should().Be(108);

            bucket = result.Facet["number"].Buckets.Should().NotBeNull().And.ContainSingle().Subject;
            bucket.Id.Should().Be((BsonInt32)0);
            bucket.Count.Should().Be(0);

            bucket = result.Facet["date"].Buckets.Should().NotBeNull().And.ContainSingle().Subject;
            bucket.Id.Should().Be((BsonDateTime)DateTime.MinValue);
            bucket.Count.Should().Be(108);
        }

        [Fact]
        public void Should()
        {
            var result = SearchSingle(
                Builders.Search.Compound().Should(
                    Builders.Search.Phrase(x => x.Body, "life, liberty"),
                    Builders.Search.Wildcard(x => x.Body, "happ*", true))
                .MinimumShouldMatch(2));
            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Sort()
        {
            var result = SearchSingle(
                Builders.Search.Text(x => x.Body, "liberty"),
                Builders.Projection.Include(x => x.Title),
                Builders.Sort.Descending(x => x.Title));

            result.Title.Should().Be("US Constitution");
        }

        [Fact]
        public void Sort_MetaSearchScore()
        {
            var results = GetSynonymTestCollection().Aggregate()
                .Search(
                    Builders<Movie>.Search.QueryString(x => x.Title, "dance"),
                    new() { Sort = Builders<Movie>.Sort.MetaSearchScoreAscending() })
                .Project<Movie>(Builders<Movie>.Projection
                    .Include(x => x.Title)
                    .MetaSearchScore(x => x.Score))
                .Limit(10)
                .ToList();
            results.First().Title.Should().Be("Invitation to the Dance");
            results.Should().BeInAscendingOrder(m => m.Score);
        }

        [Theory]
        [InlineData("first")]
        [InlineData("near")]
        [InlineData("or")]
        [InlineData("subtract")]
        public void Span(string spanType)
        {
            var spanDefinition = spanType switch
            {
                "first" => Builders.SearchSpan.First(Term("happiness"), 250),
                "near" => Builders.SearchSpan.Near(new[] { Term("life"), Term("liberty"), Term("pursuit"), Term("happiness") }, 3, true),
                "or" => Builders.SearchSpan.Or(Term("unalienable"), Term("inalienable")),
                "subtract" => Builders.SearchSpan.Subtract(Term("unalienable"), Term("inalienable")),
                _ => throw new ArgumentOutOfRangeException(nameof(spanType), spanType, "Invalid span type")
            };

            var result = SearchSingle(Builders.Search.Span(spanDefinition));
            result.Title.Should().Be("Declaration of Independence");

            SearchSpanDefinition<HistoricalDocument> Term(string term) => Builders.SearchSpan.Term(x => x.Body, term);
        }

        [Fact]
        public void Text()
        {
            var result = SearchSingle(Builders.Search.Text(x => x.Body, "life, liberty, and the pursuit of happiness"));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void TextMatchCriteria()
        {
            var result =
                GetSynonymTestCollection().Aggregate()
                    .Search(
                        Builders<Movie>.Search.Text("plot", "attire", new SearchTextOptions<Movie> { Synonyms = "attireSynonyms", MatchCriteria = MatchCriteria.Any}),
                        indexName: "synonyms-tests")
                    .Project<Movie>(Builders<Movie>.Projection.Include("Title").Exclude("_id"))
                    .Limit(5)
                    .ToList();

            result.Count.Should().Be(5);
            result[0].Title.Should().Be("The Royal Tailor");
            result[1].Title.Should().Be("La guerre des tuques");
            result[2].Title.Should().Be("The Dress");
            result[3].Title.Should().Be("The Club");
            result[4].Title.Should().Be("The Triple Echo");
        }

        [Theory]
        [InlineData("automobile", "transportSynonyms", "Blue Car")]
        [InlineData("boat", "transportSynonyms", "And the Ship Sails On")]
        public void Synonyms(string query, string synonym, string expected)
        {
            var sortDefinition = Builders<Movie>.Sort.Ascending(x => x.Title);
            var result =
                GetSynonymTestCollection().Aggregate()
                    .Search(Builders<Movie>.Search.Text(x => x.Title, query, synonym), indexName: "synonyms-tests")
                    .Sort(sortDefinition)
                    .Project<Movie>(Builders<Movie>.Projection.Include("Title").Exclude("_id"))
                    .Limit(1)
                    .Single();

            result.Title.Should().Be(expected);
        }

        [Fact]
        public void SynonymsMappings()
        {
            var automobileAndAttireSearchResults = SearchMultipleSynonymMapping(
                Builders<Movie>.Search.Text(x => x.Title, "automobile", "transportSynonyms"),
                Builders<Movie>.Search.Text(x => x.Title, "attire", "attireSynonyms"));

            var vehicleAndDressSearchResults = SearchMultipleSynonymMapping(
                Builders<Movie>.Search.Text(x => x.Title, "vehicle", "transportSynonyms"),
                Builders<Movie>.Search.Text(x => x.Title, "dress", "attireSynonyms"));

            var boatAndHatSearchResults = SearchMultipleSynonymMapping(
                Builders<Movie>.Search.Text(x => x.Title, "boat", "transportSynonyms"),
                Builders<Movie>.Search.Text(x => x.Title, "hat", "attireSynonyms"));

            var vesselAndFedoraSearchResults = SearchMultipleSynonymMapping(
                Builders<Movie>.Search.Text(x => x.Title, "vessel", "transportSynonyms"),
                Builders<Movie>.Search.Text(x => x.Title, "fedora", "attireSynonyms"));

            automobileAndAttireSearchResults.Should().NotBeNull();
            vehicleAndDressSearchResults.Should().NotBeNull();
            boatAndHatSearchResults.Should().NotBeNull();
            vesselAndFedoraSearchResults.Should().NotBeNull();

            automobileAndAttireSearchResults.Should().BeEquivalentTo(vehicleAndDressSearchResults);
            boatAndHatSearchResults.Should().NotBeEquivalentTo(vesselAndFedoraSearchResults);
        }

        [Fact]
        public void Wildcard()
        {
            var result = SearchSingle(Builders.Search.Wildcard(x => x.Body, "tranquil*", true));

            result.Title.Should().Be("US Constitution");
        }

        private List<AirbnbListing> GeoSearch(SearchDefinition<AirbnbListing> searchDefinition) =>
            GetGeoTestCollection().Aggregate().Search(searchDefinition).ToList();

        private HistoricalDocument SearchSingle(
            SearchDefinition<HistoricalDocument> searchDefinition,
            ProjectionDefinition<HistoricalDocument, HistoricalDocument> projectionDefinition = null,
            SortDefinition<HistoricalDocument> sort = null)
        {
            var fluent = GetTestCollection().Aggregate().Search(searchDefinition, new() { Sort = sort });

            if (projectionDefinition != null)
            {
                fluent = fluent.Project(projectionDefinition);
            }

            return fluent.Limit(1).Single();
        }

        private List<BsonDocument> SearchMultipleSynonymMapping(params SearchDefinition<Movie>[] clauses) =>
            GetSynonymTestCollection().Aggregate()
                .Search(Builders<Movie>.Search.Compound().Should(clauses), indexName: "synonyms-tests")
                .Project(Builders<Movie>.Projection.Include("Title").Exclude("_id"))
                .ToList();

        private IMongoCollection<HistoricalDocument> GetTestCollection() => _mongoClient
            .GetDatabase("sample_training")
            .GetCollection<HistoricalDocument>("posts");

        private IMongoCollection<T> GetTestCollection<T>() => _mongoClient
            .GetDatabase("sample_training")
            .GetCollection<T>("posts");

        private IMongoCollection<Movie> GetSynonymTestCollection() => _mongoClient
            .GetDatabase("sample_mflix")
            .GetCollection<Movie>("movies");

        private IMongoCollection<AirbnbListing> GetGeoTestCollection() => _mongoClient
            .GetDatabase("sample_airbnb")
            .GetCollection<AirbnbListing>("listingsAndReviews");

        private IMongoCollection<TestClass> GetExtraTestsCollection() => _mongoClient
            .GetDatabase("csharpExtraTests")
            .GetCollection<TestClass>("testClasses");

        [BsonIgnoreExtraElements]
        public class Comment
        {
            [BsonElement("author")]
            public string Author { get; set; }
        }

        [BsonIgnoreExtraElements]
        public class Movie
        {
            [BsonElement("genres")]
            public string[] Genres { get; set; }
            
            [BsonElement("title")]
            public string Title { get; set; }

            [BsonElement("runtime")]
            public int Runtime { get; set; }

            [BsonElement("score")]
            public double Score { get; set; }

            [BsonElement("paginationToken")]
            public string PaginationToken { get; set; }
        }

        public class EmbeddedMovie : Movie
        {
            [BsonElement("plot_embedding")]
            public double[] Embedding { get; set; }
        }

        [BsonIgnoreExtraElements]
        public class HistoricalDocumentWithCommentsOnly
        {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonElement("comments")]
            public Comment[] Comments { get; set; }
        }

        [BsonIgnoreExtraElements]
        public class HistoricalDocument
        {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonElement("body")]
            public string Body { get; set; }

            [BsonElement("author")]
            public string Author { get; set; }

            [BsonElement("title")]
            public string Title { get; set; }

            [BsonElement("highlights")]
            public List<SearchHighlight> Highlights { get; set; }

            [BsonElement("score")]
            public double Score { get; set; }

            [BsonElement("date")]
            public DateTime Date { get; set; }

            [BsonElement("index")]
            public int Index { get; set; }

            [BsonElement("metaResult")]
            public SearchMetaResult MetaResult { get; set; }

            [BsonElement("scoreDetails")]
            public SearchScoreDetails ScoreDetails { get; set; }
        }

        [BsonIgnoreExtraElements]
        public class Address
        {
            [BsonElement("location")]
            public GeoJsonObject<GeoJson2DGeographicCoordinates> Location { get; set; }

            [BsonElement("street")]
            public string Street { get; set; }
        }

        [BsonIgnoreExtraElements]
        public class AirbnbListing
        {
            [BsonElement("address")]
            public Address Address { get; set; }

            [BsonElement("bedrooms")]
            public int Bedrooms { get; set; }

            [BsonElement("beds")]
            public int Beds { get; set; }

            [BsonElement("description")]
            public string Description { get; set; }

            [BsonElement("space")]
            public string Space { get; set; }

            [BsonElement("name")]
            public string Name { get; set; }
        }

        [BsonIgnoreExtraElements]
        private class TestClass
        {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonElement("name")]
            public string Name { get; set; }

            [BsonElement("testString")]
            public string TestString { get; set; }

            [BsonGuidRepresentation(GuidRepresentation.Standard)]
            [BsonElement("testGuid")]
            public Guid TestGuid { get; set; }
        }
    }
}
