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

using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using static MongoDB.Benchmarks.BenchmarkHelper;

namespace MongoDB.Benchmarks.Bson
{
    [IterationTime(3000)]
    [BenchmarkCategory(DriverBenchmarkCategory.BsonBench)]
    public class BsonDecodingBenchmark
    {
        private byte[] _bytes;
        private BsonDeserializationContext _context;
        private BsonBinaryReader _reader;
        private MemoryStream _stream;

        [ParamsSource(nameof(BenchmarkDataSources))]
        public BsonBenchmarkData BenchmarkData { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _bytes = ReadExtendedJsonToBytes(BenchmarkData.FilePath);
            _stream = new MemoryStream(_bytes);
            _reader = new BsonBinaryReader(_stream);
            _context = BsonDeserializationContext.CreateRoot(_reader);
        }

        [Benchmark]
        public void BsonDecoding()
        {
            for (int i = 0; i < 10000; i++)
            {
                BsonDocumentSerializer.Instance.Deserialize(_context);
                _stream.Position = 0;
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _reader.Dispose();
            _stream.Dispose();
        }

        public IEnumerable<BsonBenchmarkData> BenchmarkDataSources() => new BsonBenchmarkData[]
        {
            new("extended_bson/flat_bson.json", "Flat", 75_310_000),
            new("extended_bson/full_bson.json", "Full", 57_340_000),
            new("extended_bson/deep_bson.json", "Deep", 19_640_000)
        };
    }
}
