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
    public static class MathMethod
    {
        // private static fields
        private static readonly MethodInfo __log;
        private static readonly MethodInfo __logWithNewBase;
        private static readonly MethodInfo __log10;
        private static readonly MethodInfo __sqrt;
        private static readonly MethodInfo __truncateDecimal;
        private static readonly MethodInfo __truncateDouble;

        // static constructor
        static MathMethod()
        {
            __log = new Func<double, double>(Math.Log).Method;
            __logWithNewBase = new Func<double, double, double>(Math.Log).Method;
            __log10 = new Func<double, double>(Math.Log10).Method;
            __sqrt = new Func<double, double>(Math.Sqrt).Method;
            __truncateDecimal = new Func<decimal, decimal>(Math.Truncate).Method;
            __truncateDouble = new Func<double, double>(Math.Truncate).Method;
        }

        // public properties
        public static MethodInfo Log => __log;
        public static MethodInfo LogWithNewBase => __logWithNewBase;
        public static MethodInfo Log10 => __log10;
        public static MethodInfo Sqrt => __sqrt;
        public static MethodInfo TruncateDecimal => __truncateDecimal;
        public static MethodInfo TruncateDouble => __truncateDouble;
    }
}
