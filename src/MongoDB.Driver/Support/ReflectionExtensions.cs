﻿/* Copyright 2015 MongoDB Inc.
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
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Support
{
    internal static class ReflectionExtensions
    {
        public static object GetDefaultValue(this Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        public static bool ImplementsInterface(this Type type, Type iface)
        {
            if (type.Equals(iface))
            {
                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(iface))
            {
                return true;
            }

            return type.GetInterfaces().Any(i => i.ImplementsInterface(iface));
        }

        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsNullableEnum(this Type type)
        {
            if (!IsNullable(type))
            {
                return false;
            }

            return GetNullableUnderlyingType(type).IsEnum;
        }

        public static Type GetNullableUnderlyingType(this Type type)
        {
            if (!IsNullable(type))
            {
                throw new ArgumentException("Type must be nullable.", "type");
            }

            return type.GetGenericArguments()[0];
        }


    }
}
