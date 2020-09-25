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

namespace MongoDB.Driver.Linq3.Methods
{
    public static class LinqExtensionsMethod
    {
        // private static fields
        private static readonly MethodInfo __indexOfBytes;
        private static readonly MethodInfo __strLenBytes;
        private static readonly MethodInfo __substrBytes;

        // static constructor
        static LinqExtensionsMethod()
        {
            __indexOfBytes = new Func<string, string, int, int, int>(MongoDBLinqExtensions.IndexOfBytes).Method;
            __strLenBytes = new Func<string, int>(MongoDBLinqExtensions.StrLenBytes).Method;
            __substrBytes = new Func<string, int, int , string>(MongoDBLinqExtensions.SubstrBytes).Method;
        }

        // public properties
        public static MethodInfo IndexOfBytes => __indexOfBytes;
        public static MethodInfo StrLenBytes => __strLenBytes;
        public static MethodInfo SubstrBytes => __substrBytes;
    }
}
