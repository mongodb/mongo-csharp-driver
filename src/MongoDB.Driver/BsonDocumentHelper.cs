using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    internal static class BsonDocumentHelper
    {
        public static BsonDocument ConvertToBsonDocument(IBsonSerializerRegistry registry, object document)
        {
            if (document == null)
            {
                return null;
            }

            var bsonDocument = document as BsonDocument;
            if (bsonDocument != null)
            {
                return bsonDocument;
            }

            if (document is string)
            {
                return BsonDocument.Parse((string)document);
            }

            var serializer = registry.GetSerializer(document.GetType());
            return new BsonDocumentWrapper(document, serializer);
        }
    }
}
