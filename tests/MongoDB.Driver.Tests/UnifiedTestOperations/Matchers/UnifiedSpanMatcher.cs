/* Copyright 2025-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.TestHelpers.Core;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit.Sdk;

namespace MongoDB.Driver.Tests.UnifiedTestOperations.Matchers
{
    public class UnifiedSpanMatcher
    {
        private readonly UnifiedValueMatcher _valueMatcher;

        public UnifiedSpanMatcher(UnifiedValueMatcher valueMatcher)
        {
            _valueMatcher = valueMatcher;
        }

        public void AssertSpansMatch(List<CapturedSpan> actualSpans, BsonArray expectedSpans, bool ignoreExtraSpans)
        {
            try
            {
                AssertSpans(actualSpans, expectedSpans, ignoreExtraSpans);
            }
            catch (XunitException exception)
            {
                throw new AssertionException(
                    userMessage: GetAssertionErrorMessage(actualSpans, expectedSpans),
                    innerException: exception);
            }
        }

        private void AssertSpans(List<CapturedSpan> actualSpans, BsonArray expectedSpans, bool ignoreExtraSpans)
        {
            if (ignoreExtraSpans)
            {
                actualSpans.Count.Should().BeGreaterOrEqualTo(expectedSpans.Count);

                // When ignoring extra spans, find each expected span in order within the actual spans
                int actualIndex = 0;
                for (int expectedIndex = 0; expectedIndex < expectedSpans.Count; expectedIndex++)
                {
                    var expectedSpan = expectedSpans[expectedIndex].AsBsonDocument;
                    var expectedName = expectedSpan["name"].AsString;

                    // Find the next actual span that matches this expected span's name
                    bool found = false;
                    while (actualIndex < actualSpans.Count)
                    {
                        var actualSpan = actualSpans[actualIndex];
                        if (actualSpan.Name == expectedName)
                        {
                            AssertSpan(actualSpan, expectedSpan);
                            actualIndex++;
                            found = true;
                            break;
                        }
                        actualIndex++;
                    }

                    if (!found)
                    {
                        throw new AssertionException($"Expected span with name '{expectedName}' not found in actual spans starting from index {actualIndex}");
                    }
                }
            }
            else
            {
                actualSpans.Should().HaveSameCount(expectedSpans);

                for (int i = 0; i < expectedSpans.Count; i++)
                {
                    var actualSpan = actualSpans[i];
                    var expectedSpan = expectedSpans[i].AsBsonDocument;

                    AssertSpan(actualSpan, expectedSpan);
                }
            }
        }

        private void AssertSpan(CapturedSpan actualSpan, BsonDocument expectedSpan)
        {
            foreach (var element in expectedSpan)
            {
                switch (element.Name)
                {
                    case "name":
                        actualSpan.Name.Should().Be(element.Value.AsString);
                        break;
                    case "attributes":
                        AssertAttributes(actualSpan.Attributes, element.Value.AsBsonDocument);
                        break;
                    case "nested":
                        AssertNestedSpans(actualSpan.NestedSpans, element.Value.AsBsonArray);
                        break;
                    default:
                        throw new FormatException($"Unexpected span field: '{element.Name}'.");
                }
            }
        }

        private void AssertAttributes(Dictionary<string, object> actualAttributes, BsonDocument expectedAttributes)
        {
            foreach (var expectedAttribute in expectedAttributes)
            {
                var attributeName = expectedAttribute.Name;
                var expectedValue = expectedAttribute.Value;

                // Check if this is a $$exists matcher
                if (expectedValue.IsBsonDocument)
                {
                    var expectedDoc = expectedValue.AsBsonDocument;
                    if (expectedDoc.Contains("$$exists"))
                    {
                        var shouldExist = expectedDoc["$$exists"].AsBoolean;
                        if (shouldExist)
                        {
                            actualAttributes.Should().ContainKey(attributeName,
                                $"span should have attribute '{attributeName}'");
                        }
                        else
                        {
                            actualAttributes.Should().NotContainKey(attributeName,
                                $"span should not have attribute '{attributeName}'");
                        }
                        continue;
                    }
                }

                actualAttributes.Should().ContainKey(attributeName, $"span should have attribute '{attributeName}'");
                var actualValue = actualAttributes[attributeName];

                // Convert the actual value to BsonValue
                var actualBsonValue = ConvertToBsonValue(actualValue);
                _valueMatcher.AssertValuesMatch(actualBsonValue, expectedValue);
            }
        }

        private BsonValue ConvertToBsonValue(object value)
        {
            return value switch
            {
                null => BsonNull.Value,
                BsonValue bv => bv, // Already a BsonValue (including BsonDocument), return as-is
                string s => new BsonString(s),
                int i => new BsonInt32(i),
                long l => new BsonInt64(l),
                double d => new BsonDouble(d),
                bool b => new BsonBoolean(b),
                _ => throw new InvalidOperationException($"Unsupported span attribute type: {value.GetType().Name}")
            };
        }

        private void AssertNestedSpans(List<CapturedSpan> actualNestedSpans, BsonArray expectedNestedSpans)
        {
            actualNestedSpans.Should().HaveSameCount(expectedNestedSpans, "nested spans count should match");

            for (int i = 0; i < expectedNestedSpans.Count; i++)
            {
                AssertSpan(actualNestedSpans[i], expectedNestedSpans[i].AsBsonDocument);
            }
        }

        private string GetAssertionErrorMessage(List<CapturedSpan> actualSpans, BsonArray expectedSpans)
        {
            var jsonWriterSettings = new JsonWriterSettings { Indent = true };

            var actualSpansDocuments = new BsonArray();
            foreach (var actualSpan in actualSpans)
            {
                actualSpansDocuments.Add(ConvertSpanToBsonDocument(actualSpan));
            }

            return
                $"Expected spans to be: {expectedSpans.ToJson(jsonWriterSettings)}{Environment.NewLine}" +
                $"But found: {actualSpansDocuments.ToJson(jsonWriterSettings)}.";
        }

        private BsonDocument ConvertSpanToBsonDocument(CapturedSpan span)
        {
            var spanDocument = new BsonDocument
            {
                { "name", span.Name },
                { "status", span.StatusCode.ToString() }
            };

            if (span.Attributes.Count > 0)
            {
                var attributesDocument = new BsonDocument();
                foreach (var attribute in span.Attributes)
                {
                    attributesDocument[attribute.Key] = ConvertToBsonValue(attribute.Value);
                }
                spanDocument["attributes"] = attributesDocument;
            }

            if (span.NestedSpans.Count > 0)
            {
                var nestedArray = new BsonArray();
                foreach (var nestedSpan in span.NestedSpans)
                {
                    nestedArray.Add(ConvertSpanToBsonDocument(nestedSpan));
                }
                spanDocument["nested"] = nestedArray;
            }

            return spanDocument;
        }
    }
}
