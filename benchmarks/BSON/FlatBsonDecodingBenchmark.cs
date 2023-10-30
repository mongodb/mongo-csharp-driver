using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using static benchmarks.BenchmarkExtensions;

namespace benchmarks.BSON
{
    [IterationTime(3000)]
    [BenchmarkCategory("BSONBench")]
    public class FlatBsonDecodingBenchmark
    {
        private byte[] _bytes;
        private MemoryStream _stream;
        private BsonBinaryReader _reader;
        private BsonDeserializationContext _context;

        [GlobalSetup]
        public void Setup()
        {
            _bytes = ReadExtendedJsonToBytes("../../../../../../../data/extended_bson/flat_bson.json");
            _stream = new MemoryStream(_bytes);
            _reader = new BsonBinaryReader(_stream);
            _context = BsonDeserializationContext.CreateRoot(_reader);
        }

        [Benchmark]
        public void FlatBsonDecoding()
        {
            for (int i = 0; i < 10000; i++)
            {
                BsonDocumentSerializer.Instance.Deserialize(_context);
                _stream.Position = 0;
            }
        }
    }
}
