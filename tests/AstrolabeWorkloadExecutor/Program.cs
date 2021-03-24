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
using System.Threading;
using AstrolabeWorkloadExecutor;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Tests.UnifiedTestOperations;

namespace WorkloadExecutor
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Ensure.IsEqualTo(args.Count(), 2, "WorkloadExecutorArgumentsCount");

            var connectionString = args[0];
            var driverWorkload = BsonDocument.Parse(args[1]);
            Console.WriteLine($"Income document: {driverWorkload}");

            var cancellationTokenSource = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (o, e) => HandleCancel(e, cancellationTokenSource);

            var resultsDir = Environment.GetEnvironmentVariable("RESULTS_DIR");
            var eventsPath = resultsDir == null ? "events.json" : Path.Combine(resultsDir, "events.json");
            var resultsPath = resultsDir == null ? "results.json" : Path.Combine(resultsDir, "results.json");
            Console.WriteLine($"dotnet main> Results will be written to {resultsPath}...");

            Console.CancelKeyPress += cancelHandler;

            Console.WriteLine("dotnet main> Starting workload executor...");

            if (!bool.TryParse(Environment.GetEnvironmentVariable("ASYNC"), out bool async))
            {
                async = true;
            }

            UnifiedEntityMap entityMap = null;
            try
            {
                entityMap = ExecuteWorkload(connectionString, driverWorkload, async, cancellationTokenSource.Token);
            }
            finally
            {
                var resultDetails = HandleWorkloadResult(entityMap: entityMap);

                Console.CancelKeyPress -= cancelHandler;

                Console.WriteLine("dotnet main finally> Writing final results file");
                WriteToFile(resultsPath, resultDetails.ResultsJson);
                WriteToFile(eventsPath, resultDetails.EventsJson);

                // ensure all messages are propagated to the astralable time immediately
                Console.Error.Flush();
                Console.Out.Flush();
            }
        }

        private static (string EventsJson, string ResultsJson) HandleWorkloadResult(UnifiedEntityMap entityMap)
        {
            var iterationsCount = GetValueOrDefault(entityMap.IterationCounts, "iterations", @default: -1);
            var successesCount = GetValueOrDefault(entityMap.SuccessCounts, "successes", @default: -1);

            var errorDocuments = GetValueOrDefault(entityMap.ErrorDocumentsMap, "errors", @default: new BsonArray());
            var errorCount  = errorDocuments.Count;
            var failuresDocuments = GetValueOrDefault(entityMap.FailureDocumentsMap, "failures", @default: new BsonArray());
            var failuresCount = failuresDocuments.Count;

            var events = new BsonArray();
            if (entityMap.EventCapturers.TryGetValue("events", out var eventCapturer))
            {
                var specEvents = eventCapturer.Events.Select(e => AstrolabeEventsHandler.CreateEventDocument(e));
                events.AddRange(specEvents);
            }

            var eventsDocument = new BsonDocument
            {
                { "events", events },
                { "errors", errorDocuments },
                { "failures", failuresDocuments }
            };

            var resultsDocument = new BsonDocument
            {
                { "numErrors", errorCount },
                { "numFailures", failuresCount },
                { "numSuccesses", successesCount },
                { "numIterations", iterationsCount }
            };

            var jsonWritterSettings = new JsonWriterSettings
            {
                OutputMode = JsonOutputMode.RelaxedExtendedJson
            };
            return (eventsDocument.ToJson(jsonWritterSettings), resultsDocument.ToJson(jsonWritterSettings));

            T GetValueOrDefault<T>(Dictionary<string, T> dictionary, string key, T @default) => dictionary.TryGetValue(key, out var value) ? value : @default;
        }

        private static UnifiedEntityMap ExecuteWorkload(string connectionString, BsonDocument driverWorkload, bool async, CancellationToken cancellationToken)
        {
            Environment.SetEnvironmentVariable("MONGODB_URI", connectionString); // force using atlas connection string in our internal test connection strings

            var factory = new TestCaseFactory();
            var testCase = factory.CreateTestCase(driverWorkload, async);
            using (var runner = new UnifiedTestFormatProcessor(
                allowKillSessions: false,
                terminationCancellationToken: cancellationToken))
            {
                runner.Run(testCase);
                Console.WriteLine("dotnet ExecuteWorkload> Returning...");
                return runner.EntityMap;
            }
        }

        private static void CancelWorkloadTask(CancellationTokenSource cancellationTokenSource)
        {
            Console.Write($"\ndotnet cancel workload> Canceling the workload task...");
            cancellationTokenSource.Cancel();
        }

        private static void HandleCancel(
            ConsoleCancelEventArgs args,
            CancellationTokenSource cancellationTokenSource)
        {
            // We set the Cancel property to true to prevent the process from terminating
            args.Cancel = true;
            CancelWorkloadTask(cancellationTokenSource);
        }

        private static void WriteToFile(string path, string json)
        {
            File.WriteAllText(path, json);
        }

        internal class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            public JsonDrivenTestCase CreateTestCase(BsonDocument driverWorkload, bool async)
            {
                var testCase = CreateTestCases(driverWorkload).Single();
                testCase.Test["async"] = async;

                return testCase;
            }

            protected override string GetTestCaseName(BsonDocument shared, BsonDocument test, int index) => $"Astrolabe command line arguments:{base.GetTestName(test, index)}";
        }
    }
}
