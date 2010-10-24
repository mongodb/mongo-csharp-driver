using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public interface IDefaultValueConvention {
        object GetDefaultValue(PropertyInfo property);
    }

    public class NullDefaultValueConvention : IDefaultValueConvention {
        public object GetDefaultValue(
            PropertyInfo property
        ) {
            return null;
        }
    }
}