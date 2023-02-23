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
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Tests.UnifiedTestOperations;
using Xunit;
using Xunit.Abstractions;
using System.Linq;

namespace MongoDB.Driver.Tests.Specifications.server_discovery_and_monitoring
{
    [Trait("Category", "SDAM")]
    public class ServerDiscoveryAndMonitoringUnifiedTestRunner : LoggableTestClass
    {
        public ServerDiscoveryAndMonitoringUnifiedTestRunner(ITestOutputHelper output) : base(output)
        {
        }

        // public methods
        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            using (var runner = new UnifiedTestRunner(loggingService: this, eventsProcessor: new SdamRunnerEventsProcessor(testCaseName: testCase.Name)))
            {
                runner.Run(testCase);
            }
        }

        // nested types
        public sealed class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            #region static
            private static readonly string[] __ignoredTestNameKeys =
            {
#if NET472
                // https://jira.mongodb.org/browse/CSHARP-3165
                "InUseConnections",
#endif
                // "Not implemented: https://jira.mongodb.org/browse/CSHARP-3138"
                "connectTimeoutMS=0",
                // https://jira.mongodb.org/browse/CSHARP-4459
                "Ignore network timeout error on find",
                "Network timeout on Monitor check",
                "Reset server and pool after network timeout error during authentication"
            };
            #endregion

            // protected properties
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.server_discovery_and_monitoring.tests.unified.";

            // protected methods
            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                foreach (var testCase in base.CreateTestCases(document))
                {
                    if (__ignoredTestNameKeys.Any(tc => testCase.Name.Contains(tc)))
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

        private sealed class SdamRunnerEventsProcessor : IEventsProcessor
        {
            private readonly string _testCaseName;

            public SdamRunnerEventsProcessor(string testCaseName)
            {
                _testCaseName = Ensure.IsNotNull(testCaseName, nameof(testCaseName));
            }

            public void PostProcessEvents(List<object> events, string type)
            {
                // This is workaround. Our current implementation doesn't generate connection closing events in the order expected by the spec.
                // Spec expected order:
                //      1. PoolClear; 2. CheckIn; 3. ConnectionClosed;
                // but the currently triggered:
                //      1. PoolClear; 2. ConnectionClosed; 3. CheckIn;
                // So, below we manually change the events order for single pair of events for a specific connection to match the spec expectations
                // See for details: CSHARP-4458
                if (type == "cmap" && _testCaseName.Contains("InUseConnections"))
                {
                    var clearWithCloseInUseIndex = events.FindIndex(p => p is ConnectionPoolClearedEvent cpc && cpc.CloseInUseConnections);
                    if (clearWithCloseInUseIndex != -1)
                    {
                        var connectionClosedEventIndex = events.FindIndex(startIndex: clearWithCloseInUseIndex, p => p is ConnectionClosedEvent);
                        if (connectionClosedEventIndex != -1)
                        {
                            var connectionClosedEvent = (ConnectionClosedEvent)events[connectionClosedEventIndex];
                            var relatedCheckedInEventIndex = events.FindIndex(startIndex: connectionClosedEventIndex, p => p is ConnectionPoolCheckedInConnectionEvent checkedInEvent && checkedInEvent.ConnectionId == connectionClosedEvent.ConnectionId);
                            if (relatedCheckedInEventIndex != -1)
                            {
                                (events[relatedCheckedInEventIndex], events[connectionClosedEventIndex]) = (events[connectionClosedEventIndex], events[relatedCheckedInEventIndex]);
                            }
                        }
                    }
                }
            }
        }
    }
}
