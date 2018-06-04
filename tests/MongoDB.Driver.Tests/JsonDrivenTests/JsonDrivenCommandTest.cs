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

                if (actualErrorCodeName == expectedErrorCodeName)
                {
                    return;
                }
            }

            if (_expectedException.Contains("errorContains"))
            {
                if (_actualException.Message.IndexOf(_expectedException["errorContains"].AsString, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return;
                }
            }

            throw new Exception("Unexpected exception was thrown.", _actualException);
        }

        protected override void ParseExpectedResult(BsonValue value)
        {
            var document = value as BsonDocument;
            if (document != null)
            {
                if (document.Contains("errorCodeName") || document.Contains("errorContains"))
                {
                    _expectedException = document;
                    return;
                }
            }

            base.ParseExpectedResult(value);
        }
    }
}
