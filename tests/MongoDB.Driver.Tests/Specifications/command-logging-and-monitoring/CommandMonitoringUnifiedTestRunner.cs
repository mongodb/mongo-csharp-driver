/* Copyright 2021-present MongoDB Inc.
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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Tests.UnifiedTestOperations;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.command_logging_and_monitoring
{
    public class CommandMonitoringUnifiedTestRunner : LoggableTestClass
    {
        // public constructors
        public CommandMonitoringUnifiedTestRunner(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            using (var runner = new UnifiedTestRunner(loggingService: this))
            {
                runner.Run(testCase);
            }
        }

        // nested types
        public class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            #region static
            private static readonly string[] __ignoreTests =
            {
                // CSHARP-3823
                "hello with speculative authenticate",
                "hello without speculative authenticate",
                "legacy hello with speculative authenticate",
                "legacy hello without speculative authenticate"
            };
            #endregion

            // protected properties
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.command_logging_and_monitoring.tests.unified.";

            // protected methods
            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                foreach (var testCase in base.CreateTestCases(document).Where(c => !__ignoreTests.Any(i => c.Name.Contains(i))))
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
