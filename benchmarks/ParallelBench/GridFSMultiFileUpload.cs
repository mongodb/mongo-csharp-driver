using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace benchmarks.ParallelBench;

[BenchmarkCategory("ParallelBench", "WriteBench", "DriverBench")]
public class GridFsMultiFileUpload
{
    private MongoClient _client;
    private GridFSBucket _gridFsBucket;

    [GlobalSetup]
    public void Setup()
    {
        string mongoUri = Environment.GetEnvironmentVariable("MONGO_URI");
        _client = mongoUri != null ? new MongoClient(mongoUri) : new MongoClient();
        _client.DropDatabase("perftest");
        _gridFsBucket = new GridFSBucket(_client.GetDatabase("perftest"));
    }

    [IterationSetup]
    public void BeforeTask()
    {
        _gridFsBucket.Drop();
        _gridFsBucket.UploadFromBytes("smallfile", new byte[1]);
    }

    [Benchmark]
    public void GridFsMultiUpload()
    {
        Task[] tasks = new Task[50];
        for (int i = 0; i < 50; i++)
        {
            tasks[i] = Task.Factory.StartNew(UploadFile(i));
        }
        Task.WaitAll(tasks);
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _client.DropDatabase("perftest");
    }

    private Action UploadFile(int fileNumber)
    {
        return () =>
        {
            string filename = $"file{fileNumber:D2}.txt";
            string resourcePath = $"../../../../../../../data/parallel/gridfs_multi/{filename}";
            _gridFsBucket.UploadFromStream(filename, File.Open(resourcePath, FileMode.Open));
        };
    }
}
