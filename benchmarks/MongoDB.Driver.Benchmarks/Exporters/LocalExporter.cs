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

using System.IO;
using System.Linq;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Exporters;
using System.Collections.Generic;
using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.Exporters
{
    public class LocalExporter : IExporter
    {
        public string Name => GetType().Name;

        public void ExportToLog(Summary summary, ILogger logger)
        {
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            var exportedFiles = new List<string>();

            var runtimes = summary.Reports.Select(benchmark => benchmark.GetRuntimeInfo()).Distinct().ToList();
            foreach (string runtime in runtimes)
            {
                string filename = $"local-report({runtime}).txt";
                string path = Path.Combine(summary.ResultsDirectoryPath, filename);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    var benchmarkResults = new List<BenchmarkResult>();
                    foreach (var report in summary.Reports)
                    {
                        if (report.GetRuntimeInfo() != runtime)
                        {
                            continue;
                        }

                        string benchmarkName;
                        if (report.BenchmarkCase.Descriptor.HasCategory(DriverBenchmarkCategory.BsonBench))
                        {
                            benchmarkName = report.BenchmarkCase.Parameters["benchmarkData"] + report.BenchmarkCase.Descriptor.Type.Name;
                        }
                        else
                        {
                            benchmarkName = report.BenchmarkCase.Descriptor.Type.Name;
                        }
                        benchmarkResults.Add(new BenchmarkResult(report, benchmarkName, GetDatasetSize(benchmarkName)));
                    }

                    ExportToFile(benchmarkResults, writer);
                }
                exportedFiles.Add(path);
            }
            return exportedFiles;
        }

        private static void ExportToFile(List<BenchmarkResult> benchmarkResults, TextWriter writer)
        {
            var compositeScore = new CompositeScore(benchmarkResults);
            double bsonBenchScore = compositeScore.GetScore(DriverBenchmarkCategory.BsonBench);
            double readBenchScore = compositeScore.GetScore(DriverBenchmarkCategory.ReadBench);
            double writeBenchScore = compositeScore.GetScore(DriverBenchmarkCategory.WriteBench);
            double multiBenchScore = compositeScore.GetScore(DriverBenchmarkCategory.MultiBench);
            double singleBenchScore = compositeScore.GetScore(DriverBenchmarkCategory.SingleBench);
            double parallelBenchScore = compositeScore.GetScore(DriverBenchmarkCategory.ParallelBench);
            double driverBenchScore = (readBenchScore + writeBenchScore) / 2;

            writer.WriteLine("Scores Summary: ");
            WriteScore(DriverBenchmarkCategory.BsonBench, bsonBenchScore);
            WriteScore(DriverBenchmarkCategory.ReadBench, readBenchScore);
            WriteScore(DriverBenchmarkCategory.WriteBench, writeBenchScore);
            WriteScore(DriverBenchmarkCategory.MultiBench, multiBenchScore);
            WriteScore(DriverBenchmarkCategory.SingleBench, singleBenchScore);
            WriteScore(DriverBenchmarkCategory.ParallelBench, parallelBenchScore);
            WriteScore(DriverBenchmarkCategory.DriverBench, driverBenchScore);

            foreach (var benchmark in benchmarkResults)
            {
                WriteScore(benchmark.Name, benchmark.Score);
            }

            return;

            void WriteScore(string benchName, double score)
            {
                writer.WriteLine(score != 0
                    ? $"Executed {benchName}, score: {score:F3} MB/s"
                    : $"Skipped {benchName}");
            }
        }
    }
}
