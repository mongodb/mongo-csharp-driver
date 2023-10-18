using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using static benchmarks.BenchmarkExtensions;

namespace benchmarks.Multi_Doc;

[BenchmarkCategory("MultiBench", "WriteBench", "DriverBench")]
public class GridFsUploadBenchmark
{
    private byte[] _fileBytes;
    private MongoClient _client;
    private GridFSBucket _gridFsBucket;

    [GlobalSetup]
    public void Setup()
    {
        string mongoUri = Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URI");
        _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
        _client.DropDatabase("perftest");
        _fileBytes = File.ReadAllBytes("../../../../../../../data/single_and_multi_document/gridfs_large.bin");
        _gridFsBucket = new GridFSBucket(_client.GetDatabase("perftest"));
    }

    [IterationSetup]
    public void BeforeTask()
    {
        _gridFsBucket.Drop();
        _gridFsBucket.UploadFromBytes("smallfile", new byte[1]);
    }

    [Benchmark]
    public ObjectId GridFsUpload()
    {
        return _gridFsBucket.UploadFromBytes("gridfstest", _fileBytes);
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _client.DropDatabase("perftest");
    }
}
