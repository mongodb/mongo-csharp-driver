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

    //TODO Need to fix tests
    public class ConvertMethodToAggregationExpressionTranslatorTests :
        LinqIntegrationTest<ConvertMethodToAggregationExpressionTranslatorTests.ClassFixture>
    {
        public ConvertMethodToAggregationExpressionTranslatorTests(ClassFixture fixture)
            : base(fixture)
        {
        }

        // To BinData

        [Theory]
        [InlineData(3, "AAAAAAAA4L8=", null)]
        [InlineData(1, null, "FormatException")]
        public void MongoDBFunctions_ConvertToBinDataFromDouble_should_work(int id, string expectedBase64, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToBinData(x.DoubleProperty, BsonBinarySubType.Binary, Mql.ByteOrder.BigEndian));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$DoubleProperty', to : {{ type: 'binData', subtype: 0  }}, format : 'hex' }} }}, _id : 0 }} }}",
                };

            var expectedResult = expectedBase64 is null? null : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(2, "AAAAAAAA4L8=", "AAAAAAAA4L8=", "AAAAAAAABMA=")]
        // [InlineData(0, "AAAAAAAABMA=", "AAAAAAAA4L8=", "AAAAAAAABMA=")]  //TODO Don't know how to cause an error
        // [InlineData(2, null, null, "AAAAAAAABMA=")] //TODO The issue here is that BsonBinaryDataSerializer can't serialize null values, an exception is thrown (because it's a BsonValueSerializerBase)
        // [InlineData(0, null, "AAAAAAAA4L8=", null)]
        public void MongoDBFunctions_ConvertToBinDataFromDoubleWithOnErrorAndOnNull_should_work(int id, string expectedBase64, string onErrorBase64, string onNullBase64)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var onErrorBinData = onErrorBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onErrorBase64));
            var onNullBinData = onNullBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onNullBase64));

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToBinData(x.DoubleProperty, BsonBinarySubType.Function, Mql.ByteOrder.BigEndian, onErrorBinData, onNullBinData));

            var onErrorString = onErrorBase64 == null ? "null" : $"BinData(0, '{onErrorBase64}')";
            var onNullString = onNullBase64 == null ? "null" : $"BinData(0, '{onNullBase64}')";
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$DoubleProperty', to : {{ type: 'binData', subtype: 1  }}, onError: {onErrorString}, onNull: {onNullString}, format : 'base64' }} }}, _id : 0 }} }}",
                };

            var expectedResult = expectedBase64 is null? null : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        // To Double

        [Theory]
        [InlineData(3, -0.5, null)]
        [InlineData(2, null, "MongoCommandException")]
        public void MongoDBFunctions_ConvertToDoubleFromBinData_should_work(int id, double? expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToDouble(x.BinaryProperty, Mql.ByteOrder.BigEndian));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'double', format : 'hex' }} }}, _id : 0 }} }}",
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
                .Select(x => Mql.ConvertToDouble(x.BinaryProperty, Mql.ByteOrder.BigEndian, onError, onNull));

            var onErrorString = onError == null ? "null" : onError.Value.ToString(NumberFormatInfo.InvariantInfo);
            var onNullString = onNull == null ? "null" : onNull.Value.ToString(NumberFormatInfo.InvariantInfo);
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'double', onError: {onErrorString}, onNull: {onNullString}, format : 'hex' }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        // To Int

        [Theory]
        [InlineData(4, 2, null)]
        [InlineData(2, null, "MongoCommandException")]
        public void MongoDBFunctions_ConvertToIntFromBinData_should_work(int id, int? expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToInt(x.BinaryProperty, Mql.ByteOrder.BigEndian));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'int', format : 'hex' }} }}, _id : 0 }} }}",
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
                .Select(x => Mql.ConvertToInt(x.BinaryProperty, Mql.ByteOrder.BigEndian, onError, onNull));

            var onErrorString = onError == null ? "null" : onError.Value.ToString(NumberFormatInfo.InvariantInfo);
            var onNullString = onNull == null ? "null" : onNull.Value.ToString(NumberFormatInfo.InvariantInfo);
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'int', onError: {onErrorString}, onNull: {onNullString}, format : 'hex' }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        // To Long

        [Theory]
        [InlineData(4, (long)2, null)]
        [InlineData(2, null, "MongoCommandException")]
        public void MongoDBFunctions_ConvertToLongFromBinData_should_work(int id, long? expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToLong(x.BinaryProperty, Mql.ByteOrder.BigEndian));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'long', format : 'hex' }} }}, _id : 0 }} }}",
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
                .Select(x => Mql.ConvertToLong(x.BinaryProperty, Mql.ByteOrder.BigEndian, onError, onNull));

            var onErrorString = onError == null ? "null" : onError.Value.ToString(NumberFormatInfo.InvariantInfo);
            var onNullString = onNull == null ? "null" : onNull.Value.ToString(NumberFormatInfo.InvariantInfo);
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'long', onError: {onErrorString}, onNull: {onNullString}, format : 'hex' }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        // To String

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
                    """{"$project": { "_v" : { "$convert" : { "input" : "$BinaryProperty", "to" : "string", "format" : "uuid" } }, "_id" : 0 }}""",
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
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'string' , onError: {onErrorString}, onNull: {onNullString}, format : 'uuid' }} }}, _id : 0 }} }}",
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
                Assert.NotNull(exception);
                Assert.Equal(expectedException, exception.GetType().Name);
            }
        }


        public sealed class ClassFixture : MongoCollectionFixture<TestClass>
        {
            protected override IEnumerable<TestClass> InitialData { get; } =
            [
                new TestClass {Id = 0 },
                new TestClass {Id = 1, BinaryProperty = new BsonBinaryData([0, 1, 2])},
                new TestClass {Id = 2, BinaryProperty = new BsonBinaryData(Guid.Parse("867dee52-c331-484e-92d1-c56479b8e67e"), GuidRepresentation.Standard), DoubleProperty = 2.45673345},
                new TestClass {Id = 3, BinaryProperty = new BsonBinaryData(Convert.FromBase64String("AAAAAAAA4L8=")), DoubleProperty = -0.5},
                new TestClass {Id = 4, BinaryProperty = new BsonBinaryData(Convert.FromBase64String("Ag=="))},
                new TestClass {Id = 5, BinaryProperty = new BsonBinaryData(Convert.FromBase64String("wAQAAAAAAAA=")), DoubleProperty = -2.5},
            ];
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