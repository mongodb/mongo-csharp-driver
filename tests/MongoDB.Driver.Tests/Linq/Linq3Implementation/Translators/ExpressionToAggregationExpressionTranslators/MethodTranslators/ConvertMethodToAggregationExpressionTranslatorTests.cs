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
        [InlineData(1, ByteOrder.BigEndian, null, "FormatException")]
        [InlineData(3, ByteOrder.LittleEndian,"AAAAAAAA4L8=", null)]
        [InlineData(5, ByteOrder.BigEndian, "wAQAAAAAAAA=", null )]
        public void MongoDBFunctions_ToBsonBinaryDataFromDouble_should_work(int id, ByteOrder byteOrder, string expectedBase64, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToBsonBinaryData(x.NullableDoubleProperty, BsonBinarySubType.Binary, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$DoubleProperty', to : {{ type: 'binData', subtype: 0  }}, {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            var expectedResult = expectedBase64 is null? null : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        // [Theory]
        // [InlineData(0, ByteOrder.LittleEndian, "AAAAAAAABMA=", "Ag==", "AAAAAAAABMA=")]
        // [InlineData(10,  ByteOrder.LittleEndian, "Ag==", "Ag==", "AAAAAAAABMA=")]
        // [InlineData(0, ByteOrder.BigEndian, "AAAAAAAABMA=", "Ag==", "AAAAAAAABMA=")]
        // [InlineData(10,  ByteOrder.BigEndian, "Ag==", "Ag==", "AAAAAAAABMA=")]
        // public void MongoDBFunctions_ToBsonBinaryDataFromDoubleWithOnErrorAndOnNull_should_work(int id, ByteOrder byteOrder, string expectedBase64, string onErrorBase64, string onNullBase64)
        // {
        //     RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);
        //
        //     var onErrorBinData = onErrorBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onErrorBase64));
        //     var onNullBinData = onNullBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onNullBase64));
        //
        //     var collection = Fixture.Collection;
        //     var queryable = collection.AsQueryable()
        //         .Where(x => x.Id == id)
        //         .Select(x => Mql.ToBsonBinaryData(x.DoubleProperty, BsonBinarySubType.Binary, byteOrder, onErrorBinData, onNullBinData));
        //
        //     var onErrorString = onErrorBase64 == null ? "null" : $"BinData(0, '{onErrorBase64}')";
        //     var onNullString = onNullBase64 == null ? "null" : $"BinData(0, '{onNullBase64}')";
        //     var expectedStages =
        //         new[]
        //         {
        //             $"{{ $match : {{ _id : {id} }} }}",
        //             $"{{ $project: {{ _v : {{ $convert : {{ input : '$DoubleProperty', to : {{ type: 'binData', subtype: 0  }}, onError: {onErrorString}, onNull: {onNullString}, {ByteOrderToString(byteOrder)}}} }}, _id : 0 }} }}",
        //         };
        //
        //     var expectedResult = expectedBase64 is null? null : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
        //     AssertOutcome(collection, queryable, expectedStages, expectedResult);
        // }

        [Theory]
        [InlineData(1, ByteOrder.BigEndian, null, "FormatException")]
        [InlineData(4, ByteOrder.LittleEndian,"ogIAAA==", null)]
        [InlineData(6, ByteOrder.BigEndian, "AAAAKg==", null )]
        public void MongoDBFunctions_ToBsonBinaryDataFromInt_should_work(int id, ByteOrder byteOrder, string expectedBase64, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToBsonBinaryData(x.NullableIntProperty, BsonBinarySubType.Binary, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$IntProperty', to : {{ type: 'binData', subtype: 0  }}, {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            var expectedResult = expectedBase64 is null? null : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        // [Theory]
        // [InlineData(0, ByteOrder.LittleEndian, "AAAAAAAABMA=", "Ag==", "AAAAAAAABMA=")]
        // [InlineData(10,  ByteOrder.LittleEndian, "Ag==", "Ag==", "AAAAAAAABMA=")]
        // [InlineData(0, ByteOrder.BigEndian, "AAAAAAAABMA=", "Ag==", "AAAAAAAABMA=")]
        // [InlineData(10,  ByteOrder.BigEndian, "Ag==", "Ag==", "AAAAAAAABMA=")]
        // public void MongoDBFunctions_ToBsonBinaryDataFromIntWithOnErrorAndOnNull_should_work(int id, ByteOrder byteOrder, string expectedBase64, string onErrorBase64, string onNullBase64)
        // {
        //     RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);
        //
        //     var onErrorBinData = onErrorBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onErrorBase64));
        //     var onNullBinData = onNullBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onNullBase64));
        //
        //     var collection = Fixture.Collection;
        //     var queryable = collection.AsQueryable()
        //         .Where(x => x.Id == id)
        //         .Select(x => Mql.ToBsonBinaryData(x.IntProperty, BsonBinarySubType.Binary, byteOrder, onErrorBinData, onNullBinData));
        //
        //     var onErrorString = onErrorBase64 == null ? "null" : $"BinData(0, '{onErrorBase64}')";
        //     var onNullString = onNullBase64 == null ? "null" : $"BinData(0, '{onNullBase64}')";
        //     var expectedStages =
        //         new[]
        //         {
        //             $"{{ $match : {{ _id : {id} }} }}",
        //             $"{{ $project: {{ _v : {{ $convert : {{ input : '$IntProperty', to : {{ type: 'binData', subtype: 0  }}, onError: {onErrorString}, onNull: {onNullString}, {ByteOrderToString(byteOrder)}}} }}, _id : 0 }} }}",
        //         };
        //
        //     var expectedResult = expectedBase64 is null? null : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
        //     AssertOutcome(collection, queryable, expectedStages, expectedResult);
        // }

        [Theory]
        [InlineData(1, ByteOrder.BigEndian, null, "FormatException")]
        [InlineData(4, ByteOrder.LittleEndian,"ogIAAA==", null)]
        [InlineData(6, ByteOrder.BigEndian, "AAAAKg==", null )]
        public void MongoDBFunctions_ToBsonBinaryDataFromLong_should_work(int id, ByteOrder byteOrder, string expectedBase64, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToBsonBinaryData(x.NullableLongProperty, BsonBinarySubType.Binary, byteOrder));

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
                Array.Copy(Convert.FromBase64String(expectedBase64), 0, expectedBytes, byteOrder is ByteOrder.LittleEndian ? 0 : 4, 4);
                expectedResult = new BsonBinaryData(expectedBytes);
            }

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        // [Theory]
        // [InlineData(0, ByteOrder.LittleEndian, "AAAAAAAABMA=", "Ag==", "AAAAAAAABMA=")]
        // [InlineData(10,  ByteOrder.LittleEndian, "Ag==", "Ag==", "AAAAAAAABMA=")]
        // [InlineData(0, ByteOrder.BigEndian, "AAAAAAAABMA=", "Ag==", "AAAAAAAABMA=")]
        // [InlineData(10,  ByteOrder.BigEndian, "Ag==", "Ag==", "AAAAAAAABMA=")]
        // public void MongoDBFunctions_ToLongDataFromIntWithOnErrorAndOnNull_should_work(int id, ByteOrder byteOrder, string expectedBase64, string onErrorBase64, string onNullBase64)
        // {
        //     RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);
        //
        //     var onErrorBinData = onErrorBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onErrorBase64));
        //     var onNullBinData = onNullBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onNullBase64));
        //
        //     var collection = Fixture.Collection;
        //     var queryable = collection.AsQueryable()
        //         .Where(x => x.Id == id)
        //         .Select(x => Mql.ToBsonBinaryData(x.LongProperty, BsonBinarySubType.Binary, byteOrder, onErrorBinData, onNullBinData));
        //
        //     var onErrorString = onErrorBase64 == null ? "null" : $"BinData(0, '{onErrorBase64}')";
        //     var onNullString = onNullBase64 == null ? "null" : $"BinData(0, '{onNullBase64}')";
        //     var expectedStages =
        //         new[]
        //         {
        //             $"{{ $match : {{ _id : {id} }} }}",
        //             $"{{ $project: {{ _v : {{ $convert : {{ input : '$LongProperty', to : {{ type: 'binData', subtype: 0  }}, onError: {onErrorString}, onNull: {onNullString}, {ByteOrderToString(byteOrder)}}} }}, _id : 0 }} }}",
        //         };
        //
        //     var expectedResult = expectedBase64 is null? null : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
        //     AssertOutcome(collection, queryable, expectedStages, expectedResult);
        // }

        [Theory]
        [InlineData(1, null, "FormatException")]
        [InlineData(2, "867dee52-c331-484e-92d1-c56479b8e67e", null)]
        public void MongoDBFunctions_ToBsonBinaryDataFromString_should_work(int id, string expectedGuidString, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromString);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToBsonBinaryData(x.StringProperty, BsonBinarySubType.UuidStandard, "uuid"));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$StringProperty', to : {{ type: 'binData', subtype: 4  }}, format: 'uuid' }} }}, _id : 0 }} }}",
                };

            var expectedResult = expectedGuidString is null? null : new BsonBinaryData(Guid.Parse(expectedGuidString), GuidRepresentation.Standard);
            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        // [Theory]
        // [InlineData(0, "AAAAAAAABMA=", "Ag==", "AAAAAAAABMA=")]
        // [InlineData(10, "Ag==", "Ag==", "AAAAAAAABMA=")]
        // public void MongoDBFunctions_ToBsonBinaryDataFromStringWithOnErrorAndOnNull_should_work(int id, string expectedBase64, string onErrorBase64, string onNullBase64)
        // {
        //     RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);
        //
        //     var onErrorBinData = onErrorBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onErrorBase64));
        //     var onNullBinData = onNullBase64 == null ? null : new BsonBinaryData(Convert.FromBase64String(onNullBase64));
        //
        //     var collection = Fixture.Collection;
        //     var queryable = collection.AsQueryable()
        //         .Where(x => x.Id == id)
        //         .Select(x => Mql.ToBsonBinaryData(x.StringProperty, BsonBinarySubType.UuidStandard, "uuid", onErrorBinData, onNullBinData));
        //
        //     var onErrorString = onErrorBase64 == null ? "null" : $"BinData(0, '{onErrorBase64}')";
        //     var onNullString = onNullBase64 == null ? "null" : $"BinData(0, '{onNullBase64}')";
        //
        //     var expectedStages =
        //         new[]
        //         {
        //             $"{{ $match : {{ _id : {id} }} }}",
        //             $"{{ $project: {{ _v : {{ $convert : {{ input : '$StringProperty', to : {{ type: 'binData', subtype: 4  }}, onError: {onErrorString}, onNull: {onNullString}, format: 'uuid' }} }}, _id : 0 }} }}",
        //         };
        //
        //     var expectedResult = expectedBase64 is null? null : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
        //     AssertOutcome(collection, queryable, expectedStages, expectedResult);
        // }

        // To Double

        [Theory]
        [InlineData(2, ByteOrder.BigEndian, 0, "MongoCommandException")]
        [InlineData(3, ByteOrder.LittleEndian, -0.5, null)]
        [InlineData(5, ByteOrder.BigEndian, -2.5, null)]
        public void MongoDBFunctions_ToDoubleFromBsonBinaryData_should_work(int id, ByteOrder byteOrder, double expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToDouble(x.BinaryProperty, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'double', {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(0, ByteOrder.BigEndian, null, null)]
        [InlineData(2, ByteOrder.BigEndian, null, "MongoCommandException")]
        [InlineData(3, ByteOrder.LittleEndian, -0.5, null)]
        [InlineData(5, ByteOrder.BigEndian, -2.5, null)]
        public void MongoDBFunctions_ToNullableDoubleFromBsonBinaryData_should_work(int id, ByteOrder byteOrder, double? expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToNullableDouble(x.BinaryProperty, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'double', {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(2, ByteOrder.LittleEndian, 15.2, true, 15.2, true, 22.3)]
        [InlineData(0, ByteOrder.LittleEndian, 22.3, true, 15.2, true, 22.3)]
        [InlineData(2, ByteOrder.BigEndian, 15.2,true, 15.2, true,22.3)]
        [InlineData(0, ByteOrder.BigEndian, 22.3, true, 15.2, true, 22.3)]
        [InlineData(2, ByteOrder.LittleEndian, 0, true, 0, true, 22.3)]
        [InlineData(0, ByteOrder.LittleEndian, 0, true, 15.2, true, 0)]
        [InlineData(0, ByteOrder.LittleEndian, 0, false, 15.2, true, 0)]
        public void MongoDBFunctions_ToDoubleFromBsonBinaryDataWithOnErrorAndOnNull_should_work(int id,  ByteOrder byteOrder, double expectedResult, bool setOnError, double onError, bool setOnNull, double onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var options = new ConvertOptions<double>();
            if (setOnError) options.OnError = onError;
            if (setOnNull) options.OnNull = onNull;

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToDouble(x.BinaryProperty, byteOrder, options));

            var onErrorStr = setOnError ? $"onError: {Format(onError)}," : "";
            var onNullStr = setOnNull ? $"onNull: {Format(onNull)}," : "";

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'double', {onErrorStr} {onNullStr} {ByteOrderToString(byteOrder)}}} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(2, ByteOrder.LittleEndian, 15.2, true, 15.2, true, 22.3)]
        [InlineData(0, ByteOrder.LittleEndian, 22.3, true, 15.2, true, 22.3)]
        [InlineData(2, ByteOrder.BigEndian, 15.2,true, 15.2, true,22.3)]
        [InlineData(0, ByteOrder.BigEndian, 22.3, true, 15.2, true, 22.3)]
        [InlineData(2, ByteOrder.LittleEndian, null, true, null, true, 22.3)]
        [InlineData(0, ByteOrder.LittleEndian, null, true, 15.2, true, null)]
        [InlineData(2, ByteOrder.LittleEndian, null, true, null, false, 22.3)]
        [InlineData(0, ByteOrder.LittleEndian, null, false, 15.2, true, null)]
        public void MongoDBFunctions_ToNullableDoubleFromBsonBinaryDataWithOnErrorAndOnNull_should_work(int id,  ByteOrder byteOrder, double? expectedResult, bool setOnError, double? onError, bool setOnNull, double? onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var options = new ConvertOptions<double?>();
            if (setOnError) options.OnError = onError;
            if (setOnNull) options.OnNull = onNull;

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToNullableDouble(x.BinaryProperty, byteOrder, options));

            var onErrorStr = setOnError ? $"onError: {Format(onError)}," : "";
            var onNullStr = setOnNull ? $"onNull: {Format(onNull)}," : "";

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'double', {onErrorStr} {onNullStr} {ByteOrderToString(byteOrder)}}} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        // To Int

        [Theory]
        [InlineData(2, ByteOrder.LittleEndian, 0, "MongoCommandException")]
        [InlineData(4, ByteOrder.LittleEndian, 674, null)]
        [InlineData(6, ByteOrder.BigEndian, 42, null)]
        public void MongoDBFunctions_ToIntFromBsonBinaryData_should_work(int id, ByteOrder byteOrder, int expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToInt(x.BinaryProperty, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'int',  {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(0, ByteOrder.BigEndian, null, null)]
        [InlineData(2, ByteOrder.LittleEndian, null, "MongoCommandException")]
        [InlineData(4, ByteOrder.LittleEndian, 674, null)]
        [InlineData(6, ByteOrder.BigEndian, 42, null)]
        public void MongoDBFunctions_ToNullableIntFromBsonBinaryData_should_work(int id, ByteOrder byteOrder, int? expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToNullableInt(x.BinaryProperty, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'int',  {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(2, ByteOrder.LittleEndian, 15, true, 15, true, 22)]
        [InlineData(0, ByteOrder.LittleEndian, 22, true, 15, true, 22)]
        [InlineData(2, ByteOrder.BigEndian, 15, true, 15, true, 22)]
        [InlineData(0, ByteOrder.BigEndian, 22, true, 15, true, 22)]
        [InlineData(2, ByteOrder.LittleEndian, 0, true, 0, true, 22)]
        [InlineData(0, ByteOrder.LittleEndian, 0, true, 15, true, 0)]
        [InlineData(0, ByteOrder.LittleEndian, 0, false, 15, true, 0)]
        public void MongoDBFunctions_ToIntFromBsonBinaryDataWithOnErrorAndOnNull_should_work(int id, ByteOrder byteOrder, int expectedResult, bool setOnError, int onError, bool setOnNull, int onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var options = new ConvertOptions<int>();
            if (setOnError) options.OnError = onError;
            if (setOnNull) options.OnNull = onNull;

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToInt(x.BinaryProperty, byteOrder, options));

            var onErrorStr = setOnError ? $"onError: {Format(onError)}," : "";
            var onNullStr = setOnNull ? $"onNull: {Format(onNull)}," : "";

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'int', {onErrorStr} {onNullStr} {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(2, ByteOrder.LittleEndian, 15, true, 15, true, 22)]
        [InlineData(0, ByteOrder.LittleEndian, 22, true, 15, true, 22)]
        [InlineData(2, ByteOrder.BigEndian, 15, true, 15, true, 22)]
        [InlineData(0, ByteOrder.BigEndian, 22, true, 15, true, 22)]
        [InlineData(2, ByteOrder.LittleEndian, null, true, null, true, 22)]
        [InlineData(0, ByteOrder.LittleEndian, null, true, 15, true, null)]
        [InlineData(2, ByteOrder.LittleEndian, null, true, null, false, 22)]
        [InlineData(0, ByteOrder.LittleEndian, null, false, 15, true, null)]
        public void MongoDBFunctions_ToNullableIntFromBsonBinaryDataWithOnErrorAndOnNull_should_work(int id, ByteOrder byteOrder, int? expectedResult, bool setOnError, int? onError, bool setOnNull, int? onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var options = new ConvertOptions<int?>();
            if (setOnError) options.OnError = onError;
            if (setOnNull) options.OnNull = onNull;

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToNullableInt(x.BinaryProperty, byteOrder, options));

            var onErrorStr = setOnError ? $"onError: {Format(onError)}," : "";
            var onNullStr = setOnNull ? $"onNull: {Format(onNull)}," : "";

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'int', {onErrorStr} {onNullStr} {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        // To Long

        [Theory]
        [InlineData(2, ByteOrder.LittleEndian, 0, "MongoCommandException")]
        [InlineData(4, ByteOrder.LittleEndian, (long)674, null)]
        [InlineData(6, ByteOrder.BigEndian, (long)42, null)]
        public void MongoDBFunctions_ToLongFromBsonBinaryData_should_work(int id, ByteOrder byteOrder, long expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToLong(x.BinaryProperty, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'long', {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(0, ByteOrder.BigEndian, null, null)]
        [InlineData(2, ByteOrder.LittleEndian, null, "MongoCommandException")]
        [InlineData(4, ByteOrder.LittleEndian, (long)674, null)]
        [InlineData(6, ByteOrder.BigEndian, (long)42, null)]
        public void MongoDBFunctions_ToNullableLongFromBsonBinaryData_should_work(int id, ByteOrder byteOrder, long? expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToNullableLong(x.BinaryProperty, byteOrder));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'long', {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(2, ByteOrder.LittleEndian, (long)15, true, (long)15, true, (long)22)]
        [InlineData(0, ByteOrder.LittleEndian, (long)22, true, (long)15, true, (long)22)]
        [InlineData(2, ByteOrder.BigEndian, (long)15, true, (long)15, true, (long)22)]
        [InlineData(0, ByteOrder.BigEndian, (long)22, true, (long)15, true, (long)22)]
        [InlineData(2, ByteOrder.LittleEndian, 0, true, 0, true, (long)22)]
        [InlineData(0, ByteOrder.LittleEndian, 0, true, (long)15, true, 0)]
        [InlineData(0, ByteOrder.LittleEndian, 0, false, (long)15, true, 0)]
        public void MongoDBFunctions_ToLongFromBsonBinaryDataWithOnErrorAndOnNull_should_work(int id, ByteOrder byteOrder, long expectedResult, bool setOnError, long onError, bool setOnNull, long onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var options = new ConvertOptions<long>();
            if (setOnError) options.OnError = onError;
            if (setOnNull) options.OnNull = onNull;

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToLong(x.BinaryProperty, byteOrder, options));

            var onErrorStr = setOnError ? $"onError: {Format(onError)}," : "";
            var onNullStr = setOnNull ? $"onNull: {Format(onNull)}," : "";

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'long',  {onErrorStr} {onNullStr} {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(2, ByteOrder.LittleEndian, (long)15, true, (long)15, true, (long)22)]
        [InlineData(0, ByteOrder.LittleEndian, (long)22, true, (long)15, true, (long)22)]
        [InlineData(2, ByteOrder.BigEndian, (long)15, true, (long)15, true, (long)22)]
        [InlineData(0, ByteOrder.BigEndian, (long)22, true, (long)15, true, (long)22)]
        [InlineData(2, ByteOrder.LittleEndian, null, true, null, true, (long)22)]
        [InlineData(0, ByteOrder.LittleEndian, null, true, (long)15, true, null)]
        [InlineData(2, ByteOrder.LittleEndian, null, true, null, false, (long)22)]
        [InlineData(0, ByteOrder.LittleEndian, null, false, (long)15, true, null)]
        public void MongoDBFunctions_ToNullableLongFromBsonBinaryDataWithOnErrorAndOnNull_should_work(int id, ByteOrder byteOrder, long? expectedResult, bool setOnError, long? onError, bool setOnNull, long? onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var options = new ConvertOptions<long?>();
            if (setOnError) options.OnError = onError;
            if (setOnNull) options.OnNull = onNull;

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToNullableLong(x.BinaryProperty, byteOrder, options));

            var onErrorStr = setOnError ? $"onError: {Format(onError)}," : "";
            var onNullStr = setOnNull ? $"onNull: {Format(onNull)}," : "";

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'long',  {onErrorStr} {onNullStr} {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        // To String

        [Theory]
        [InlineData(2, "867dee52-c331-484e-92d1-c56479b8e67e", null)]
        [InlineData(1, null, "MongoCommandException")]
        public void MongoDBFunctions_ToStringFromBsonBinaryData_should_work(int id, string expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromString);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToString(x.BinaryProperty, "uuid"));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    """{"$project": { "_v" : { "$convert" : { "input" : "$BinaryProperty", "to" : "string", "format" : "uuid" } }, "_id" : 0 }}""",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(0, "onNull", true, "onError", true, "onNull")]
        [InlineData(1, "onError", true, "onError", true, "onNull")]
        [InlineData(0, null, true, "onError", true, null)]
        [InlineData(1, null, true, null, true, "onNull")]
        [InlineData(0, null, false, "onError", true, null)]
        [InlineData(1, null, true, null, false, "onNull")]
        public void MongoDBFunctions_ToStringFromBsonBinaryDataWithOnErrorAndOnNull_should_work(int id, string expectedResult, bool setOnError, string onError, bool setOnNull, string onNull)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromString);

            var options = new ConvertOptions<string>();
            if (setOnError) options.OnError = onError;
            if (setOnNull) options.OnNull = onNull;

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ToString(x.BinaryProperty, "uuid", options));

            var onErrorStr = setOnError ? $"onError: {Format(onError)}," : "";
            var onNullStr = setOnNull ? $"onNull: {Format(onNull)}," : "";

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'string', {onErrorStr} {onNullStr} format : 'uuid' }} }}, _id : 0 }} }}",
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

        private string ByteOrderToString(ByteOrder byteOrder)
        {
            var byteOrderString = byteOrder switch
            {
                ByteOrder.BigEndian => "big",
                ByteOrder.LittleEndian => "little",
                _ => throw new ArgumentOutOfRangeException(nameof(byteOrder), byteOrder, null)
            };

            return $"byteOrder: '{byteOrderString}'";
        }

        private static string Format(double? value) =>
            value?.ToString(NumberFormatInfo.InvariantInfo) ?? "null";

        private static string Format(int? value) =>
            value?.ToString(NumberFormatInfo.InvariantInfo) ?? "null";

        private static string Format(long? value) =>
            value?.ToString(NumberFormatInfo.InvariantInfo) ?? "null";

        private static string Format(string value) =>
            value is null ? "null" : $"'{value}'";

        public sealed class ClassFixture : MongoCollectionFixture<TestClass, BsonDocument>
        {
            protected override IEnumerable<BsonDocument> InitialData =>
            [
                BsonDocument.Parse("{ _id : 0 }"),
                BsonDocument.Parse("{ _id : 1, BinaryProperty : BinData(0, 'ogIAAA==') }"),
                BsonDocument.Parse("{ _id : 2, BinaryProperty : BinData(4, 'hn3uUsMxSE6S0cVkebjmfg=='), StringProperty: '867dee52-c331-484e-92d1-c56479b8e67e' }"),
                BsonDocument.Parse("{ _id : 3, BinaryProperty : BinData(0, 'AAAAAAAA4L8='), DoubleProperty: -0.5 }"), //LittleEndian
                BsonDocument.Parse("{ _id : 4, BinaryProperty : BinData(0, 'ogIAAA=='), IntProperty: 674, LongProperty: NumberLong('674') }"), //LittleEndian
                BsonDocument.Parse("{ _id : 5, BinaryProperty : BinData(0, 'wAQAAAAAAAA='), DoubleProperty: -2.5 }"), //BigEndian
                BsonDocument.Parse("{ _id : 6, BinaryProperty : BinData(0, 'AAAAKg=='), IntProperty: 42, LongProperty: NumberLong('42') }"), //BigEndian
                BsonDocument.Parse("{ _id: 10, DoubleProperty: NumberDecimal('-32768'), IntProperty: NumberDecimal('-32768'), LongProperty: NumberDecimal('-32768'), StringProperty: NumberDecimal('-233') }")
            ];
        }

        public class TestClass
        {
            public int Id { get; set; }
            public BsonBinaryData BinaryProperty { get; set; }
            public double DoubleProperty { get; set; }
            public int IntProperty { get; set; }
            public long LongProperty { get; set; }
            public double? NullableDoubleProperty { get; set; }
            public int? NullableIntProperty { get; set; }
            public long? NullableLongProperty { get; set; }
            public string StringProperty { get; set; }
        }
    }
}