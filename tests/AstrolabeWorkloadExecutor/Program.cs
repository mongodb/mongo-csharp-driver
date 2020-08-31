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
using System.IO;
using System.Threading;
using MongoDB.Bson;

namespace WorkloadExecutor
{
    public static class Program
    {
        private static long __numberOfSuccessfulOperations;
        private static long __numberOfFailedOperations;
        private static long __numberOfOperationErrors;

        public static void Main(string[] args)
        {
            var connectionString = args[0];
            var driverWorkload = BsonDocument.Parse(args[1]);

            var cancellationTokenSource = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (o, e) => HandleCancel(e, cancellationTokenSource);

            var resultsDir = Environment.GetEnvironmentVariable("RESULTS_DIR");
            var resultsPath = resultsDir == null ? "results.json" : Path.Combine(resultsDir, "results.json");
            Console.WriteLine($"dotnet main> Results will be written to {resultsPath}...");

            try
            {
                Console.CancelKeyPress += cancelHandler;

                Console.WriteLine("dotnet main> Starting workload executor...");

                if (!bool.TryParse(Environment.GetEnvironmentVariable("ASYNC"), out bool async))
                {
                    async = true;
                }

                ExecuteWorkload(connectionString, driverWorkload, async, cancellationTokenSource.Token);
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
                Console.WriteLine("dotnet main finally> Writing final results file");
                var resultsJson = ConvertResultsToJson();
                Console.WriteLine(resultsJson);
#if NETCOREAPP2_1
                File.WriteAllTextAsync(resultsPath, resultsJson).Wait();
#else
                File.WriteAllText(resultsPath, resultsJson);
#endif
            }
        }

        private static string ConvertResultsToJson()
        {
            var resultsJson =
            @"{ " +
            $"  \"numErrors\" : {__numberOfOperationErrors}, " +
            $"  \"numFailures\" : {__numberOfFailedOperations}, " +
            $"  \"numSuccesses\" : {__numberOfSuccessfulOperations} " +
            @"}";

            return resultsJson;
        }

        private static void ExecuteWorkload(string connectionString, BsonDocument driverWorkload, bool async, CancellationToken cancellationToken)
        {
            Environment.SetEnvironmentVariable("MONGODB_URI", connectionString);

            var testRunner = new AstrolabeTestRunner(
                incrementOperationSuccesses: () => __numberOfSuccessfulOperations++,
                incrementOperationErrors: () => __numberOfOperationErrors++,
                incrementOperationFailures: () => __numberOfFailedOperations++,
                cancellationToken: cancellationToken);
            var factory = new AstrolabeTestRunner.TestCaseFactory();
            var testCase = factory.CreateTestCase(driverWorkload, async);
            testRunner.Run(testCase);
            Console.WriteLine("dotnet ExecuteWorkload> Returning...");
        }

        private static void CancelWorkloadTask(CancellationTokenSource cancellationTokenSource)
        {
            Console.Write($"\ndotnet cancel workload> Canceling the workload task...");
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
    }
}
