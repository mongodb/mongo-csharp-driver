using System.IO;
using MongoDB.Bson;

namespace benchmarks;
public static class BenchmarkExtensions
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

    public static int GetDatasetSize(string benchmarkName)
    {
        return benchmarkName switch
        {
            "GridFsMultiUpload" or "GridFsMultiDownload" => 262144000,
            "MultiFileImport" or "MultiFileExport" => 565000000,
            "FlatBsonEncoding" or "FlatBsonDecoding" => 75310000,
            "DeepBsonEncoding" or "DeepBsonDecoding" => 19640000,
            "FullBsonEncoding" or "FullBsonDecoding" => 57340000,
            "FindOne" or "FindManyAndEmptyCursor" => 16220000,
            "InsertOneLarge" or "InsertManyLarge" => 27310890,
            "InsertOneSmall" or "InsertManySmall" => 2750000,
            "GridFsUpload" or "GridFsDownload" => 52428800,
            "RunCommand" => 130000,
            _ => 0
        };
    }
}
