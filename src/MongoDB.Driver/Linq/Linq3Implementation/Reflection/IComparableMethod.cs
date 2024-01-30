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
using System.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class IComparableMethod
    {
        // public static methods
        public static bool IsCompareToMethod(MethodInfo method)
        {
            var parameters = method.GetParameters();

            if (method.Name == "CompareTo" &&
                method.IsPublic &&
                !method.IsStatic &&
                method.ReturnType == typeof(int) &&
                parameters.Length == 1)
            {
                var declaringType = method.DeclaringType;
                var parameterType = parameters[0].ParameterType;

                if (parameterType == typeof(object) || parameterType == declaringType)
                {
                    return true;
                }

                if (declaringType.IsConstructedGenericType &&
                    declaringType.GetGenericTypeDefinition() == typeof(IComparable<>) &&
                    declaringType.GetGenericArguments()[0] is var valueType &&
                    parameterType == valueType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
