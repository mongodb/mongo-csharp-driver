using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public interface IUseCompactRepresentationConvention {
        bool UseCompactRepresentation(Type type);
    }

    public class NeverUseCompactRepresentationConvention : IUseCompactRepresentationConvention {
        public bool UseCompactRepresentation(
            Type type
        ) {
            return false;
        }
    }

    public class AlwaysUseCompactRepresentationConvention : IUseCompactRepresentationConvention {
        public bool UseCompactRepresentation(
            Type type
        ) {
            return true ;
        }
    }
}
