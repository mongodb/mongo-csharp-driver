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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
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
        [InlineData(4, ByteOrder.LittleEndian,"ogIAAA==", null)]
        [InlineData(6, ByteOrder.BigEndian, "AAAAKg==", null )]
        [InlineData(10, ByteOrder.BigEndian, null, "MongoCommandException")]
        public void Convert_to_BsonBinaryData_from_int_should_work(int id, ByteOrder byteOrder, string expectedBase64, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.IntProperty, new ConvertOptions<BsonBinaryData> { ByteOrder = byteOrder, SubType = BsonBinarySubType.Binary }));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$IntProperty', to : {{ type: 'binData', subtype: 0  }}, {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            var expectedResult = expectedBase64 == null ? default : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(4, ByteOrder.LittleEndian,"ogIAAA==", null)]
        [InlineData(6, ByteOrder.BigEndian, "AAAAKg==", null )]
        [InlineData(10, ByteOrder.BigEndian, null, "MongoCommandException")]
        public void Convert_to_BsonBinaryData_from_long_should_work(int id, ByteOrder byteOrder, string expectedBase64, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.LongProperty, new ConvertOptions<BsonBinaryData> { ByteOrder = byteOrder, SubType = BsonBinarySubType.Binary }));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$LongProperty', to : {{ type: 'binData', subtype: 0  }}, {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            BsonBinaryData expectedResult = null;

            if (expectedBase64 is not null)
            {
                //$convert to BinData returns always 8 bytes when from long
                var expectedBytes = new byte[8];
                Array.Copy(Convert.FromBase64String(expectedBase64), 0, expectedBytes, byteOrder is ByteOrder.LittleEndian ? 0 : 4, 4);
                expectedResult = new BsonBinaryData(expectedBytes);
            }

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(3, ByteOrder.LittleEndian,"AAAAAAAA4L8=", null)]
        [InlineData(5, ByteOrder.BigEndian, "wAQAAAAAAAA=", null )]
        [InlineData(10, ByteOrder.BigEndian, null, "MongoCommandException")]
        public void Convert_to_BsonBinaryData_from_double_should_work(int id, ByteOrder byteOrder, string expectedBase64, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.DoubleProperty, new ConvertOptions<BsonBinaryData> { ByteOrder = byteOrder, SubType = BsonBinarySubType.Binary }));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$DoubleProperty', to : {{ type: 'binData', subtype: 0  }}, {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            var expectedResult = expectedBase64 == null ? default : new BsonBinaryData(Convert.FromBase64String(expectedBase64));
            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(2, ByteOrder.LittleEndian, 0, "MongoCommandException")]
        [InlineData(4, ByteOrder.LittleEndian, 674, null)]
        [InlineData(6, ByteOrder.BigEndian, 42, null)]
        public void Convert_to_int_from_BsonBinaryData_should_work(int id, ByteOrder byteOrder, int expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.BinaryProperty, new ConvertOptions<int> { ByteOrder = byteOrder }));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'int',  {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(2, ByteOrder.LittleEndian, 0, "MongoCommandException")]
        [InlineData(4, ByteOrder.LittleEndian, (long)674, null)]
        [InlineData(6, ByteOrder.BigEndian, (long)42, null)]
        public void Convert_to_long_from_BsonBinaryData_should_work(int id, ByteOrder byteOrder, long expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.BinaryProperty, new ConvertOptions<long> { ByteOrder = byteOrder }));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'long', {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(2, ByteOrder.BigEndian, 0, "MongoCommandException")]
        [InlineData(3, ByteOrder.LittleEndian, -0.5, null)]
        [InlineData(5, ByteOrder.BigEndian, -2.5, null)]
        public void Convert_to_double_from_BsonBinaryData_should_work(int id, ByteOrder byteOrder, double expectedResult, string expectedException)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.BinaryProperty, new ConvertOptions<double> { ByteOrder = byteOrder }));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'double', {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(25)]
        public void Convert_with_constant_OnError_should_work(int? onError)
        {
            const int id = 20;
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.StringProperty, new ConvertOptions<int?> { OnError = onError }));

            var onErrorVal = onError == null ? "null" : onError.ToString();
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$StringProperty', to : 'int', onError: {onErrorVal} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, onError);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(25)]
        public void Convert_with_constant_OnNull_should_work(int? onNull)
        {
            const int id = 0;
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.StringProperty, new ConvertOptions<int?> { OnNull = onNull }));

            var onNullVal = onNull == null ? "null" : onNull.ToString();
            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$StringProperty', to : 'int', onNull: {onNullVal} }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, onNull);
        }

        [Fact]
        public void Convert_with_field_OnError_should_work()
        {
            const int id = 20;
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.StringProperty, new ConvertOptions<int> { OnError = x.IntProperty }));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$StringProperty', to : 'int', onError: '$IntProperty' }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, 22);
        }

        [Fact]
        public void Convert_with_field_OnNull_should_work()
        {
            const int id = 22;
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.StringProperty, new ConvertOptions<int> { OnNull = x.IntProperty }));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$StringProperty', to : 'int', onNull: '$IntProperty' }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, 33);
        }

        [Theory]
        [InlineData("uuid", "867dee52-c331-484e-92d1-c56479b8e67e")]
        [InlineData("base64", "hn3uUsMxSE6S0cVkebjmfg==")]
        public void Convert_with_format_should_be_rendered_correctly(string format, string expectedResult)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromString);

            const int id = 2;
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.BinaryProperty, new ConvertOptions<string> { Format = format }));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$BinaryProperty', to : 'string', format: '{format}' }} }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(3, ByteOrder.LittleEndian,"AAAAAAAA4L8=")]
        [InlineData(5, ByteOrder.BigEndian, "wAQAAAAAAAA=" )]
        public void Convert_with_byteOrder_should_be_rendered_correctly(int id, ByteOrder byteOrder, string expectedBase64)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.DoubleProperty, new ConvertOptions<BsonBinaryData> { ByteOrder = byteOrder, SubType = BsonBinarySubType.Binary }));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$DoubleProperty', to : {{ type: 'binData', subtype: 0  }}, {ByteOrderToString(byteOrder)} }} }}, _id : 0 }} }}",
                };

            var expectedResult = new BsonBinaryData(Convert.FromBase64String(expectedBase64));
            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Theory]
        [InlineData(BsonBinarySubType.Binary, 0)]
        [InlineData(BsonBinarySubType.Sensitive, 8)]
        [InlineData(BsonBinarySubType.UserDefined, 128)]
        public void Convert_with_subtype_should_be_rendered_correctly(BsonBinarySubType subType, int expectedSubTypeValue)
        {
            RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);

            const int id = 3;
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.DoubleProperty, new ConvertOptions<BsonBinaryData> { ByteOrder = ByteOrder.LittleEndian, SubType = subType }));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $convert : {{ input : '$DoubleProperty', to : {{ type: 'binData', subtype: {expectedSubTypeValue}  }}, 'byteOrder': 'little' }} }}, _id : 0 }} }}",
                };

            var expectedResult = new BsonBinaryData(Convert.FromBase64String("AAAAAAAA4L8="), subType);
            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        [Fact]
        public void Convert_with_null_options_should_be_reduced_if_possible()
        {
            const int id = 21;
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert<string, int>(x.StringProperty, null));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $toInt : '$StringProperty' }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, 15);
        }

        [Fact]
        public void Convert_with_empty_options_should_be_reduced_if_possible()
        {
            const int id = 21;
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert(x.StringProperty, new ConvertOptions<int>()));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $toInt : '$StringProperty' }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, 15);
        }

        [Fact]
        public void Convert_with_constant_should_work()
        {
            const int id = 21;
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert("123", new ConvertOptions<int>()));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $toInt : '123' }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, 123);
        }

        [Theory]
        [InlineData(0, null)]
        [InlineData(21, 15)]
        public void Convert_to_nullable_value_type_should_work(int id, int? expectedResult)
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.Convert<string, int?>(x.StringProperty, null));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    $"{{ $project: {{ _v : {{ $toInt : '$StringProperty' }}, _id : 0 }} }}",
                };

            AssertOutcome(collection, queryable, expectedStages, expectedResult);
        }

        public static IEnumerable<object[]> ConvertTestData => new List<object[]>
        {
            // To int
            new object[] { 22, (Expression<Func<TestClass, object>>)(x => Mql.Convert<int, int>(x.IntProperty, null)),
                "{ $project: { _v : { $toInt : '$IntProperty' }, _id : 0 } }",
                33 },

            // To long
            new object[] { 22, (Expression<Func<TestClass, object>>)(x => Mql.Convert<int, long>(x.IntProperty, null)),
                "{ $project: { _v : { $toLong : '$IntProperty' }, _id : 0 } }",
                (long)33 },

            // To bool
            new object[] { 22, (Expression<Func<TestClass, object>>)(x => Mql.Convert<int, bool>(x.IntProperty, null)),
                "{ $project: { _v : { $toBool : '$IntProperty' }, _id : 0 } }",
                true },

            // To decimal
            new object[] { 22, (Expression<Func<TestClass, object>>)(x => Mql.Convert<int, decimal>(x.IntProperty, null)),
                "{ $project: { _v : { $toDecimal : '$IntProperty' }, _id : 0 } }",
                33m },

            // To decimal128
            new object[] { 22, (Expression<Func<TestClass, object>>)(x => Mql.Convert<int, Decimal128>(x.IntProperty, null)),
                "{ $project: { _v : { $toDecimal : '$IntProperty' }, _id : 0 } }",
                new Decimal128(33) },

            // To float
            new object[] { 22, (Expression<Func<TestClass, object>>)(x => Mql.Convert<int, float>(x.IntProperty, null)),
                "{ $project: { _v : { $toDouble : '$IntProperty' }, _id : 0 } }",
                (float)33.0 },

            // To double
            new object[] { 22, (Expression<Func<TestClass, object>>)(x => Mql.Convert<int, double>(x.IntProperty, null)),
                "{ $project: { _v : { $toDouble : '$IntProperty' }, _id : 0 } }",
                33.0 },

            // To objectId
            new object[] { 24, (Expression<Func<TestClass, object>>)(x => Mql.Convert<string, ObjectId>(x.StringProperty, null)),
                "{ $project: { _v : { $toObjectId : '$StringProperty' }, _id : 0 } }",
                ObjectId.Parse("5ab9cbfa31c2ab715d42129e") },

            // To date
            new object[] { 23, (Expression<Func<TestClass, object>>)(x => Mql.Convert<string, DateTime>(x.StringProperty, null)),
                "{ $project: { _v : { $toDate : '$StringProperty' }, _id : 0 } }",
                new DateTime(2018, 03, 03) },

            // To string
            new object[] { 22, (Expression<Func<TestClass, object>>)(x => Mql.Convert<int, string>(x.IntProperty, null)),
                "{ $project: { _v : { $toString : '$IntProperty' }, _id : 0 } }",
                "33" },

            // To BsonBinaryData
            new object[] { 4, (Expression<Func<TestClass, object>>)(x => Mql.Convert<int, BsonBinaryData>(x.IntProperty, new ConvertOptions<BsonBinaryData> { SubType = BsonBinarySubType.Binary, ByteOrder = ByteOrder.LittleEndian })),
                "{ $project: { _v : { $convert : { input : '$IntProperty', to : { type: 'binData', subtype: 0  }, byteOrder : 'little' } }, _id : 0 } }",
                new BsonBinaryData(Convert.FromBase64String("ogIAAA==")) },

            // To byte[]
            new object[] { 4, (Expression<Func<TestClass, object>>)(x => Mql.Convert<int, byte[]>(x.IntProperty, new ConvertOptions<byte[]> { SubType = BsonBinarySubType.Binary, ByteOrder = ByteOrder.LittleEndian })),
                "{ $project: { _v : { $convert : { input : '$IntProperty', to : { type: 'binData', subtype: 0  }, byteOrder : 'little' } }, _id : 0 } }",
                Convert.FromBase64String("ogIAAA==") },
        };

        [Theory]
        [MemberData(nameof(ConvertTestData))]
        public void Convert_should_work(
            int id,
            Expression<Func<TestClass, object>> projection,
            string expectedStage,
            object expectedValue)
        {
            if (expectedStage.Contains("byteOrder"))
            {
                RequireServer.Check().Supports(Feature.ConvertOperatorBinDataToFromNumeric);
            }

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(projection);

            var expectedStages = new[] { $"{{ $match : {{ _id : {id} }} }}", expectedStage };

            AssertOutcome(collection, queryable, expectedStages, expectedValue);
        }

        [Fact]
        public void Convert_should_throw_when_using_unrecognized_to_type()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == 20)
                .Select(x => Mql.Convert<string, TestClass>("123", new ConvertOptions<TestClass>()));

            var exception = Record.Exception(() => Translate(collection, queryable));

            Assert.NotNull(exception);
            Assert.IsType<ExpressionNotSupportedException>(exception);
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

        public sealed class ClassFixture : MongoCollectionFixture<TestClass, BsonDocument>
        {
            protected override IEnumerable<BsonDocument> InitialData =>
            [
                BsonDocument.Parse("{ _id : 0 }"),
                BsonDocument.Parse("{ _id : 1, BinaryProperty : BinData(0, 'ogIAAA==') }"),
                BsonDocument.Parse("{ _id : 2, BinaryProperty : BinData(4, 'hn3uUsMxSE6S0cVkebjmfg=='), StringProperty: '867dee52-c331-484e-92d1-c56479b8e67e' }"),
                BsonDocument.Parse("{ _id : 3, BinaryProperty : BinData(0, 'AAAAAAAA4L8='), DoubleProperty: -0.5, NullableDoubleProperty: -0.5 }"), // LittleEndian
                BsonDocument.Parse("{ _id : 4, BinaryProperty : BinData(0, 'ogIAAA=='), IntProperty: 674, LongProperty: NumberLong('674'), NullableIntProperty: 674, NullableLongProperty: NumberLong('674') }"), // LittleEndian
                BsonDocument.Parse("{ _id : 5, BinaryProperty : BinData(0, 'wAQAAAAAAAA='), DoubleProperty: -2.5, NullableDoubleProperty: -2.5 }"), // BigEndian
                BsonDocument.Parse("{ _id : 6, BinaryProperty : BinData(0, 'AAAAKg=='), IntProperty: 42, LongProperty: NumberLong('42'), NullableIntProperty: 42, NullableLongProperty: NumberLong('42') }"), // BigEndian
                BsonDocument.Parse("{ _id: 10, DoubleProperty: NumberDecimal('-32768'), IntProperty: NumberDecimal('-32768'), LongProperty: NumberDecimal('-32768'), " +
                                   "NullableDoubleProperty: NumberDecimal('-32768'), NullableIntProperty: NumberDecimal('-32768'), NullableLongProperty: NumberDecimal('-32768'), StringProperty: NumberDecimal('-233') }"),  // Invalid conversions
                BsonDocument.Parse("{ _id : 20, StringProperty: 'inValidInt', IntProperty: 22 }"),
                BsonDocument.Parse("{ _id : 21, StringProperty: '15' }"),
                BsonDocument.Parse("{ _id : 22, IntProperty: 33 }"),
                BsonDocument.Parse("{ _id : 23, StringProperty: '2018-03-03' }"),
                BsonDocument.Parse("{ _id : 24, StringProperty: '5ab9cbfa31c2ab715d42129e' }"),
            ];
        }

        public class TestClass
        {
            public int Id { get; set; }
            public BsonBinaryData BinaryProperty { get; set; }
            public double DoubleProperty { get; set; }
            public int IntProperty { get; set; }
            public long LongProperty { get; set; }
            public string StringProperty { get; set; }
        }
    }
}
