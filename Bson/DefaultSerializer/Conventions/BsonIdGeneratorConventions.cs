using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public interface IBsonIdGeneratorConvention {
        IBsonIdGenerator GetBsonIdGenerator(PropertyInfo property); 
    }

    public class BsonSerializerBsonIdGeneratorConvention : IBsonIdGeneratorConvention {
        public IBsonIdGenerator GetBsonIdGenerator(
            PropertyInfo property
        ) {
            return BsonSerializer.LookupIdGenerator(property.PropertyType);
        }
    }
}