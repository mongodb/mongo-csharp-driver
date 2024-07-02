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

using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class DictionaryMethod
    {
        // public static methods
        public static bool IsGetItemWithStringMethod(MethodInfo method)
        {
            return
                !method.IsStatic &&
                method.Name == "get_Item" &&
                method.GetParameters() is var parameters &&
                parameters.Length == 1 &&
                parameters[0].ParameterType == typeof(string) &&
                method.DeclaringType.ImplementsDictionaryInterface(out var keyType, out var valueType) &&
                keyType == typeof(string) &&
                method.ReturnType == valueType;
        }
    }
}
