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
using System.Linq;
using System.Runtime.CompilerServices;

namespace MongoDB.Bson.Serialization
{
    internal static class TypeExtensions
    {
        public static bool IsAnonymousType(this Type type)
        {
            // don't test for too many things in case implementation details change in the future
            return
                type.GetCustomAttributes(false).Any(x => x is CompilerGeneratedAttribute) &&
                type.IsGenericType &&
                type.Name.Contains("Anon"); // don't check for more than "Anon" so it works in mono also
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
