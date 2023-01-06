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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.Search;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;
using Builders = MongoDB.Driver.Builders<MongoDB.Driver.Tests.Search.AtlasSearchTest.HistoricalDocument>;
using GeoBuilders = MongoDB.Driver.Builders<MongoDB.Driver.Tests.Search.AtlasSearchTest.AirbnbListing>;

namespace MongoDB.Driver.Tests.Search
{
    [Trait("Category", "AtlasSearch")]
    public class AtlasSearchTest : LoggableTestClass
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

        private readonly DisposableMongoClient _disposableMongoClient;

        public AtlasSearchTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            RequireEnvironment.Check().EnvironmentVariable("ATLAS_SEARCH_TESTS_ENABLED");

            var atlasSearchUri = Environment.GetEnvironmentVariable("ATLAS_SEARCH");
            Ensure.IsNotNullOrEmpty(atlasSearchUri, nameof(atlasSearchUri));

            _disposableMongoClient = new(new MongoClient(atlasSearchUri), CreateLogger<DisposableMongoClient>());
        }

        protected override void DisposeInternal() => _disposableMongoClient.Dispose();

        [Fact]
        public void Autocomplete()
        {
            var result = SearchSingle(Builders.Search.Autocomplete("Declaration of Ind", x => x.Title));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Compound()
        {
            var result = SearchSingle(Builders.Search.Compound()
                .Must(Builders.Search.Text("life", x => x.Body), Builders.Search.Text("liberty", x => x.Body))
                .MustNot(Builders.Search.Text("property", x => x.Body))
                .Must(Builders.Search.Text("pursuit of happiness", x => x.Body)));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Count_total()
        {
            var results = GetTestCollection().Aggregate()
                .Search(
                    Builders.Search.Phrase("life, liberty, and the pursuit of happiness", x => x.Body),
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
        public void Exists()
        {
            var result = SearchSingle(
                Builders.Search.Compound().Must(
                    Builders.Search.Text("life, liberty, and the pursuit of happiness", x => x.Body),
                    Builders.Search.Exists(x => x.Title)));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Filter()
        {
            var result = SearchSingle(
                Builders.Search.Compound().Filter(
                    Builders.Search.Phrase("life, liberty", x => x.Body),
                    Builders.Search.Wildcard("happ*", x => x.Body, true)));

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
                "add" => Builders.ScoreFunction.Add(Constant(1), Constant(2)),
                "constant" => Constant(1),
                "gauss" => Builders.ScoreFunction.Gauss(x => x.Score, 100, 1, 0.1, 1),
                "log" => Builders.ScoreFunction.Log(Constant(1)),
                "log1p" => Builders.ScoreFunction.Log1p(Constant(1)),
                "multiply" => Builders.ScoreFunction.Multiply(Constant(1), Constant(2)),
                "path" => Builders.ScoreFunction.Path(x => x.Score, 1),
                "relevance" => Builders.ScoreFunction.Relevance(),
                _ => throw new ArgumentOutOfRangeException(nameof(functionScoreType), functionScoreType, "Invalid score function")
            };

            var result = SearchSingle(Builders.Search.Phrase(
                "life, liberty, and the pursuit of happiness",
                x => x.Body,
                score: Builders.Score.Function(scoreFunction)));

            result.Title.Should().Be("Declaration of Independence");

            ScoreFunction<HistoricalDocument> Constant(double value) =>
                Builders.ScoreFunction.Constant(value);
        }

        [Fact]
        public void GeoShape()
        {
            var results = GeoSearch(
                GeoBuilders.Search.GeoShape(
                    __testPolygon,
                    x => x.Address.Location,
                    GeoShapeRelation.Intersects));

            results.Count.Should().Be(25);
            results.First().Name.Should().Be("Ribeira Charming Duplex");
        }

        [Theory]
        [InlineData("box")]
        [InlineData("circle")]
        [InlineData("polygon")]
        public void GeoWithin(string geometryType)
        {
            GeoWithin<GeoJson2DGeographicCoordinates> geoWithin = geometryType switch
            {
                "box" => __testBox,
                "circle" => __testCircle,
                "polygon" => new GeoWithinGeometry<GeoJson2DGeographicCoordinates>(__testPolygon),
                _ => throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType, "Invalid geometry type")
            };

            var results = GeoSearch(GeoBuilders.Search.GeoWithin(geoWithin, x => x.Address.Location));

            results.Count.Should().Be(25);
            results.First().Name.Should().Be("Ribeira Charming Duplex");
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
                    Builders.Search.Phrase("life, liberty", x => x.Body),
                    Builders.Search.Wildcard("happ*", x => x.Body, true)));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void MustNot()
        {
            var result = SearchSingle(
                Builders.Search.Compound().MustNot(
                    Builders.Search.Phrase("life, liberty", x => x.Body)));
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
            var coll = GetTestCollection();
            var results = GetTestCollection().Aggregate()
                .Search(Builders.Search.Phrase("life, liberty, and the pursuit of happiness", x => x.Body),
                    new HighlightOptions<HistoricalDocument>(x => x.Body),
                    indexName: "default",
                    returnStoredSource: true)
                .Limit(1)
                .Project<HistoricalDocument>(Builders.Projection
                    .Include(x => x.Title)
                    .Include(x => x.Body)
                    .MetaSearchScore("score")
                    .MetaSearchHighlights("highlights"))
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
        }

        [Fact]
        public void PhraseMultiPath()
        {
            var result = SearchSingle(
                Builders.Search.Phrase(
                    "life, liberty, and the pursuit of happiness",
                    Builders.Path.Multi(x => x.Title, x => x.Body)));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void PhraseAnalyzerPath()
        {
            var result = SearchSingle(
                Builders.Search.Phrase(
                    "life, liberty, and the pursuit of happiness",
                    Builders.Path.Analyzer(x => x.Body, "english")));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void PhraseWildcardPath()
        {
            var result = SearchSingle(
                Builders.Search.Phrase(
                    "life, liberty, and the pursuit of happiness",
                    Builders.Path.Wildcard("b*")));

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
                    GeoBuilders.Search.Range(SearchRangeBuilder.Gt(2).Lt(4), x => x.Bedrooms),
                    GeoBuilders.Search.Range(SearchRangeBuilder.Gte(14).Lte(14), x => x.Beds)));

            results.Should().ContainSingle().Which.Name.Should().Be("House close to station & direct to opera house....");
        }

        [Fact]
        public void Search_count_lowerBound()
        {
            var results = GetTestCollection().Aggregate()
                .Search(
                    Builders.Search.Phrase("life, liberty, and the pursuit of happiness", x => x.Body),
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
                    Builders.Search.Phrase("life, liberty, and the pursuit of happiness", x => x.Body),
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
                    Builders.Search.Phrase("life, liberty, and the pursuit of happiness", x => x.Body),
                    Builders.Facet.String("string", x => x.Author, 100),
                    Builders.Facet.Number("number", x => x.Index, 0, 100),
                    Builders.Facet.Date("date", x => x.Date, DateTime.MinValue, DateTime.MaxValue)))
                .Single();

            result.Should().NotBeNull();
            result.Facet.Should().NotBeNull().And.ContainKeys("date", "number", "string");

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
                    Builders.Search.Phrase("life, liberty", x => x.Body),
                    Builders.Search.Wildcard("happ*", x => x.Body, true))
                .MinimumShouldMatch(2));
            result.Title.Should().Be("Declaration of Independence");
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
                "first" => Builders.Span.First(Term("happiness"), 250),
                "near" => Builders.Span.Near(new[] { Term("life"), Term("liberty"), Term("pursuit"), Term("happiness") }, 3, true),
                "or" => Builders.Span.Or(Term("unalienable"), Term("inalienable")),
                "subtract" => Builders.Span.Subtract(Term("unalienable"), Term("inalienable")),
                _ => throw new ArgumentOutOfRangeException(nameof(spanType), spanType, "Invalid span type")
            };

            var result = SearchSingle(Builders.Search.Span(spanDefinition));
            result.Title.Should().Be("Declaration of Independence");

            SpanDefinition<HistoricalDocument> Term(string term) => Builders.Span.Term(term, x => x.Body);
        }

        [Fact]
        public void Text()
        {
            var result = SearchSingle(Builders.Search.Text("life, liberty, and the pursuit of happiness", x => x.Body));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Wildcard()
        {
            var result = SearchSingle(Builders.Search.Wildcard("tranquil*", x => x.Body, true));

            result.Title.Should().Be("US Constitution");
        }

        private List<AirbnbListing> GeoSearch(SearchDefinition<AirbnbListing> searchDefintion) =>
            GetGeoTestCollection().Aggregate().Search(searchDefintion).ToList();

        private HistoricalDocument SearchSingle(SearchDefinition<HistoricalDocument> searchDefintion) =>
            GetTestCollection()
                .Aggregate()
                .Search(searchDefintion)
                .Limit(1)
                .ToList()
                .Single();

        private IMongoCollection<HistoricalDocument> GetTestCollection() => _disposableMongoClient
            .GetDatabase("sample_training")
            .GetCollection<HistoricalDocument>("posts");

        private IMongoCollection<AirbnbListing> GetGeoTestCollection() => _disposableMongoClient
            .GetDatabase("sample_airbnb")
            .GetCollection<AirbnbListing>("listingsAndReviews");

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
            public List<Highlight> Highlights { get; set; }

            [BsonElement("score")]
            public double Score { get; set; }

            [BsonElement("date")]
            public DateTime Date { get; set; }

            [BsonElement("index")]
            public int Index { get; set; }

            [BsonElement("metaResult")]
            public SearchMetaResult MetaResult { get; set; }
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
    }
}
