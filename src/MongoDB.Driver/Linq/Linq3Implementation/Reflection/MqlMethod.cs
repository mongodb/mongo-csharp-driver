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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class MqlMethod
    {
        // private static fields
        private static readonly MethodInfo __constantWithRepresentation;
        private static readonly MethodInfo __constantWithSerializer;
        private static readonly MethodInfo __convert;
        private static readonly MethodInfo __dateFromString;
        private static readonly MethodInfo __dateFromStringWithFormat;
        private static readonly MethodInfo __dateFromStringWithFormatAndTimezone;
        private static readonly MethodInfo __dateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull;
        private static readonly MethodInfo __exists;
        private static readonly MethodInfo __field;
        private static readonly MethodInfo __isMissing;
        private static readonly MethodInfo __isNullOrMissing;
        private static readonly MethodInfo __sigmoid;

        // sets of methods
        private static readonly IReadOnlyMethodInfoSet __dateFromStringOverloads;
        private static readonly IReadOnlyMethodInfoSet __dateFromStringWithFormatOverloads;
        private static readonly IReadOnlyMethodInfoSet __dateFromStringWithTimezoneOverloads;

        // static constructor
        static MqlMethod()
        {
            // initialize methods before sets of methods
            __constantWithRepresentation = ReflectionInfo.Method((object value, BsonType representation) => Mql.Constant(value, representation));
            __constantWithSerializer = ReflectionInfo.Method((object value, IBsonSerializer<object> serializer) => Mql.Constant(value, serializer));
            __convert = ReflectionInfo.Method((object value, ConvertOptions<object> options) => Mql.Convert(value, options));
            __dateFromString = ReflectionInfo.Method((string dateStringl) => Mql.DateFromString(dateStringl));
            __dateFromStringWithFormat = ReflectionInfo.Method((string dateString, string format) => Mql.DateFromString(dateString, format));
            __dateFromStringWithFormatAndTimezone = ReflectionInfo.Method((string dateString, string format, string timezone) => Mql.DateFromString(dateString, format, timezone));
            __dateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull = ReflectionInfo.Method((string dateString, string format, string timezone, DateTime? onError, DateTime? onNull) => Mql.DateFromString(dateString, format, timezone, onError, onNull));
            __exists = ReflectionInfo.Method((object field) => Mql.Exists(field));
            __field = ReflectionInfo.Method((object container, string fieldName, IBsonSerializer<object> serializer) => Mql.Field<object, object>(container, fieldName, serializer));
            __isMissing = ReflectionInfo.Method((object field) => Mql.IsMissing(field));
            __isNullOrMissing = ReflectionInfo.Method((object field) => Mql.IsNullOrMissing(field));
            __sigmoid = ReflectionInfo.Method((double value) => Mql.Sigmoid(value));

            // initialize sets of methods after methods
            __dateFromStringOverloads = MethodInfoSet.Create(
            [
                __dateFromString,
                __dateFromStringWithFormat,
                __dateFromStringWithFormatAndTimezone,
                __dateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull
            ]);

            __dateFromStringWithFormatOverloads = MethodInfoSet.Create(
            [
               __dateFromStringWithFormat,
               __dateFromStringWithFormatAndTimezone,
               __dateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull
            ]);

            __dateFromStringWithTimezoneOverloads = MethodInfoSet.Create(
            [
                __dateFromStringWithFormatAndTimezone,
                __dateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull
            ]);
        }

        // public properties
        public static MethodInfo ConstantWithRepresentation => __constantWithRepresentation;
        public static MethodInfo ConstantWithSerializer => __constantWithSerializer;
        public static MethodInfo Convert => __convert;
        public static MethodInfo DateFromString => __dateFromString;
        public static MethodInfo DateFromStringWithFormat => __dateFromStringWithFormat;
        public static MethodInfo DateFromStringWithFormatAndTimezone => __dateFromStringWithFormatAndTimezone;
        public static MethodInfo DateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull => __dateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull;
        public static MethodInfo Exists => __exists;
        public static MethodInfo Field => __field;
        public static MethodInfo IsMissing => __isMissing;
        public static MethodInfo IsNullOrMissing => __isNullOrMissing;
        public static MethodInfo Sigmoid => __sigmoid;

        // sets of methods
        public static IReadOnlyMethodInfoSet DateFromStringOverloads => __dateFromStringOverloads;
        public static IReadOnlyMethodInfoSet DateFromStringWithFormatOverloads => __dateFromStringWithFormatOverloads;
        public static IReadOnlyMethodInfoSet DateFromStringWithTimezoneOverloads => __dateFromStringWithTimezoneOverloads;
    }
}
