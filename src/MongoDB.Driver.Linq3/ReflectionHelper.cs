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

namespace MongoDB.Driver.Linq3
{
    public static class ReflectionHelper
    {
        public static Type GetCollectionElementType(Type enumerableType)
        {
            if (enumerableType.IsArray)
            {
                return enumerableType.GetElementType();
            }

            foreach (var @interface in enumerableType.GetInterfaces())
            {
                if (@interface.IsConstructedGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return @interface.GetGenericArguments()[0];
                }
            }

            throw new ArgumentException($"Type {enumerableType} does not implement IEnumerable<T>.", nameof(enumerableType));
        }
    }
}
