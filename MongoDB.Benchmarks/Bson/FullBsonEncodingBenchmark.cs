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
using MongoDB.Bson.IO;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using static MongoDB.Benchmarks.BenchmarkExtensions;

namespace MongoDB.Benchmarks.Bson
{
    [IterationTime(3000)]
    [BenchmarkCategory("BSONBench")]
    public class FullBsonEncodingBenchmark
    {
        private MemoryStream _stream;
        private BsonDocument _document;
        private BsonBinaryWriter _writer;
        private BsonSerializationContext _context;

        [GlobalSetup]
        public void Setup()
        {
            _stream = new MemoryStream();
            _writer = new BsonBinaryWriter(_stream);
            _context = BsonSerializationContext.CreateRoot(_writer);
            _document = ReadExtendedJson("../../../../../../../data/extended_bson/full_bson.json");
        }

        [Benchmark]
        public void FullBsonEncoding()
        {
            for (int i = 0; i < 10000; i++)
            {
                BsonDocumentSerializer.Instance.Serialize(_context, _document);
                _stream.Position = 0;
            }
        }
    }
}
