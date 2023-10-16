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

    public static byte[] ReadBytes(string resourcePath)
    {
        return File.ReadAllBytes(resourcePath);
    }
}
