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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using AstrolabeWorkloadExecutor;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Tests.Specifications.unified_test_format;
using MongoDB.Driver.Tests.UnifiedTestOperations;

namespace WorkloadExecutor
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Ensure.IsEqualTo(args.Length, 2, nameof(args.Length));

            var connectionString = args[0];
            var driverWorkload = BsonDocument.Parse(args[1]);

            var cancellationTokenSource = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (o, e) => HandleCancel(e, cancellationTokenSource);

            var resultsDir = Environment.GetEnvironmentVariable("RESULTS_DIR") ?? "";
            var eventsPath = Path.Combine(resultsDir, "events.json");
            var resultsPath = Path.Combine(resultsDir, "results.json");
            Console.WriteLine($"dotnet main> Results will be written to {resultsPath}");
            Console.WriteLine($"dotnet main> Events will be written to {eventsPath}");

            Console.CancelKeyPress += cancelHandler;

            Console.WriteLine($"dotnet main> Starting workload executor...");

            var async = bool.Parse(Environment.GetEnvironmentVariable("ASYNC") ?? throw new Exception($"ASYNC environment variable must be configured."));

            var (resultsJson, eventsJson) = ExecuteWorkload(connectionString, driverWorkload, async, cancellationTokenSource.Token);

            Console.CancelKeyPress -= cancelHandler;

            Console.WriteLine($"dotnet main finally> Writing final results and events files");
            File.WriteAllText(resultsPath, resultsJson);
            File.WriteAllText(eventsPath, eventsJson);

            // ensure all messages are propagated to the astrolabe immediately
            Console.Error.Flush();
            Console.Out.Flush();
        }

        private static (string EventsJson, string ResultsJson) CreateWorkloadResult(UnifiedEntityMap entityMap)
        {
            Ensure.IsNotNull(entityMap, nameof(entityMap));

            var iterationsCount = GetValueOrDefault(entityMap.IterationCounts, "iterations", @default: -1);
            var successesCount = GetValueOrDefault(entityMap.SuccessCounts, "successes", @default: -1);

            var errorDocuments = GetValueOrDefault(entityMap.ErrorDocumentsMap, "errors", @default: new BsonArray());
            var errorCount  = errorDocuments.Count;
            var failuresDocuments = GetValueOrDefault(entityMap.FailureDocumentsMap, "failures", @default: new BsonArray());
            var failuresCount = failuresDocuments.Count;

            string eventsJson = "[]";
            if (entityMap.EventCapturers.TryGetValue("events", out var eventCapturer))
            {
                Console.WriteLine($"dotnet events> Number of generated events {eventCapturer.Count}");
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("[");
                for (int i = 0; i < eventCapturer.Events.Count; i++)
                {
                    stringBuilder.Append(AstrolabeEventsHandler.CreateEventDocument(eventCapturer.Events[i]));
                    if (i < eventCapturer.Events.Count - 1)
                    {
                        stringBuilder.Append(",");
                    }
                }
                stringBuilder.Append("]");
                eventsJson = stringBuilder.ToString();
            }

            var eventsDocument = @$"{{ ""events"" : {eventsJson}, ""errors"" : {errorDocuments}, ""failures"" : {failuresDocuments} }}";
            var resultsDocument = $@"{{ ""numErrors"" : {errorCount}, ""numFailures"" : {failuresCount}, ""numSuccesses"" : {successesCount},  ""numIterations"" : {iterationsCount} }}";

            return (eventsDocument, resultsDocument);

            T GetValueOrDefault<T>(Dictionary<string, T> dictionary, string key, T @default) => dictionary.TryGetValue(key, out var value) ? value : @default;
        }

        private static (string EventsJson, string ResultsJson) ExecuteWorkload(string connectionString, BsonDocument driverWorkload, bool async, CancellationToken cancellationToken)
        {
            Environment.SetEnvironmentVariable("MONGODB_URI", connectionString); // force using atlas connection string in our internal test connection strings

            var additionalArgs = new Dictionary<string, object>();
            additionalArgs["AstrolabeCancellationToken"] = cancellationToken;
            using (var testRunner = new UnifiedTestFormatTestRunner(
                allowKillSessions: false,
                additionalArgs: additionalArgs))
            {
                var factory = new TestCaseFactory();
                var testCase = factory.CreateTestCase(driverWorkload, async);
                testRunner.Run(testCase);
                Console.WriteLine($"dotnet ExecuteWorkload> Returning...");
                return CreateWorkloadResult(entityMap: testRunner.EntityMap);
            }
        }

        private static void CancelWorkloadTask(CancellationTokenSource cancellationTokenSource)
        {
            Console.Write($"dotnet cancel workload> Canceling the workload task...");
            cancellationTokenSource.Cancel();
            Console.WriteLine($"Done.");
        }

        private static void HandleCancel(
            ConsoleCancelEventArgs args,
            CancellationTokenSource cancellationTokenSource)
        {
            // We set the Cancel property to true to prevent the process from terminating
            args.Cancel = true;
            CancelWorkloadTask(cancellationTokenSource);
        }

        internal class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            public JsonDrivenTestCase CreateTestCase(BsonDocument driverWorkload, bool async)
            {
                var testCase = CreateTestCases(driverWorkload).Single();
                testCase.Test["async"] = async;

                return testCase;
            }

            protected override string GetTestCaseName(BsonDocument shared, BsonDocument test, int index) =>
                $"Astrolabe command line arguments:{base.GetTestName(test, index)}";
        }
    }
}
