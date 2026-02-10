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
using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class MethodInfoExtensions
    {
        public static bool Has1GenericArgument(this MethodInfo method, out Type genericArgument)
        {
            if (method.IsGenericMethod &&
                method.GetGenericArguments() is var genericArguments &&
                genericArguments.Length == 1)
            {
                genericArgument = genericArguments[0];
                return true;
            }

            genericArgument = null;
            return false;
        }

        public static bool Has2Parameters(this MethodInfo method, out ParameterInfo parameter1, out ParameterInfo parameter2)
        {
            if (method.GetParameters() is var parameters &&
                parameters.Length == 2)
            {
                parameter1 = parameters[0];
                parameter2 = parameters[1];
                return true;
            }

            parameter1 = null;
            parameter2 = null;
            return false;
        }

        public static bool Has3Parameters(this MethodInfo method, out ParameterInfo parameter1, out ParameterInfo parameter2, out ParameterInfo parameter3)
        {
            if (method.GetParameters() is var parameters &&
                parameters.Length == 3)
            {
                parameter1 = parameters[0];
                parameter2 = parameters[1];
                parameter3 = parameters[2];
                return true;
            }

            parameter1 = null;
            parameter2 = null;
            parameter3 = null;
            return false;
        }

        public static bool Is(this MethodInfo method, MethodInfo comparand)
        {
            if (comparand != null)
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
            }

            return false;
        }

        public static bool IsInstanceCompareToMethod(this MethodInfo method)
        {
            if (method.IsPublic &&
                !method.IsStatic &&
                method.ReturnType == typeof(int) &&
                method.Name == "CompareTo" &&
                method.GetParameters() is var parameters &&
                parameters.Length == 1)
            {
                var declaringType = method.DeclaringType;
                var comparandType = declaringType switch
                {
                    _ when declaringType == typeof(IComparable) => typeof(object),
                    _ when declaringType.IsConstructedGenericType && declaringType.GetGenericTypeDefinition() == typeof(IComparable<>) => declaringType.GetGenericArguments().Single(),
                    _ => declaringType
                };

                var parameterType = parameters[0].ParameterType;
                return parameterType == comparandType;
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

        public static bool IsOneOf(this MethodInfo method, IReadOnlyMethodInfoSet set) => set.Contains(method);

        public static bool IsOneOf(this MethodInfo method, IReadOnlyMethodInfoSet set1, IReadOnlyMethodInfoSet set2) => set1.Contains(method) || set2.Contains(method);

        public static bool IsStaticCompareMethod(this MethodInfo method)
        {
            return
                method.IsPublic &&
                method.IsStatic &&
                method.ReturnType == typeof(int) &&
                method.Name == "Compare" &&
                method.GetParameters() is var parameters &&
                parameters.Length == 2 &&
                parameters[0].ParameterType == method.DeclaringType &&
                parameters[1].ParameterType == method.DeclaringType;
        }
    }
}
