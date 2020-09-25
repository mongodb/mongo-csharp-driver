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

namespace MongoDB.Driver.Linq3.Misc
{
    public static class StringMethod
    {
        // private static fields
        private static readonly MethodInfo __splitWithChars;
        private static readonly MethodInfo __splitWithCharsAndCount;
        private static readonly MethodInfo __splitWithCharsAndCountAndOptions;
        private static readonly MethodInfo __splitWithCharsAndOptions;
        private static readonly MethodInfo __splitWithStringsAndCountAndOptions;
        private static readonly MethodInfo __splitWithStringsAndOptions;
        private static readonly MethodInfo __substring;
        private static readonly MethodInfo __substringWithLength;

        // static constructor
        static StringMethod()
        {
            __splitWithChars = new Func<char[], string[]>("".Split).Method;
            __splitWithCharsAndCount = new Func<char[], int, string[]>("".Split).Method;
            __splitWithCharsAndCountAndOptions = new Func<char[], int, StringSplitOptions, string[]>("".Split).Method;
            __splitWithCharsAndOptions = new Func<char[], StringSplitOptions, string[]>("".Split).Method;
            __splitWithStringsAndCountAndOptions = new Func<string[], int, StringSplitOptions, string[]>("".Split).Method;
            __splitWithStringsAndOptions = new Func<string[], StringSplitOptions, string[]>("".Split).Method;
            __substring = new Func<int, string>("".Substring).Method;
            __substringWithLength = new Func<int, int, string>("".Substring).Method;
        }

        // public properties
        public static MethodInfo SplitWithChars => __splitWithChars;
        public static MethodInfo SplitWithCharsAndCount => __splitWithCharsAndCount;
        public static MethodInfo SplitWithCharsAndCountAndOptions => __splitWithCharsAndCountAndOptions;
        public static MethodInfo SplitWithCharsAndOptions => __splitWithCharsAndOptions;
        public static MethodInfo SplitWithStringsAndCountAndOptions => __splitWithStringsAndCountAndOptions;
        public static MethodInfo SplitWithStringsAndOptions => __splitWithStringsAndOptions;
        public static MethodInfo Substring => __substring;
        public static MethodInfo SubstringWithLength => __substringWithLength;
    }
}
