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
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MongoDB.Bson.Serialization
{
    internal static class TypeExtensions
    {
        private static readonly ConcurrentDictionary<Type, bool> __isAnonymousTypeDefinition = new();

        public static bool IsAnonymousType(this Type type)
        {
            return
                type.IsGenericType &&
                HasAnonymousTypeName(type.Name) &&
                __isAnonymousTypeDefinition.GetOrAdd(type.GetGenericTypeDefinition(), IsAnonymousTypeDefinition);
        }

        // C# and F# use <>f__AnonymousType (Roslyn puts the module name between the angle brackets when compiling a netmodule),
        // VB uses VB$AnonymousType_ and the Mono mcs compiler uses <>__AnonType
        private static bool HasAnonymousTypeName(string name)
        {
            if (name.StartsWith("VB$AnonymousType_", StringComparison.Ordinal) ||
                name.StartsWith("<>__AnonType", StringComparison.Ordinal))
            {
                return true;
            }

            const string suffix = ">f__AnonymousType";
            var closingBracket = name.IndexOf('>');
            return
                name.StartsWith("<", StringComparison.Ordinal) &&
                closingBracket > 0 &&
                string.CompareOrdinal(name, closingBracket, suffix, 0, suffix.Length) == 0;
        }

        // An anonymous type is only safe to deserialize when its generic arguments are (see ObjectSerializer.DefaultAllowedTypes)
        // if every member that can hold data is typed by one of its generic parameters. Other compiler-generated types don't
        // meet that, e.g. the <>4__this field of an async state machine references an instance of the declaring class.
        private static bool IsAnonymousTypeDefinition(Type definition)
        {
            const BindingFlags DeclaredInstanceMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            return
                definition.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) &&
                // DeclaredOnly doesn't see inherited members, so there must be no base class that could declare any
                (definition.BaseType == typeof(object) || definition.BaseType == typeof(ValueType)) &&
                definition.GetFields(DeclaredInstanceMembers).All(f => f.FieldType.IsGenericParameter) &&
                definition.GetProperties(DeclaredInstanceMembers).All(p => p.PropertyType.IsGenericParameter);
        }

        public static bool IsNullable(this Type type)
        {
            return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsNullableEnum(this Type type)
        {
            return
                type.IsNullable() &&
                Nullable.GetUnderlyingType(type).IsEnum;
        }
    }
}
