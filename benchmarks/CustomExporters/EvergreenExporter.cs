using System.IO;
using System.Linq;
using MongoDB.Bson.IO;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Exporters;
using System.Collections.Generic;
using static benchmarks.BenchmarkExtensions;

namespace benchmarks.CustomExporters
{
    public class EvergreenExporter : IExporter
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
            List<BenchmarkResult> benchmarkResults =
                (from report in summary.Reports
                    let benchmarkName = report.BenchmarkCase.Descriptor.WorkloadMethod.Name
                    select new BenchmarkResult(report, GetDatasetSize(benchmarkName)))
                .ToList();

            string resultsPath = Path.Combine(summary.ResultsDirectoryPath, _outputFile);
            if (File.Exists(resultsPath))
            {
                File.Delete(resultsPath);
            }

            using (var jsonWriter = new JsonWriter(File.CreateText(resultsPath), new JsonWriterSettings { Indent = true }))
            {
                var compositeScore = new CompositeScore(benchmarkResults);
                double bsonBenchScore = compositeScore.GetScore(BsonBenchmarks);
                double readBenchScore = compositeScore.GetScore(ReadBenchmarks);
                double multiBenchScore = compositeScore.GetScore(MultiBenchmarks);
                double writeBenchScore = compositeScore.GetScore(WriteBenchmarks);
                double singleBenchScore = compositeScore.GetScore(SingleBenchmarks);
                double parallelBenchScore = compositeScore.GetScore(ParallelBenchmarks);
                double driverBenchScore = (readBenchScore + writeBenchScore) / 2;

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
