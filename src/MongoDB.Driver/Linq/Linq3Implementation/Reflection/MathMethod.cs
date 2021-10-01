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
    internal static class MathMethod
    {
        // private static fields
        private static readonly MethodInfo __absDecimal;
        private static readonly MethodInfo __absDouble;
        private static readonly MethodInfo __absInt16;
        private static readonly MethodInfo __absInt32;
        private static readonly MethodInfo __absInt64;
        private static readonly MethodInfo __absSByte;
        private static readonly MethodInfo __absSingle;
        private static readonly MethodInfo __ceilingWithDecimal;
        private static readonly MethodInfo __ceilingWithDouble;
        private static readonly MethodInfo __exp;
        private static readonly MethodInfo __floorWithDecimal;
        private static readonly MethodInfo __floorWithDouble;
        private static readonly MethodInfo __log;
        private static readonly MethodInfo __logWithNewBase;
        private static readonly MethodInfo __log10;
        private static readonly MethodInfo __pow;
        private static readonly MethodInfo __sqrt;
        private static readonly MethodInfo __truncateDecimal;
        private static readonly MethodInfo __truncateDouble;

        // static constructor
        static MathMethod()
        {
            __absDecimal = ReflectionInfo.Method((decimal value) => Math.Abs(value));
            __absDouble = ReflectionInfo.Method((double value) => Math.Abs(value));
            __absInt16 = ReflectionInfo.Method((short value) => Math.Abs(value));
            __absInt32 = ReflectionInfo.Method((int value) => Math.Abs(value));
            __absInt64 = ReflectionInfo.Method((long value) => Math.Abs(value));
            __absSByte = ReflectionInfo.Method((sbyte value) => Math.Abs(value));
            __absSingle = ReflectionInfo.Method((float value) => Math.Abs(value));
            __ceilingWithDecimal = ReflectionInfo.Method((decimal d) => Math.Ceiling(d));
            __ceilingWithDouble = ReflectionInfo.Method((double a) => Math.Ceiling(a));
            __exp = ReflectionInfo.Method((double d) => Math.Exp(d));
            __floorWithDecimal = ReflectionInfo.Method((decimal d) => Math.Floor(d));
            __floorWithDouble = ReflectionInfo.Method((double d) => Math.Floor(d));
            __log = ReflectionInfo.Method((double d) => Math.Log(d));
            __logWithNewBase = ReflectionInfo.Method((double a, double newBase) => Math.Log(a, newBase));
            __log10 = ReflectionInfo.Method((double d) => Math.Log10(d));
            __pow = ReflectionInfo.Method((double x, double y) => Math.Pow(x, y));
            __sqrt = ReflectionInfo.Method((double d) => Math.Sqrt(d));
            __truncateDecimal = ReflectionInfo.Method((decimal d) => Math.Truncate(d));
            __truncateDouble = ReflectionInfo.Method((double d) => Math.Truncate(d));
        }

        // public properties
        public static MethodInfo AbsDecimal => __absDecimal;
        public static MethodInfo AbsDouble => __absDouble;
        public static MethodInfo AbsInt16 => __absInt16;
        public static MethodInfo AbsInt32 => __absInt32;
        public static MethodInfo AbsInt64 => __absInt64;
        public static MethodInfo AbsSByte => __absSByte;
        public static MethodInfo AbsSingle => __absSingle;
        public static MethodInfo CeilingWithDecimal => __ceilingWithDecimal;
        public static MethodInfo CeilingWithDouble => __ceilingWithDouble;
        public static MethodInfo Exp => __exp;
        public static MethodInfo FloorWithDecimal => __floorWithDecimal;
        public static MethodInfo FloorWithDouble => __floorWithDouble;
        public static MethodInfo Log => __log;
        public static MethodInfo LogWithNewBase => __logWithNewBase;
        public static MethodInfo Log10 => __log10;
        public static MethodInfo Pow => __pow;
        public static MethodInfo Sqrt => __sqrt;
        public static MethodInfo TruncateDecimal => __truncateDecimal;
        public static MethodInfo TruncateDouble => __truncateDouble;
    }
}
