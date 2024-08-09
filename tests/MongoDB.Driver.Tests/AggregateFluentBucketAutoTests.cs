/* Copyright 2016-present MongoDB Inc.
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
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AggregateFluentBucketAutoTests : Linq3IntegrationTest
    {
        #region static
        // private static fields
        private static string __collectionName;
        private static IMongoDatabase __database;
        private static Lazy<bool> __ensureTestData;

        // static constructor
        static AggregateFluentBucketAutoTests()
        {
            var databaseNamespace = DriverTestConfiguration.DatabaseNamespace;
            __database = DriverTestConfiguration.Client.GetDatabase(databaseNamespace.DatabaseName);
            __collectionName = "bucketAuto";
            __ensureTestData = new Lazy<bool>(CreateTestData);
        }

        // private static methods
        private static bool CreateTestData()
        {
            __database.DropCollection(__collectionName);

            var collection = __database.GetCollection<BsonDocument>(__collectionName);
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
            var collection = __database.GetCollection<BsonDocument>(__collectionName);
            var subject = collection.Aggregate();
            var groupBy = (AggregateExpressionDefinition<BsonDocument, BsonValue>)"$year";
            var buckets = 4;

            var result = subject.BucketAuto(groupBy, buckets);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be("{ $bucketAuto : { groupBy : \"$year\", buckets : 4 } }");
        }

        [Fact]
        public void BucketAuto_should_return_expected_result()
        {
            RequireServer.Check();
            EnsureTestData();
            var collection = __database.GetCollection<BsonDocument>(__collectionName);
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
            var collection = __database.GetCollection<BsonDocument>(__collectionName);
            var subject = collection.Aggregate();
            var groupBy = (AggregateExpressionDefinition<BsonDocument, BsonValue>)"$_id";
            var buckets = 4;
            var options = new AggregateBucketAutoOptions { Granularity = AggregateBucketAutoGranularity.PowersOf2 };

            var result = subject.BucketAuto(groupBy, buckets, options);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be("{ $bucketAuto : { groupBy : \"$_id\", buckets : 4, granularity : 'POWERSOF2' } }");
        }

        [Fact]
        public void BucketAuto_with_granularity_should_return_expected_result()
        {
            RequireServer.Check();
            EnsureTestData();
            var collection = __database.GetCollection<BsonDocument>(__collectionName);
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
            var collection = __database.GetCollection<BsonDocument>(__collectionName);
            var subject = collection.Aggregate();
            var groupBy = (AggregateExpressionDefinition<BsonDocument, BsonValue>)"$year";
            var buckets = 4;
            var output = (ProjectionDefinition<BsonDocument, BsonDocument>)"{ years : { $push : \"$year\" }, count : { $sum : 1 } }";

            var result = subject.BucketAuto(groupBy, buckets, output);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be("{ $bucketAuto : { groupBy : \"$year\", buckets : 4, output : { years : { $push : \"$year\" }, count : { $sum : 1 } } } }");
        }

        [Fact]
        public void BucketAuto_with_output_should_return_expected_result()
        {
            RequireServer.Check();
            EnsureTestData();
            var collection = __database.GetCollection<BsonDocument>(__collectionName);
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
            var collection = __database.GetCollection<Exhibit>(__collectionName);
            var subject = collection.Aggregate();
            var buckets = 4;

            var result = subject.BucketAuto(x => (int?)x.Year, buckets);

            var stage = result.Stages.Single();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var exhibitSerializer = serializerRegistry.GetSerializer<Exhibit>();
            var renderedStage = stage.Render(exhibitSerializer, serializerRegistry);
            renderedStage.Document.Should().Be("{ $bucketAuto : { groupBy : \"$year\", buckets : 4 } }");
        }

        [Fact]
        public void BucketAuto_typed_should_return_expected_result()
        {
            RequireServer.Check();
            EnsureTestData();
            var collection = __database.GetCollection<Exhibit>(__collectionName);
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
            var collection = GetCollection<Exhibit>(__collectionName);
            var subject = collection.Aggregate();
            var buckets = 4;

            var result = subject.BucketAuto(
                e => (int?)e.Year,
                buckets,
                g => new { Key = g.Key, Years = g.Select(e => e.Year), Count = g.Count() });

            var stage = result.Stages.Single();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var exhibitSerializer = serializerRegistry.GetSerializer<Exhibit>();
            var renderedStage = stage.Render(exhibitSerializer, serializerRegistry);
            renderedStage.Documents.Should().HaveCount(2);
            renderedStage.Documents[0].Should().Be("{ $bucketAuto : { groupBy : '$year', buckets : 4, output : { __agg0 : { $push : '$year' }, __agg1 : { $sum : 1 } } } }");
            renderedStage.Documents[1].Should().Be("{ $project : { Key : '$_id', Years : '$__agg0', Count : '$__agg1', _id : 0 } }");
        }

        [Fact]
        public void BucketAuto_typed_with_output_should_return_expected_result()
        {
            RequireServer.Check();
            EnsureTestData();
            var collection = GetCollection<Exhibit>(__collectionName);
            var buckets = 4;

            var aggregate = collection.Aggregate()
                .BucketAuto(
                    e => (int?)e.Year,
                    buckets,
                    g => new { Key = g.Key, Years = g.Select(e => e.Year), Count = g.Count() });

            var stages = Translate(collection, aggregate);
            AssertStages(
                stages,
                "{ $bucketAuto : { groupBy : '$year', buckets : 4, output : { __agg0 : { $push : '$year' }, __agg1 : { $sum : 1 } } } }",
                "{ $project : { Key : '$_id', Years : '$__agg0', Count : '$__agg1', _id : 0 } }");

            var results = aggregate.ToList();
            results.Select(r => r.Key.Min).Should().Equal(null, 1902, 1925, 1926);
            results.Select(r => r.Key.Max).Should().Equal(1902, 1925, 1926, 1926);
            results[0].Years.Should().Equal(new int[0]);
            results[1].Years.Should().Equal(new int[] { 1902 });
            results[2].Years.Should().Equal(new int[] { 1925 });
            results[3].Years.Should().Equal(new int[] { 1926 });
            results.Select(r => r.Count).Should().Equal(1, 1, 1, 1);
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
