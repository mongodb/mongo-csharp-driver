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

namespace MongoDB.Driver.Linq3.Misc
{
    public static class TypeExtensions
    {
        public static Type GetIEnumerableGenericInterface(this Type enumerableType)
        {
            if (enumerableType.TryGetIEnumerableGenericInterface(out var ienumerableGenericInterface))
            {
                return ienumerableGenericInterface;
            }

            throw new InvalidOperationException($"Could not find IEnumerable<T> interface of type: {enumerableType}.");
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

        public static bool TryGetIEnumerableGenericInterface(this Type type, out Type ienumerableGenericInterface)
        {
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
    }
}
