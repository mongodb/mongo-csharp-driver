/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class TypeExtensions
    {
#if NETSTANDARD1_5
        public static ConstructorInfo GetConstructor(this Type type, Type[] types)
        {
            return type.GetTypeInfo().GetConstructor(types);
        }
#endif

#if NETSTANDARD1_5
        public static ConstructorInfo[] GetConstructors(this Type type)
        {
            return type.GetTypeInfo().GetConstructors();
        }
#endif

#if NETSTANDARD1_5
        public static Type GetEnumUnderlyingType(this Type type)
        {
            return type.GetTypeInfo().GetEnumUnderlyingType();
        }
#endif

#if NETSTANDARD1_5
        public static Type[] GetGenericArguments(this Type type)
        {
            return type.GetTypeInfo().GetGenericArguments();
        }
#endif

        public static Type GetIEnumerableGenericInterface(this Type enumerableType)
        {
            if (enumerableType.TryGetIEnumerableGenericInterface(out var ienumerableGenericInterface))
            {
                return ienumerableGenericInterface;
            }

            throw new InvalidOperationException($"Could not find IEnumerable<T> interface of type: {enumerableType}.");
        }

#if NETSTANDARD1_5
        public static Type[] GetInterfaces(this Type type)
        {
            return type.GetTypeInfo().GetInterfaces();
        }
#endif

#if NETSTANDARD1_5
        public static PropertyInfo GetProperty(this Type type, string name)
        {
            return type.GetTypeInfo().GetProperty(name);
        }
#endif

        public static bool Implements(this Type type, Type @interface)
        {
            Type interfaceDefinition = null;
            if (@interface.IsGenericType())
            {
                interfaceDefinition = @interface.GetGenericTypeDefinition();
            }

            foreach (var implementedInterface in type.GetInterfaces())
            {
                if (implementedInterface == @interface)
                {
                    return true;
                }

                if (implementedInterface.IsGenericType() && implementedInterface.GetGenericTypeDefinition() == interfaceDefinition)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Is(this Type type, Type comparand)
        {
            if (type == comparand)
            {
                return true;
            }

            if (type.IsGenericType() && comparand.IsGenericTypeDefinition())
            {
                if (type.GetGenericTypeDefinition() == comparand)
                {
                    return true;
                }
            }

            return false;
        }

#if NETSTANDARD1_5
        public static bool IsAssignableFrom(this Type type, Type c)
        {
            return type.GetTypeInfo().IsAssignableFrom(c);
        }
#endif

        public static bool IsEnum(this Type type)
        {
#if NETSTANDARD1_5
            return type.GetTypeInfo().IsEnum;
#else
            return type.IsEnum;
#endif
        }

        public static bool IsGenericType(this Type type)
        {
#if NETSTANDARD1_5
            return type.GetTypeInfo().IsGenericType;
#else
            return type.IsGenericType;
#endif
        }

        public static bool IsGenericTypeDefinition(this Type type)
        {
#if NETSTANDARD1_5
            return type.GetTypeInfo().IsGenericTypeDefinition;
#else
            return type.IsGenericType;
#endif
        }

        public static bool TryGetIDictionaryGenericInterface(this Type type, out Type idictionaryGenericInterface)
        {
            foreach (var interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType() && interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    idictionaryGenericInterface = interfaceType;
                    return true;
                }
            }

            idictionaryGenericInterface = null;
            return false;
        }

        public static bool TryGetIEnumerableGenericInterface(this Type type, out Type ienumerableGenericInterface)
        {
            foreach (var interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType() && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    ienumerableGenericInterface = interfaceType;
                    return true;
                }
            }

            ienumerableGenericInterface = null;
            return false;
        }
    }
}
