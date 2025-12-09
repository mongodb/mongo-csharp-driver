/* Copyright 2020-present MongoDB Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit.Sdk;

namespace MongoDB.Driver.Tests.UnifiedTestOperations.Matchers
{
    public class UnifiedValueMatcher
    {
        private static readonly List<string> __numericTypes = ["int", "long", "double", "decimal"];

        private UnifiedEntityMap _entityMap;

        public UnifiedValueMatcher(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public void AssertValuesMatch(BsonValue actual, BsonValue expected)
        {
            try
            {
                AssertValuesMatch(actual, expected, isRoot: true, isRecursiveCall: false);
            }
            catch (XunitException exception)
            {
                var jsonWriterSettings = new JsonWriterSettings { Indent = true };
                var message =
                    $"Expected value to be: {expected?.ToJson(jsonWriterSettings)}{Environment.NewLine}" +
                    $"But found: {actual?.ToJson(jsonWriterSettings)}.";
                throw new AssertionException(message, exception);
            }
        }

        private void AssertValuesMatch(BsonValue actual, BsonValue expected, bool isRoot, bool isRecursiveCall = true)
        {
            if (expected.IsBsonDocument &&
                expected.AsBsonDocument.ElementCount == 1 &&
                expected.AsBsonDocument.GetElement(0).Name.StartsWith("$$"))
            {
                var specialOperatorDocument = expected.AsBsonDocument;
                var operatorName = specialOperatorDocument.GetElement(0).Name;
                var operatorValue = specialOperatorDocument[0];

                switch (operatorName)
                {
                    case "$$exists":
                        actual.Should().NotBeNull();
                        break;
                    case "$$type":
                        AssertExpectedType(actual, operatorValue);
                        break;
                    case "$$matchesHexBytes":
                    case "$$matchAsRoot":
                        AssertValuesMatch(actual, operatorValue, true);
                        break;
                    case "$$matchAsDocument":
                        var parsedDocument = BsonDocument.Parse(actual.AsString);
                        AssertValuesMatch(parsedDocument, operatorValue, false);
                        break;
                    case "$$unsetOrMatches":
                        if (actual != null)
                        {
                            AssertValuesMatch(actual, operatorValue, true);
                        }
                        break;
                    case "$$sessionLsid":
                        var sessionId = operatorValue.AsString;
                        var expectedSessionLsid = _entityMap.SessionIds[sessionId];
                        AssertValuesMatch(actual, expectedSessionLsid, isRoot: false);
                        break;
                    default:
                        throw new FormatException($"Unrecognized root level special operator: '{operatorName}'.");
                }

                return;
            }

            if (expected.IsBsonDocument)
            {
                actual.BsonType.Should().Be(BsonType.Document);

                var expectedDocument = expected.AsBsonDocument;
                var actualDocument = actual.AsBsonDocument;

                foreach (var expectedElement in expectedDocument)
                {
                    var expectedName = expectedElement.Name;
                    var expectedValue = expectedElement.Value;
                    var matchCurrentElementAsRoot = false;

                    if (expectedValue.IsBsonDocument &&
                        expectedValue.AsBsonDocument.ElementCount == 1 &&
                        expectedValue.AsBsonDocument.GetElement(0).Name.StartsWith("$$"))
                    {
                        var specialOperatorDocument = expectedValue.AsBsonDocument;
                        var operatorName = specialOperatorDocument.GetElement(0).Name;
                        var operatorValue = specialOperatorDocument[0];

                        switch (operatorName)
                        {
                            case "$$exists":
                                if (operatorValue.AsBoolean)
                                {
                                    actualDocument.Names.Should().Contain(expectedName);
                                }
                                else
                                {
                                    actualDocument.Names.Should().NotContain(expectedName);
                                }
                                continue;
                            case "$$type":
                                actualDocument.Names.Should().Contain(expectedName);
                                AssertExpectedType(actualDocument[expectedName], operatorValue);
                                continue;
                            case "$$lte":
                                var actualElement = actualDocument[expectedName];
                                actualElement.IsNumeric.Should().BeTrue();
                                actualElement.ToDouble().Should().BeLessOrEqualTo(operatorValue.ToDouble());
                                continue;
                            case "$$matchAsDocument":
                                var parsedDocument = BsonDocument.Parse(actualDocument[expectedName].AsString);
                                AssertValuesMatch(parsedDocument, operatorValue, false);
                                continue;
                            case "$$matchAsRoot":
                                matchCurrentElementAsRoot = true;
                                break;
                            case "$$matchesEntity":
                                var resultId = operatorValue.AsString;
                                expectedValue = _entityMap.Results[resultId];
                                break;
                            case "$$matchesHexBytes":
                                expectedValue = operatorValue;
                                break;
                            case "$$unsetOrMatches":
                                if (!actualDocument.Contains(expectedName))
                                {
                                    continue;
                                }
                                expectedValue = operatorValue;
                                break;
                            case "$$sessionLsid":
                                var sessionId = operatorValue.AsString;
                                expectedValue = _entityMap.SessionIds[sessionId];
                                break;
                            default:
                                throw new FormatException($"Unrecognized special operator: '{operatorName}'.");
                        }
                    }

                    actualDocument.Names.Should().Contain(expectedName);
                    AssertValuesMatch(actualDocument[expectedName], expectedValue, isRoot: matchCurrentElementAsRoot);
                }

                if (!isRoot)
                {
                    actualDocument.Names.Should().BeSubsetOf(expectedDocument.Names);
                }
            }
            else if (expected.IsBsonArray)
            {
                actual.BsonType.Should().Be(BsonType.Array);
                actual.AsBsonArray.Values.Should().HaveSameCount(expected.AsBsonArray.Values);

                var expectedArray = expected.AsBsonArray;
                var actualArray = actual.AsBsonArray;

                for (int i = 0; i < expectedArray.Count; i++)
                {
                    AssertValuesMatch(actualArray[i], expectedArray[i], isRoot: isRoot && !isRecursiveCall);
                }
            }
            else if (expected.IsNumeric)
            {
                actual.IsNumeric.Should().BeTrue();
                actual.ToDouble().Should().Be(expected.ToDouble());
            }
            else
            {
                (actual ?? BsonNull.Value).BsonType.Should().Be(expected.BsonType);
                (actual ?? BsonNull.Value).Should().Be(expected ?? BsonNull.Value);
            }
        }

        private void AssertExpectedType(BsonValue actual, BsonValue expectedTypes)
        {
            var actualTypeName = GetBsonTypeNameAsString(actual.BsonType);
            List<string> expectedTypeNames;

            if (expectedTypes.IsString)
            {
                var expectedType = expectedTypes.AsString;
                expectedTypeNames = expectedType == "number" ? __numericTypes : [expectedType];
            }
            else if (expectedTypes.IsBsonArray)
            {
                expectedTypeNames = expectedTypes.AsBsonArray.Select(t => t.AsString).ToList();
            }
            else
            {
                throw new FormatException($"Unexpected $$type value BsonType: '{expectedTypes.BsonType}'.");
            }

            actualTypeName.Should().BeOneOf(expectedTypeNames);
        }

        private string GetBsonTypeNameAsString(BsonType bsonType)
        {
            switch (bsonType)
            {
                case BsonType.Double:
                    return "double";
                case BsonType.String:
                    return "string";
                case BsonType.Document:
                    return "object";
                case BsonType.Array:
                    return "array";
                case BsonType.Binary:
                    return "binData";
                case BsonType.ObjectId:
                    return "objectId";
                case BsonType.Boolean:
                    return "bool";
                case BsonType.DateTime:
                    return "date";
                case BsonType.Null:
                    return "null";
                case BsonType.RegularExpression:
                    return "regex";
                case BsonType.Int32:
                    return "int";
                case BsonType.Timestamp:
                    return "timestamp";
                case BsonType.Int64:
                    return "long";
                case BsonType.Decimal128:
                    return "decimal";
                default:
                    throw new FormatException($"Unrecognized bson type string conversion: '{bsonType}'.");
            }
        }
    }
}
