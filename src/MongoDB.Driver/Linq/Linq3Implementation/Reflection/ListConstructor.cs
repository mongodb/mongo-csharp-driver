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

using System.Collections.Generic;
using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class ListConstructor
    {
        public static bool IsWithCollectionConstructor(ConstructorInfo constructor)
        {
            if (constructor != null)
            {
                var declaringType = constructor.DeclaringType;
                var parameters = constructor.GetParameters();
                return
                    declaringType.IsConstructedGenericType &&
                    declaringType.GetGenericTypeDefinition() == typeof(List<>) &&
                    parameters.Length == 1 &&
                    parameters[0].ParameterType.ImplementsIEnumerable(out var itemType) &&
                    itemType == declaringType.GenericTypeArguments[0];
            }

            return false;
        }
    }
}
