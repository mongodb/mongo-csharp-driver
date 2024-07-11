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
using System.Linq;
using System.Runtime.CompilerServices;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class TypeExtensions
    {
        private static readonly Type[] __dictionaryInterfaces =
        {
            typeof(IDictionary<,>),
            typeof(IReadOnlyDictionary<,>)
        };

        private static Type[] __tupleTypeDefinitions =
        {
            typeof(Tuple<>),
            typeof(Tuple<,>),
            typeof(Tuple<,,>),
            typeof(Tuple<,,,>),
            typeof(Tuple<,,,,>),
            typeof(Tuple<,,,,,>),
            typeof(Tuple<,,,,,,>),
            typeof(Tuple<,,,,,,,>)
        };

        private static Type[] __valueTupleTypeDefinitions =
        {
            typeof(ValueTuple<>),
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
            typeof(ValueTuple<,,,,,,,>)
        };

        public static Type GetIEnumerableGenericInterface(this Type enumerableType)
        {
            if (enumerableType.TryGetIEnumerableGenericInterface(out var ienumerableGenericInterface))
            {
                return ienumerableGenericInterface;
            }

            throw new InvalidOperationException($"Could not find IEnumerable<T> interface of type: {enumerableType}.");
        }

        public static bool Implements(this Type type, Type @interface)
        {
            if (type == @interface)
            {
                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == @interface)
            {
                return true;
            }

            foreach (var implementedInterface in type.GetInterfaces())
            {
                if (implementedInterface == @interface)
                {
                    return true;
                }

                if (implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == @interface)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ImplementsDictionaryInterface(this Type type, out Type keyType, out Type valueType)
        {
            if (TryGetGenericInterface(type, __dictionaryInterfaces, out var dictionaryInterface))
            {
                var genericArguments = dictionaryInterface.GetGenericArguments();
                keyType = genericArguments[0];
                valueType = genericArguments[1];
                return true;
            }

            keyType = null;
            valueType = null;
            return false;
        }

        public static bool ImplementsIEnumerable(this Type type, out Type itemType)
        {
            if (TryGetIEnumerableGenericInterface(type, out var ienumerableType))
            {
                itemType = ienumerableType.GetGenericArguments()[0];
                return true;
            }

            itemType = null;
            return false;
        }

        public static bool ImplementsIEnumerableOf(this Type type, Type itemType)
        {
            return
                ImplementsIEnumerable(type, out var actualItemType) &&
                actualItemType == itemType;
        }

        public static bool ImplementsIList(this Type type, out Type itemType)
        {
            if (TryGetIListGenericInterface(type, out var ilistType))
            {
                itemType = ilistType.GetGenericArguments()[0];
                return true;
            }

            itemType = null;
            return false;
        }

        public static bool Is(this Type type, Type comparand)
        {
            if (type == comparand)
            {
                return true;
            }

            if (type.IsGenericType && comparand.IsGenericTypeDefinition)
            {
                if (type.GetGenericTypeDefinition() == comparand)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsAnonymous(this Type type)
        {
            // don't test for too many things in case implementation details change in the future
            return
                type.GetCustomAttributes(false).Any(x => x is CompilerGeneratedAttribute) &&
                (type.IsGenericType || type.GetProperties().Length == 0) && // type is not generic for "new { }"
                type.Name.Contains("Anon"); // don't check for more than "Anon" so it works in mono also
        }

        public static bool IsEnum(this Type type, out Type underlyingType)
        {
            if (type.IsEnum)
            {
                underlyingType = Enum.GetUnderlyingType(type);
                return true;
            }
            else
            {
                underlyingType = null;
                return false;
            }
        }

        public static bool IsEnum(this Type type, out Type enumType, out Type underlyingType)
        {
            if (type.IsEnum)
            {
                enumType = type;
                underlyingType = Enum.GetUnderlyingType(type);
                return true;
            }
            else
            {
                enumType = null;
                underlyingType = null;
                return false;
            }
        }

        public static bool IsEnumOrNullableEnum(this Type type, out Type enumType, out Type underlyingType)
        {
            return
                type.IsEnum(out enumType, out underlyingType) ||
                type.IsNullableEnum(out enumType, out underlyingType);
        }

        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsNullable(this Type type, out Type valueType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                valueType = type.GetGenericArguments()[0];
                return true;
            }
            else
            {
                valueType = null;
                return false;
            }
        }

        public static bool IsNullableEnum(this Type type)
        {
            return type.IsNullable(out var valueType) && valueType.IsEnum;
        }

        public static bool IsNullableEnum(this Type type, out Type enumType, out Type underlyingType)
        {
            enumType = null;
            underlyingType = null;
            return type.IsNullable(out var valueType) && valueType.IsEnum(out enumType, out underlyingType);
        }

        public static bool IsNullableOf(this Type type, Type valueType)
        {
            return type.IsNullable(out var nullableValueType) && nullableValueType == valueType;
        }

        public static bool IsSameAsOrNullableOf(this Type type, Type valueType)
        {
            return type == valueType || type.IsNullableOf(valueType);
        }

        public static bool IsTuple(this Type type)
        {
            return
                type.IsConstructedGenericType &&
                type.GetGenericTypeDefinition() is var typeDefinition &&
                __tupleTypeDefinitions.Contains(typeDefinition);

        }

        public static bool IsTupleOrValueTuple(this Type type)
        {
            return IsTuple(type) || IsValueTuple(type);
        }

        public static bool IsValueTuple(this Type type)
        {
            return
                type.IsConstructedGenericType &&
                type.GetGenericTypeDefinition() is var typeDefinition &&
                __valueTupleTypeDefinitions.Contains(typeDefinition);
        }

        public static bool TryGetGenericInterface(this Type type, Type[] interfaceDefinitions, out Type genericInterface)
        {
            genericInterface =
                type.IsConstructedGenericType && interfaceDefinitions.Contains(type.GetGenericTypeDefinition()) ?
                    type :
                    type.GetInterfaces().FirstOrDefault(i => i.IsConstructedGenericType && interfaceDefinitions.Contains(i.GetGenericTypeDefinition()));
            return genericInterface != null;
        }

        public static bool TryGetIEnumerableGenericInterface(this Type type, out Type ienumerableGenericInterface)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                ienumerableGenericInterface = type;
                return true;
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    ienumerableGenericInterface = interfaceType;
                    return true;
                }
            }

            ienumerableGenericInterface = null;
            return false;
        }

        public static bool TryGetIListGenericInterface(this Type type, out Type ilistGenericInterface)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
            {
                ilistGenericInterface = type;
                return true;
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    ilistGenericInterface = interfaceType;
                    return true;
                }
            }

            ilistGenericInterface = null;
            return false;
        }
    }
}
