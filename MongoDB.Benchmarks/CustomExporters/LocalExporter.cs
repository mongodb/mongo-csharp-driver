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
using static MongoDB.Benchmarks.BenchmarkExtensions;

namespace MongoDB.Benchmarks.CustomExporters
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
                    var benchmarkResults =
                        (from report in summary.Reports
                            where report.GetRuntimeInfo() == runtime
                            let benchmarkName = report.BenchmarkCase.Descriptor.WorkloadMethod.Name
                            select new BenchmarkResult(report.ResultStatistics, benchmarkName, GetDatasetSize(benchmarkName)))
                        .ToList();

                    ExportToFile(benchmarkResults, writer);
                }
                exportedFiles.Add(path);
            }
            return exportedFiles;
        }

        private static void ExportToFile(List<BenchmarkResult> benchmarkResults, TextWriter writer)
        {
            var compositeScore = new CompositeScore(benchmarkResults);
            double bsonBenchScore = compositeScore.GetScore(BsonBenchmarks);
            double readBenchScore = compositeScore.GetScore(ReadBenchmarks);
            double writeBenchScore = compositeScore.GetScore(WriteBenchmarks);
            double multiBenchScore = compositeScore.GetScore(MultiBenchmarks);
            double singleBenchScore = compositeScore.GetScore(SingleBenchmarks);
            double parallelBenchScore = compositeScore.GetScore(ParallelBenchmarks);
            double driverBenchScore = (readBenchScore + writeBenchScore) / 2;

            writer.WriteLine("Scores Summary: ");
            WriteScore("BSONBench", bsonBenchScore);
            WriteScore("ReadBench", readBenchScore);
            WriteScore("WriteBench", writeBenchScore);
            WriteScore("MultiBench", multiBenchScore);
            WriteScore("SingleBench", singleBenchScore);
            WriteScore("ParallelBench", parallelBenchScore);
            WriteScore("DriverBench", driverBenchScore);

            foreach (var benchmark in benchmarkResults)
            {
                WriteScore(benchmark.Name, benchmark.Score);
            }

            // return;

            void WriteScore(string benchName, double score)
            {
                writer.WriteLine(score != 0
                    ? $"Executed {benchName}, score: {score:F3} MB/s"
                    : $"Skipped {benchName}");
            }
        }
    }
}
