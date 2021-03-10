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
using System.Text.RegularExpressions;

namespace MongoDB.Driver.Linq3.Methods
{
    public static class RegexMethod
    {
        // private static fields
        private static readonly MethodInfo __isMatch;
        private static readonly MethodInfo __staticIsMatch;
        private static readonly MethodInfo __staticIsMatchWithOptions;

        // static constructor
        static RegexMethod()
        {
            __isMatch = new Func<string, bool>(new Regex("").IsMatch).Method;
            __staticIsMatch = new Func<string, string, bool>(Regex.IsMatch).Method;
            __staticIsMatchWithOptions = new Func<string, string, RegexOptions, bool>(Regex.IsMatch).Method;
        }

        // public properties
        public static MethodInfo IsMatch => __isMatch;
        public static MethodInfo StaticIsMatch => __staticIsMatch;
        public static MethodInfo StaticIsMatchWithOptions => __staticIsMatchWithOptions;
    }
}
