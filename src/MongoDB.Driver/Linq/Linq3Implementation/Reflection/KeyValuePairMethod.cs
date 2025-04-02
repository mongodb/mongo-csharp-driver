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

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class KeyValuePairMethod
    {
        // private static fields
        private static readonly MethodInfo __create;

        // static constructor
        static KeyValuePairMethod()
        {
            __create = ReflectionInfo.Method((object key, object value) => KeyValuePair.Create(key, value));
        }

        // public properties
        public static MethodInfo Create => __create;
    }
}

#endif
