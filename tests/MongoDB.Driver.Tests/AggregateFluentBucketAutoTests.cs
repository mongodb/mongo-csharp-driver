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
    public class AggregateFluentBucketAutoTests
    {
        #region static
        // private static fields
        private static CollectionNamespace __collectionNamespace;
        private static IMongoDatabase __database;
        private static Lazy<bool> __ensureTestData;

        // static constructor
        static AggregateFluentBucketAutoTests()
        {
            var databaseNamespace = DriverTestConfiguration.DatabaseNamespace;
            __database = DriverTestConfiguration.Client.GetDatabase(databaseNamespace.DatabaseName);
            __collectionNamespace = DriverTestConfiguration.CollectionNamespace;
            __ensureTestData = new Lazy<bool>(CreateTestData);
        }

        // private static methods
        private static bool CreateTestData()
        {
            // test data is from: https://docs.mongodb.com/master/release-notes/3.4-reference/#pipe._S_bucketAuto

            __database.DropCollection(__collectionNamespace.CollectionName);

            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            collection.InsertMany(new[]
                {
                    BsonDocument.Parse("{ _id: 1, title: \"The Pillars of Society\", artist : \"Grosz\", year: 1926, tags: [ \"painting\", \"satire\", \"Expressionism\", \"caricature\" ] }"),
                    BsonDocument.Parse("{ _id: 2, title: \"Melancholy III\", \"artist\" : \"Munch\", year: 1902, tags: [ \"woodcut\", \"Expressionism\" ] }"),
                    BsonDocument.Parse("{ _id: 3, title: \"Dancer\", \"artist\" : \"Miro\", year: 1925, tags: [ \"oil\", \"Surrealism\", \"painting\" ] }"),
                    BsonDocument.Parse("{ _id: 4, title: \"The Great Wave off Kanagawa\", artist: \"Hokusai\", tags: [ \"woodblock\", \"ukiyo-e\" ] }")
                });

            return true;
        }

        private static void EnsureTestData()
        {
            var _ = __ensureTestData.Value;
        }

        #endregion

        // public methods
        [Fact]
        public void BucketAuto_should_add_expected_stage()
        {
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (AggregateExpressionDefinition<BsonDocument, BsonValue>)"$year";
            var buckets = 4;

            var result = subject.BucketAuto(groupBy, buckets);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be("{ $bucketAuto : { groupBy : \"$year\", buckets : 4 } }");
        }

        [SkippableFact]
        public void BucketAuto_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateBucketStage);
            EnsureTestData();
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (AggregateExpressionDefinition<BsonDocument, BsonValue>)"$year";
            var buckets = 4;

            var result = subject.BucketAuto(groupBy, buckets).ToList();

            var comparer = AggregateBucketAutoResultEqualityComparer<BsonValue>.Instance;
            result.WithComparer(comparer).Should().Equal(
                new AggregateBucketAutoResult<BsonValue>(BsonNull.Value, 1902, 1),
                new AggregateBucketAutoResult<BsonValue>(1902, 1925, 1),
                new AggregateBucketAutoResult<BsonValue>(1925, 1926, 1),
                new AggregateBucketAutoResult<BsonValue>(1926, 1926, 1));
        }

        [Fact]
        public void BucketAuto_with_granularity_should_add_expected_stage()
        {
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (AggregateExpressionDefinition<BsonDocument, BsonValue>)"$_id";
            var buckets = 4;
            var options = new AggregateBucketAutoOptions { Granularity = AggregateBucketAutoGranularity.PowersOf2 };

            var result = subject.BucketAuto(groupBy, buckets, options);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be("{ $bucketAuto : { groupBy : \"$_id\", buckets : 4, granularity : 'POWERSOF2' } }");
        }

        [SkippableFact]
        public void BucketAuto_with_granularity_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateBucketStage);
            EnsureTestData();
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (AggregateExpressionDefinition<BsonDocument, double>)"$_id";
            var buckets = 4;
            var options = new AggregateBucketAutoOptions { Granularity = AggregateBucketAutoGranularity.PowersOf2 };

            var result = subject.BucketAuto(groupBy, buckets, options).ToList();

            var comparer = AggregateBucketAutoResultEqualityComparer<double>.Instance;
            result.WithComparer(comparer).Should().Equal(
                new AggregateBucketAutoResult<double>(0.5, 2.0, 1),
                new AggregateBucketAutoResult<double>(2.0, 4.0, 2),
                new AggregateBucketAutoResult<double>(4.0, 8.0, 1));
        }

        [Fact]
        public void BucketAuto_with_output_should_add_expected_stage()
        {
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (AggregateExpressionDefinition<BsonDocument, BsonValue>)"$year";
            var buckets = 4;
            var output = (ProjectionDefinition<BsonDocument, BsonDocument>)"{ years : { $push : \"$year\" }, count : { $sum : 1 } }";

            var result = subject.BucketAuto(groupBy, buckets, output);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be("{ $bucketAuto : { groupBy : \"$year\", buckets : 4, output : { years : { $push : \"$year\" }, count : { $sum : 1 } } } }");
        }

        [SkippableFact]
        public void BucketAuto_with_output_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateBucketStage);
            EnsureTestData();
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (AggregateExpressionDefinition<BsonDocument, BsonValue>)"$year";
            var buckets = 4;
            var output = (ProjectionDefinition<BsonDocument, BsonDocument>)"{ years : { $push : \"$year\" }, count : { $sum : 1 } }";

            var result = subject.BucketAuto(groupBy, buckets, output).ToList();

            result.Should().Equal(
                BsonDocument.Parse("{ _id : { min : null, max : 1902 }, years : [ ], count : 1 }"),
                BsonDocument.Parse("{ _id : { min : 1902, max : 1925 }, years : [ 1902 ], count : 1}"),
                BsonDocument.Parse("{ _id : { min : 1925, max : 1926 }, years : [ 1925 ], count : 1 }"),
                BsonDocument.Parse("{ _id : { min : 1926, max : 1926 }, years : [ 1926 ], count : 1 }"));
        }

        [Fact]
        public void BucketAuto_typed_should_add_expected_stage()
        {
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var buckets = 4;

            var result = subject.BucketAuto(x => (int?)x.Year, buckets);

            var stage = result.Stages.Single();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var exhibitSerializer = serializerRegistry.GetSerializer<Exhibit>();
            var renderedStage = stage.Render(exhibitSerializer, serializerRegistry);
            renderedStage.Document.Should().Be("{ $bucketAuto : { groupBy : \"$year\", buckets : 4 } }");
        }

        [SkippableFact]
        public void BucketAuto_typed_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateBucketStage);
            EnsureTestData();
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var buckets = 4;

            var result = subject.BucketAuto(x => (int?)x.Year, buckets).ToList();

            var comparer = AggregateBucketAutoResultEqualityComparer<int?>.Instance;
            result.WithComparer(comparer).Should().Equal(
                new AggregateBucketAutoResult<int?>(null, 1902, 1),
                new AggregateBucketAutoResult<int?>(1902, 1925, 1),
                new AggregateBucketAutoResult<int?>(1925, 1926, 1),
                new AggregateBucketAutoResult<int?>(1926, 1926, 1));
        }

        [Fact]
        public void BucketAuto_typed_with_output_should_add_expected_stage()
        {
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var buckets = 4;

            var result = subject.BucketAuto(
                e => e.Year,
                buckets,
                g => new { _id = default(AggregateBucketAutoResultId<int?>), Years = g.Select(e => e.Year), Count = g.Count() });

            var stage = result.Stages.Single();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var exhibitSerializer = serializerRegistry.GetSerializer<Exhibit>();
            var renderedStage = stage.Render(exhibitSerializer, serializerRegistry);
            renderedStage.Document.Should().Be("{ $bucketAuto : { groupBy : \"$year\", buckets : 4, output : { Years : { $push : \"$year\" }, Count : { $sum : 1 } } } }");
        }

        [SkippableFact]
        public void BucketAuto_typed_with_output_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateBucketStage);
            EnsureTestData();
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var buckets = 4;

            var result = subject
                .BucketAuto(
                    e => e.Year,
                    buckets,
                    g => new { _id = default(AggregateBucketAutoResultId<int?>), Years = g.Select(e => e.Year), Count = g.Count() })
                .ToList();

            result.Select(r => r._id.Min).Should().Equal(null, 1902, 1925, 1926);
            result.Select(r => r._id.Max).Should().Equal(1902, 1925, 1926, 1926);
            result[0].Years.Should().Equal(new int[0]);
            result[1].Years.Should().Equal(new int[] { 1902 });
            result[2].Years.Should().Equal(new int[] { 1925 });
            result[3].Years.Should().Equal(new int[] { 1926 });
            result.Select(r => r.Count).Should().Equal(1, 1, 1, 1);
        }

        // nested types
        private class Exhibit
        {
            [BsonId]
            public int Id { get; set; }
            [BsonElement("title")]
            public string Title { get; set; }
            [BsonElement("year")]
            public int Year { get; set; }
            [BsonElement("tags")]
            public string[] Tags { get; set; }
        }
    }
}
