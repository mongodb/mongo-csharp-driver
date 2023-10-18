using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace benchmarks.Multi_Doc;

[BenchmarkCategory("MultiBench", "ReadBench", "DriverBench")]
public class GridFsDownloadBenchmark
{
    private ObjectId _fileId;
    private MongoClient _client;
    private GridFSBucket _gridFsBucket;

    [GlobalSetup]
    public void Setup()
    {
        string mongoUri = Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URI");
        _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
        _client.DropDatabase("perftest");
        _gridFsBucket = new GridFSBucket(_client.GetDatabase("perftest"));
        _fileId = _gridFsBucket.UploadFromStream("gridfstest", File.OpenRead("../../../../../../../data/single_and_multi_document/gridfs_large.bin"));
    }

    [IterationSetup]
    public void BeforeTask()
    {
    }

    [Benchmark]
    public byte[] GridFsDownload()
    {
       return  _gridFsBucket.DownloadAsBytes(_fileId);
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _client.DropDatabase("perftest");
    }
}
