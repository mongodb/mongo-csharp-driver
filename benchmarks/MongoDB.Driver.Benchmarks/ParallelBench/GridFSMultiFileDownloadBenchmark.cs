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

using BenchmarkDotNet.Attributes;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.TestHelpers;
using System.IO;

using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.ParallelBench
{
    [IterationCount(100)]
    [BenchmarkCategory(DriverBenchmarkCategory.ParallelBench, DriverBenchmarkCategory.ReadBench, DriverBenchmarkCategory.DriverBench)]
    public class GridFSMultiFileDownloadBenchmark
    {
        private DisposableMongoClient _client;
        private GridFSBucket _gridFsBucket;
        private DirectoryInfo _tmpDirectory;

        [GlobalSetup]
        public void Setup()
        {
            _client = MongoConfiguration.CreateDisposableClient();
            _gridFsBucket = new GridFSBucket(_client.GetDatabase("perftest"));
            _gridFsBucket.Drop();
            _tmpDirectory = Directory.CreateDirectory($"{DataFolderPath}parallel/tmpGridFS");

            PopulateDatabase();
        }

        [IterationSetup]
        public void BeforeTask()
        {
            ClearDirectory();
        }

        [Benchmark]
        public void GridFsMultiDownload()
        {
            ThreadingUtilities.ExecuteOnNewThreads(50, i =>
            {
                string filename = $"file{i:D2}.txt";
                string resourcePath = $"{DataFolderPath}parallel/tmpGridFS/{filename}";

                using var file = File.Create(resourcePath);
                _gridFsBucket.DownloadToStreamByName(filename, file);
            });
        }

        [GlobalCleanup]
        public void Teardown()
        {
            ClearDirectory();
            _tmpDirectory.Delete();
            _client.Dispose();
        }

        private void ClearDirectory()
        {
            foreach (var file in _tmpDirectory.EnumerateFiles())
            {
                file.Delete();
            }
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
