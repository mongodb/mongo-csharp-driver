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

namespace MongoDB.Benchmarks
{
    public static class BenchmarkHelper
    {
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
                "GridFSMultiFileUploadBenchmark" or "GridFSMultiFileDownloadBenchmark" => 262144000,
                "FlatBsonEncodingBenchmark" or "FlatBsonDecodingBenchmark" => 75310000,
                "DeepBsonEncodingBenchmark" or "DeepBsonDecodingBenchmark" => 19640000,
                "FullBsonEncodingBenchmark" or "FullBsonDecodingBenchmark" => 57340000,
                "MultiFileImportBenchmark" or "MultiFileExportBenchmark" => 565000000,
                "InsertOneLargeBenchmark" or "InsertManyLargeBenchmark" => 27310890,
                "InsertOneSmallBenchmark" or "InsertManySmallBenchmark" => 2750000,
                "GridFsUploadBenchmark" or "GridFsDownloadBenchmark" => 52428800,
                "FindOneBenchmark" or "FindManyBenchmark" => 16220000,
                "RunCommandBenchmark" => 130000,
                _ => 0
            };
    }
}
