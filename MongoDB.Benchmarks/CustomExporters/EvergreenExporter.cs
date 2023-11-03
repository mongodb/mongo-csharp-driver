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
using MongoDB.Bson.IO;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Exporters;
using System.Collections.Generic;
using static MongoDB.Benchmarks.BenchmarkExtensions;

namespace MongoDB.Benchmarks.CustomExporters
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
            var benchmarkResults =
                (from report in summary.Reports
                    let benchmarkName = report.BenchmarkCase.Descriptor.WorkloadMethod.Name
                    select new BenchmarkResult(report.ResultStatistics, benchmarkName, GetDatasetSize(benchmarkName)))
                .ToList();

            var resultsPath = Path.Combine(summary.ResultsDirectoryPath, _outputFile);
            if (File.Exists(resultsPath))
            {
                File.Delete(resultsPath);
            }

            using (var jsonWriter = new JsonWriter(File.CreateText(resultsPath), new JsonWriterSettings { Indent = true }))
            {
                var compositeScore = new CompositeScore(benchmarkResults);
                var bsonBenchScore = compositeScore.GetScore(BsonBenchmarks);
                var readBenchScore = compositeScore.GetScore(ReadBenchmarks);
                var multiBenchScore = compositeScore.GetScore(MultiBenchmarks);
                var writeBenchScore = compositeScore.GetScore(WriteBenchmarks);
                var singleBenchScore = compositeScore.GetScore(SingleBenchmarks);
                var parallelBenchScore = compositeScore.GetScore(ParallelBenchmarks);
                var driverBenchScore = (readBenchScore + writeBenchScore) / 2;

                jsonWriter.WriteStartArray();

                WriteCompositeScoreToResults(jsonWriter,"BSONBench", bsonBenchScore);
                WriteCompositeScoreToResults(jsonWriter,"ReadBench", readBenchScore);
                WriteCompositeScoreToResults(jsonWriter,"WriteBench", writeBenchScore);
                WriteCompositeScoreToResults(jsonWriter,"MultiBench", multiBenchScore);
                WriteCompositeScoreToResults(jsonWriter,"SingleBench", singleBenchScore);
                WriteCompositeScoreToResults(jsonWriter,"ParallelBench", parallelBenchScore);
                WriteCompositeScoreToResults(jsonWriter,"DriverBench", driverBenchScore);

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
