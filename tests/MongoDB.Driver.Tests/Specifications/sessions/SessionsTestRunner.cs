﻿/* Copyright 2019-present MongoDB Inc.
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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core;
using MongoDB.Driver.Tests.Specifications.Runner;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.sessions
{
    [Trait("Category", "Serverless")]
    public class SessionsTestRunner : MongoClientJsonDrivenSessionsTestRunner
    {
        protected override string[] ExpectedTestColumns => new string[] { "async", "clientOptions", "failPoint", "description", "operations", "expectations", "outcome" };

        // public methods
        public SessionsTestRunner(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            SetupAndRunTest(testCase);
        }

        protected override void RunTest(BsonDocument shared, BsonDocument test, EventCapturer eventCapturer)
        {
            using (var client = CreateDisposableClient(test, eventCapturer))
            using (var session0 = StartSession(client, test, "session0"))
            {
                var objectMap = new Dictionary<string, object>
                {
                    { "session0", session0 },
                };

                ExecuteOperations(client, objectMap, test, eventCapturer);
            }
        }

        // nested types
        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.sessions.tests.legacy";

            // protected methods
            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                foreach (var testCase in base.CreateTestCases(document))
                {
                    foreach (var async in new[] { false, true })
                    {
                        var name = $"{testCase.Name}:async={async}";
                        var test = testCase.Test.DeepClone().AsBsonDocument.Add("async", async);
                        yield return new JsonDrivenTestCase(name, testCase.Shared, test);
                    }
                }
            }
        }
    }
}
