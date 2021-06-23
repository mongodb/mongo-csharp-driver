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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;

namespace MongoDB.Driver.Tests.UnifiedTestOperations.Matchers
{
    public class UnifiedErrorMatcher
    {
        public void AssertErrorsMatch(Exception actualException, BsonDocument expectedError)
        {
            if (expectedError.ElementCount == 0)
            {
                throw new FormatException("Expected error document should contain at least one element.");
            }

            foreach (var element in expectedError)
            {
                switch (element.Name)
                {
                    case "isError":
                        AssertIsError(actualException, element.Value.AsBoolean);
                        break;
                    case "isClientError":
                        AssertIsClientError(actualException, element.Value.AsBoolean);
                        break;
                    case "errorContains":
                        AssertErrorContains(actualException, element.Value.AsString);
                        break;
                    case "errorCode":
                        AssertErrorCode(actualException, element.Value.AsInt32);
                        break;
                    case "errorCodeName":
                        AssertErrorCodeName(actualException, element.Value.AsString);
                        break;
                    case "errorLabelsContain":
                        AssertErrorLabelsContain(actualException, element.Value.AsBsonArray.Select(x => x.AsString));
                        break;
                    case "errorLabelsOmit":
                        AssertErrorLabelsOmit(actualException, element.Value.AsBsonArray.Select(x => x.AsString));
                        break;
                    case "expectResult":
                        AssertExpectResult(actualException, element.Value.AsBsonDocument);
                        break;
                    default:
                        throw new FormatException($"Unrecognized error assertion: '{element.Name}'.");
                }
            }
        }

        private void AssertErrorCode(Exception actualException, int expectedErrorCode)
        {
            var mongoCommandException = actualException.Should().BeAssignableTo<MongoCommandException>().Subject;
            mongoCommandException.Code.Should().Be(expectedErrorCode);
        }

        private void AssertErrorCodeName(Exception actualException, string expectedErrorCodeName)
        {
            var mongoCommandException = actualException.Should().BeAssignableTo<MongoCommandException>().Subject;
            mongoCommandException.CodeName.Should().Be(expectedErrorCodeName);
        }

        private void AssertErrorContains(Exception actualException, string expectedSubstring)
        {
            actualException.Message.Should().ContainEquivalentOf(expectedSubstring);
        }

        private void AssertErrorLabelsContain(Exception actualException, IEnumerable<string> expectedErrorLabels)
        {
            var mongoException = actualException.Should().BeAssignableTo<MongoException>().Subject;
            mongoException.ErrorLabels.Should().Contain(expectedErrorLabels);
        }

        private void AssertErrorLabelsOmit(Exception actualException, IEnumerable<string> expectedAbsentErrorLabels)
        {
            var mongoException = actualException.Should().BeAssignableTo<MongoException>().Subject;
            mongoException.ErrorLabels.Should().NotContain(expectedAbsentErrorLabels);
        }

        private void AssertExpectResult(Exception actualException, BsonDocument expectedResult)
        {
            var bulkWriteException = actualException.Should().BeOfType<MongoBulkWriteException<BsonDocument>>().Subject;
            var bulkWriteResult = bulkWriteException.Result;
            var actualResult = new BsonDocument
            {
                { "deletedCount", bulkWriteResult.DeletedCount },
                { "insertedCount", bulkWriteResult.InsertedCount },
                { "matchedCount", bulkWriteResult.MatchedCount },
                { "modifiedCount", bulkWriteResult.ModifiedCount },
                { "upsertedCount", bulkWriteResult.Upserts.Count },
                { "upsertedIds", new BsonDocument(bulkWriteResult.Upserts.Select(x => new BsonElement(x.Index.ToString(), x.Id))) }
            };

            actualResult.Should().Be(expectedResult);
        }

        private void AssertIsClientError(Exception actualException, bool expectedIsClientError)
        {
            var actualIsClientError = actualException is
                MongoClientException or
                BsonException or
                MongoConnectionException or
                TimeoutException;

            if (actualIsClientError != expectedIsClientError)
            {
                var message = $"Expected exception to {(expectedIsClientError ? "" : "not ")}be a client exception, but found {actualException}.";
                throw new AssertionException(message);
            }
        }

        private void AssertIsError(Exception actualException, bool expectedIsError)
        {
            if (expectedIsError == false)
            {
                throw new FormatException("Test files MUST NOT specify false.");
            }

            actualException.Should().NotBeNull();
        }
    }
}
