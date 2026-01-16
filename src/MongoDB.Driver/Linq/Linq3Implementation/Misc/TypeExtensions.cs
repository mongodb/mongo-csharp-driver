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
using System.Reflection;
using System.Runtime.CompilerServices;
using MongoDB.Bson;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class TypeExtensions
    {
        private static readonly Type[] __dictionaryInterfaceDefinitions =
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

        public static object GetDefaultValue(this Type type)
        {
            var genericMethod = typeof(TypeExtensions)
                .GetMethod(nameof(GetDefaultValueGeneric), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(type);
            return genericMethod.Invoke(null, null);
        }

        public static Type GetIEnumerableGenericInterface(this Type enumerableType)
        {
            if (enumerableType.TryGetIEnumerableGenericInterface(out var ienumerableGenericInterface))
            {
                return ienumerableGenericInterface;
            }

            throw new InvalidOperationException($"Could not find IEnumerable<T> interface of type: {enumerableType}.");
        }

        public static bool ImplementsInterface(this Type type, Type @interface)
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
            // note: returns true for IReadOnlyDictionary also
            if (TryGetGenericInterface(type, __dictionaryInterfaceDefinitions, out var dictionaryInterface))
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

        public static bool ImplementsIOrderedEnumerable(this Type type, out Type itemType)
        {
            if (TryGetIOrderedEnumerableGenericInterface(type, out var iOrderedEnumerableType))
            {
                itemType = iOrderedEnumerableType.GetGenericArguments()[0];
                return true;
            }

            itemType = null;
            return false;
        }

        public static bool ImplementsIOrderedQueryable(this Type type, out Type itemType)
        {
            if (TryGetIOrderedQueryableGenericInterface(type, out var iorderedQueryableType))
            {
                itemType = iorderedQueryableType.GetGenericArguments()[0];
                return true;
            }

            itemType = null;
            return false;
        }

        public static bool ImplementsIQueryable(this Type type, out Type itemType)
        {
            if (TryGetIQueryableGenericInterface(type, out var iqueryableType))
            {
                itemType = iqueryableType.GetGenericArguments()[0];
                return true;
            }

            itemType = null;
            return false;
        }

        public static bool ImplementsIQueryableOf(this Type type, Type itemType)
        {
            return
                ImplementsIEnumerable(type, out var actualItemType) &&
                actualItemType == itemType;
        }

        public static bool ImplementsISet(this Type type, out Type itemType)
        {
            if (TryGetISetGenericInterface(type, out var isetType))
            {
                itemType = isetType.GetGenericArguments()[0];
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

        public static bool IsArray(this Type type, out Type itemType)
        {
            if (type.IsArray)
            {
                itemType = type.GetElementType();
                return true;
            }

            itemType = null;
            return false;
        }

        public static bool IsBoolean(this Type type)
        {
            return type == typeof(bool);
        }

        public static bool IsBooleanOrNullableBoolean(this Type type)
        {
            return IsBoolean(type) || type.IsNullable(out var valueType) && IsBoolean(valueType);
        }

        public static bool IsConvertibleToEnum(this Type type)
        {
            return
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(byte) ||
                type == typeof(ushort) ||
                type == typeof(uint) ||
                type == typeof(ulong) ||
                type == typeof(Enum) ||
                type == typeof(string);
        }

        public static bool IsEnum(this Type type, out Type underlyingType)
        {
            if (type.IsEnum)
            {
                underlyingType = Enum.GetUnderlyingType(type);
                return true;
            }

            underlyingType = null;
            return false;
        }

        public static bool IsEnumOrNullableEnum(this Type type, out Type enumType, out Type underlyingType)
        {
            if (type.IsEnum(out underlyingType))
            {
                enumType = type;
                return true;
            }

            return IsNullableEnum(type, out enumType, out underlyingType);
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

            valueType = null;
            return false;
        }

        public static bool IsNullableEnum(this Type type)
        {
            return type.IsNullable(out var valueType) && valueType.IsEnum;
        }

        public static bool IsNullableEnum(this Type type, out Type enumType)
        {
            if (type.IsNullable(out var valueType) && valueType.IsEnum)
            {
                enumType = valueType;
                return true;
            }

            enumType = null;
            return false;
        }

        public static bool IsNullableEnum(this Type type, out Type enumType, out Type underlyingType)
        {
            if (type.IsNullable(out var valueType) && valueType.IsEnum(out underlyingType))
            {
                enumType = valueType;
                return true;
            }

            enumType = null;
            underlyingType = null;
            return false;
        }

        public static bool IsNullableOf(this Type type, Type valueType)
        {
            return type.IsNullable(out var nullableValueType) && nullableValueType == valueType;
        }

        public static bool IsNumeric(this Type type)
        {
            // note: treating more types as numeric would require careful analysis of impact on callers of this method
            return
                type == typeof(char) ||  // TODO: should we really treat char as numeric?
                type == typeof(decimal) ||
                type == typeof(Decimal128) ||
                type == typeof(double) ||
                type == typeof(float) ||
                type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(short);
        }

        public static bool IsNumericOrNullableNumeric(this Type type)
        {
            return
                type.IsNumeric() ||
                type.IsNullable(out var valueType) && valueType.IsNumeric();
        }

        public static bool IsReadOnlySpanOf(this Type type, Type itemType)
        {
            return
                type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>) &&
                type.GetGenericArguments()[0] == itemType;
        }

        public static bool IsSameAsOrNullableOf(this Type type, Type valueType)
        {
            return type == valueType || type.IsNullableOf(valueType);
        }

        public static bool IsSpanOf(this Type type, Type itemType)
        {
            return
                type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Span<>) &&
                type.GetGenericArguments()[0] == itemType;
        }

        public static bool IsSubclassOfOrImplements(this Type type, Type baseTypeOrInterface)
        {
            return
                type.IsSubclassOf(baseTypeOrInterface) ||
                type.ImplementsInterface(baseTypeOrInterface);
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

        public static bool TryGetGenericInterface(this Type type, Type genericInterfaceDefintion, out Type genericInterface)
        {
            genericInterface =
                type.IsConstructedGenericType && type.GetGenericTypeDefinition() == genericInterfaceDefintion ?
                    type :
                    type.GetInterfaces().FirstOrDefault(i => i.IsConstructedGenericType && i.GetGenericTypeDefinition() == genericInterfaceDefintion);
            return genericInterface != null;
        }

        public static bool TryGetGenericInterface(this Type type, Type[] genericInterfaceDefinitions, out Type genericInterface)
        {
            genericInterface =
                type.IsConstructedGenericType && genericInterfaceDefinitions.Contains(type.GetGenericTypeDefinition()) ?
                    type :
                    type.GetInterfaces().FirstOrDefault(i => i.IsConstructedGenericType && genericInterfaceDefinitions.Contains(i.GetGenericTypeDefinition()));
            return genericInterface != null;
        }

        public static bool TryGetIEnumerableGenericInterface(this Type type, out Type ienumerableGenericInterface)
            => TryGetGenericInterface(type, typeof(IEnumerable<>), out ienumerableGenericInterface);

        public static bool TryGetIListGenericInterface(this Type type, out Type ilistGenericInterface)
            => TryGetGenericInterface(type, typeof(IList<>), out ilistGenericInterface);

        public static bool TryGetIOrderedEnumerableGenericInterface(this Type type, out Type iorderedEnumerableGenericInterface)
            => TryGetGenericInterface(type, typeof(IOrderedEnumerable<>), out iorderedEnumerableGenericInterface);

        public static bool TryGetIOrderedQueryableGenericInterface(this Type type, out Type iorderedQueryableGenericInterface)
            => TryGetGenericInterface(type, typeof(IOrderedQueryable<>), out iorderedQueryableGenericInterface);

        public static bool TryGetIQueryableGenericInterface(this Type type, out Type iqueryableGenericInterface)
            => TryGetGenericInterface(type, typeof(IQueryable<>), out iqueryableGenericInterface);

        public static bool TryGetISetGenericInterface(this Type type, out Type isetGenericInterface)
            => TryGetGenericInterface(type, typeof(ISet<>), out isetGenericInterface);

        private static TValue GetDefaultValueGeneric<TValue>()
        {
            return default(TValue);
        }
    }
}
