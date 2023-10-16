using System.IO;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using static benchmarks.BenchmarkExtensions;

namespace benchmarks.BSON;

[BenchmarkCategory("BSONBench")]
public class FullBsonDecodingBenchmark
{
    private byte[] _bytes;
    private MemoryStream _stream;
    private BsonDocument _document;
    private BsonBinaryReader _reader;
    private BsonDeserializationContext _context;

    [GlobalSetup]
    public void Setup()
    {
        _bytes = ReadExtendedJsonToBytes("../../../../../../../data/extended_bson/full_bson.json");
        _stream = new MemoryStream(_bytes);
        _reader = new BsonBinaryReader(_stream);
        _context = BsonDeserializationContext.CreateRoot(_reader);
    }

    [Benchmark]
    public BsonDocument FullBsonDecoding()
    {
        for (int i = 0; i < 10000; i++)
        {
            _document = BsonDocumentSerializer.Instance.Deserialize(_context);
            _stream.Position = 0;
        }
        return _document;
    }
}
