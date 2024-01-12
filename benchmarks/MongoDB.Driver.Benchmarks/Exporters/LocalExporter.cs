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
using System.IO;
using System.Linq;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.Exporters
{
    public sealed class LocalExporter : IExporter
    {
        public string Name => GetType().Name;

        public void ExportToLog(Summary summary, ILogger logger)
        {
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            var exportedFiles = new List<string>();

            var benchmarksGroupedByRuntime = summary.Reports.GroupBy(b => b.GetRuntimeInfo()).ToArray();
            foreach (var benchmarkGroup in benchmarksGroupedByRuntime)
            {
                var runtime = benchmarkGroup.Key;
                var filename = $"local-report({runtime}).txt";
                var path = Path.Combine(summary.ResultsDirectoryPath, filename);

                using var writer = new StreamWriter(path, false);
                var benchmarkResults = benchmarkGroup.Select(report => new BenchmarkResult(report)).ToArray();

                writer.WriteLine("Scores Summary: ");
                foreach (var category in DriverBenchmarkCategory.AllCategories)
                {
                    WriteScore(writer, category, CalculateCompositeScore(benchmarkResults, category));
                }

                foreach (var benchmark in benchmarkResults)
                {
                    WriteScore(writer, benchmark.Name, benchmark.Score);
                }

                exportedFiles.Add(path);
            }

            return exportedFiles;
        }

        private static void WriteScore(StreamWriter writer, string benchName, double score)
        {
            writer.WriteLine(score != 0
                ? $"Executed {benchName}, score: {score:F3} MB/s"
                : $"Skipped {benchName}");
        }
    }
}
