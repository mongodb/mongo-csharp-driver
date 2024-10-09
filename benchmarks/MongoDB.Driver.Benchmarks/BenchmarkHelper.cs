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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.Benchmarks
{
    public static class BenchmarkHelper
    {
        public const string DataFolderPath = "../../../../../../../data/";

        public static void AddFilesToQueue(ConcurrentQueue<(string, int)> filesQueue, string directoryPath, string fileNamePrefix, int fileCount)
        {
            var addingLDJSONfiles = fileNamePrefix == "ldjson";
            for (int i = 0; i < fileCount; i++)
            {
                var fileName = addingLDJSONfiles ? $"{fileNamePrefix}{i:D3}.txt" : $"{fileNamePrefix}{i:D2}.txt";
                filesQueue.Enqueue(($"{directoryPath}/{fileName}", i)); // enqueue complete filepath and filenumber
            }
        }

        public static double CalculateCompositeScore(IEnumerable<BenchmarkResult> benchmarkResults, string benchmarkCategory)
        {
            var identifiedBenchmarksScores = benchmarkResults
                .Where(benchmark => benchmark.Categories.Contains(benchmarkCategory))
                .Select(benchmark => benchmark.Score).ToArray();

            if (identifiedBenchmarksScores.Any())
            {
                return identifiedBenchmarksScores.Average();
            }

            return 0;
        }

        public static void CreateEmptyDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            Directory.CreateDirectory(path);
        }

        public static BsonDocument ReadExtendedJson(string resourcePath)
        {
            var extendedJson = File.ReadAllText(DataFolderPath + resourcePath);
            return BsonDocument.Parse(extendedJson);
        }

        public static byte[] ReadExtendedJsonToBytes(string resourcePath)
        {
            var extendedJson = File.ReadAllText(DataFolderPath + resourcePath);
            var document = BsonDocument.Parse(extendedJson);
            return document.ToBson();
        }

        public static class MongoConfiguration
        {
            public const string PerfTestDatabaseName = "perftest";
            public const string PerfTestCollectionName = "corpus";

            public static IMongoClient CreateClient()
            {
                var mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI");
                var settings = mongoUri != null ? MongoClientSettings.FromConnectionString(mongoUri) : new();
                settings.ClusterSource = DisposingClusterSource.Instance;

                var client = new MongoClient(settings);
                client.DropDatabase(PerfTestDatabaseName);

                return client;
            }
        }
    }
}
