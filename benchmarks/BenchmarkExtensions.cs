using System.IO;
using MongoDB.Bson;
using System.Collections.Generic;

namespace benchmarks
{
    public static class BenchmarkExtensions
    {
        public static IReadOnlyList<string> SingleBenchmarks { get; } =
            new List<string> { "FindOne", "InsertOneLarge", "InsertOneSmall" };

        public static IReadOnlyList<string> ParallelBenchmarks { get; } = new List<string>
        {
            "MultiFileImport",
            "MultiFileExport",
            "GridFsMultiUpload",
            "GridFsMultiDownload"
        };

        public static IReadOnlyList<string> ReadBenchmarks { get; } = new List<string>
        {
            "FindOne",
            "FindManyAndEmptyCursor",
            "GridFsDownload",
            "GridFsMultiDownload",
            "MultiFileExport"
        };

        public static IReadOnlyList<string> MultiBenchmarks { get; } = new List<string>
        {
            "InsertManyLarge",
            "InsertManySmall",
            "GridFsUpload",
            "GridFsDownload",
            "FindManyAndEmptyCursor"
        };

        public static IReadOnlyList<string> BsonBenchmarks { get; } = new List<string>
        {
            "FlatBsonEncoding",
            "FlatBsonDecoding",
            "DeepBsonEncoding",
            "DeepBsonDecoding",
            "FullBsonEncoding",
            "FullBsonDecoding"
        };

        public static IReadOnlyList<string> WriteBenchmarks { get; } = new List<string>
        {
            "MultiFileImport",
            "GridFsMultiUpload",
            "GridFsUpload",
            "InsertManyLarge",
            "InsertManySmall",
            "InsertOneLarge",
            "InsertOneSmall"
        };

        public static BsonDocument ReadExtendedJson(string resourcePath)
        {
            string extendedJson = File.ReadAllText(resourcePath);
            return BsonDocument.Parse(extendedJson);
        }

        public static byte[] ReadExtendedJsonToBytes(string resourcePath)
        {
            string extendedJson = File.ReadAllText(resourcePath);
            var document = BsonDocument.Parse(extendedJson);
            return document.ToBson();
        }

        public static int GetDatasetSize(string benchmarkName)
        {
            return benchmarkName switch
            {
                "GridFsMultiUpload" or "GridFsMultiDownload" => 262144000,
                "FlatBsonEncoding" or "FlatBsonDecoding" => 75310000,
                "DeepBsonEncoding" or "DeepBsonDecoding" => 19640000,
                "FullBsonEncoding" or "FullBsonDecoding" => 57340000,
                "MultiFileImport" or "MultiFileExport" => 565000000,
                "FindOne" or "FindManyAndEmptyCursor" => 16220000,
                "InsertOneLarge" or "InsertManyLarge" => 27310890,
                "InsertOneSmall" or "InsertManySmall" => 2750000,
                "GridFsUpload" or "GridFsDownload" => 52428800,
                "RunCommand" => 130000,
                _ => 0
            };
        }
    }
}
