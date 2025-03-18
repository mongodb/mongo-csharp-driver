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
using System.Globalization;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{

    public class ConvertMethodToAggregationExpressionTranslatorTests :
        LinqIntegrationTest<ConvertMethodToAggregationExpressionTranslatorTests.ClassFixture>
    {
        public ConvertMethodToAggregationExpressionTranslatorTests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [InlineData(3, -0.5, null)]
        [InlineData(2, null, "MongoCommandException")]
        public void MongoDBFunctions_ConvertToDoubleFromBinData_should_work(int id, double? expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToDouble(x.BinaryProperty, "hex"));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 1, format : 'hex' }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(2, 15.2, 15.2, 22.3)]
        [InlineData(0, 22.3, 15.2, 22.3)]
        [InlineData(2, null, null, 22.3)]
        [InlineData(0, null, 15.2, null)]
        public void MongoDBFunctions_ConvertToDoubleFromBinDataWithOnErrorAndOnNull_should_work(int id, double? expectedResult, double? onError, double? onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToDouble(x.BinaryProperty, "hex", onError, onNull));

            var onErrorString = onError == null ? "null" : onError.Value.ToString(NumberFormatInfo.InvariantInfo);
            var onNullString = onNull == null ? "null" : onNull.Value.ToString(NumberFormatInfo.InvariantInfo);
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 1, onError: {onErrorString}, onNull: {onNullString}, format : 'hex' }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(4, 2, null)]
        [InlineData(2, null, "MongoCommandException")]
        public void MongoDBFunctions_ConvertToIntFromBinData_should_work(int id, int? expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToInt(x.BinaryProperty, "hex"));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 16, format : 'hex' }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(2, 15, 15, 22)]
        [InlineData(0, 22, 15, 22)]
        [InlineData(2, null, null, 22)]
        [InlineData(0, null, 15, null)]
        public void MongoDBFunctions_ConvertToIntFromBinDataWithOnErrorAndOnNull_should_work(int id, int? expectedResult, int? onError, int? onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToInt(x.BinaryProperty, "hex", onError, onNull));

            var onErrorString = onError == null ? "null" : onError.Value.ToString(NumberFormatInfo.InvariantInfo);
            var onNullString = onNull == null ? "null" : onNull.Value.ToString(NumberFormatInfo.InvariantInfo);
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 16, onError: {onErrorString}, onNull: {onNullString}, format : 'hex' }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(4, (long)2, null)]
        [InlineData(2, null, "MongoCommandException")]
        public void MongoDBFunctions_ConvertToLongFromBinData_should_work(int id, long? expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToLong(x.BinaryProperty, "hex"));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 18, format : 'hex' }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(2, (long)15, (long)15, (long)22)]
        [InlineData(0, (long)22, (long)15, (long)22)]
        [InlineData(2, null, null, (long)22)]
        [InlineData(0, null, (long)15, null)]
        public void MongoDBFunctions_ConvertToLongFromBinDataWithOnErrorAndOnNull_should_work(int id, long? expectedResult, long? onError, long? onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToLong(x.BinaryProperty, "hex", onError, onNull));

            var onErrorString = onError == null ? "null" : onError.Value.ToString(NumberFormatInfo.InvariantInfo);
            var onNullString = onNull == null ? "null" : onNull.Value.ToString(NumberFormatInfo.InvariantInfo);
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 18, onError: {onErrorString}, onNull: {onNullString}, format : 'hex' }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(2, "867dee52-c331-484e-92d1-c56479b8e67e", null)]
        [InlineData(1, null, "MongoCommandException")]
        public void MongoDBFunctions_ConvertToStringFromBinData_should_work(int id, string expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToString(x.BinaryProperty, "uuid"));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    """{"$project": { "_v" : { "$convert" : { "input" : "$BinaryProperty", "to" : 2, "format" : "uuid" } }, "_id" : 0 }}""",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(0, "onNull", "onError", "onNull")]
        [InlineData(1, "onError", "onError", "onNull")]
        [InlineData(0, null, "onError", null)]
        [InlineData(1, null, null, "onNull")]
        public void MongoDBFunctions_ConvertToStringFromBinDataWithOnErrorAndOnNull_should_work(int id, string expectedResult, string onError, string onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToString(x.BinaryProperty, "uuid", onError, onNull));

            var onErrorString = onError == null ? "null" : $"'{onError}'";
            var onNullString = onNull == null ? "null" : $"'{onNull}'";

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 2, onError: {onErrorString}, onNull: {onNullString}, format : 'uuid' }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        private void AssertOutcome<TResult>(IMongoCollection<TestClass> collection,
            IQueryable<TResult> queryable,
            string[] expectedStages,
            TResult expectedResult,
            string expectedException = null)
        {
            TResult result = default;

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStages);
            var exception = Record.Exception(() => result = queryable.Single());

            if (string.IsNullOrEmpty(expectedException))
            {
                Assert.Null(exception);
                Assert.Equal(expectedResult, result);
            }
            else
            {
                Assert.Equal(expectedException, exception.GetType().Name);
            }
        }


        public sealed class ClassFixture : MongoCollectionFixture<TestClass>
        {
            protected override IEnumerable<TestClass> InitialData { get; } =
            [
                new TestClass {Id = 0 },
                new TestClass {Id = 1, BinaryProperty = new BsonBinaryData([0, 1, 2])},
                new TestClass {Id = 2, BinaryProperty = new BsonBinaryData(Guid.Parse("867dee52-c331-484e-92d1-c56479b8e67e"), GuidRepresentation.Standard)},
                new TestClass {Id = 3, BinaryProperty = new BsonBinaryData(Convert.FromBase64String("AAAAAAAA4L8="))},
                new TestClass {Id = 4, BinaryProperty = new BsonBinaryData(Convert.FromBase64String("Ag=="))}
            ];

            private IEnumerable<BsonDocument> InitialDataUnTyped { get; } =
            [
                BsonDocument.Parse("{ _id : 7 }")
            ];


            //TODO Remove all of this
            protected override void InitializeTestCase()
            {
                base.InitializeTestCase();
                // var collection = Database.GetCollection<BsonDocument>(Collection.CollectionNamespace.CollectionName);
                // collection.InsertMany(InitialDataUnTyped);
            }

            public IMongoCollection<BsonDocument> UnTypedCollection =>
                Database.GetCollection<BsonDocument>(Collection.CollectionNamespace.CollectionName);
        }

        public class TestClass
        {
            public int Id { get; set; }

            [BsonIgnoreIfDefault]
            public BsonBinaryData BinaryProperty { get; set; }
            public double? DoubleProperty { get; set; }
            public int? IntProperty { get; set; }
            public long? LongProperty { get; set; }
            public string StringProperty { get; set; }

        }
    }
}