using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Core.Tests.Helpers
{
    public static class RawBsonDocumentHelper
    {
        public static RawBsonDocument FromBsonDocument(BsonDocument document)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var bsonWriter = new BsonBinaryWriter(memoryStream, BsonBinaryWriterSettings.Defaults))
                {
                    var context = BsonSerializationContext.CreateRoot<BsonDocument>(bsonWriter);
                    BsonDocumentSerializer.Instance.Serialize(context, document);
                }
                return new RawBsonDocument(memoryStream.ToArray());
            }
        }
    }
}