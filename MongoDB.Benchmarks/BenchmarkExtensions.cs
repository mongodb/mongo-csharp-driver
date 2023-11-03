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
using MongoDB.Bson;
using System.Collections.Generic;

namespace MongoDB.Benchmarks
{
    public static class BenchmarkExtensions
    {
        public static IReadOnlyList<string> SingleBenchmarks { get; } =
            new [] { "FindOne", "InsertOneLarge", "InsertOneSmall" };

        public static IReadOnlyList<string> ParallelBenchmarks { get; } = new []
        {
            "MultiFileImport",
            "MultiFileExport",
            "GridFsMultiUpload",
            "GridFsMultiDownload"
        };

        public static IReadOnlyList<string> ReadBenchmarks { get; } = new []
        {
            "FindOne",
            "FindManyAndEmptyCursor",
            "GridFsDownload",
            "GridFsMultiDownload",
            "MultiFileExport"
        };

        public static IReadOnlyList<string> MultiBenchmarks { get; } = new []
        {
            "InsertManyLarge",
            "InsertManySmall",
            "GridFsUpload",
            "GridFsDownload",
            "FindManyAndEmptyCursor"
        };

        public static IReadOnlyList<string> BsonBenchmarks { get; } = new []
        {
            "FlatBsonEncoding",
            "FlatBsonDecoding",
            "DeepBsonEncoding",
            "DeepBsonDecoding",
            "FullBsonEncoding",
            "FullBsonDecoding"
        };

        public static IReadOnlyList<string> WriteBenchmarks { get; } = new []
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

        public static int GetDatasetSize(string benchmarkName) =>
            benchmarkName switch
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
