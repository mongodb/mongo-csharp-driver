using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public interface ISerializeDefaultValueConvention {
        bool SerializeDefaultValue(PropertyInfo property);
    }

    public class NeverSerializeDefaultValueConvention : ISerializeDefaultValueConvention {
        public bool SerializeDefaultValue(
            PropertyInfo property
        ) {
            return false;
        }
    }

    public class AlwaysSerializeDefaultValueConvention : ISerializeDefaultValueConvention {
        public bool SerializeDefaultValue(
            PropertyInfo property
        ) {
            return true ;
        }
    }
}
