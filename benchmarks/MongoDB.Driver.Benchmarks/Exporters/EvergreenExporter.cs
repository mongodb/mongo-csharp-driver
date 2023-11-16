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
using MongoDB.Bson.IO;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Exporters;
using System.Collections.Generic;
using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.Exporters
{
    public sealed class EvergreenExporter : IExporter
    {
        private readonly string _outputFile;

        public string Name => GetType().Name;

        public EvergreenExporter(string outputFile)
        {
            _outputFile = outputFile;
        }

        public void ExportToLog(Summary summary, ILogger logger)
        {
        }

        public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
        {
            var benchmarkResults = new List<BenchmarkResult>();
            foreach (var report in summary.Reports)
            {
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

            var resultsPath = Path.Combine(summary.ResultsDirectoryPath, _outputFile);
            if (File.Exists(resultsPath))
            {
                File.Delete(resultsPath);
            }

            using (var jsonWriter = new JsonWriter(File.CreateText(resultsPath), new JsonWriterSettings { Indent = true }))
            {
                var compositeScore = new CompositeScore(benchmarkResults);
                var bsonBenchScore = compositeScore.GetScore(DriverBenchmarkCategory.BsonBench);
                var readBenchScore = compositeScore.GetScore(DriverBenchmarkCategory.ReadBench);
                var multiBenchScore = compositeScore.GetScore(DriverBenchmarkCategory.MultiBench);
                var writeBenchScore = compositeScore.GetScore(DriverBenchmarkCategory.WriteBench);
                var singleBenchScore = compositeScore.GetScore(DriverBenchmarkCategory.SingleBench);
                var parallelBenchScore = compositeScore.GetScore(DriverBenchmarkCategory.ParallelBench);
                var driverBenchScore = (readBenchScore + writeBenchScore) / 2;

                jsonWriter.WriteStartArray();

                WriteCompositeScoreToResults(jsonWriter, DriverBenchmarkCategory.BsonBench, bsonBenchScore);
                WriteCompositeScoreToResults(jsonWriter, DriverBenchmarkCategory.ReadBench, readBenchScore);
                WriteCompositeScoreToResults(jsonWriter, DriverBenchmarkCategory.WriteBench, writeBenchScore);
                WriteCompositeScoreToResults(jsonWriter, DriverBenchmarkCategory.MultiBench, multiBenchScore);
                WriteCompositeScoreToResults(jsonWriter, DriverBenchmarkCategory.SingleBench, singleBenchScore);
                WriteCompositeScoreToResults(jsonWriter, DriverBenchmarkCategory.ParallelBench, parallelBenchScore);
                WriteCompositeScoreToResults(jsonWriter, DriverBenchmarkCategory.DriverBench, driverBenchScore);

                foreach (var benchmark in benchmarkResults)
                {
                    WriteIndividualBenchmarkToResults(jsonWriter, benchmark);
                }

                jsonWriter.WriteEndArray();
            }

            return new[] { resultsPath };
        }

        private static void WriteCompositeScoreToResults(JsonWriter jsonWriter, string name, double score)
        {
            jsonWriter.WriteStartDocument();
            jsonWriter.WriteStartDocument("info");
            jsonWriter.WriteString("test_name", name);
            jsonWriter.WriteEndDocument();

            jsonWriter.WriteStartArray("metrics");
            jsonWriter.WriteStartDocument();
            jsonWriter.WriteString("name", "megabytes_per_second");
            jsonWriter.WriteDouble("value", score);
            jsonWriter.WriteEndDocument();
            jsonWriter.WriteEndArray();

            jsonWriter.WriteEndDocument();
        }

        private static void WriteIndividualBenchmarkToResults(JsonWriter jsonWriter, BenchmarkResult benchmarkResult)
        {
            jsonWriter.WriteStartDocument();
            jsonWriter.WriteStartDocument("info");
            jsonWriter.WriteString("test_name", benchmarkResult.Name);
            jsonWriter.WriteEndDocument();

            jsonWriter.WriteStartArray("metrics");
            jsonWriter.WriteStartDocument();
            jsonWriter.WriteString("name", "megabytes_per_second");
            jsonWriter.WriteDouble("value", benchmarkResult.Score);
            jsonWriter.WriteEndDocument();
            jsonWriter.WriteEndArray();

            jsonWriter.WriteEndDocument();
        }
    }
}
