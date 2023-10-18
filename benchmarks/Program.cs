using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using MongoDB.Bson.IO;

namespace benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IReadOnlyList<string> singleBenchmarks = new List<string> { "FindOne", "InsertOneLarge", "InsertOneSmall"};
            IReadOnlyList<string> parallelBenchmarks = new List<string> { "MultiFileImport", "MultiFileExport", "GridFsMultiUpload", "GridFsMultiDownload" };
            IReadOnlyList<string> readBenchmarks = new List<string> { "FindOne", "FindManyAndEmptyCursor", "GridFsDownload", "GridFsMultiDownload", "MultiFileExport" };
            IReadOnlyList<string> multiBenchmarks = new List<string> { "InsertManyLarge", "InsertManySmall", "GridFsUpload", "GridFsDownload", "FindManyAndEmptyCursor" };
            IReadOnlyList<string> bsonBenchmarks = new List<string> { "FlatBsonEncoding", "FlatBsonDecoding", "DeepBsonEncoding", "DeepBsonDecoding", "FullBsonEncoding", "FullBsonDecoding" };
            IReadOnlyList<string> writeBenchmarks = new List<string> { "MultiFileImport", "GridFsMultiUpload", "GridFsUpload", "InsertManyLarge", "InsertManySmall", "InsertOneLarge", "InsertOneSmall" };

            // Use this to select benchmarks from the console:
            List<BenchmarkResult> benchmarkResults = new();
            var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, DefaultConfig.Instance);
            foreach (var summary in summaries)
            {
                string benchmarkName = summary.Reports.Single().BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo;
                benchmarkResults.Add(new BenchmarkResult(summary.Reports.Single(), BenchmarkExtensions.GetDatasetSize(benchmarkName)));
            }

            if (File.Exists("results.json"))
            {
                File.Delete("results.json");
            }

            var jsonWriter = new JsonWriter(File.CreateText("results.json"), new JsonWriterSettings() {Indent = true});

            double bsonBenchScore = GetCompositeScore(bsonBenchmarks);
            double readBenchScore = GetCompositeScore(readBenchmarks);
            double multiBenchScore = GetCompositeScore(multiBenchmarks);
            double writeBenchScore = GetCompositeScore(writeBenchmarks);
            double singleBenchScore = GetCompositeScore(singleBenchmarks);
            double parallelBenchScore = GetCompositeScore(parallelBenchmarks);
            double driverBenchScore = (readBenchScore + writeBenchScore) / 2;

            jsonWriter.WriteStartArray();
            Console.WriteLine("\nScores Summaries: ");
            OutputCompositeScoreToResults("BSONBench", bsonBenchScore);
            ConsoleOutput("BSONBench", bsonBenchScore);

            OutputCompositeScoreToResults("ReadBench", readBenchScore);
            ConsoleOutput("ReadBench", readBenchScore);

            OutputCompositeScoreToResults("MultiBench", multiBenchScore);
            ConsoleOutput("MultiBench", multiBenchScore);

            OutputCompositeScoreToResults("WriteBench", writeBenchScore);
            ConsoleOutput("WriteBench", writeBenchScore);

            OutputCompositeScoreToResults("SingleBench", singleBenchScore);
            ConsoleOutput("SingleBench", singleBenchScore);

            OutputCompositeScoreToResults("ParallelBench", parallelBenchScore);
            ConsoleOutput("ParallelBench", parallelBenchScore);

            OutputCompositeScoreToResults("DriverBench", driverBenchScore);
            ConsoleOutput("DriverBench", driverBenchScore);

            foreach (var benchmark in benchmarkResults)
            {
                OutputIndividualBenchmarkToResults(benchmark);
                ConsoleOutput(benchmark.Name, benchmark.Score);
            }

            jsonWriter.WriteEndArray();
            jsonWriter.Close();

            return;


            double GetCompositeScore(IReadOnlyList<string> benchmarkGroup)
            {
                var identifiedBenchmarks = benchmarkResults.Where(bench => benchmarkGroup.Contains(bench.Name)).Select(bench => bench.Score).ToList();
                if (identifiedBenchmarks.Any())
                {
                    return identifiedBenchmarks.Average();
                }
                return 0;
            }

            void ConsoleOutput(string benchname, double score)
            {
                Console.WriteLine(score != 0
                    ? $"Executed {benchname}, score: {score:F3} MB/s"
                    : $"Skipped {benchname}");
            }

            void OutputCompositeScoreToResults(string name, double score)
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

            void OutputIndividualBenchmarkToResults(BenchmarkResult benchmarkResult)
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

                foreach (int percentile in new[]{50, 80, 90, 95, 99})
                {
                    jsonWriter.WriteStartDocument();
                    jsonWriter.WriteString("name", $"{percentile}TH PERCENTILE");
                    jsonWriter.WriteString("type", $"PERCENTILE_{percentile}TH");
                    jsonWriter.WriteDouble("value", benchmarkResult.GetPercentile(percentile) / 1000);
                    jsonWriter.WriteEndDocument();
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WriteEndDocument();
            }
        }
    }
}
