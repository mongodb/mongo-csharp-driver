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
using System.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class MongoEnumerableMethod
    {
        // private static fields
        private static readonly MethodInfo __allElements;
        private static readonly MethodInfo __allMatchingElements;
        private static readonly MethodInfo __firstMatchingElement;
        private static readonly MethodInfo __whereWithLimit;

        // static constructor
        static MongoEnumerableMethod()
        {
            __allElements = ReflectionInfo.Method((IEnumerable<object> source) => source.AllElements());
            __allMatchingElements = ReflectionInfo.Method((IEnumerable<object> source, string identifier) => source.AllMatchingElements(identifier));
            __firstMatchingElement = ReflectionInfo.Method((IEnumerable<object> source) => source.FirstMatchingElement());
            __whereWithLimit = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate, int limit) => source.Where(predicate, limit));
        }

        // public properties
        public static MethodInfo AllElements => __allElements;
        public static MethodInfo AllMatchingElements => __allMatchingElements;
        public static MethodInfo FirstMatchingElement => __firstMatchingElement;
        public static MethodInfo WhereWithLimit => __whereWithLimit;
    }
}
