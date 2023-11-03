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
    [BenchmarkCategory("ParallelBench", "WriteBench", "DriverBench")]
    public class GridFsMultiFileUpload
    {
        private MongoClient _client;
        private GridFSBucket _gridFsBucket;

        [GlobalSetup]
        public void Setup()
        {
            string mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI");
            _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
            _client.DropDatabase("perftest");
            _gridFsBucket = new GridFSBucket(_client.GetDatabase("perftest"));
        }

        [IterationSetup]
        public void BeforeTask()
        {
            _gridFsBucket.Drop();
            _gridFsBucket.UploadFromBytes("smallfile", new byte[1]);
        }

        [Benchmark]
        public void GridFsMultiUpload()
        {
            Task[] tasks = new Task[50];
            for (int i = 0; i < 50; i++)
            {
                tasks[i] = Task.Factory.StartNew(UploadFile(i));
            }
            Task.WaitAll(tasks);
        }

        [GlobalCleanup]
        public void Teardown()
        {
            _client.DropDatabase("perftest");
        }

        private Action UploadFile(int fileNumber)
        {
            return () =>
            {
                string filename = $"file{fileNumber:D2}.txt";
                string resourcePath = $"../../../../../../../data/parallel/gridfs_multi/{filename}";
                _gridFsBucket.UploadFromStream(filename, File.Open(resourcePath, FileMode.Open));
            };
        }
    }
}
