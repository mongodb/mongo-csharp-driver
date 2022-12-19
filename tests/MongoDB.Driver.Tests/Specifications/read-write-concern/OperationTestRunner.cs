﻿/* Copyright 2020-present MongoDB Inc.
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
using MongoDB.Driver.Tests.Specifications.Runner;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.read_write_concern
{
    public class OperationTestRunner : MongoClientJsonDrivenTestRunnerBase
    {
        protected override string[] ExpectedTestColumns => new[] { "description", "operations", "outcome", "expectations", "async" };

        public OperationTestRunner(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            DefaultCommandsToNotCapture.Add("find");
        }

        // public methods
        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            SetupAndRunTest(testCase);
        }

        // nested types
        public class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            // protected properties
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.read_write_concern.tests.operation.";

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
