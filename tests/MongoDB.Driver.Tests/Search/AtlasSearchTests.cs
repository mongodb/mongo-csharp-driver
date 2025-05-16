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
            var results = GetMoviesCollection<Movie>().Aggregate()
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
            var results = GetMoviesCollection<Movie>().Aggregate()
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
            var results = GetMoviesCollection<Movie>()
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
                GetMoviesCollection<Movie>().Aggregate()
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
            var results = GetMoviesCollection<Movie>().Aggregate()
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

        [Fact]
        public void RankFusion()
        {
            const int limit = 5;

            var vector = new[] { -0.00028408386, -0.030921403, 0.0017461961, -0.007926553, -0.008247016, 0.029273309, 0.0027844305, 0.0088290805, 0.0020862792, 0.001850837, 0.0004868257, 0.004914855, 0.030529, -0.033118863, 0.022419326, 0.0031359587, 0.030842923, -0.016101629, 0.018940013, -0.006186897, -0.008390897, 0.007514529, 0.008175075, -0.012380334, 0.007200606, 0.0015352791, 0.0071482854, -0.011484345, 0.007194066, -0.006736262, 0.0009049808, -0.01856069, 0.008959882, -0.02718049, -0.030031955, -0.012609236, -0.011432025, -0.000060597744, 0.0390834, -0.00019783681, 0.0071025053, 0.01747504, -0.015918506, -0.0062261373, -0.0049966057, 0.008534778, 0.009247645, -0.007959253, -0.04015597, 0.013838767, 0.013969569, 0.010934981, -0.0040025166, 0.0022285255, -0.0067820423, -0.008194695, -0.0096335085, 0.006209787, 0.010261354, -0.006445229, -0.0066839415, -0.0025702436, -0.0028007806, 0.0009164259, -0.012151431, -0.00014674259, 0.011314304, -0.019737901, 0.0068997634, 0.007331407, 0.036336575, -0.0021680298, 0.0024606977, -0.0007745884, 0.00985587, -0.0049573653, -0.022066163, -0.0065040896, -0.010745319, -0.008802921, 0.00021173444, -0.028880905, -0.021098234, 0.03481928, 0.03822011, 0.003809585, -0.011693628, 0.012726957, -0.012197211, 0.0019865432, -0.0028776263, -0.008436677, -0.0021631247, 0.0118375085, -0.0044962913, 0.002622564, -0.011360084, 0.00865904, -0.009659668, -0.027677534, -0.019397818, 0.0040875375, 0.011386245, -0.0011322479, 0.003714754, 0.005578671, 0.025218472, 0.012112191, 0.014623574, -0.002947932, 0.0041954485, 0.009456927, 0.018142127, -0.055878274, -0.014335811, -0.03162773, 0.0075733894, 0.015840026, 0.005258208, -0.015879266, 0.033354305, -0.004542072, -0.006638161, -0.0075930096, -0.011366624, 0.019332416, -0.019515539, -0.022445485, 0.005876244, -0.016559431, 0.018220607, -0.0039894367, 0.031209165, -0.0049737156, -0.020195706, 0.0175012, -0.024669107, 0.0014339081, -0.005912214, -0.015800785, 0.0117197875, 0.008161995, -0.00982971, -0.0023348015, -0.008292796, 0.035420965, -0.00040343995, 0.0022628608, 0.00032904677, -0.009273805, 0.01975098, -0.013420203, 0.016650993, -0.009143004, 0.024865309, 0.0035185523, 0.007305247, 0.024132822, -0.012635396, -0.0118375085, -0.00873098, 0.011706707, 0.0009687464, -0.012295313, 0.0057160123, 0.03508088, 0.003142499, 0.00035152823, 0.009476547, -0.028410021, -0.009908191, -0.0033109053, -0.009188784, 0.0148720965, -0.031183006, 0.022066163, 0.014021888, 0.022144644, -0.0108565, -0.008155455, -0.009005663, 0.025231551, 0.018966174, 0.009156084, -0.017017236, -0.017893605, -0.021320596, -0.008103134, 0.015840026, -0.013420203, 0.0027533653, 0.0054249796, -0.009339206, 0.0120271705, -0.6701207, -0.029901154, -0.012995099, -0.008050814, -0.005356309, 0.016977996, 0.021202875, -0.008207776, -0.006464849, 0.0027942406, -0.004453781, 0.027154328, -0.0054249796, -0.015905425, 0.0003684915, -0.011432025, 0.023544217, -0.024289783, 0.0054053594, -0.020313427, -0.03398215, 0.030031955, 0.020993592, -0.0015753369, -0.01965942, 0.0072725466, 0.02515307, -0.0078022913, -0.0094438465, 0.015683064, -0.022288524, 0.028880905, -0.0020388637, 0.009620428, 0.056296837, -0.02825306, -0.0049508256, 0.030319719, -0.020561948, 0.02189612, -0.009594268, -0.014885177, -0.004842914, -0.0040940777, 0.0069651636, -0.0061836266, 0.013564085, -0.0030018876, 0.023504976, 0.007946173, 0.00439492, 0.030895242, -0.007109045, -0.002619294, -0.0028759914, 0.008711359, 0.030215077, -0.013551004, 0.012406494, -0.010562196, -0.010503336, -0.0016023146, 0.005088167, 0.0026634394, -0.0011150802, 0.04154246, 0.015447621, 0.0417779, 0.016781794, -0.03377287, 0.037696905, 0.014466613, -0.0071417456, 0.017775884, -0.010137093, 0.0028972465, 0.024315942, -0.011026541, -0.008103134, -0.004470131, -0.0019244127, -0.0027975107, -0.025859397, -0.0050293063, 0.02404126, -0.012275693, -0.010719159, 0.01203371, 0.0030002524, 0.005892594, -0.008632879, 0.027154328, -0.0020241486, 0.000026620088, 0.007913472, -0.0123345535, 0.007821912, 0.0049508256, 0.0072463863, -0.039449643, 0.017592762, -0.034583837, 0.0058173835, -0.0136164045, 0.0045191813, -0.0022432406, -0.0027599053, 0.004872345, 0.061633524, -0.020287266, -0.008672119, 0.0023658667, 0.014440453, -0.004061377, -0.011785188, -0.02391046, 0.0074360482, 0.009411146, 0.030999884, -0.022602446, -0.0059449147, 0.016755633, 0.008417057, -0.014675895, 0.024760667, 0.015643824, -0.025950957, -0.01975098, -0.034688476, 0.013256702, -0.015853105, -0.004542072, 0.0031147036, -0.0036428133, 0.016167028, -0.0038815255, 0.013838767, -0.0061705466, 0.01963326, 0.0041006175, -0.008436677, 0.013276322, 0.011327384, -0.01757968, 0.00070837024, -0.029377948, -0.0011714882, -0.007501449, -0.04912893, 0.026591884, 0.017736642, -0.009371906, -0.015604583, 0.00028060944, 0.009581188, 0.00657276, -0.024826068, -0.006252297, -0.0038651754, -0.016729474, 0.01858685, 0.0048265643, -0.011628226, 0.011360084, -0.0044014603, -0.017736642, 0.016023146, 0.033171184, -0.008338576, -0.024211302, -0.009025283, -0.035525605, -0.02287713, 0.029037867, -0.002849831, -0.010627598, -0.0019881781, -0.022602446, -0.015316821, 0.008227396, -0.0026863297, 0.011700167, -0.00043777528, -0.001362785, 0.03604881, -0.0003721703, 0.00057225523, 0.017867444, -0.009757769, 0.037749227, -0.021516798, 0.027991457, 0.012864298, -0.011248903, -0.0052778283, 0.013498683, 0.008528238, 0.021137474, 0.008515158, 0.031339966, -0.001971828, -0.009947431, 0.028462341, -0.005150297, 0.020575028, -0.009522327, 0.0011649482, -0.028907064, 0.03508088, 0.003377941, 0.004287009, -0.024865309, 0.02064043, -0.0020192435, 0.006477929, 0.014911337, -0.005856624, 0.0015025787, -0.01629783, -0.0022285255, 0.019070815, -0.012426114, -0.01739656, -0.0031245137, -0.015539182, 0.02084971, 0.0005252486, 0.026539564, 0.006029935, -0.042222627, 0.013152061, 0.008031194, -0.015146779, 0.009221485, 0.022759408, -0.023060251, -0.0026634394, -0.015094458, 0.023295693, -0.014518933, 0.014924417, 0.026448002, 0.04350448, -0.010405235, -0.012184132, 0.014793616, 0.004015597, 0.00004401767, -0.019031575, 0.023714257, -0.017592762, 0.016873354, -0.012511135, 0.020391908, 0.0036297333, 0.012262613, 0.009430766, 0.0035545225, 0.022497807, 0.036336575, 0.0074229683, -0.013269782, 0.020221865, 0.021438317, 0.017972086, -0.013328643, 0.016821034, 0.009169164, 0.005117597, 0.024564466, -0.008037734, -0.01408729, -0.008070434, -0.00977085, 0.023884298, -0.0017412909, 0.006203247, -0.004578042, 0.013799527, -0.008279716, -0.013904167, -0.034296073, 0.020234946, 0.0153299, -0.011006921, -0.012753117, -0.015382221, 0.0047317334, -0.03144461, 0.013655645, -0.025715515, 0.012870838, -0.015055218, -0.011275063, 0.0025244632, -0.0068997634, 0.037696905, -0.005261478, 0.034296073, -0.021987682, 0.0059710746, 0.012746577, -0.02843618, -0.01739656, 0.011870209, -0.004018867, -0.024525225, -0.004571502, 0.0066774013, 0.005133947, -0.0029708222, -0.007534149, -0.0174358, 0.011190043, 0.0009000758, -0.012896998, -0.008541319, 0.013511764, 0.023884298, 0.009156084, -0.007095965, -0.032909583, -0.02733745, -0.002501573, 0.11887213, 0.017605841, 0.018390648, 0.013969569, 0.013577164, -0.004463591, -0.019358577, -0.018024405, 0.014100369, -0.0023462465, 0.005761793, -0.02281173, 0.002182745, 0.0037376443, 0.01747504, 0.00044840286, -0.022026923, -0.016101629, 0.013825687, -0.011772108, 0.0066970214, 0.0108892, -0.0031915493, 0.01631091, -0.021909202, -0.024172062, 0.008619799, 0.012569996, -0.008207776, -0.015042138, -0.015486862, -0.010542576, 0.0008943532, 0.021307515, -0.0015557167, 0.009476547, -0.00005768537, 0.011791728, 0.036388893, -0.0036657036, 0.023871219, 0.024747588, 0.0113404635, -0.037592266, 0.014819776, -0.005568861, 0.013825687, 0.017134957, 0.001968558, -0.021556038, 0.056453798, 0.0017069556, -0.009404606, 0.009842791, 0.02172608, -0.0067493417, 0.014728215, 0.00878984, -0.018286008, 0.01642863, 0.006304618, -0.023151813, 0.009731609, -0.0062457575, 0.011464725, -0.01866533, -0.009496167, 0.007403348, -0.011020001, 0.0038717154, 0.00972507, -0.031758532, -0.010228653, -0.015996987, 0.008384357, 0.0018998875, 0.020234946, -0.02733745, 0.018652251, 0.011229283, -0.0021189793, -0.014466613, -0.018246768, -0.014074209, -0.0106537575, 0.02817458, 0.013642565, 0.007749971, -0.006690481, 0.0053955493, 0.013851847, 0.009306505, 0.0070044044, -0.0015965921, -0.0022481456, -0.025767837, -0.0016824304, 0.02733745, -0.002951202, 0.018299088, 0.021320596, -0.028593142, 0.0017134957, -0.015552263, 0.036179613, -0.010954601, -0.014453532, -0.005362849, -0.025754755, 0.0014151054, -0.0055982913, -0.013551004, 0.0053922795, -0.009417687, 0.0050947065, 0.0112685235, 0.01861301, 0.025924798, 0.00058533537, -0.017723562, -0.008855241, -0.0075603095, 0.027075848, 0.016598672, -0.0017331159, -0.004211799, 0.01522526, -0.026644204, 0.018848453, 0.0065073594, -0.0028253058, -0.0076060896, 0.012550375, 0.0034008313, -0.030790603, 0.002516288, -0.021268275, 0.0025244632, -0.005313799, -0.0014077479, 0.0015107539, -0.009640048, -0.008253556, -0.023387255, -0.024368264, -0.027049689, -0.00092950603, 0.00087718555, 0.00011823202, 0.017723562, -0.0239497, -0.009947431, -0.00163992, 0.0102024935, -0.031078365, -0.04379224, -0.011870209, 0.0063569383, 0.0218438, 0.015905425, 0.034243755, -0.008430137, 0.03497624, 0.019463219, 0.0010153443, 0.00015430454, -0.0060626357, 0.013799527, -0.06629005, 0.036755137, 0.018756893, 0.011170423, 0.032569498, -0.008443218, 0.026107918, 0.010594897, -0.008933722, -0.014610494, -0.023622697, -0.011641307, 0.0030721931, -0.011229283, -0.00072063284, 0.012393414, -0.014597414, -0.015015977, 0.071522094, 0.017775884, -0.029220987, -0.0095157875, 0.023243373, -0.028985545, -0.0049704458, -0.0065073594, -0.006661051, -0.014963658, 0.020967431, -0.00006550279, -0.0013088295, 0.01975098, 0.009430766, 0.0014952212, 0.01962018, -0.009718529, 0.009267265, 0.039737403, 0.022013841, -0.0054347897, 0.010091312, -0.02718049, -0.023792738, -0.022746328, -0.026487242, -0.03573489, -0.017723562, 0.0049867956, -0.005673502, 0.009659668, -0.016821034, -0.0390834, 0.009509247, 0.014545093, 0.04007749, 0.0084497575, 0.02817458, 0.0099408915, -0.013472524, -0.016114708, 0.028148418, 0.015683064, -0.0009205134, 0.0010096218, 0.01844297, -0.001850837, -0.020823551, -0.01089574, 0.012341093, -0.04996606, -0.014335811, 0.015604583, -0.009156084, -0.00436876, -0.008855241, -0.016860275, 0.0019244127, 0.014100369, -0.02504843, 0.010424855, 0.001983273, -0.030921403, -0.047925558, -0.011942149, -0.014558174, 0.0027811604, 0.010509877, -0.0136164045, 0.0014894987, 0.0009164259, 0.0106995385, 0.014597414, 0.020483468, 0.004143128, -0.0028759914, -0.0067035616, 0.007769591, -0.020470388, -0.00028776264, -0.00763879, 0.0091364635, 0.027023528, 0.00545441, 0.01083688, -0.004649983, 0.01625859, -0.007213686, 0.0111508025, -0.001973463, -0.01090882, -0.015866185, -0.0050391164, 0.011418945, 0.015434542, -0.020692749, -0.013152061, -0.022223124, 0.0023609616, -0.01858685, -0.012641936, 0.019031575, -0.014532013, -0.0054871105, -0.0094438465, 0.005022766, 0.0053824694, 0.017252678, 0.0034923921, -0.0151991, 0.017710483, -0.04245807, 0.008881401, 0.0034825818, 0.0064942795, -0.015748464, -0.0071352054, 0.011301223, -0.0069324635, 0.01408729, -0.038743313, -0.019293176, -0.014702055, -0.009208404, 0.00432952, -0.021477558, -0.010640678, 0.010012832, 0.015094458, 0.009280345, -0.02077123, -0.02494379, 0.012635396, 0.0021238844, -0.011752488, 0.028619302, -0.024773747, 0.007965793, 0.008848701, 0.013969569, 0.006860523, -0.034191433, -0.00011067008, -0.008175075, 0.017815124, 0.0091364635, -0.012962399, -0.019855622, -0.013387503, -0.022367004, 0.021385996, 0.013943408, -0.021542957, 0.010019372, 0.027703693, 0.0058500837, -0.0016202999, 0.0306598, -0.0012123636, 0.006111686, -0.023217212, -0.028540822, -0.014034969, -0.0067820423, 0.030738281, 0.003590493, -0.004908315, -0.032988064, -0.0035316325, -0.041804064, 0.0141134495, -0.022000762, 0.0136164045, 0.030764442, -0.0040057865, 0.0040548374, 0.021242116, 0.010065152, -0.0031310536, -0.029848834, -0.0050587365, -0.0028563712, -0.0018328518, -0.003597033, 0.014793616, -0.019554779, -0.0020143385, 0.029874993, -0.0034302615, -0.006455039, 0.014728215, -0.034479197, -0.009731609, -0.010725698, 0.015591503, 0.0067951223, 0.0044308905, 0.009509247, -0.012301853, 0.0017707213, -0.00058697036, 0.010097853, -0.012386873, -0.0082600955, 0.0023969319, -0.0043033594, 0.028854745, -0.0066839415, -0.006742802, -0.00549365, -0.021137474, 0.017331159, -0.011392784, -0.0044308905, 0.01747504, 0.0050521963, -0.03413911, -0.028828584, 0.0075799297, 0.014832856, -0.014571253, 0.0045191813, -0.0038847956, 0.02172608, -0.008462838, 0.018809212, -0.016611753, 0.009156084, -0.02393662, 0.0037180241, -0.03563025, 0.0262518, 0.010032452, -0.01523834, -0.0015123888, 0.0066774013, -0.0015393667, -0.024054341, -0.025506234, -0.010477176, -0.0048559946, -0.0070305644, -0.010869579, -0.017252678, -0.029953474, 0.0061738165, -0.024420584, -0.04258887, 0.00983625, 0.21576966, -0.004787324, 0.0048592645, 0.012046791, -0.014924417, 0.024669107, 0.036493536, 0.016951835, -0.0010063518, -0.0034596918, -0.009299966, 0.008299336, -0.014832856, 0.0019113325, -0.0090645235, -0.018926933, -0.037435304, -0.0015508117, -0.016768714, -0.018756893, 0.021869961, -0.00439492, -0.003606843, -0.010280974, 0.015983906, -0.0073771877, -0.0032471397, 0.0007529244, 0.0005877879, -0.0003386525, -0.006314428, -0.031026045, -0.0051045166, -0.016533272, -0.013577164, -0.004048297, 0.0063536684, 0.005035846, -0.0060887956, 0.015643824, 0.02494379, 0.015042138, -0.015787704, 0.002517923, -0.015316821, 0.0149898175, -0.008364737, -0.01752736, 0.010254814, 0.006435419, -0.024185142, -0.01083688, 0.016624833, 0.051718794, -0.0037572645, -0.00067485246, 0.010575277, 0.022353925, 0.007429508, 0.004564962, -0.005689852, 0.019044654, 0.006186897, 0.019842543, -0.024381343, 0.018992335, -0.048788846, 0.005163377, 0.023034092, -0.021699918, -0.006723182, -0.036022652, -0.012138351, -0.01204025, -0.0103921555, -0.0348716, 0.018024405, 0.008959882, 0.013158601, -0.007043645, 0.00870482, 0.0023282613, 0.008024653, 0.004247769, -0.017252678, -0.020470388, 0.033406626, 0.0104640955, -0.011660927, 0.0021663948, 0.0018230417, -0.02943027, 0.0013913978, -0.0047480837, 0.013184761, 0.032386377, -0.0060855257, -0.001146963, -0.006978244, -0.0086852, -0.018338328, -0.036205772, 0.0196071, -0.01417885, -0.021608358, 0.0071679056, -0.0008632879, 0.012256073, 0.039449643, -0.030712122, -0.0015181114, -0.0136164045, 0.0074360482, -0.010326754, 0.022523966, 0.015395301, -0.011713248, -0.02067967, 0.0015311915, -0.018979253, -0.007318327, -0.038560193, -0.01304088, -0.008914102, -0.010692998, -0.028017618, 0.003175199, 0.020143384, 0.04052221, -0.014034969, 0.028200738, -0.03612729, -0.012936238, -0.0063536684, 0.008358196, 0.00763225, 0.005778143, -0.006886683, 0.010241734, 0.011347004, 0.021215955, 0.01746196, 0.00326022, -0.010287514, 0.016873354, -0.011490885, 0.012144892, 0.0038488253, -0.0055230805, 0.001145328, -0.015552263, -0.024747588, 0.004551882, -0.0039796266, 0.03709522, -0.021032833, -0.020418067, -0.016127788, -0.0037899648, 0.00014061129, -0.016716393, 0.013027799, 0.0050489265, -0.018142127, -0.011732868, 0.01852145, -0.16721626, 0.024682187, 0.008887942, 0.002089549, 0.041908704, -0.002836751, 0.01644171, -0.009646588, -0.008044274, 0.002403472, 0.004103888, -0.00068098377, -0.0370429, -0.0032520448, -0.005133947, 0.007063265, -0.010712618, 0.008606719, 0.019437058, 0.0027811604, 0.0071352054, -0.0064910096, 0.0055525107, 0.007763051, 0.011046161, 0.012968939, 0.009384986, -0.016624833, -0.010228653, -0.014963658, -0.009463467, -0.0031375939, 0.011674007, 0.0144927725, 0.024224382, 0.0009205134, -0.013269782, -0.017605841, -0.0122887725, 0.012779277, 0.005261478, 0.014244251, 0.013263241, -0.004829834, 0.014702055, 0.04805636, 0.003606843, 0.0020143385, -0.0008780031, -0.012347633, 0.006304618, -0.029744193, 0.008135835, 0.00035193696, 0.023871219, 0.022353925, 0.010006292, -0.012831598, 0.014754375, -0.03272646, -0.009201865, 0.0013251797, 0.009522327, -0.014675895, -0.025571635, -0.015879266, -0.004022137, 0.0018819022, -0.02956107, 0.019737901, -0.036781296, -0.013348263, 0.02623872, -0.017893605, 0.006177087, -0.0050718165, -0.014715135, 0.006958624, -0.010071692, -0.0097054485, -0.038455553, 0.03361591, 0.003809585, -0.008436677, 0.014061129, 0.0005665327, -0.0033109053, -0.014675895, -0.013721046, -0.030947564, -0.0021124394, -0.016088547, -0.011922529, -0.03152309, 0.020575028, 0.029325629, -0.0003155579, 0.0038913356, 0.012491515, -0.011883289, -0.009404606, 0.018037485, -0.021477558, 0.020431148, 0.030764442, 0.02510075, 0.021987682, 0.0077761314, 0.042981274, -0.0045322618, -0.0073902677, 0.008574018, -0.002056849, 0.027625212, -0.0017592761, 0.019214695, 0.010019372, -0.020012584, 0.04146398, -0.019201616, 0.03897876, 0.0049671754, -0.02935179, 0.021072073, -0.021490637, -0.010287514, -0.106419854, -0.0026716145, -0.019018494, 0.013171681, -0.0020094335, 0.010640678, -0.0046401727, 0.012321473, -0.0073771877, 0.008390897, -0.0058533535, -0.0218438, 0.019123135, -0.013930327, 0.035342485, 0.000036966667, -0.015460702, 0.0045682318, -0.023191053, 0.015813865, -0.010542576, 0.005549241, 0.0137341255, -0.013407123, 0.012962399, -0.0041202377, -0.032255575, 0.0044374308, 0.007534149, 0.00766495, -0.01194869, -0.015604583, 0.01836449, -0.020182624, 0.0030934485, -0.001960383, -0.030110436, 0.0072725466, 0.034400716, -0.033668227, 0.009175704, 0.009620428, 0.0027942406, -0.017932845, 0.006458309, 0.0027746204, -0.008384357, 0.012419574, -0.0028988817, -0.027546732, -0.030738281, 0.00016186648, -0.029848834, 0.007939633, 0.017344238, 0.0048690746, 0.0046532527, 0.008927182, 0.028017618, 0.005791223, -0.0019947183, -0.01314552, 0.002277576, 0.00044431532, -0.0069847843, -0.0079527125, -0.014976737, 0.011732868, 0.00035398075, -0.0018017865, 0.010137093, 0.021359837, -0.008220855, 0.008632879, -0.037775386, 0.020195706, -0.013995728, 0.0012401589, 0.013799527, -0.017815124, -0.007920013, -0.021346755, 0.009993211, -0.01633707, 0.006873603, 0.004810214, 0.0032896502, -0.00025383607, -0.010163253, -0.012275693, 0.0037605346, -0.008999122, -0.011817888, -0.03623193, -0.021242116, 0.015826944, 0.0061607365, -0.0064975494, -0.0067951223, -0.012393414, -0.013551004, 0.008626339, -0.06644701, 0.024656026, 0.0024590625, -0.00020263967, -0.002517923, -0.012798897, 0.016925676, -0.0026814246, -0.004296819, -0.005748713, -0.0056931223, 0.02725897, 0.0030967183, -0.002411647, -0.006736262, -0.019463219, -0.00075496815, -0.008488998, 0.016572513, 0.011360084, 0.009489627, 0.012504594, 0.001317822, 0.007926553, -0.0066185407, -0.0020862792, -0.0036885939, 0.021569118, -0.0022138103, 0.0018606471, -0.009646588, -0.023491895, -0.010732238, 0.0017592761, 0.0011714882, 0.008273176, -0.001744561, 0.0064942795, 0.01635015, 0.06372634, -0.02731129, -0.014571253, 0.010294055, -0.011052702, -0.019267017, 0.0031441338, 0.008593638, 0.007893852, 0.03495008, -0.019175455, 0.016703313, 0.037644584, -0.00975123, 0.00041938134, -0.00976431, -0.0039730864, 0.018181367, -0.00050440215, 0.014767455, -0.0042379587, 0.02524463, -0.0049966057, 0.009280345, 0.014924417, -0.0012115461, -0.029874993, -0.0132370815, -0.010006292, 0.0035152822, -0.016206268, -0.004914855, -0.023675017, -0.0016619927, 0.005330149, -0.004453781, 0.0003075872, -0.0053857393, -0.0017069556, -0.008384357, 0.021516798, -0.006232677, 0.0031097985, -0.042039506, 0.00438184, 0.024917629, 0.001138788, 0.006853983, 0.015696144, -0.013348263, 0.016114708, -0.010791099, 0.017867444, -0.0035185523, 0.022445485, -0.014270411, 0.014963658, -0.040705334, -0.007279087, 0.01627167, 0.012373794, 0.0070567247, 0.0008632879, 0.0070567247, -0.011588986, -0.011772108, 0.0046140123, -0.032229416, -0.043059755, -0.011013461, 0.004780784, 0.023635777, -0.016546352, -0.013034339, 0.011445105, -0.005814113, 0.00977085, -0.0045061014, -0.002202365, -0.017200358, 0.014675895, 0.007501449, 0.00761917, 0.014021888, 0.0014012079, 0.002275941, 0.015630743, 0.010405235, -0.004264119, -0.009424226, -0.02506151, -0.0010733873, 0.0074949088, -0.01963326, 0.00031433164, -0.018063646, -0.017383479, 0.0000909988, 0.018050566, -0.027102008, 0.028645463, -0.008332036, 0.0029528372, 0.014702055, -0.027154328, 0.021909202, 0.0034466116, 0.03897876, -0.0031228787, -0.015015977, 0.0062915375, -0.035604086, 0.02185688, 0.008613259, -0.008194695, 0.030188916, -0.0010096218, 0.0070174844, 0.0066970214, 0.004607472, 0.022000762, 0.012655016, 0.0087179, -0.007279087, -0.016860275, 0.000018994277, 0.0010177968, 0.01618011, -0.012046791, -0.035473283, 0.013361342, -0.0067035616, -0.013577164, 0.012825058, -0.007965793, -0.021699918, 0.011588986, -0.007161366, -0.0050652763, -0.00540863, -0.019123135, 0.026304122, -0.0042150686, -0.022393165, -0.013943408, 0.00078439846, -0.005801033, -0.00087555056, -0.027232809 };

            var vectorOptions = new VectorSearchOptions<EmbeddedMovie>
            {
                IndexName = "vector_search_embedded_movies"
            };
            var vectorPipeline = new EmptyPipelineDefinition<EmbeddedMovie>().VectorSearch(m => m.Embedding, vector, limit, vectorOptions);

            var searchDefinition = Builders<EmbeddedMovie>.Search.Text(new[]{"fullplot", "title"}, "ape");
            var searchPipeline = new EmptyPipelineDefinition<EmbeddedMovie>().Search(searchDefinition, indexName: "sample_mflix__embedded_movies").Limit(limit);

            var result = GetEmbeddedMoviesCollection<EmbeddedMovie>()
                .Aggregate()
                .RankFusion(new Dictionary<string, PipelineDefinition<EmbeddedMovie, EmbeddedMovie>>
                {
                    { "vector", vectorPipeline },
                    { "search", searchPipeline }
                })
                .Limit(limit)
                .ToList();

            result.Count.Should().Be(limit);
            result.Select(m => m.Title).Should().BeEquivalentTo("Tarzan the Ape Man", "King Kong",
                "Battle for the Planet of the Apes", "King Kong Lives", "Mighty Joe Young");
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
            var baseSearchResults = GetMoviesCollection<Movie>()
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
            var searchAfterResults = GetMoviesCollection<Movie>()
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
            var searchBeforeResults = GetMoviesCollection<Movie>()
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
            var results = GetMoviesCollection<Movie>().Aggregate()
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
                GetMoviesCollection<Movie>().Aggregate()
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
                GetMoviesCollection<Movie>().Aggregate()
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
            GetMoviesCollection<Movie>().Aggregate()
                .Search(Builders<Movie>.Search.Compound().Should(clauses), indexName: "synonyms-tests")
                .Project(Builders<Movie>.Projection.Include("Title").Exclude("_id"))
                .ToList();

        private IMongoCollection<HistoricalDocument> GetTestCollection() => _mongoClient
            .GetDatabase("sample_training")
            .GetCollection<HistoricalDocument>("posts");

        private IMongoCollection<T> GetTestCollection<T>() => _mongoClient
            .GetDatabase("sample_training")
            .GetCollection<T>("posts");

        private IMongoCollection<T> GetEmbeddedMoviesCollection<T>() => _mongoClient
            .GetDatabase("sample_mflix")
            .GetCollection<T>("embedded_movies");

        private IMongoCollection<T> GetMoviesCollection<T>() => _mongoClient
            .GetDatabase("sample_mflix")
            .GetCollection<T>("movies");

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

        [BsonIgnoreExtraElements]
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
