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
using MongoDB.Bson;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class BsonValueMethod
    {
        // private static fields
        private static readonly MethodInfo __getItemWithInt;
        private static readonly MethodInfo __getItemWithString;

        // static constructor
        static BsonValueMethod()
        {
            __getItemWithInt = ReflectionInfo.IndexerGet((BsonValue v, int index) => v[index]);
            __getItemWithString = ReflectionInfo.IndexerGet((BsonValue v, string index) => v[index]);
        }

        // public static properties
        public static MethodInfo GetItemWithInt => __getItemWithInt;
        public static MethodInfo GetItemWithString => __getItemWithString;

        // public static methods
        public static bool IsGetItemWithIntMethod(MethodInfo method)
        {
            return
                (method.DeclaringType == typeof(BsonValue) || method.DeclaringType.IsSubclassOf(typeof(BsonValue))) &&
                !method.IsStatic &&
                method.Name == "get_Item" &&
                method.GetParameters() is var parameters &&
                parameters.Length == 1 &&
                parameters[0].ParameterType == typeof(int) &&
                method.ReturnType == typeof(BsonValue);
        }

        public static bool IsGetItemWithStringMethod(MethodInfo method)
        {
            return
                (method.DeclaringType == typeof(BsonValue) || method.DeclaringType.IsSubclassOf(typeof(BsonValue))) &&
                !method.IsStatic &&
                method.Name == "get_Item" &&
                method.GetParameters() is var parameters &&
                parameters.Length == 1 &&
                parameters[0].ParameterType == typeof(string) &&
                method.ReturnType == typeof(BsonValue);
        }
    }
}
