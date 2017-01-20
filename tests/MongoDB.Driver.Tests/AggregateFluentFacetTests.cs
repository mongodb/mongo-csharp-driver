/* Copyright 2016 MongoDB Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AggregateFluentFacetTests
    {
        #region static
        // private static fields
        private static readonly CollectionNamespace __collectionNamespace;
        private static readonly IMongoDatabase __database;
        private static readonly Lazy<bool> __ensureTestData;

        // static constructor
        static AggregateFluentFacetTests()
        {
            var client = DriverTestConfiguration.Client;
            var databaseNamespace = DriverTestConfiguration.DatabaseNamespace;
            __database = client.GetDatabase(databaseNamespace.DatabaseName);
            __collectionNamespace = DriverTestConfiguration.CollectionNamespace;
            __ensureTestData = new Lazy<bool>(CreateTestData);
        }

        // private static methods
        private static bool CreateTestData()
        {
            // see: https://docs.mongodb.com/master/release-notes/3.4-reference/#pipe._S_facet
            var documents = new[]
            {
                BsonDocument.Parse("{ _id: 1, title: \"The Pillars of Society\", artist : \"Grosz\", year: 1926, tags: [ \"painting\", \"satire\", \"Expressionism\", \"caricature\" ] }"),
                BsonDocument.Parse("{ _id: 2, title: \"Melancholy III\", artist : \"Munch\", year: 1902, tags: [ \"woodcut\", \"Expressionism\" ] }"),
                BsonDocument.Parse("{ _id: 3, title: \"Dancer\", artist : \"Miro\", year: 1925, tags: [ \"oil\", \"Surrealism\", \"painting\" ] }"),
                BsonDocument.Parse("{ _id: 4, title: \"The Great Wave off Kanagawa\", artist: \"Hokusai\", tags: [ \"woodblock\", \"ukiyo-e\" ] }")
            };

            __database.DropCollection(__collectionNamespace.CollectionName);
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            collection.InsertMany(documents);

            return true;
        }

        private static void EnsureTestData()
        {
            var _ = __ensureTestData.Value;
        }
        #endregion

        [Fact]
        public void Facet_with_1_facet_should_add_the_expected_stage()
        {
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var name1 = "categorizedByTags";
            var pipeline1 = PipelineDefinition<BsonDocument, BsonDocument>.Create(
                "{ $unwind : \"$tags\" }",
                "{ $sortByCount : \"$tags\" }");
            var facet1 = AggregateFacet.Create(name1, pipeline1);

            var result = subject.Facet(facet1);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be(
                @"{
                    $facet : {
                        categorizedByTags : [
                            { $unwind : '$tags' },
                            { $sortByCount : '$tags' }
                        ]
                    }
                }");
        }

        [SkippableFact]
        public void Facet_with_1_facet_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateFacetStage);
            EnsureTestData();
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var name1 = "categorizedByTags";
            var pipeline1 = PipelineDefinition<BsonDocument, BsonDocument>.Create(
                "{ $unwind : \"$tags\" }",
                "{ $sortByCount : \"$tags\" }");
            var facet1 = AggregateFacet.Create(name1, pipeline1);

            var result = subject.Facet(facet1).Single();

            result.Facets.Select(f => f.Name).Should().Equal("categorizedByTags");
            result.Facets[0].Output<BsonDocument>().Should().Equal(
                BsonDocument.Parse("{ _id: 'Expressionism', count: 2 }"),
                BsonDocument.Parse("{ _id: 'painting', count: 2 }"),
                BsonDocument.Parse("{ _id: 'ukiyo-e', count: 1 }"),
                BsonDocument.Parse("{ _id: 'woodblock', count: 1 }"),
                BsonDocument.Parse("{ _id: 'Surrealism', count: 1 }"),
                BsonDocument.Parse("{ _id: 'woodcut', count: 1 }"),
                BsonDocument.Parse("{ _id: 'oil', count: 1 }"),
                BsonDocument.Parse("{ _id: 'satire', count: 1 }"),
                BsonDocument.Parse("{ _id: 'caricature', count: 1 }"));
        }

        [Fact]
        public void Facet_with_2_facets_should_add_the_expected_stage()
        {
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var name1 = "categorizedByTags";
            var pipeline1 = PipelineDefinition<BsonDocument, BsonDocument>.Create(
                "{ $unwind : \"$tags\" }",
                "{ $sortByCount : \"$tags\" }");
            var name2 = "categorizedByYears";
            var pipeline2 = PipelineDefinition<BsonDocument, BsonDocument>.Create(
                "{ $match : { year : { $exists : 1 } } }",
                "{ $bucket : { groupBy : \"$year\", boundaries: [ 1900, 1920, 1950 ] } }");
            var facet1 = AggregateFacet.Create(name1, pipeline1);
            var facet2 = AggregateFacet.Create(name2, pipeline2);

            var result = subject.Facet(facet1, facet2);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be(
                @"{
                    $facet : {
                        categorizedByTags : [
                            { $unwind : '$tags' },
                            { $sortByCount : '$tags' }
                        ],
                        categorizedByYears: [
                            { $match : { year : { $exists : 1 } } },
                            { $bucket :  { groupBy : '$year', boundaries:  [ 1900, 1920, 1950 ] } }
                        ]
                    }
                }");
        }

        [SkippableFact]
        public void Facet_with_2_facets_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateFacetStage);
            EnsureTestData();
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var name1 = "categorizedByTags";
            var pipeline1 = PipelineDefinition<BsonDocument, BsonDocument>.Create(
                "{ $unwind : \"$tags\" }",
                "{ $sortByCount : \"$tags\" }");
            var name2 = "categorizedByYears";
            var pipeline2 = PipelineDefinition<BsonDocument, BsonDocument>.Create(
                "{ $match : { year : { $exists : 1 } } }",
                "{ $bucket : { groupBy : \"$year\", boundaries: [ 1900, 1920, 1950 ] } }");
            var facet1 = AggregateFacet.Create(name1, pipeline1);
            var facet2 = AggregateFacet.Create(name2, pipeline2);

            var result = subject.Facet(facet1, facet2).Single();

            result.Facets.Select(f => f.Name).Should().Equal("categorizedByTags", "categorizedByYears");
            result.Facets[0].Output<BsonDocument>().Should().Equal(
                BsonDocument.Parse("{ _id: 'Expressionism', count: 2 }"),
                BsonDocument.Parse("{ _id: 'painting', count: 2 }"),
                BsonDocument.Parse("{ _id: 'ukiyo-e', count: 1 }"),
                BsonDocument.Parse("{ _id: 'woodblock', count: 1 }"),
                BsonDocument.Parse("{ _id: 'Surrealism', count: 1 }"),
                BsonDocument.Parse("{ _id: 'woodcut', count: 1 }"),
                BsonDocument.Parse("{ _id: 'oil', count: 1 }"),
                BsonDocument.Parse("{ _id: 'satire', count: 1 }"),
                BsonDocument.Parse("{ _id: 'caricature', count: 1 }"));
            result.Facets[1].Output<BsonDocument>().Should().Equal(
                BsonDocument.Parse("{ _id: 1900, count: 1 }"),
                BsonDocument.Parse("{ _id: 1920, count: 2 }"));
        }

        [Fact]
        public void Facet_with_3_facets_should_add_the_expected_stage()
        {
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var name1 = "categorizedByTags";
            var pipeline1 = PipelineDefinition<BsonDocument, BsonDocument>.Create(
                "{ $unwind : \"$tags\" }",
                "{ $sortByCount : \"$tags\" }");
            var name2 = "categorizedByYears";
            var pipeline2 = PipelineDefinition<BsonDocument, BsonDocument>.Create(
                "{ $match : { year : { $exists : 1 } } }",
                "{ $bucket : { groupBy : \"$year\", boundaries: [ 1900, 1920, 1950 ] } }");
            var name3 = "categorizedByYears(Auto)";
            var pipeline3 = PipelineDefinition<BsonDocument, BsonDocument>.Create(
                "{ $bucketAuto : { groupBy: '$year', buckets: 4 } }");
            var facet1 = AggregateFacet.Create(name1, pipeline1);
            var facet2 = AggregateFacet.Create(name2, pipeline2);
            var facet3 = AggregateFacet.Create(name3, pipeline3);

            var result = subject.Facet(facet1, facet2, facet3);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be(
                @"{
                    $facet : {
                        categorizedByTags : [
                            { $unwind : '$tags' },
                            { $sortByCount : '$tags' }
                        ],
                        categorizedByYears: [
                            { $match : { year : { $exists : 1 } } },
                            { $bucket :  { groupBy : '$year', boundaries:  [ 1900, 1920, 1950 ] } }
                        ],
                        'categorizedByYears(Auto)': [
                            { $bucketAuto : { groupBy: '$year', buckets: 4 } }
                        ]
                    }
                }");
        }

        [SkippableFact]
        public void Facet_with_3_facets_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateFacetStage);
            EnsureTestData();
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var name1 = "categorizedByTags";
            var pipeline1 = PipelineDefinition<BsonDocument, BsonDocument>.Create(
                "{ $unwind : \"$tags\" }",
                "{ $sortByCount : \"$tags\" }");
            var name2 = "categorizedByYears";
            var pipeline2 = PipelineDefinition<BsonDocument, BsonDocument>.Create(
                "{ $match : { year : { $exists : 1 } } }",
                "{ $bucket : { groupBy : \"$year\", boundaries: [ 1900, 1920, 1950 ] } }");
            var name3 = "categorizedByYears(Auto)";
            var pipeline3 = PipelineDefinition<BsonDocument, BsonDocument>.Create(
                "{ $bucketAuto : { groupBy: '$year', buckets: 4 } }");
            var facet1 = AggregateFacet.Create(name1, pipeline1);
            var facet2 = AggregateFacet.Create(name2, pipeline2);
            var facet3 = AggregateFacet.Create(name3, pipeline3);

            var result = subject.Facet(facet1, facet2, facet3).Single();

            result.Facets.Select(f => f.Name).Should().Equal("categorizedByTags", "categorizedByYears", "categorizedByYears(Auto)");
            result.Facets[0].Output<BsonDocument>().Should().Equal(
                BsonDocument.Parse("{ _id: 'Expressionism', count: 2 }"),
                BsonDocument.Parse("{ _id: 'painting', count: 2 }"),
                BsonDocument.Parse("{ _id: 'ukiyo-e', count: 1 }"),
                BsonDocument.Parse("{ _id: 'woodblock', count: 1 }"),
                BsonDocument.Parse("{ _id: 'Surrealism', count: 1 }"),
                BsonDocument.Parse("{ _id: 'woodcut', count: 1 }"),
                BsonDocument.Parse("{ _id: 'oil', count: 1 }"),
                BsonDocument.Parse("{ _id: 'satire', count: 1 }"),
                BsonDocument.Parse("{ _id: 'caricature', count: 1 }"));
            result.Facets[1].Output<BsonDocument>().Should().Equal(
                BsonDocument.Parse("{ _id: 1900, count: 1 }"),
                BsonDocument.Parse("{ _id: 1920, count: 2 }"));
            result.Facets[2].Output<BsonDocument>().Should().Equal(
                BsonDocument.Parse("{ _id: { min: null, max: 1902 }, count: 1 }"),
                BsonDocument.Parse("{ _id: { min: 1902, max: 1925 }, count: 1 }"),
                BsonDocument.Parse("{ _id: { min: 1925, max: 1926 }, count: 1 }"),
                BsonDocument.Parse("{ _id: { min: 1926, max: 1926 }, count: 1 }"));
        }

        [Fact]
        public void Facet_typed_with_1_facet_should_add_the_expected_stage()
        {
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var name1 = "categorizedByTags";
            var pipeline1 = PipelineDefinition<Exhibit, CategorizedByTag>.Create(
                "{ $unwind : \"$tags\" }",
                "{ $sortByCount : \"$tags\" }");
            var facet1 = AggregateFacet.Create(name1, pipeline1);

            var result = subject.Facet<Exhibit, CategorizedByTagsResults>(facet1);

            var stage = result.Stages.Single();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var inputSerializer = serializerRegistry.GetSerializer<Exhibit>();
            var renderedStage = stage.Render(inputSerializer, serializerRegistry);
            renderedStage.Document.Should().Be(
                @"{
                    $facet : {
                        categorizedByTags : [
                            { $unwind : '$tags' },
                            { $sortByCount : '$tags' }
                        ]
                    }
                }");
        }

        [SkippableFact]
        public void Facet_typed_with_1_facet_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateFacetStage);
            EnsureTestData();
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var name1 = "categorizedByTags";
            var pipeline1 = PipelineDefinition<Exhibit, CategorizedByTag>.Create(
                "{ $unwind : \"$tags\" }",
                "{ $sortByCount : \"$tags\" }");
            var facet1 = AggregateFacet.Create(name1, pipeline1);

            var result = subject.Facet<Exhibit, CategorizedByTagsResults>(facet1).Single();

            result.CategorizedByTags.WithComparer(new CategorizedByTagComparer()).Should().Equal(
                new CategorizedByTag { Id = "Expressionism", Count = 2 },
                new CategorizedByTag { Id = "painting", Count = 2 },
                new CategorizedByTag { Id = "ukiyo-e", Count = 1 },
                new CategorizedByTag { Id = "woodblock", Count = 1 },
                new CategorizedByTag { Id = "Surrealism", Count = 1 },
                new CategorizedByTag { Id = "woodcut", Count = 1 },
                new CategorizedByTag { Id = "oil", Count = 1 },
                new CategorizedByTag { Id = "satire", Count = 1 },
                new CategorizedByTag { Id = "caricature", Count = 1 });
        }

        [Fact]
        public void Facet_typed_with_2_facets_should_add_the_expected_stage()
        {
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var name1 = "categorizedByTags";
            var pipeline1 = PipelineDefinition<Exhibit, CategorizedByTag>.Create(
                "{ $unwind : \"$tags\" }",
                "{ $sortByCount : \"$tags\" }");
            var facet1 = AggregateFacet.Create(name1, pipeline1);
            var name2 = "categorizedByYears";
            var pipeline2 = PipelineDefinition<Exhibit, CategorizedByYear>.Create(
                "{ $match : { year : { $exists : 1 } } }",
                "{ $bucket : { groupBy : \"$year\", boundaries: [ 1900, 1920, 1950 ] } }");
            var facet2 = AggregateFacet.Create(name2, pipeline2);

            var result = subject.Facet<Exhibit, CategorizedByTagsAndYearsResults>(facet1, facet2);

            var stage = result.Stages.Single();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var inputSerializer = serializerRegistry.GetSerializer<Exhibit>();
            var renderedStage = stage.Render(inputSerializer, serializerRegistry);
            renderedStage.Document.Should().Be(
                @"{
                    $facet : {
                        categorizedByTags : [
                            { $unwind : '$tags' },
                            { $sortByCount : '$tags' }
                        ],
                         categorizedByYears: [
                            { $match : { year : { $exists : 1 } } },
                            { $bucket :  { groupBy : '$year', boundaries:  [ 1900, 1920, 1950 ] } }
                        ]
                   }
                }");
        }

        [SkippableFact]
        public void Facet_typed_with_2_facets_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateFacetStage);
            EnsureTestData();
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var name1 = "categorizedByTags";
            var pipeline1 = PipelineDefinition<Exhibit, CategorizedByTag>.Create(
                "{ $unwind : \"$tags\" }",
                "{ $sortByCount : \"$tags\" }");
            var facet1 = AggregateFacet.Create(name1, pipeline1);
            var name2 = "categorizedByYears";
            var pipeline2 = PipelineDefinition<Exhibit, CategorizedByYear>.Create(
                "{ $match : { year : { $exists : 1 } } }",
                "{ $bucket : { groupBy : \"$year\", boundaries: [ 1900, 1920, 1950 ] } }");
            var facet2 = AggregateFacet.Create(name2, pipeline2);

            var result = subject.Facet<Exhibit, CategorizedByTagsAndYearsResults>(facet1, facet2).Single();

            result.CategorizedByTags.WithComparer(new CategorizedByTagComparer()).Should().Equal(
                new CategorizedByTag { Id = "Expressionism", Count = 2 },
                new CategorizedByTag { Id = "painting", Count = 2 },
                new CategorizedByTag { Id = "ukiyo-e", Count = 1 },
                new CategorizedByTag { Id = "woodblock", Count = 1 },
                new CategorizedByTag { Id = "Surrealism", Count = 1 },
                new CategorizedByTag { Id = "woodcut", Count = 1 },
                new CategorizedByTag { Id = "oil", Count = 1 },
                new CategorizedByTag { Id = "satire", Count = 1 },
                new CategorizedByTag { Id = "caricature", Count = 1 });
            result.CategorizedByYears.WithComparer(new CategorizedByYearComparer()).Should().Equal(
                new CategorizedByYear { Id = 1900, Count = 1 },
                new CategorizedByYear { Id = 1920, Count = 2 });
       }

        [Fact]
        public void Facet_typed_with_3_facets_should_add_the_expected_stage()
        {
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var name1 = "categorizedByTags";
            var pipeline1 = PipelineDefinition<Exhibit, CategorizedByTag>.Create(
                "{ $unwind : \"$tags\" }",
                "{ $sortByCount : \"$tags\" }");
            var facet1 = AggregateFacet.Create(name1, pipeline1);
            var name2 = "categorizedByYears";
            var pipeline2 = PipelineDefinition<Exhibit, CategorizedByYear>.Create(
                "{ $match : { year : { $exists : 1 } } }",
                "{ $bucket : { groupBy : \"$year\", boundaries: [ 1900, 1920, 1950 ] } }");
            var facet2 = AggregateFacet.Create(name2, pipeline2);
            var name3 = "categorizedByYears(Auto)";
            var pipeline3 = PipelineDefinition<Exhibit, CategorizedByYearAuto>.Create(
                "{ $bucketAuto : { groupBy: '$year', buckets: 4 } }");
            var facet3 = AggregateFacet.Create(name3, pipeline3);

            var result = subject.Facet<Exhibit, CategorizedByTagsAndYearsAndYearsAutoResults>(facet1, facet2, facet3);

            var stage = result.Stages.Single();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var inputSerializer = serializerRegistry.GetSerializer<Exhibit>();
            var renderedStage = stage.Render(inputSerializer, serializerRegistry);
            renderedStage.Document.Should().Be(
                @"{
                    $facet : {
                        categorizedByTags : [
                            { $unwind : '$tags' },
                            { $sortByCount : '$tags' }
                        ],
                         categorizedByYears: [
                            { $match : { year : { $exists : 1 } } },
                            { $bucket :  { groupBy : '$year', boundaries:  [ 1900, 1920, 1950 ] } }
                        ],
                        'categorizedByYears(Auto)': [
                            { $bucketAuto : { groupBy: '$year', buckets: 4 } }
                        ]
                   }
                }");
        }

        [SkippableFact]
        public void Facet_typed_with_3_facets_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateFacetStage);
            EnsureTestData();
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var name1 = "categorizedByTags";
            var pipeline1 = PipelineDefinition<Exhibit, CategorizedByTag>.Create(
                "{ $unwind : \"$tags\" }",
                "{ $sortByCount : \"$tags\" }");
            var facet1 = AggregateFacet.Create(name1, pipeline1);
            var name2 = "categorizedByYears";
            var pipeline2 = PipelineDefinition<Exhibit, CategorizedByYear>.Create(
                "{ $match : { year : { $exists : 1 } } }",
                "{ $bucket : { groupBy : \"$year\", boundaries: [ 1900, 1920, 1950 ] } }");
            var facet2 = AggregateFacet.Create(name2, pipeline2);
            var name3 = "categorizedByYears(Auto)";
            var pipeline3 = PipelineDefinition<Exhibit, CategorizedByYearAuto>.Create(
                "{ $bucketAuto : { groupBy: '$year', buckets: 4 } }");
            var facet3 = AggregateFacet.Create(name3, pipeline3);

            var result = subject.Facet<Exhibit, CategorizedByTagsAndYearsAndYearsAutoResults>(facet1, facet2, facet3).Single();

            result.CategorizedByTags.WithComparer(new CategorizedByTagComparer()).Should().Equal(
                new CategorizedByTag { Id = "Expressionism", Count = 2 },
                new CategorizedByTag { Id = "painting", Count = 2 },
                new CategorizedByTag { Id = "ukiyo-e", Count = 1 },
                new CategorizedByTag { Id = "woodblock", Count = 1 },
                new CategorizedByTag { Id = "Surrealism", Count = 1 },
                new CategorizedByTag { Id = "woodcut", Count = 1 },
                new CategorizedByTag { Id = "oil", Count = 1 },
                new CategorizedByTag { Id = "satire", Count = 1 },
                new CategorizedByTag { Id = "caricature", Count = 1 });
            result.CategorizedByYears.WithComparer(new CategorizedByYearComparer()).Should().Equal(
                new CategorizedByYear { Id = 1900, Count = 1 },
                new CategorizedByYear { Id = 1920, Count = 2 });
            result.CategorizedByYearsAuto.WithComparer(new CategorizedByYearAutoComparer()).Should().Equal(
                new CategorizedByYearAuto { Id = new MinMax { Min = null, Max = 1902 }, Count = 1 },
                new CategorizedByYearAuto { Id = new MinMax { Min = 1902, Max = 1925 }, Count = 1 },
                new CategorizedByYearAuto { Id = new MinMax { Min = 1925, Max = 1926 }, Count = 1 },
                new CategorizedByYearAuto { Id = new MinMax { Min = 1926, Max = 1926 }, Count = 1 });
        }

        // nested types
        private class Exhibit
        {
            public int Id { get; set; }
            [BsonElement("title")]
            public string Title { get; set; }
            [BsonElement("artist")]
            public string Artists { get; set; }
            [BsonElement("year")]
            public int Year { get; set; }
            [BsonElement("tags")]
            public string[] Tags { get; set; }
        }

        private class CategorizedByTagsResults
        {
            [BsonElement("categorizedByTags")]
            public CategorizedByTag[] CategorizedByTags { get; set; }
        }

        private class CategorizedByTagsAndYearsResults
        {
            [BsonElement("categorizedByTags")]
            public CategorizedByTag[] CategorizedByTags { get; set; }
            [BsonElement("categorizedByYears")]
            public CategorizedByYear[] CategorizedByYears { get; set; }
        }

        private class CategorizedByTagsAndYearsAndYearsAutoResults
        {
            [BsonElement("categorizedByTags")]
            public CategorizedByTag[] CategorizedByTags { get; set; }
            [BsonElement("categorizedByYears")]
            public CategorizedByYear[] CategorizedByYears { get; set; }
            [BsonElement("categorizedByYears(Auto)")]
            public CategorizedByYearAuto[] CategorizedByYearsAuto { get; set; }
        }

        private class CategorizedByTag
        {
            public string Id { get; set; }
            [BsonElement("count")]
            public long Count { get; set; }
        }

        private class CategorizedByYear
        {
            public int Id { get; set; }
            [BsonElement("count")]
            public long Count { get; set; }
        }

        private class CategorizedByYearAuto
        {
            public MinMax Id { get; set; }
            [BsonElement("count")]
            public long Count { get; set; }
        }

        private class MinMax
        {
            [BsonElement("min")]
            public int? Min { get; set; }
            [BsonElement("max")]
            public int Max { get; set; }
        }

        private class CategorizedByTagComparer : IEqualityComparer<CategorizedByTag>
        {
            public bool Equals(CategorizedByTag x, CategorizedByTag y)
            {
                return
                    x.Id == y.Id &&
                    x.Count == y.Count;
            }

            public int GetHashCode(CategorizedByTag obj)
            {
                throw new NotImplementedException();
            }
        }

        private class CategorizedByYearComparer : IEqualityComparer<CategorizedByYear>
        {
            public bool Equals(CategorizedByYear x, CategorizedByYear y)
            {
                return
                    x.Id == y.Id &&
                    x.Count == y.Count;
            }

            public int GetHashCode(CategorizedByYear obj)
            {
                throw new NotImplementedException();
            }
        }

        private class CategorizedByYearAutoComparer : IEqualityComparer<CategorizedByYearAuto>
        {
            public bool Equals(CategorizedByYearAuto x, CategorizedByYearAuto y)
            {
                return
                    x.Id.Min == y.Id.Min &&
                    x.Id.Max == y.Id.Max &&
                    x.Count == y.Count;
            }

            public int GetHashCode(CategorizedByYearAuto obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
