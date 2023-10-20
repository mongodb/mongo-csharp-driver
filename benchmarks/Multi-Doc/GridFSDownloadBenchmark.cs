using System;
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using BenchmarkDotNet.Attributes;

namespace benchmarks.Multi_Doc;

[IterationTime(3000)]
[BenchmarkCategory("MultiBench", "ReadBench", "DriverBench")]
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
