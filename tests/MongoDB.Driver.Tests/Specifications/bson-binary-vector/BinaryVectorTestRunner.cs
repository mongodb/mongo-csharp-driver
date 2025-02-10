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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.bson_corpus
{
    public class BinaryVectorTestRunner
    {
        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            var shared = testCase.Shared;
            var test = testCase.Test;

            JsonDrivenHelper.EnsureAllFieldsAreValid(
                shared,
                "_path",
                "test_key",
                "description",
                "tests");

            JsonDrivenHelper.EnsureAllFieldsAreValid(
                test,
                "description",
                "valid",
                "vector",
                "dtype_hex",
                "dtype_alias",
                "padding",
                "canonical_bson");

            var isValidTest = test["valid"].AsBoolean;
            if (isValidTest)
            {
                RunValidTest(test, shared["test_key"].AsString);
            }
            else
            {
                RunInvalidTest(test, shared["test_key"].AsString);
            }
        }

        private void RunValidTest(BsonDocument test, string testKey)
        {
            var vector = test["vector"].AsBsonArray;
            var dataType = ReadVectorDataType(test);
            var padding = (byte)test["padding"].AsInt32;
            var canonicalBson = test["canonical_bson"].AsString;

            AssertEncoding(testKey, vector, padding, dataType, canonicalBson);
            AssertDecoding(testKey, vector, padding, dataType, canonicalBson);
        }

        private void RunInvalidTest(BsonDocument test, string testKey)
        {
            var dataType = ReadVectorDataType(test);

            if (test.TryGetValue("vector", out var vectorElements))
            {
                var padding = (byte)test["padding"].AsInt32;
                var exception = Record.Exception(() => AssertEncoding(testKey, vectorElements?.AsBsonArray, padding, dataType, default));
                exception.Should().BeAssignableTo<ArgumentException>();
                exception.Source.Should().Be("MongoDB.Bson");
            }

            if (test.TryGetValue("canonical_bson", out var canonicalBson))
            {
                var exception = Record.Exception(() => AssertDecoding(testKey, default, default, dataType, canonicalBson.AsString));
                exception.Should().BeAssignableTo<FormatException>();
                exception.Source.Should().Be("MongoDB.Bson");
            }
        }

        private void AssertEncoding(string testKey, BsonArray vector, byte padding, BinaryVectorDataType dataType, string canonicalBson)
        {
            BsonBinaryData vectorBinaryData;

            switch (dataType)
            {
                case BinaryVectorDataType.Float32:
                    {
                        var values = ToFloats(vector);
                        var vectorFloat32 = new BinaryVectorFloat32(values);

                        vectorBinaryData = vectorFloat32.ToBsonBinaryData();
                        break;
                    }
                case BinaryVectorDataType.Int8:
                    {
                        var values = vector.Select(v => (sbyte)v).ToArray();
                        var vectorInt8 = new BinaryVectorInt8(values);

                        vectorBinaryData = vectorInt8.ToBsonBinaryData();
                        break;
                    }
                case BinaryVectorDataType.PackedBit:
                    {
                        var values = vector.Select(v => (byte)v).ToArray();
                        var vectorInt8 = new BinaryVectorPackedBit(values, padding);

                        vectorBinaryData = vectorInt8.ToBsonBinaryData();
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Invalid data type");
            }

            var documentToEncode = new BsonDocument(testKey, vectorBinaryData);
            var encodedDocumentBsonHex = BsonUtils.ToHexString(documentToEncode.ToBson()).ToUpperInvariant();

            encodedDocumentBsonHex.Should().Be(canonicalBson);
        }

        private void AssertDecoding(string testKey, BsonArray vector, byte padding, BinaryVectorDataType dataType, string canonicalBson)
        {
            var canonicalBsonBytes = BsonUtils.ParseHexString(canonicalBson);
            var decodedDocument = BsonSerializer.Deserialize<BsonDocument>(canonicalBsonBytes);
            var vectorBsonData = decodedDocument[testKey].AsBsonBinaryData;

            var (_, actualPaddingActual, actualVectorDataType) = vectorBsonData.ToBinaryVectorAsBytes();

            actualVectorDataType.Should().Be(dataType);
            actualPaddingActual.Should().Be(padding);

            Array expectedArray;
            Array actualArray;

            switch (dataType)
            {
                case BinaryVectorDataType.Float32:
                    {
                        var actualVector = vectorBsonData.ToBinaryVector<float>();
                        actualArray = actualVector.Data.ToArray();
                        expectedArray = ToFloats(vector);

                        break;
                    }
                case BinaryVectorDataType.Int8:
                    {
                        var actualVector = vectorBsonData.ToBinaryVector<sbyte>();
                        actualArray = actualVector.Data.ToArray();
                        expectedArray = vector.Select(v => (sbyte)v).ToArray();
                        break;
                    }
                case BinaryVectorDataType.PackedBit:
                    {
                        var actualVector = vectorBsonData.ToBinaryVector<byte>();
                        actualArray = actualVector.Data.ToArray();
                        expectedArray = vector.Select(v => (byte)v).ToArray();
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Invalid data type");
            }

            actualArray.ShouldBeEquivalentTo(expectedArray);
        }

        private float[] ToFloats(BsonArray bsonArray) =>
            bsonArray.Select(v => v is BsonString bsonString ?
                (bsonString.AsString == "-inf" ? float.NegativeInfinity :
                bsonString.AsString == "inf" ? float.PositiveInfinity : throw new Exception("Unsupported value")) :
                (float)v.AsDouble).ToArray();

        private BinaryVectorDataType ReadVectorDataType(BsonDocument test) =>
            (BinaryVectorDataType)Convert.ToInt32(test["dtype_hex"].AsString, 16);

        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            private static readonly string[] __ignoredTestNames =
            {
                "FLOAT32 with padding", // no applicable API
                "INT8 with padding", // no applicable API
                "INT8 with float inputs", // no applicable API
                "Underflow Vector PACKED_BIT", // no applicable API
                "Vector with float values PACKED_BIT", // no applicable API
                "Overflow Vector PACKED_BIT", // no applicable API
                "Overflow Vector INT8", // no applicable API
                "Underflow Vector INT8"// no applicable API
            };

            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.bson_binary_vector.tests.";

            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                var shared = document;

                if (shared.TryGetElement("tests", out var testSection))
                {
                    foreach (var test in CreateTestCases(shared, testSection))
                    {
                        if (__ignoredTestNames.Any(test.Name.Contains))
                        {
                            continue;
                        }

                        yield return test;
                    }
                }
            }

            private IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument shared, BsonElement testSection) =>
                testSection.Value.AsBsonArray
                .Cast<BsonDocument>()
                .Select((test, i) =>
                    {
                        var name = GetTestCaseName(shared, test, i);
                        return new JsonDrivenTestCase(name, shared, test.DeepClone().AsBsonDocument);
                    });
        }
    }
}
