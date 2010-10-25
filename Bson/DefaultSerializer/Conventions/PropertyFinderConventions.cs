using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public interface IPropertyFinderConvention{
        IEnumerable<PropertyInfo> FindProperties(Type type);
    }

    public class PublicPropertyFinderConvention : IPropertyFinderConvention {
        public IEnumerable<PropertyInfo> FindProperties(
            Type type
        ) {
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            return type.GetProperties(bindingFlags);
        }
    }

}