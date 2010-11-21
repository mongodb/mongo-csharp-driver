using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace MongoDB.Linq.Util
{
    /// <summary>
    /// 
    /// </summary>
    internal static class ReflectionExtensions
    {
        private class MethodDefinition : IEquatable<MethodDefinition>
        {
            private readonly Type declaringType;
            private readonly string methodName;
            private readonly Type[] genericTypes;
            private readonly int hashCode;

            public MethodDefinition(Type declaringType, string methodName, Type[] genericTypes)
            {
                this.declaringType = declaringType;
                this.genericTypes = genericTypes;
                this.methodName = methodName;
                this.hashCode = CalculateHashCode();
            }

            public bool Equals(MethodDefinition other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                if (!Equals(other.declaringType, declaringType)) return false;
                if (!Equals(other.methodName, methodName)) return false;
                if (ReferenceEquals(genericTypes, other.genericTypes)) return true;
                if (genericTypes==null && other.genericTypes==null) return true;
                if (genericTypes!=null && other.genericTypes==null) return false;
                if (genericTypes==null && other.genericTypes!=null) return false;
                if (genericTypes.Length!=other.genericTypes.Length) return false;
                return genericTypes.SequenceEqual(other.genericTypes);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof (MethodDefinition)) return false;
                return Equals((MethodDefinition) obj);
            }

            private int CalculateHashCode()
            {
                unchecked
                {
                    int result = (declaringType != null ? declaringType.GetHashCode() : 0);
                    result = (result*397) ^ (methodName != null ? methodName.GetHashCode() : 0);
                    result = (result*397) ^ (genericTypes != null ? GetHashCode(genericTypes) : 0);
                    return result;
                }
            }

            private int GetHashCode(Type[] types)
            {
                unchecked
                {
                    if (types ==null) return 0;
                    return types.Aggregate<Type,int>(types.Length, (ret, type) => ret*37+type.GetHashCode());
                }
            }
        }

        static readonly Dictionary<MethodDefinition, MethodInfo> dict = new Dictionary<MethodDefinition, MethodInfo>();
        /// <summary>
        /// Gets the custom attribute.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="member">The member.</param>
        /// <param name="inherit">if set to <c>true</c> [inherit].</param>
        /// <returns></returns>
        public static T GetCustomAttribute<T>(this MemberInfo member, bool inherit) where T : Attribute
        {
            var atts = member.GetCustomAttributes(typeof(T), inherit);
            if (atts.Length > 0)
                return (T)atts[0];

            return null;
        }

        /// <summary>
        /// Gets the return type of the member.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns></returns>
        public static Type GetReturnType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                case MemberTypes.Method:
                    return ((MethodInfo)member).ReturnType;
            }

            throw new NotSupportedException("Only fields, properties, and methods are supported.");
        }

        /// <summary>
        /// Determines whether [is open type assignable from] [the specified open type].
        /// </summary>
        /// <param name="openType">Type of the open.</param>
        /// <param name="closedType">Type of the closed.</param>
        /// <returns>
        /// 	<c>true</c> if [is open type assignable from] [the specified open type]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsOpenTypeAssignableFrom(this Type openType, Type closedType)
        {
            if (!openType.IsGenericTypeDefinition)
                throw new ArgumentException("Must be an open generic type.", "openType");
            if (!closedType.IsGenericType || closedType.IsGenericTypeDefinition)
                return false;

            var openArgs = openType.GetGenericArguments();
            var closedArgs = closedType.GetGenericArguments();
            if (openArgs.Length != closedArgs.Length)
                return false;
            try
            {
                var newType = openType.MakeGenericType(closedArgs);
                return newType.IsAssignableFrom(closedType);
            }
            catch
            {
                //we don't really care here, it just means the answer is false.
                return false;
            }
        }

        public static MethodInfo GetGenericMethod(this Type type, string methodName, Type[] genericTypes)
        {
            MethodDefinition def = new MethodDefinition(type, methodName, genericTypes);
            MethodInfo del;
            lock(dict)
            {
                if (dict.TryGetValue(def, out del))
                    return del;
            }
            del = GenerateDelegate(type, methodName, genericTypes);
            lock (dict)
            {
                dict[def] = del;
            }
            return del;
        }

        private static MethodInfo GenerateDelegate(Type type, string methodName, Type[] genericTypes)
        {
            var methods = type.GetMethods();
            genericTypes = genericTypes ?? Type.EmptyTypes;
            var filter = from method in methods
                         where method.Name == methodName &&
                               (
                                ((genericTypes.Length==0) && !method.IsGenericMethodDefinition) ||
                                ((genericTypes.Length > 0) && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == genericTypes.Length
                                )
                                )
                         select method;
            var selectedMethod = filter.Single();
            return selectedMethod.MakeGenericMethod(genericTypes);
        }
    }
}