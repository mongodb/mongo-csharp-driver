using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public interface IIdPropertyConvention {
        BsonPropertyMap FindIdPropertyMap(IEnumerable<BsonPropertyMap> propertyMaps); 
    }

    public class NamedIdPropertyConvention : IIdPropertyConvention {
        public string Name { get; private set; }

        public NamedIdPropertyConvention(
            string name
        ) {
            Name = name;
        }

        public BsonPropertyMap FindIdPropertyMap(
            IEnumerable<BsonPropertyMap> propertyMaps
        ) {
            return propertyMaps.FirstOrDefault(x => x.PropertyName == Name);
        }
    }
}