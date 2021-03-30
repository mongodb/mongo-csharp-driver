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

namespace MongoDB.Driver.Linq3.Misc
{
    public static class MethodInfoExtensions
    {
        public static bool Is(this MethodInfo method, MethodInfo comparand)
        {
            if (method == comparand)
            {
                return true;
            }

            if (method.IsGenericMethod && comparand.IsGenericMethodDefinition)
            {
                var methodDefinition = method.GetGenericMethodDefinition();
                return methodDefinition == comparand;
            }

            return false;
        }

        public static bool IsOneOf(this MethodInfo method, MethodInfo comparand1, MethodInfo comparand2)
        {
            return method.Is(comparand1) || method.Is(comparand2);
        }

        public static bool IsOneOf(this MethodInfo method, MethodInfo comparand1, MethodInfo comparand2, MethodInfo comparand3)
        {
            return method.Is(comparand1) || method.Is(comparand2) || method.Is(comparand3);
        }

        public static bool IsOneOf(this MethodInfo method, MethodInfo comparand1, MethodInfo comparand2, MethodInfo comparand3, MethodInfo comparand4)
        {
            return method.Is(comparand1) || method.Is(comparand2) || method.Is(comparand3) || method.Is(comparand4);
        }

        public static bool IsOneOf(this MethodInfo method, params MethodInfo[] comparands)
        {
            for (var i = 0; i < comparands.Length; i++)
            {
                if (method.Is(comparands[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsOneOf(this MethodInfo method, params MethodInfo[][] comparands)
        {
            for (var i = 0; i < comparands.Length; i++)
            {
                if (method.IsOneOf(comparands[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
