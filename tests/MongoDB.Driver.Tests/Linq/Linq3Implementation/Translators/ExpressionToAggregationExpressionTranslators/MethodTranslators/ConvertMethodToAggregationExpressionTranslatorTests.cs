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

        [Fact]
        public void Test1()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x =>
                    Mql.ConvertToBinData(x.StringProperty, BsonBinarySubType.Binary, Mql.ConvertBinDataFormat.base64));

            var expectedStages =
                new[]
                {
                    "{ $project : { _v : { $dateFromString : { dateString : '$S' } }, _id : 0 } }"
                };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void MongoDBFunctions_ConvertToStringFromBson_should_work()
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var id = 1;

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id)
                .Select(x => Mql.ConvertToString(x.BinaryProperty, Mql.ConvertBinDataFormat.uuid));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    """{"$project": { "_v" : { "$convert" : { "input" : "$BinaryProperty", "to" : 2, "format" : "uuid" } }, "_id" : 0 }}""",
                };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStages);

            var expectedResult = "867dee52-c331-484e-92d1-c56479b8e67e";

            var result = queryable.Single();
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void MongoDBFunctions_ConvertToStringFromBsonWithOnErrorAndOnNull_should_work()
        {
            RequireServer.Check().Supports(Feature.ConvertBinDataToFromNumeric);

            var id = 0;

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(x => x.Id == id);
                //.Select(x => Mql.ConvertToString(x.BinaryProperty, Mql.ConvertBinDataFormat.hex, "onError", "onNull"));

            var expectedStages =
                new[]
                {
                    $"{{ $match : {{ _id : {id} }} }}",
                    //"""{"$project": { "_v" : { "$convert" : { "input" : "$BinaryProperty", "to" : 2, "onError": "onError", "onNull": "onNull", "format" : "hex" } }, "_id" : 0 }}""",
                };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStages);

            var expectedResult = "867dee52-c331-484e-92d1-c56479b8e67e";

            var result = queryable.Single();
            Assert.Equal(expectedResult, result.StringProperty);
        }

        /**
         *
         * What to test
         *
         */


        public sealed class ClassFixture : MongoCollectionFixture<TestClass>
        {
            protected override IEnumerable<TestClass> InitialData { get; } =
            [
                new TestClass {Id = 0 },
                new TestClass {Id = 1, BinaryProperty = new BsonBinaryData(Guid.Parse("867dee52-c331-484e-92d1-c56479b8e67e"), GuidRepresentation.Standard)},
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