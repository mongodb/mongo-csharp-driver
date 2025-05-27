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
    internal static class KeyValuePairConstructor
    {
        public static bool IsWithKeyAndValueConstructor(ConstructorInfo constructor)
        {
            if (constructor != null)
            {
                var declaringType = constructor.DeclaringType;
                var parameters = constructor.GetParameters();
                return
                    declaringType.IsConstructedGenericType &&
                    declaringType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>) &&
                    declaringType.GetGenericArguments() is var typeParameters &&
                    typeParameters[0] is var keyType &&
                    typeParameters[1] is var valueType &&
                    parameters.Length == 2 &&
                    parameters[0].ParameterType == keyType &&
                    parameters[1].ParameterType == valueType;
            }

            return false;
        }
    }
}
