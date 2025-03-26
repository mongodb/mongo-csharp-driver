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
        private static readonly MethodInfo __toBinDataFromNullableDouble;
        private static readonly MethodInfo __toBinDataFromNullableDoubleWithOnErrorAndOnNull;
        private static readonly MethodInfo __toBinDataFromNullableInt;
        private static readonly MethodInfo __toBinDataFromNullableIntWithOnErrorAndOnNull;
        private static readonly MethodInfo __toBinDataFromNullableLong;
        private static readonly MethodInfo __toBinDataFromNullableLongWithOnErrorAndOnNull;
        private static readonly MethodInfo __toBinDataFromString;
        private static readonly MethodInfo __toDoubleFromBinData;
        private static readonly MethodInfo __toDoubleFromBinDataWithOnErrorAndOnNull;
        private static readonly MethodInfo __toIntFromBinData;
        private static readonly MethodInfo __toIntFromBinDataWithOnErrorAndOnNull;
        private static readonly MethodInfo __toLongFromBinData;
        private static readonly MethodInfo __toLongFromBinDataWithOnErrorAndOnNull;
        private static readonly MethodInfo __toBinDataFromStringWithOnErrorAndOnNull;
        private static readonly MethodInfo __toNullableDoubleFromBinData;
        private static readonly MethodInfo __toNullableDoubleFromBinDataWithOnErrorAndOnNull;
        private static readonly MethodInfo __toNullableIntFromBinData;
        private static readonly MethodInfo __toNullableIntFromBinDataWithOnErrorAndOnNull;
        private static readonly MethodInfo __toNullableLongFromBinData;
        private static readonly MethodInfo __toNullableLongFromBinDataWithOnErrorAndOnNull;
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

            __toBinDataFromDouble = ReflectionInfo.Method((double field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBsonBinaryData(field, subType, byteOrder));
            __toBinDataFromDoubleWithOnErrorAndOnNull = ReflectionInfo.Method((double field, BsonBinarySubType subType, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, byteOrder, options));
            __toBinDataFromInt = ReflectionInfo.Method((int field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBsonBinaryData(field, subType, byteOrder));
            __toBinDataFromIntWithOnErrorAndOnNull = ReflectionInfo.Method((int field, BsonBinarySubType subType, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, byteOrder, options));
            __toBinDataFromLong = ReflectionInfo.Method((long field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBsonBinaryData(field, subType, byteOrder));
            __toBinDataFromLongWithOnErrorAndOnNull = ReflectionInfo.Method((long field, BsonBinarySubType subType, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, byteOrder, options));

            __toBinDataFromNullableDouble = ReflectionInfo.Method((double? field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBsonBinaryData(field, subType, byteOrder));
            __toBinDataFromNullableDoubleWithOnErrorAndOnNull = ReflectionInfo.Method((double? field, BsonBinarySubType subType, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, byteOrder, options));
            __toBinDataFromNullableInt = ReflectionInfo.Method((int? field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBsonBinaryData(field, subType, byteOrder));
            __toBinDataFromNullableIntWithOnErrorAndOnNull = ReflectionInfo.Method((int? field, BsonBinarySubType subType, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, byteOrder, options));
            __toBinDataFromNullableLong = ReflectionInfo.Method((long? field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBsonBinaryData(field, subType, byteOrder));
            __toBinDataFromNullableLongWithOnErrorAndOnNull = ReflectionInfo.Method((long? field, BsonBinarySubType subType, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, byteOrder, options));
            __toBinDataFromString = ReflectionInfo.Method((string field, BsonBinarySubType subType, string format) => Mql.ToBsonBinaryData(field, subType, format));
            __toBinDataFromStringWithOnErrorAndOnNull = ReflectionInfo.Method((string field, BsonBinarySubType subType, string format, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, format, options));

            __toDoubleFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToDouble(field, byteOrder));
            __toDoubleFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, ConvertOptions<double> options) => Mql.ToDouble(field, byteOrder, options));
            __toIntFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToInt(field, byteOrder));
            __toIntFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, ConvertOptions<int> options) => Mql.ToInt(field, byteOrder, options));
            __toLongFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToLong(field, byteOrder));
            __toLongFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, ConvertOptions<long> options) => Mql.ToLong(field, byteOrder, options));

            __toNullableDoubleFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToNullableDouble(field, byteOrder));
            __toNullableDoubleFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, ConvertOptions<double?> options) => Mql.ToNullableDouble(field, byteOrder, options));
            __toNullableIntFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToNullableInt(field, byteOrder));
            __toNullableIntFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, ConvertOptions<int?> options) => Mql.ToNullableInt(field, byteOrder, options));
            __toNullableLongFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToNullableLong(field, byteOrder));
            __toNullableLongFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, ConvertOptions<long?> options) => Mql.ToNullableLong(field, byteOrder, options));
            __toStringFromBinData = ReflectionInfo.Method((BsonBinaryData field, string format) => Mql.ToString(field, format));
            __toStringFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, string format, ConvertOptions<string> options) => Mql.ToString(field, format, options));
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
        public static MethodInfo ToBinDataFromNullableDouble => __toBinDataFromNullableDouble;
        public static MethodInfo ToBinDataFromNullableDoubleWithOnErrorAndOnNull => __toBinDataFromNullableDoubleWithOnErrorAndOnNull;
        public static MethodInfo ToBinDataFromNullableInt => __toBinDataFromNullableInt;
        public static MethodInfo ToBinDataFromNullableIntWithOnErrorAndOnNull => __toBinDataFromNullableIntWithOnErrorAndOnNull;
        public static MethodInfo ToBinDataFromNullableLong => __toBinDataFromNullableLong;
        public static MethodInfo ToBinDataFromNullableLongWithOnErrorAndOnNull => __toBinDataFromNullableLongWithOnErrorAndOnNull;
        public static MethodInfo ToBinDataFromString => __toBinDataFromString;
        public static MethodInfo ToDoubleFromBinData => __toDoubleFromBinData;
        public static MethodInfo ToDoubleFromBinDataWithOnErrorAndOnNull => __toDoubleFromBinDataWithOnErrorAndOnNull;
        public static MethodInfo ToIntFromBinData => __toIntFromBinData;
        public static MethodInfo ToIntFromBinDataWithOnErrorAndOnNull => __toIntFromBinDataWithOnErrorAndOnNull;
        public static MethodInfo ToLongFromBinData => __toLongFromBinData;
        public static MethodInfo ToLongFromBinDataWithOnErrorAndOnNull => __toLongFromBinDataWithOnErrorAndOnNull;
        public static MethodInfo ToBinDataFromStringWithOnErrorAndOnNull => __toBinDataFromStringWithOnErrorAndOnNull;
        public static MethodInfo ToNullableDoubleFromBinData => __toNullableDoubleFromBinData;
        public static MethodInfo ToNullableDoubleFromBinDataWithOnErrorAndOnNull => __toNullableDoubleFromBinDataWithOnErrorAndOnNull;
        public static MethodInfo ToNullableIntFromBinData => __toNullableIntFromBinData;
        public static MethodInfo ToNullableIntFromBinDataWithOnErrorAndOnNull => __toNullableIntFromBinDataWithOnErrorAndOnNull;
        public static MethodInfo ToNullableLongFromBinData => __toNullableLongFromBinData;
        public static MethodInfo ToNullableLongFromBinDataWithOnErrorAndOnNull => __toNullableLongFromBinDataWithOnErrorAndOnNull;
        public static MethodInfo ToStringFromBinData => __toStringFromBinData;
        public static MethodInfo ToStringFromBinDataWithOnErrorAndOnNull => __toStringFromBinDataWithOnErrorAndOnNull;
    }
}
