using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using static benchmarks.BenchmarkExtensions;

namespace benchmarks.BSON
{
    [IterationTime(3000)]
    [BenchmarkCategory("BSONBench")]
    public class DeepBsonEncodingBenchmark
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
            _document = ReadExtendedJson("../../../../../../../data/extended_bson/deep_bson.json");
        }

        [Benchmark]
        public void DeepBsonEncoding()
        {
            for (int i = 0; i < 10000; i++)
            {
                BsonDocumentSerializer.Instance.Serialize(_context, _document);
                _stream.Position = 0;
            }
        }
    }
}
