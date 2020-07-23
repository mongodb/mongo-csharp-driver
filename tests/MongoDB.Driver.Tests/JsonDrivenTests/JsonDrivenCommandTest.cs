/* Copyright 2018-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public abstract class JsonDrivenCommandTest : JsonDrivenTest
    {
        // constructors
        protected JsonDrivenCommandTest(Dictionary<string, object> objectMap)
            : base(objectMap)
        {
        }

        // protected methods
        protected override void AssertException()
        {
            if (_expectedException.Contains("errorCodeName"))
            {
                var expectedErrorCodeName = _expectedException["errorCodeName"].AsString;
                string actualErrorCodeName = null;

                var commandException = _actualException as MongoCommandException;
                if (commandException != null)
                {
                    var writeConcernException = commandException as MongoWriteConcernException; // MongoWriteConcernException is a subclass of MongoCommandException
                    if (writeConcernException != null)
                    {
                        actualErrorCodeName = writeConcernException.Result.GetValue("writeConcernError", null)?.AsBsonDocument.GetValue("codeName", null)?.AsString;
                    }
                    else
                    {
                        actualErrorCodeName = commandException.CodeName;
                    }
                }

                if (actualErrorCodeName == null && _actualException is MongoExecutionTimeoutException mongoExecutionTimeout)
                {
                    actualErrorCodeName = mongoExecutionTimeout.CodeName;
                }

                if (actualErrorCodeName == null)
                {
                    throw new Exception("Exception was missing \"errorCodeName\".", _actualException);
                }

                if (actualErrorCodeName != expectedErrorCodeName)
                {
                    throw new Exception($"Exception errorCodeName was \"{actualErrorCodeName}\", expected \"{expectedErrorCodeName}\".", _actualException);
                }
            }

            if (_expectedException.Contains("errorContains"))
            {
                var actualMessage = _actualException.Message;
                var expectedContains = _expectedException["errorContains"].AsString;

                if (actualMessage.IndexOf(expectedContains, StringComparison.OrdinalIgnoreCase) == -1)
                {
                    throw new Exception($"Exception message \"{actualMessage}\" does not contain \"{expectedContains}\".", _actualException);
                }
            }

            if (_expectedException.Contains("errorLabelsContain"))
            {
                var mongoException = _actualException as MongoException;
                if (mongoException == null)
                {
                    throw new Exception($"Exception was of type {_actualException.GetType().FullName}, expected a MongoException.", _actualException);
                }

                foreach (var expectedLabel in _expectedException["errorLabelsContain"].AsBsonArray.OfType<BsonString>().Select(x => x.AsString))
                {
                    if (!mongoException.HasErrorLabel(expectedLabel))
                    {
                        throw new Exception($"Exception should contain ErrorLabel: \"{expectedLabel}\".", _actualException);
                    }
                }
            }

            if (_expectedException.Contains("errorLabelsOmit"))
            {
                var mongoException = _actualException as MongoException;
                if (mongoException == null)
                {
                    throw new Exception($"Exception was of type {_actualException.GetType().FullName}, expected a MongoException.", _actualException);
                }

                foreach (var omittedLabel in _expectedException["errorLabelsOmit"].AsBsonArray.OfType<BsonString>().Select(x => x.AsString))
                {
                    if (mongoException.HasErrorLabel(omittedLabel))
                    {
                        throw new Exception($"Exception should not contain ErrorLabel: \"{omittedLabel}\".", _actualException);
                    }
                }
            }
        }

        protected override void ParseExpectedResult(BsonValue value)
        {
            var document = value as BsonDocument;
            if (document != null)
            {
                if (LooksLikeAnExpectedException(document))
                {
                    _expectedException = document;
                    return;
                }
            }

            base.ParseExpectedResult(value);
        }

        // private methods
        private bool LooksLikeAnExpectedException(BsonDocument document)
        {
            var errorFieldNames = new[] { "error", "errorCodeName", "errorContains", "errorLabelsContain", "errorLabelsOmit" };
            return document.Names.Any(x => errorFieldNames.Contains(x));
        }
    }
}
