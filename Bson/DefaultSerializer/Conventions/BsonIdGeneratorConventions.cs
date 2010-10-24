using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public interface IBsonIdGeneratorConvention {
        IBsonIdGenerator GetBsonIdGenerator(Type type); 
    }

    public class BsonSerializerBsonIdGeneratorConvention : IBsonIdGeneratorConvention {
        public IBsonIdGenerator GetBsonIdGenerator(
            Type type
        ) {
            return BsonSerializer.LookupIdGenerator(type);
        }
    }
}