using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MongoDB.Bson.DefaultSerializer.Conventions
{
    public interface IElementNameConvention {
        string GetElementName(MemberInfo member);
    }

    public class MemberNameElementNameConvention : IElementNameConvention {
        public string GetElementName(
            MemberInfo member
        ) {
            return member.Name;
        }
    }

    public class CamelCaseElementNameConvention : IElementNameConvention {
        public string GetElementName(
            MemberInfo member
        ) {
            string name = member.Name;
            return Char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }

}
