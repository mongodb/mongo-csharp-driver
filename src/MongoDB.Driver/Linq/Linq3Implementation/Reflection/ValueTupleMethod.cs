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
    internal static class ValueTupleMethod
    {
        // private static fields
        private static readonly MethodInfo __create1;
        private static readonly MethodInfo __create2;
        private static readonly MethodInfo __create3;
        private static readonly MethodInfo __create4;
        private static readonly MethodInfo __create5;
        private static readonly MethodInfo __create6;
        private static readonly MethodInfo __create7;
        private static readonly MethodInfo __create8;

        // sets of methods
        private static readonly IReadOnlyMethodInfoSet __createOverloads;

        // static constructor
        static ValueTupleMethod()
        {
            // initialize methods before sets of methods
            __create1 = ReflectionInfo.Method((object item1) => ValueTuple.Create(item1));
            __create2 = ReflectionInfo.Method((object item1, object item2) => ValueTuple.Create(item1, item2));
            __create3 = ReflectionInfo.Method((object item1, object item2, object item3) => ValueTuple.Create(item1, item2, item3));
            __create4 = ReflectionInfo.Method((object item1, object item2, object item3, object item4) => ValueTuple.Create(item1, item2, item3, item4));
            __create5 = ReflectionInfo.Method((object item1, object item2, object item3, object item4, object item5) => ValueTuple.Create(item1, item2, item3, item4, item5));
            __create6 = ReflectionInfo.Method((object item1, object item2, object item3, object item4, object item5, object item6) => ValueTuple.Create(item1, item2, item3, item4, item5, item6));
            __create7 = ReflectionInfo.Method((object item1, object item2, object item3, object item4, object item5, object item6, object item7) => ValueTuple.Create(item1, item2, item3, item4, item5, item6, item7));
            __create8 = ReflectionInfo.Method((object item1, object item2, object item3, object item4, object item5, object item6, object item7, object item8) => ValueTuple.Create(item1, item2, item3, item4, item5, item6, item7, item8));

            // initialize sets of methods after methods
            __createOverloads = MethodInfoSet.Create(
            [
                __create1,
                __create2,
                __create3,
                __create4,
                __create5,
                __create6,
                __create7,
                __create8
            ]);
        }

        // public properties
        public static MethodInfo Create1 => __create1;
        public static MethodInfo Create2 => __create2;
        public static MethodInfo Create3 => __create3;
        public static MethodInfo Create4 => __create4;
        public static MethodInfo Create5 => __create5;
        public static MethodInfo Create6 => __create6;
        public static MethodInfo Create7 => __create7;
        public static MethodInfo Create8 => __create8;

        // sets of methods
        public static IReadOnlyMethodInfoSet CreateOverloads => __createOverloads;
    }
}
