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

using MongoDB.Bson;
using MongoDB.Benchmarks.MultiDoc;
using MongoDB.Benchmarks.ParallelBench;
using MongoDB.Benchmarks.SingleDoc;
using MongoDB.Driver;
using MongoDB.Driver.TestHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MongoDB.Benchmarks
{
    public static class BenchmarkHelper
    {
        public const string DataFolderPath = "../../../../../../../data/";

        public static BsonDocument ReadExtendedJson(string resourcePath)
        {
            string extendedJson = File.ReadAllText(DataFolderPath + resourcePath);
            return BsonDocument.Parse(extendedJson);
        }

        public static byte[] ReadExtendedJsonToBytes(string resourcePath)
        {
            string extendedJson = File.ReadAllText(DataFolderPath + resourcePath);
            var document = BsonDocument.Parse(extendedJson);
            return document.ToBson();
        }

        public static double CalculateCompositeScore(IEnumerable<BenchmarkResult> benchmarkResults, string benchmarkCategory)
        {
            Func<BenchmarkResult, bool> predicate;
            if (benchmarkCategory == DriverBenchmarkCategory.DriverBench)
            {
                var categoriesToMatch = new List<string>()
                {
                    DriverBenchmarkCategory.ReadBench, DriverBenchmarkCategory.WriteBench
                };
                predicate = benchmark => categoriesToMatch.Any(s => benchmark.Categories.Contains(s)); // select any benchmarks part of the read or write categories
            }
            else
            {
                predicate = benchmark => benchmark.Categories.Contains(benchmarkCategory);
            }

            var identifiedBenchmarksScores = benchmarkResults
                .Where(predicate)
                .Select(benchmark => benchmark.Score).ToList();

            if (identifiedBenchmarksScores.Any())
            {
                return identifiedBenchmarksScores.Average();
            }

            return 0;
        }

        public static int GetDatasetSize(string benchmarkName) =>
            benchmarkName switch
            {
                "FlatBsonEncodingBenchmark" or "FlatBsonDecodingBenchmark" => 75310000,
                "DeepBsonEncodingBenchmark" or "DeepBsonDecodingBenchmark" => 19640000,
                "FullBsonEncodingBenchmark" or "FullBsonDecodingBenchmark" => 57340000,
                nameof(GridFSMultiFileUploadBenchmark) or nameof(GridFSMultiFileDownloadBenchmark) => 262144000,
                nameof(MultiFileImportBenchmark) or nameof(MultiFileExportBenchmark) => 565000000,
                nameof(InsertOneLargeBenchmark) or nameof(InsertManyLargeBenchmark) => 27310890,
                nameof(InsertOneSmallBenchmark) or nameof(InsertManySmallBenchmark) => 2750000,
                nameof(GridFsUploadBenchmark) or nameof(GridFsDownloadBenchmark) => 52428800,
                nameof(FindOneBenchmark) or nameof(FindManyBenchmark) => 16220000,
                nameof(RunCommandBenchmark) => 130000,
                _ => 0
            };

        public static class MongoConfiguration
        {
            public static DisposableMongoClient CreateDisposableClient()
            {
                string mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI");
                var client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
                client.DropDatabase("perftest");

                return  new DisposableMongoClient(client, null);
            }
        }
    }
}
