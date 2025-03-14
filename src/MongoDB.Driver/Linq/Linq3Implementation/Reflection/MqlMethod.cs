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
        private static readonly MethodInfo __dateFromString;
        private static readonly MethodInfo __dateFromStringWithFormat;
        private static readonly MethodInfo __dateFromStringWithFormatAndTimezone;
        private static readonly MethodInfo __dateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull;
        private static readonly MethodInfo __exists;
        private static readonly MethodInfo __field;
        private static readonly MethodInfo __isMissing;
        private static readonly MethodInfo __isNullOrMissing;
        private static readonly MethodInfo __toBinDataFromDouble;

        private static readonly MethodInfo __toBinDataFromDoubleWithOnErrorAndOnNull;
        private static readonly MethodInfo __toBinDataFromInt;
        private static readonly MethodInfo __toBinDataFromIntWithOnErrorAndOnNull;
        private static readonly MethodInfo __toBinDataFromLong;
        private static readonly MethodInfo __toBinDataFromLongWithOnErrorAndOnNull;
        private static readonly MethodInfo __toBinDataFromString;
        private static readonly MethodInfo __toBinDataFromStringWithOnErrorAndOnNull;
        private static readonly MethodInfo __toDoubleFromBinData;
        private static readonly MethodInfo __toDoubleFromBinDataWithOnErrorAndOnNull;
        private static readonly MethodInfo __toIntFromBinData;
        private static readonly MethodInfo __toIntFromBinDataWithOnErrorAndOnNull;
        private static readonly MethodInfo __toLongFromBinData;
        private static readonly MethodInfo __toLongFromBinDataWithOnErrorAndOnNull;
        private static readonly MethodInfo __toStringFromBinData;
        private static readonly MethodInfo __toStringFromBinDataWithOnErrorAndOnNull;

        // static constructor
        static MqlMethod()
        {
            __constantWithRepresentation = ReflectionInfo.Method((object value, BsonType representation) => Mql.Constant(value, representation));
            __constantWithSerializer = ReflectionInfo.Method((object value, IBsonSerializer<object> serializer) => Mql.Constant(value, serializer));
            __dateFromString = ReflectionInfo.Method((string dateStringl) => Mql.DateFromString(dateStringl));
            __dateFromStringWithFormat = ReflectionInfo.Method((string dateString, string format) => Mql.DateFromString(dateString, format));
            __dateFromStringWithFormatAndTimezone = ReflectionInfo.Method((string dateString, string format, string timezone) => Mql.DateFromString(dateString, format, timezone));
            __dateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull = ReflectionInfo.Method((string dateString, string format, string timezone, DateTime? onError, DateTime? onNull) => Mql.DateFromString(dateString, format, timezone, onError, onNull));
            __exists = ReflectionInfo.Method((object field) => Mql.Exists(field));
            __field = ReflectionInfo.Method((object container, string fieldName, IBsonSerializer<object> serializer) => Mql.Field<object, object>(container, fieldName, serializer));
            __isMissing = ReflectionInfo.Method((object field) => Mql.IsMissing(field));
            __isNullOrMissing = ReflectionInfo.Method((object field) => Mql.IsNullOrMissing(field));

            // Convert methods
            __toBinDataFromDouble = ReflectionInfo.Method((double field, BsonBinarySubType subType, Mql.ConvertBinDataFormat format) => Mql.ConvertToBinData(field, subType, format));
            __toBinDataFromDoubleWithOnErrorAndOnNull = ReflectionInfo.Method((double field, BsonBinarySubType subType, Mql.ConvertBinDataFormat format, BsonBinaryData onError, BsonBinaryData onNull)
                => Mql.ConvertToBinData(field, subType, format, onError, onNull));
            __toBinDataFromInt = ReflectionInfo.Method((int field, BsonBinarySubType subType, Mql.ConvertBinDataFormat format) => Mql.ConvertToBinData(field, subType, format));
            __toBinDataFromIntWithOnErrorAndOnNull = ReflectionInfo.Method((int field, BsonBinarySubType subType, Mql.ConvertBinDataFormat format, BsonBinaryData onError, BsonBinaryData onNull)
                => Mql.ConvertToBinData(field, subType, format, onError, onNull));
            __toBinDataFromLong = ReflectionInfo.Method((long field, BsonBinarySubType subType, Mql.ConvertBinDataFormat format) => Mql.ConvertToBinData(field, subType, format));
            __toBinDataFromLongWithOnErrorAndOnNull = ReflectionInfo.Method((long field, BsonBinarySubType subType, Mql.ConvertBinDataFormat format, BsonBinaryData onError, BsonBinaryData onNull)
                => Mql.ConvertToBinData(field, subType, format, onError, onNull));
            __toBinDataFromString = ReflectionInfo.Method((string field, BsonBinarySubType subType, Mql.ConvertBinDataFormat format) => Mql.ConvertToBinData(field, subType, format));
            __toBinDataFromStringWithOnErrorAndOnNull = ReflectionInfo.Method((string field, BsonBinarySubType subType, Mql.ConvertBinDataFormat format, BsonBinaryData onError, BsonBinaryData onNull)
                => Mql.ConvertToBinData(field, subType, format, onError, onNull));

            __toDoubleFromBinData = ReflectionInfo.Method((BsonBinaryData field, Mql.ConvertBinDataFormat format) => Mql.ToDouble(field, format));
            __toDoubleFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, Mql.ConvertBinDataFormat format, double? onError, double? onNull) => Mql.ToDouble(field, format, onError, onNull));

            __toIntFromBinData = ReflectionInfo.Method((BsonBinaryData field, Mql.ConvertBinDataFormat format) => Mql.ConvertToInt(field, format));
            __toIntFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, Mql.ConvertBinDataFormat format, int? onError, int? onNull) => Mql.ToInt(field, format, onError, onNull));

            __toLongFromBinData = ReflectionInfo.Method((BsonBinaryData field, Mql.ConvertBinDataFormat format) => Mql.ConvertToLong(field, format));
            __toLongFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, Mql.ConvertBinDataFormat format, long? onError, long? onNull) => Mql.ConvertToLong(field, format, onError, onNull));

            __toStringFromBinData = ReflectionInfo.Method((BsonBinaryData field, Mql.ConvertBinDataFormat format) => Mql.ConvertToString(field, format));
            __toStringFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, Mql.ConvertBinDataFormat format, string onError, string onNull) => Mql.ConvertToString(field, format, onError, onNull));
        }

        // public properties
        public static MethodInfo ConstantWithRepresentation => __constantWithRepresentation;
        public static MethodInfo ConstantWithSerializer => __constantWithSerializer;
        public static MethodInfo DateFromString => __dateFromString;
        public static MethodInfo DateFromStringWithFormat => __dateFromStringWithFormat;
        public static MethodInfo DateFromStringWithFormatAndTimezone => __dateFromStringWithFormatAndTimezone;
        public static MethodInfo DateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull => __dateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull;
        public static MethodInfo Exists => __exists;
        public static MethodInfo Field => __field;
        public static MethodInfo IsMissing => __isMissing;
        public static MethodInfo IsNullOrMissing => __isNullOrMissing;
        public static MethodInfo ToBinDataFromDouble => __toBinDataFromDouble;
        public static MethodInfo ToBinDataFromDoubleWithOnErrorAndOnNull => __toBinDataFromDoubleWithOnErrorAndOnNull;

        public static MethodInfo ToBinDataFromInt => __toBinDataFromInt;
        public static MethodInfo ToBinDataFromIntWithOnErrorAndOnNull => __toBinDataFromIntWithOnErrorAndOnNull;

        public static MethodInfo ToBinDataFromLong => __toBinDataFromLong;
        public static MethodInfo ToBinDataFromLongWithOnErrorAndOnNull => __toBinDataFromLongWithOnErrorAndOnNull;

        public static MethodInfo ToBinDataFromString => __toBinDataFromString;
        public static MethodInfo ToBinDataFromStringWithOnErrorAndOnNull => __toBinDataFromStringWithOnErrorAndOnNull;
        public static MethodInfo ToDoubleFromBinData => __toDoubleFromBinData;
        public static MethodInfo ToDoubleFromBinDataWithOnErrorAndOnNull => __toDoubleFromBinDataWithOnErrorAndOnNull;
        public static MethodInfo ToIntFromBinData => __toIntFromBinData;
        public static MethodInfo ToIntFromBinDataWithOnErrorAndOnNull => __toIntFromBinDataWithOnErrorAndOnNull;
        public static MethodInfo ToLongFromBinData => __toLongFromBinData;
        public static MethodInfo ToLongFromBinDataWithOnErrorAndOnNull => __toLongFromBinDataWithOnErrorAndOnNull;
        public static MethodInfo ToStringFromBinData => __toStringFromBinData;
        public static MethodInfo ToStringFromBinDataWithOnErrorAndOnNull => __toStringFromBinDataWithOnErrorAndOnNull;
    }
}
