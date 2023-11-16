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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using BenchmarkDotNet.Attributes;

namespace MongoDB.Benchmarks.MultiDoc
{
    [IterationTime(3000)]
    [BenchmarkCategory(DriverBenchmarkCategory.MultiBench, DriverBenchmarkCategory.ReadBench, DriverBenchmarkCategory.DriverBench)]
    public class GridFsDownloadBenchmark
    {
        private ObjectId _fileId;
        private MongoClient _client;
        private GridFSBucket _gridFsBucket;

        [GlobalSetup]
        public void Setup()
        {
            string mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI");
            _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
            _client.DropDatabase("perftest");
            _gridFsBucket = new GridFSBucket(_client.GetDatabase("perftest"));
            _fileId = _gridFsBucket.UploadFromStream("gridfstest", File.OpenRead("../../../../../../../data/single_and_multi_document/gridfs_large.bin"));
        }

        [Benchmark]
        public void GridFsDownload()
        {
            _gridFsBucket.DownloadAsBytes(_fileId);
        }

        [GlobalCleanup]
        public void Teardown()
        {
            _client.DropDatabase("perftest");
        }
    }
}
