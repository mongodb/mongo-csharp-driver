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

using System;
using System.IO;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace MongoDB.Benchmarks.ParallelBench
{
    [IterationCount(100)]
    [BenchmarkCategory("ParallelBench", "ReadBench", "DriverBench")]
    public class GridFsMultiFileDownload
    {
        private MongoClient _client;
        private GridFSBucket _gridFsBucket;
        private DirectoryInfo _tmpDirectory;

        [GlobalSetup]
        public void Setup()
        {
            string mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI");
            _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
            _client.DropDatabase("perftest");
            _gridFsBucket = new GridFSBucket(_client.GetDatabase("perftest"));
            _gridFsBucket.Drop();
            _tmpDirectory = Directory.CreateDirectory("../../../../../../../data/parallel/tmpGridFS");
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
            Task[] tasks = new Task[50];
            for (int i = 0; i < 50; i++)
            {
                tasks[i] = Task.Factory.StartNew(DownloadFile(i));
            }
            Task.WaitAll(tasks);
        }

        [GlobalCleanup]
        public void Teardown()
        {
            _client.DropDatabase("perftest");
            ClearDirectory();
            _tmpDirectory.Delete();
        }

        private void ClearDirectory()
        {
            foreach (var file in _tmpDirectory.EnumerateFiles())
            {
                file.Delete();
            }
        }

        private Action DownloadFile(int fileNumber)
        {
            return () =>
            {
                string filename = $"file{fileNumber:D2}.txt";
                string resourcePath = $"../../../../../../../data/parallel/tmpGridFS/{filename}";
                _gridFsBucket.DownloadToStreamByName(filename, File.Create(resourcePath));
            };
        }

        private void PopulateDatabase()
        {
            for (int i = 0; i < 50; i++)
            {
                string filename = $"file{i:D2}.txt";
                string resourcePath = $"../../../../../../../data/parallel/gridfs_multi/{filename}";
                _gridFsBucket.UploadFromStream(filename, File.Open(resourcePath, FileMode.Open));
            }
        }
    }
}
