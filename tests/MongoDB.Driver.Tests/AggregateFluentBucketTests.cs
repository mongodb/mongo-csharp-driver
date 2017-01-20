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
    public class AggregateFluentBucketTests
    {
        #region static
        // private static fields
        private static CollectionNamespace __collectionNamespace;
        private static IMongoDatabase __database;
        private static Lazy<bool> __ensureTestData;

        // static constructor
        static AggregateFluentBucketTests()
        {
            var databaseNamespace = DriverTestConfiguration.DatabaseNamespace;
            __database = DriverTestConfiguration.Client.GetDatabase(databaseNamespace.DatabaseName);
            __collectionNamespace = DriverTestConfiguration.CollectionNamespace;
            __ensureTestData = new Lazy<bool>(CreateTestData);
        }

        // private static methods
        private static bool CreateTestData()
        {
            // test data is from: https://docs.mongodb.com/master/release-notes/3.4-reference/#pipe._S_bucket

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
        public void Bucket_should_add_expected_stage()
        {
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (AggregateExpressionDefinition<BsonDocument, BsonValue>)"$year";
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var options = new AggregateBucketOptions<BsonValue> { DefaultBucket = (BsonValue)"Unknown" };

            var result = subject.Bucket(groupBy, boundaries, options);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be("{ $bucket : { groupBy : \"$year\", boundaries : [ 1900, 1920, 1950 ], default : \"Unknown\" } }");
        }

        [SkippableFact]
        public void Bucket_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateBucketStage);
            EnsureTestData();
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (AggregateExpressionDefinition<BsonDocument, BsonValue>)"$year";
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var options = new AggregateBucketOptions<BsonValue> { DefaultBucket = (BsonValue)"Unknown" };

            var result = subject.Bucket(groupBy, boundaries, options).ToList();

            var comparer = AggregateBucketResultEqualityComparer<BsonValue>.Instance;
            result.WithComparer(comparer).Should().Equal(
                new AggregateBucketResult<BsonValue>(1900, 1),
                new AggregateBucketResult<BsonValue>(1920, 2),
                new AggregateBucketResult<BsonValue>("Unknown", 1));
        }

        [Fact]
        public void Bucket_with_output_should_add_expected_stage()
        {
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (AggregateExpressionDefinition<BsonDocument, BsonValue>)"$year";
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var output = (ProjectionDefinition<BsonDocument, BsonDocument>)"{ years : { $push : \"$year\" }, count : { $sum : 1 } }";
            var options = new AggregateBucketOptions<BsonValue> { DefaultBucket = (BsonValue)"Unknown" };

            var result = subject.Bucket(groupBy, boundaries, output, options);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be("{ $bucket : { groupBy : \"$year\", boundaries : [ 1900, 1920, 1950 ], default : \"Unknown\", output : { years : { $push : \"$year\" }, count : { $sum : 1 } } } }");
        }

        [SkippableFact]
        public void Bucket_with_output_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateBucketStage);
            EnsureTestData();
            var collection = __database.GetCollection<BsonDocument>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var groupBy = (AggregateExpressionDefinition<BsonDocument, BsonValue>)"$year";
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var output = (ProjectionDefinition<BsonDocument, BsonDocument>)"{ years : { $push : \"$year\" }, count : { $sum : 1 } }";
            var options = new AggregateBucketOptions<BsonValue> { DefaultBucket = (BsonValue)"Unknown" };

            var result = subject.Bucket(groupBy, boundaries, output, options).ToList();

            result.Should().Equal(
                BsonDocument.Parse("{ _id : 1900, years : [ 1902 ], count : 1 }"),
                BsonDocument.Parse("{ _id : 1920, years : [ 1926, 1925 ], count : 2 }"),
                BsonDocument.Parse("{ _id : \"Unknown\", years : [ ], count : 1 }"));
        }

        [Fact]
        public void Bucket_typed_should_add_expected_stage()
        {
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var options = new AggregateBucketOptions<BsonValue> { DefaultBucket = (BsonValue)"Unknown" };

            var result = subject.Bucket(x => x.Year, boundaries, options);

            var stage = result.Stages.Single();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var exhibitSerializer = serializerRegistry.GetSerializer<Exhibit>();
            var renderedStage = stage.Render(exhibitSerializer, serializerRegistry);
            renderedStage.Document.Should().Be("{ $bucket : { groupBy : \"$year\", boundaries : [ 1900, 1920, 1950 ], default : \"Unknown\" } }");
        }

        [SkippableFact]
        public void Bucket_typed_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateBucketStage);
            EnsureTestData();
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var options = new AggregateBucketOptions<BsonValue> { DefaultBucket = (BsonValue)"Unknown" };

            var result = subject.Bucket(x => x.Year, boundaries, options).ToList();

            var comparer = AggregateBucketResultEqualityComparer<BsonValue>.Instance;
            result.WithComparer(comparer).Should().Equal(
                new AggregateBucketResult<BsonValue>(1900, 1),
                new AggregateBucketResult<BsonValue>(1920, 2),
                new AggregateBucketResult<BsonValue>("Unknown", 1));
        }

        [Fact]
        public void Bucket_typed_with_output_should_add_expected_stage()
        {
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var options = new AggregateBucketOptions<BsonValue> { DefaultBucket = (BsonValue)"Unknown" };

            var result = subject.Bucket(
                e => e.Year,
                boundaries,
                g => new { _id = default(BsonValue), Years = g.Select(e => e.Year), Count = g.Count() },
                options);

            var stage = result.Stages.Single();
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var exhibitSerializer = serializerRegistry.GetSerializer<Exhibit>();
            var renderedStage = stage.Render(exhibitSerializer, serializerRegistry);
            renderedStage.Document.Should().Be("{ $bucket : { groupBy : \"$year\", boundaries : [ 1900, 1920, 1950 ], default : \"Unknown\", output : { Years : { $push : \"$year\" }, Count : { $sum : 1 } } } }");
        }

        [SkippableFact]
        public void Bucket_typed_with_output_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateBucketStage);
            EnsureTestData();
            var collection = __database.GetCollection<Exhibit>(__collectionNamespace.CollectionName);
            var subject = collection.Aggregate();
            var boundaries = new BsonValue[] { 1900, 1920, 1950 };
            var options = new AggregateBucketOptions<BsonValue> { DefaultBucket = (BsonValue)"Unknown" };

            var result = subject
                .Bucket(
                    e => e.Year,
                    boundaries,
                    g => new { _id = default(BsonValue), Years = g.Select(e => e.Year), Count = g.Count() },
                    options)
                .ToList();

            result.Select(b => b._id).Should().Equal(1900, 1920, "Unknown");
            result[0].Years.Should().Equal(new[] { 1902 });
            result[1].Years.Should().Equal(new[] { 1926, 1925 });
            result[2].Years.Should().Equal(new int[0]);
            result.Select(b => b.Count).Should().Equal(1, 2, 1);
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
