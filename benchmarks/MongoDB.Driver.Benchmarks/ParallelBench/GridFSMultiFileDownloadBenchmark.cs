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

using System.Collections.Concurrent;
using System.IO;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.ParallelBench
{
    [IterationCount(100)]
    [BenchmarkCategory(DriverBenchmarkCategory.ParallelBench, DriverBenchmarkCategory.ReadBench, DriverBenchmarkCategory.DriverBench)]
    public class GridFSMultiFileDownloadBenchmark
    {
        private IMongoClient _client;
        private GridFSBucket _gridFsBucket;
        private string _tmpDirectoryPath;
        private ConcurrentQueue<(string, int)> _filesToDownload;

        [Params(262_144_000)]
        public int BenchmarkDataSetSize { get; set; } // used in BenchmarkResult.cs

        [GlobalSetup]
        public void Setup()
        {
            _client = MongoConfiguration.CreateClient();
            _gridFsBucket = new GridFSBucket(_client.GetDatabase(MongoConfiguration.PerfTestDatabaseName));
            _gridFsBucket.Drop();
            _tmpDirectoryPath = $"{DataFolderPath}parallel/tmpGridFS";
            _filesToDownload = new ConcurrentQueue<(string, int)>();

            PopulateDatabase();
        }

        [IterationSetup]
        public void BeforeTask()
        {
            CreateEmptyDirectory(_tmpDirectoryPath);
            AddFilesToQueue(_filesToDownload, _tmpDirectoryPath, "file", 50);
        }

        [Benchmark]
        public void GridFsMultiDownload()
        {
            ThreadingUtilities.ExecuteOnNewThreads(16, _ =>
            {
                while (_filesToDownload.TryDequeue(out var fileToDownloadInfo))
                {
                    var filename = $"file{fileToDownloadInfo.Item2:D2}.txt";
                    var resourcePath = fileToDownloadInfo.Item1;

                    using (var file = File.Create(resourcePath))
                    {
                        _gridFsBucket.DownloadToStreamByName(filename, file);
                    }
                }
            });
        }

        [GlobalCleanup]
        public void Teardown()
        {
            Directory.Delete(_tmpDirectoryPath, true);
            _client.Dispose();
        }

        private void PopulateDatabase()
        {
            for (int i = 0; i < 50; i++)
            {
                var filename = $"file{i:D2}.txt";
                var resourcePath = $"{DataFolderPath}parallel/gridfs_multi/{filename}";

                using var file = File.Open(resourcePath, FileMode.Open);
                _gridFsBucket.UploadFromStream(filename, file);
            }
        }
    }
}
