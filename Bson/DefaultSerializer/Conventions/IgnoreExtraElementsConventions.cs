using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public interface IIgnoreExtraElementsConvention {
        bool IgnoreExtraElements(Type type);
    }

    public class NeverIgnoreExtraElementsConvention : IIgnoreExtraElementsConvention {
        public bool IgnoreExtraElements(
            Type type
        ) {
            return false;
        }
    }

    public class AlwaysIgnoreExtraElementsConvention : IIgnoreExtraElementsConvention {
        public bool IgnoreExtraElements(
            Type type
        ) {
            return true ;
        }
    }
}
