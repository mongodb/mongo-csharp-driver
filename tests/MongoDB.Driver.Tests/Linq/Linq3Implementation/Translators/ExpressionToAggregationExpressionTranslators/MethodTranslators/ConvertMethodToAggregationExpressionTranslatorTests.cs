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

        // To BinData

        [Theory]
        [InlineData(1, Mql.ByteOrder.BigEndian, null, "FormatException")]
        [InlineData(3, Mql.ByteOrder.LittleEndian,"AAAAAAAA4L8=", null)]
        [InlineData(5, Mql.ByteOrder.BigEndian, "wAQAAAAAAAA=", null )]
        public void MongoDBFunctions_ConvertToBinDataFromDouble_should_work(int id, Mql.ByteOrder byteOrder, string expectedBase64, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToBinData(x.DoubleProperty, BsonBinarySubType.Binary, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$DoubleProperty', to : {{ type: 'binData', subtype: 0  }}, {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            var expectedResult = expectedBase64 is null? null : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(0, Mql.ByteOrder.LittleEndian, "AAAAAAAABMA=", "Ag==", "AAAAAAAABMA=")]
        [InlineData(10,  Mql.ByteOrder.LittleEndian, "Ag==", "Ag==", "AAAAAAAABMA=")]
        [InlineData(0, Mql.ByteOrder.BigEndian, "AAAAAAAABMA=", "Ag==", "AAAAAAAABMA=")]
        [InlineData(10,  Mql.ByteOrder.BigEndian, "Ag==", "Ag==", "AAAAAAAABMA=")]
        public void MongoDBFunctions_ConvertToBinDataFromDoubleWithOnErrorAndOnNull_should_work(int id, Mql.ByteOrder byteOrder, string expectedBase64, string onErrorBase64, string onNullBase64)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var onErrorBinData = onErrorBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onErrorBase64));
            var onNullBinData = onNullBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onNullBase64));

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToBinData(x.DoubleProperty, BsonBinarySubType.Binary, byteOrder, onErrorBinData, onNullBinData));

            var onErrorString = onErrorBase64 == null ? "null" : $"BinData(0, '{onErrorBase64}')";
            var onNullString = onNullBase64 == null ? "null" : $"BinData(0, '{onNullBase64}')";
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$DoubleProperty', to : {{ type: 'binData', subtype: 0  }}, onError: {onErrorString}, onNull: {onNullString}, {ByteOrderToString(byteOrder)}}} }}, _id : 0 }} }}",
                };

            var expectedResult = expectedBase64 is null? null : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(1, Mql.ByteOrder.BigEndian, null, "FormatException")]
        [InlineData(4, Mql.ByteOrder.LittleEndian,"ogIAAA==", null)]
        [InlineData(6, Mql.ByteOrder.BigEndian, "AAAAKg==", null )]
        public void MongoDBFunctions_ConvertToBinDataFromInt_should_work(int id, Mql.ByteOrder byteOrder, string expectedBase64, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToBinData(x.IntProperty, BsonBinarySubType.Binary, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$IntProperty', to : {{ type: 'binData', subtype: 0  }}, {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            var expectedResult = expectedBase64 is null? null : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(0, Mql.ByteOrder.LittleEndian, "AAAAAAAABMA=", "Ag==", "AAAAAAAABMA=")]
        [InlineData(10,  Mql.ByteOrder.LittleEndian, "Ag==", "Ag==", "AAAAAAAABMA=")]
        [InlineData(0, Mql.ByteOrder.BigEndian, "AAAAAAAABMA=", "Ag==", "AAAAAAAABMA=")]
        [InlineData(10,  Mql.ByteOrder.BigEndian, "Ag==", "Ag==", "AAAAAAAABMA=")]
        public void MongoDBFunctions_ConvertToBinDataFromIntWithOnErrorAndOnNull_should_work(int id, Mql.ByteOrder byteOrder, string expectedBase64, string onErrorBase64, string onNullBase64)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var onErrorBinData = onErrorBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onErrorBase64));
            var onNullBinData = onNullBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onNullBase64));

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToBinData(x.IntProperty, BsonBinarySubType.Binary, byteOrder, onErrorBinData, onNullBinData));

            var onErrorString = onErrorBase64 == null ? "null" : $"BinData(0, '{onErrorBase64}')";
            var onNullString = onNullBase64 == null ? "null" : $"BinData(0, '{onNullBase64}')";
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$IntProperty', to : {{ type: 'binData', subtype: 0  }}, onError: {onErrorString}, onNull: {onNullString}, {ByteOrderToString(byteOrder)}}} }}, _id : 0 }} }}",
                };

            var expectedResult = expectedBase64 is null? null : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(1, Mql.ByteOrder.BigEndian, null, "FormatException")]
        [InlineData(4, Mql.ByteOrder.LittleEndian,"ogIAAA==", null)]
        [InlineData(6, Mql.ByteOrder.BigEndian, "AAAAKg==", null )]
        public void MongoDBFunctions_ConvertToLongDataFromInt_should_work(int id, Mql.ByteOrder byteOrder, string expectedBase64, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToBinData(x.LongProperty, BsonBinarySubType.Binary, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$LongProperty', to : {{ type: 'binData', subtype: 0  }}, {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            BsonBinaryData expectedResult = null;
            if (expectedBase64 is not null)
            {
                //$convert to bindata returns always 8 bytes when from long
                var expectedBytes = new byte[8];
                Array.Copy(Convert.FromBase64String(expectedBase64), 0, expectedBytes, byteOrder is Mql.ByteOrder.LittleEndian ? 0 : 4, 4);
                expectedResult = new BsonBinaryData(expectedBytes);
            }

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(0, Mql.ByteOrder.LittleEndian, "AAAAAAAABMA=", "Ag==", "AAAAAAAABMA=")]
        [InlineData(10,  Mql.ByteOrder.LittleEndian, "Ag==", "Ag==", "AAAAAAAABMA=")]
        [InlineData(0, Mql.ByteOrder.BigEndian, "AAAAAAAABMA=", "Ag==", "AAAAAAAABMA=")]
        [InlineData(10,  Mql.ByteOrder.BigEndian, "Ag==", "Ag==", "AAAAAAAABMA=")]
        public void MongoDBFunctions_ConvertToLongDataFromIntWithOnErrorAndOnNull_should_work(int id, Mql.ByteOrder byteOrder, string expectedBase64, string onErrorBase64, string onNullBase64)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var onErrorBinData = onErrorBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onErrorBase64));
            var onNullBinData = onNullBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onNullBase64));

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToBinData(x.LongProperty, BsonBinarySubType.Binary, byteOrder, onErrorBinData, onNullBinData));

            var onErrorString = onErrorBase64 == null ? "null" : $"BinData(0, '{onErrorBase64}')";
            var onNullString = onNullBase64 == null ? "null" : $"BinData(0, '{onNullBase64}')";
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$LongProperty', to : {{ type: 'binData', subtype: 0  }}, onError: {onErrorString}, onNull: {onNullString}, {ByteOrderToString(byteOrder)}}} }}, _id : 0 }} }}",
                };

            var expectedResult = expectedBase64 is null? null : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }


        // To Double

        [Theory]
        [InlineData(2, Mql.ByteOrder.BigEndian, null, "MongoCommandException")]
        [InlineData(3, Mql.ByteOrder.LittleEndian, -0.5, null)]
        [InlineData(5, Mql.ByteOrder.BigEndian, -2.5, null)]
        public void MongoDBFunctions_ConvertToDoubleFromBinData_should_work(int id, Mql.ByteOrder byteOrder, double? expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToDouble(x.BinaryProperty, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'double', {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(2, Mql.ByteOrder.LittleEndian, 15.2, 15.2, 22.3)]
        [InlineData(0, Mql.ByteOrder.LittleEndian, 22.3, 15.2, 22.3)]
        [InlineData(2, Mql.ByteOrder.BigEndian, 15.2, 15.2, 22.3)]
        [InlineData(0, Mql.ByteOrder.BigEndian, 22.3, 15.2, 22.3)]
        [InlineData(2, Mql.ByteOrder.LittleEndian, null, null, 22.3)]
        [InlineData(0, Mql.ByteOrder.LittleEndian, null, 15.2, null)]
        public void MongoDBFunctions_ConvertToDoubleFromBinDataWithOnErrorAndOnNull_should_work(int id,  Mql.ByteOrder byteOrder, double? expectedResult, double? onError, double? onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToDouble(x.BinaryProperty, byteOrder, onError, onNull));

            var onErrorString = onError == null ? "null" : onError.Value.ToString(NumberFormatInfo.InvariantInfo);
            var onNullString = onNull == null ? "null" : onNull.Value.ToString(NumberFormatInfo.InvariantInfo);
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'double', onError: {onErrorString}, onNull: {onNullString}, {ByteOrderToString(byteOrder)}}} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        // To Int

        [Theory]
        [InlineData(2, Mql.ByteOrder.LittleEndian, null, "MongoCommandException")]
        [InlineData(4, Mql.ByteOrder.LittleEndian, 674, null)]
        [InlineData(6, Mql.ByteOrder.BigEndian, 42, null)]
        public void MongoDBFunctions_ConvertToIntFromBinData_should_work(int id, Mql.ByteOrder byteOrder, int? expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToInt(x.BinaryProperty, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'int',  {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(2, Mql.ByteOrder.LittleEndian, 15, 15, 22)]
        [InlineData(0, Mql.ByteOrder.LittleEndian, 22, 15, 22)]
        [InlineData(2, Mql.ByteOrder.BigEndian, 15, 15, 22)]
        [InlineData(0, Mql.ByteOrder.BigEndian, 22, 15, 22)]
        [InlineData(2, Mql.ByteOrder.LittleEndian, null, null, 22)]
        [InlineData(0, Mql.ByteOrder.LittleEndian, null, 15, null)]
        public void MongoDBFunctions_ConvertToIntFromBinDataWithOnErrorAndOnNull_should_work(int id, Mql.ByteOrder byteOrder, int? expectedResult, int? onError, int? onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToInt(x.BinaryProperty, byteOrder, onError, onNull));

            var onErrorString = onError == null ? "null" : onError.Value.ToString(NumberFormatInfo.InvariantInfo);
            var onNullString = onNull == null ? "null" : onNull.Value.ToString(NumberFormatInfo.InvariantInfo);
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'int', onError: {onErrorString}, onNull: {onNullString}, {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        // To Long

        [Theory]
        [InlineData(2, Mql.ByteOrder.LittleEndian, null, "MongoCommandException")]
        [InlineData(4, Mql.ByteOrder.LittleEndian, (long)674, null)]
        [InlineData(6, Mql.ByteOrder.BigEndian, (long)42, null)]
        public void MongoDBFunctions_ConvertToLongFromBinData_should_work(int id, Mql.ByteOrder byteOrder, long? expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToLong(x.BinaryProperty, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'long', {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(2, Mql.ByteOrder.LittleEndian, (long)15, (long)15, (long)22)]
        [InlineData(0, Mql.ByteOrder.LittleEndian, (long)22, (long)15, (long)22)]
        [InlineData(2, Mql.ByteOrder.BigEndian, (long)15, (long)15, (long)22)]
        [InlineData(0, Mql.ByteOrder.BigEndian, (long)22, (long)15, (long)22)]
        [InlineData(2, Mql.ByteOrder.LittleEndian, null, null, (long)22)]
        [InlineData(0, Mql.ByteOrder.LittleEndian, null, (long)15, null)]
        public void MongoDBFunctions_ConvertToLongFromBinDataWithOnErrorAndOnNull_should_work(int id, Mql.ByteOrder byteOrder, long? expectedResult, long? onError, long? onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToLong(x.BinaryProperty, byteOrder, onError, onNull));

            var onErrorString = onError == null ? "null" : onError.Value.ToString(NumberFormatInfo.InvariantInfo);
            var onNullString = onNull == null ? "null" : onNull.Value.ToString(NumberFormatInfo.InvariantInfo);
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'long', onError: {onErrorString}, onNull: {onNullString}, {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
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

        private string ByteOrderToString(Mql.ByteOrder byteOrder)
        {
            var byteOrderString = byteOrder switch
            {
                Mql.ByteOrder.BigEndian => "big",
                Mql.ByteOrder.LittleEndian => "little",
                _ => throw new ArgumentOutOfRangeException(nameof(byteOrder), byteOrder, null)
            };

            return $"byteOrder: '{byteOrderString}'";
        }

        public sealed class ClassFixture : MongoCollectionFixture<TestClass>
        {

            private bool _initialed = false;
            protected override IEnumerable<TestClass> InitialData { get; } =
            [
                new TestClass {Id = 0 },
                new TestClass {Id = 1, BinaryProperty = new BsonBinaryData([0, 1, 2])},
                new TestClass {Id = 2, BinaryProperty = new BsonBinaryData(Guid.Parse("867dee52-c331-484e-92d1-c56479b8e67e"), GuidRepresentation.Standard)},
                new TestClass {Id = 3, BinaryProperty = new BsonBinaryData(Convert.FromBase64String("AAAAAAAA4L8=")), DoubleProperty = -0.5},  //LittleEndian
                new TestClass {Id = 4, BinaryProperty = new BsonBinaryData(Convert.FromBase64String("ogIAAA==")), IntProperty = 674, LongProperty = 674}, //LittleEndian
                new TestClass {Id = 5, BinaryProperty = new BsonBinaryData(Convert.FromBase64String("wAQAAAAAAAA=")), DoubleProperty = -2.5},  //BigEndian
                new TestClass {Id = 6, BinaryProperty = new BsonBinaryData(Convert.FromBase64String("AAAAKg==")), IntProperty = 42, LongProperty = 42}, //BigEndian
                new TestClass {Id = 7, BinaryProperty = new BsonBinaryData(Convert.FromBase64String("AQCAfwA="))}, //5 byte single precision double should error  //TODO Maybe we don't use this
            ];

            protected override void InitializeTestCase()
            {
                base.InitializeTestCase();

                if (_initialed)
                {
                    return;
                }

                _initialed = true;

                var errorTestCase = "{ _id: 10, DoubleProperty: NumberDecimal('-32768'), IntProperty: NumberDecimal('-32768'), LongProperty: NumberDecimal('-32768') }";
                var parsed = BsonDocument.Parse(errorTestCase);

                var untypedCollection = Database.GetCollection<BsonDocument>(GetCollectionName());

                untypedCollection.InsertOne(parsed);
            }
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