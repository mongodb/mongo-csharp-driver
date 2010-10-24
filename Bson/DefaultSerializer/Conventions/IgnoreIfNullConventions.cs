using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public interface IIgnoreIfNullConvention {
        bool IgnoreIfNull(PropertyInfo property);
    }

    public class NeverIgnoreIfNullConvention : IIgnoreIfNullConvention {
        public bool IgnoreIfNull(
            PropertyInfo property
        ) {
            return false;
        }
    }

    public class AlwaysIgnoreIfNullConvention : IIgnoreIfNullConvention {
        public bool IgnoreIfNull(
            PropertyInfo property
        ) {
            return true ;
        }
    }
}
