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
using MongoDB.Bson.IO;
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
            var benchmarkResults = summary.Reports.Select(report => new BenchmarkResult(report)).ToArray();

            var resultsPath = Path.Combine(summary.ResultsDirectoryPath, _outputFile);

            using (var resultsFileWriter = File.CreateText(resultsPath))
            using (var jsonWriter = new JsonWriter(resultsFileWriter, new JsonWriterSettings { Indent = true }))
            {
                jsonWriter.WriteStartArray();

                // write composite scores e.g ReadBench
                foreach (var category in DriverBenchmarkCategory.AllCategories)
                {
                    WriteScoreToResults(jsonWriter, category, CalculateCompositeScore(benchmarkResults, category));
                }

                // write individual benchmarks results
                foreach (var benchmark in benchmarkResults)
                {
                    WriteScoreToResults(jsonWriter, benchmark.Name, benchmark.Score);
                }

                jsonWriter.WriteEndArray();
            }

            return new[] { resultsPath };
        }

        private static void WriteScoreToResults(JsonWriter jsonWriter, string name, double score)
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
    }
}
