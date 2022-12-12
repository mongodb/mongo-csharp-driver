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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Tests.UnifiedTestOperations;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.command_logging_and_monitoring
{
    public class CmapUnifiedTestRunner : LoggableTestClass
    {
        private static readonly HashSet<string> __ignoredLogsMessages = new HashSet<string>(
            new[]
            {
               "Connection pool opening",
               "Connection adding",
               "Connection added",
               "Connection adding",
               "Connection checking in",
               "Connection removing",
               "Connection removed",
               "Connection pool closing",
               "Connection pool clearing",
               "Connection opening",
               "Connection failed",
               "Connection opening failed",
               "Connection closing",
               "Sending",
               "Sending failed",
               "Sent",
               "Receiving",
               "Receiving failed",
               "Received"
            });

        private static string __connectionCategoryName = LogCategoryHelper.GetCategoryName<LogCategories.Connection>();

        public CmapUnifiedTestRunner(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper, true)
        {
        }

        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            using (var runner = new UnifiedTestRunner(loggingService: this, loggingFilter: IsLogValid))
            {
                runner.Run(testCase);
            }
        }

        public class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.connection_monitoring_and_pooling.tests.logging.";

            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                foreach (var testCase in base.CreateTestCases(document))
                {
                    // skip tests for obsolete pool parameters
                    if (testCase.Name.Contains("waitQueueMultiple"))
                    {
                        continue;
                    }

                    foreach (var async in new[] { false, true })
                    {
                        var name = $"{testCase.Name}:async={async}";
                        var test = testCase.Test.DeepClone().AsBsonDocument.Add("async", async);
                        yield return new JsonDrivenTestCase(name, testCase.Shared, test);
                    }
                }
            }
        }

        private static bool IsLogValid(LogEntry logEntry) =>
            logEntry.Category != __connectionCategoryName ||
            !__ignoredLogsMessages.Contains(logEntry.GetParameter<string>(StructuredLogTemplateProviders.Message));
    }
}
