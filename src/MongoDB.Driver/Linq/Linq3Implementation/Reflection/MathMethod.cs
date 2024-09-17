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
        private static readonly MethodInfo __acos;
        private static readonly MethodInfo __acosh = null; // null when target framework does not have this method
        private static readonly MethodInfo __asin;
        private static readonly MethodInfo __asinh = null; // null when target framework does not have this method
        private static readonly MethodInfo __atan;
        private static readonly MethodInfo __atan2;
        private static readonly MethodInfo __atanh = null; // null when target framework does not have this method
        private static readonly MethodInfo __ceilingWithDecimal;
        private static readonly MethodInfo __ceilingWithDouble;
        private static readonly MethodInfo __cos;
        private static readonly MethodInfo __cosh;
        private static readonly MethodInfo __exp;
        private static readonly MethodInfo __floorWithDecimal;
        private static readonly MethodInfo __floorWithDouble;
        private static readonly MethodInfo __log;
        private static readonly MethodInfo __logWithNewBase;
        private static readonly MethodInfo __log10;
        private static readonly MethodInfo __pow;
        private static readonly MethodInfo __roundWithDecimal;
        private static readonly MethodInfo __roundWithDecimalAndDecimals;
        private static readonly MethodInfo __roundWithDouble;
        private static readonly MethodInfo __roundWithDoubleAndDigits;
        private static readonly MethodInfo __sin;
        private static readonly MethodInfo __sinh;
        private static readonly MethodInfo __sqrt;
        private static readonly MethodInfo __tan;
        private static readonly MethodInfo __tanh;
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
            __acos = ReflectionInfo.Method((double d) => Math.Acos(d));
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            __acosh = ReflectionInfo.Method((double d) => Math.Acosh(d));
#endif
            __asin = ReflectionInfo.Method((double d) => Math.Asin(d));
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            __asinh = ReflectionInfo.Method((double d) => Math.Asinh(d));
#endif
            __atan = ReflectionInfo.Method((double d) => Math.Atan(d));
            __atan2 = ReflectionInfo.Method((double x, double y) => Math.Atan2(x, y));
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            __atanh = ReflectionInfo.Method((double d) => Math.Atanh(d));
#endif
            __ceilingWithDecimal = ReflectionInfo.Method((decimal d) => Math.Ceiling(d));
            __ceilingWithDouble = ReflectionInfo.Method((double a) => Math.Ceiling(a));
            __cos = ReflectionInfo.Method((double d) => Math.Cos(d));
            __cosh = ReflectionInfo.Method((double a) => Math.Cosh(a));
            __exp = ReflectionInfo.Method((double d) => Math.Exp(d));
            __floorWithDecimal = ReflectionInfo.Method((decimal d) => Math.Floor(d));
            __floorWithDouble = ReflectionInfo.Method((double d) => Math.Floor(d));
            __log = ReflectionInfo.Method((double d) => Math.Log(d));
            __logWithNewBase = ReflectionInfo.Method((double a, double newBase) => Math.Log(a, newBase));
            __log10 = ReflectionInfo.Method((double d) => Math.Log10(d));
            __pow = ReflectionInfo.Method((double x, double y) => Math.Pow(x, y));
            __roundWithDecimal = ReflectionInfo.Method((decimal d) => Math.Round(d));
            __roundWithDecimalAndDecimals = ReflectionInfo.Method((decimal d, int decimals) => Math.Round(d, decimals));
            __roundWithDouble = ReflectionInfo.Method((double d) => Math.Round(d));
            __roundWithDoubleAndDigits = ReflectionInfo.Method((double d, int digits) => Math.Round(d, digits));
            __sin = ReflectionInfo.Method((double a) => Math.Sin(a));
            __sinh = ReflectionInfo.Method((double a) => Math.Sinh(a));
            __sqrt = ReflectionInfo.Method((double d) => Math.Sqrt(d));
            __tan = ReflectionInfo.Method((double a) => Math.Tan(a));
            __tanh = ReflectionInfo.Method((double a) => Math.Tanh(a));
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
        public static MethodInfo Acos => __acos;
        public static MethodInfo Acosh => __acosh;
        public static MethodInfo Asin => __asin;
        public static MethodInfo Asinh => __asinh;
        public static MethodInfo Atan => __atan;
        public static MethodInfo Atan2 => __atan2;
        public static MethodInfo Atanh => __atanh;
        public static MethodInfo CeilingWithDecimal => __ceilingWithDecimal;
        public static MethodInfo CeilingWithDouble => __ceilingWithDouble;
        public static MethodInfo Cos => __cos;
        public static MethodInfo Cosh => __cosh;
        public static MethodInfo Exp => __exp;
        public static MethodInfo FloorWithDecimal => __floorWithDecimal;
        public static MethodInfo FloorWithDouble => __floorWithDouble;
        public static MethodInfo Log => __log;
        public static MethodInfo LogWithNewBase => __logWithNewBase;
        public static MethodInfo Log10 => __log10;
        public static MethodInfo Pow => __pow;
        public static MethodInfo RoundWithDecimal => __roundWithDecimal;
        public static MethodInfo RoundWithDecimalAndDecimals => __roundWithDecimalAndDecimals;
        public static MethodInfo RoundWithDouble => __roundWithDouble;
        public static MethodInfo RoundWithDoubleAndDigits => __roundWithDoubleAndDigits;
        public static MethodInfo Sin => __sin;
        public static MethodInfo Sinh => __sinh;
        public static MethodInfo Sqrt => __sqrt;
        public static MethodInfo Tan => __tan;
        public static MethodInfo Tanh => __tanh;
        public static MethodInfo TruncateDecimal => __truncateDecimal;
        public static MethodInfo TruncateDouble => __truncateDouble;
    }
}
