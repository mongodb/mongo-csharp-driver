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
using MongoDB.TestHelpers.XunitExtensions;

namespace MongoDB.Driver.Tests.UnifiedTestOperations.Matchers
{
    public class UnifiedErrorMatcher
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedErrorMatcher(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

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
                    case "errorResponse":
                        AssertErrorResponse(actualException, element.Value.AsBsonDocument);
                        break;
                    case "expectResult":
                        AssertExpectResult(actualException, element.Value.AsBsonDocument);
                        break;
                    case "writeConcernErrors":
                        AssertWriteConcernErrors(actualException, element.Value.AsBsonArray);
                        break;
                    case "writeErrors":
                        AssertWriteErrors(actualException, element.Value.AsBsonDocument);
                        break;
                    default:
                        throw new FormatException($"Unrecognized error assertion: '{element.Name}'.");
                }
            }
        }

        private void AssertErrorResponse(Exception actualException, BsonDocument expected)
        {
            actualException = UnwrapCommandException(actualException);
            var mongoCommandException = actualException.Should().BeAssignableTo<MongoCommandException>().Subject;
            var valueMatcher = new UnifiedValueMatcher(_entityMap);
            valueMatcher.AssertValuesMatch(mongoCommandException.Result, expected);
        }

        private void AssertErrorCode(Exception actualException, int expectedErrorCode)
        {
            actualException = UnwrapCommandException(actualException);
            var mongoCommandException = actualException.Should().BeAssignableTo<MongoCommandException>().Subject;
            mongoCommandException.Code.Should().Be(expectedErrorCode);
        }

        private void AssertErrorCodeName(Exception actualException, string expectedErrorCodeName)
        {
            actualException = UnwrapCommandException(actualException);
            var mongoCommandException = actualException.Should().BeAssignableTo<MongoCommandException>().Subject;
            mongoCommandException.CodeName.Should().Be(expectedErrorCodeName);
        }

        private void AssertErrorContains(Exception actualException, string expectedSubstring)
        {
            actualException
                .Should()
                .Match<Exception>(e =>
                    e.Message.Contains(expectedSubstring) ||
                    (e.InnerException != null && e.InnerException.Message.Contains(expectedSubstring)));
        }

        private void AssertErrorLabelsContain(Exception actualException, IEnumerable<string> expectedErrorLabels)
        {
            if (actualException is ClientBulkWriteException bulkWriteException)
            {
                actualException = bulkWriteException.InnerException;
            }

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
            BsonDocument actualResult;

            switch (actualException)
            {
                case MongoBulkWriteException<BsonDocument> bulkWriteException:
                    actualResult =  UnifiedBulkWriteOperationResultConverter.Convert(bulkWriteException.Result);
                break;

                case ClientBulkWriteException clientBulkWriteException:
                    actualResult = UnifiedClientBulkWriteOperation.ConvertClientBulkWriteResult(clientBulkWriteException.PartialResult);
                    break;

                default:
                    throw new NotSupportedException($"Unrecognized exception type: '{actualException.GetType().FullName}'.");
            }

            new UnifiedValueMatcher(_entityMap).AssertValuesMatch(actualResult, expectedResult);
        }

        private void AssertIsClientError(Exception actualException, bool expectedIsClientError)
        {
            var actualIsClientError = IsClientError(actualException) ||
                (actualException is ClientBulkWriteException bulkWriteException && IsClientError(bulkWriteException.InnerException));

            if (actualIsClientError != expectedIsClientError)
            {
                var message = $"Expected exception to {(expectedIsClientError ? "" : "not ")}be a client exception, but found {actualException}.";
                throw new AssertionException(message);
            }

            bool IsClientError(Exception exception)
                => exception is
                    ArgumentException or
                    MongoClientException or
                    BsonException or
                    MongoConnectionException or
                    NotSupportedException or
                    TimeoutException;
        }

        private void AssertIsError(Exception actualException, bool expectedIsError)
        {
            if (expectedIsError == false)
            {
                throw new FormatException("Test files MUST NOT specify false.");
            }

            actualException.Should().NotBeNull();
        }

        private void AssertWriteConcernErrors(Exception actualException, BsonArray expectedWriteConcernErrors)
        {
            var clientBulkWriteException = actualException.Should().BeAssignableTo<ClientBulkWriteException>().Subject;
            var actualErrors = new BsonArray(
                clientBulkWriteException.WriteConcernErrors.Select(
                    e => new BsonDocument
                    {
                        { "code", ((MongoCommandException)e.MappedWriteConcernResultException).Code },
                        { "message", e.Message }
                    }));

            new UnifiedValueMatcher(_entityMap).AssertValuesMatch(actualErrors, expectedWriteConcernErrors);
        }

        private void AssertWriteErrors(Exception actualException, BsonDocument expectedWriteErrors)
        {
            var clientBulkWriteException = actualException.Should().BeAssignableTo<ClientBulkWriteException>().Subject;
            var actualErrors = new BsonDocument(
                clientBulkWriteException.WriteErrors.ToDictionary(
                    k => k.Key.ToString(),
                    v => new BsonDocument
                    {
                        { "code", v.Value.Code }
                    }));

            new UnifiedValueMatcher(_entityMap).AssertValuesMatch(actualErrors, expectedWriteErrors);
        }

        private static Exception UnwrapCommandException(Exception ex)
        {
            if (ex is MongoConnectionException connectionException)
            {
                ex = connectionException.InnerException;
            }

            if (ex is ClientBulkWriteException bulkWriteException)
            {
                ex = bulkWriteException.InnerException;
            }

            return ex;
        }
    }
}
