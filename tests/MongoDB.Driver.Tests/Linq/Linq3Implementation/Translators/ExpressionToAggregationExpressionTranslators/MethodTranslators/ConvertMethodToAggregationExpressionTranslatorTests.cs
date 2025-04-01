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


        [Theory]
        [InlineData(3, ByteOrder.LittleEndian,"AAAAAAAA4L8=", null)]
        [InlineData(5, ByteOrder.BigEndian, "wAQAAAAAAAA=", null )]
        [InlineData(10, ByteOrder.BigEndian, null, "MongoCommandException")]
        public void MongoDBFunctions_ToBsonBinaryDataFromDouble_should_work(int id, ByteOrder byteOrder, string expectedBase64, string expectedException)
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
            AssertOutcome(collection, queryable, expectedStages, expectedResult, expectedException);
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

        private static string FormatBase64(string base64String) =>
            base64String is null ? "null" :$"BinData(0, '{base64String}')";

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
                                   "NullableDoubleProperty: NumberDecimal('-32768'), NullableIntProperty: NumberDecimal('-32768'), NullableLongProperty: NumberDecimal('-32768'), StringProperty: NumberDecimal('-233') }")  // Invalid conversions
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