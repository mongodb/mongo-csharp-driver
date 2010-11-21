using System.Linq;
using MongoDB.Driver;

namespace MongoDB.Linq
{
    internal interface IMongoQueryable : IQueryable
    {
        string CollectionName { get; }

        MongoDatabase Database { get; }

        MongoQueryObject GetQueryObject();
    }
}