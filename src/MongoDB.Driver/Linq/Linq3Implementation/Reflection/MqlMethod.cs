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
        private static readonly MethodInfo __toBinDataFromDoubleWithOptions;
        private static readonly MethodInfo __toBinDataFromInt;
        private static readonly MethodInfo __toBinDataFromIntWithOptions;
        private static readonly MethodInfo __toBinDataFromLong;
        private static readonly MethodInfo __toBinDataFromLongWithOptions;
        private static readonly MethodInfo __toBinDataFromNullableDouble;
        private static readonly MethodInfo __toBinDataFromNullableDoubleWithOptions;
        private static readonly MethodInfo __toBinDataFromNullableInt;
        private static readonly MethodInfo __toBinDataFromNullableIntWithOptions;
        private static readonly MethodInfo __toBinDataFromNullableLong;
        private static readonly MethodInfo __toBinDataFromNullableLongWithOptions;
        private static readonly MethodInfo __toBinDataFromString;
        private static readonly MethodInfo __toDoubleFromBinData;
        private static readonly MethodInfo __toDoubleFromBinDataWithOptions;
        private static readonly MethodInfo __toIntFromBinData;
        private static readonly MethodInfo __toIntFromBinDataWithOptions;
        private static readonly MethodInfo __toLongFromBinData;
        private static readonly MethodInfo __toLongFromBinDataWithOptions;
        private static readonly MethodInfo __toBinDataFromStringWithOptions;
        private static readonly MethodInfo __toNullableDoubleFromBinData;
        private static readonly MethodInfo __toNullableDoubleFromBinDataWithOptions;
        private static readonly MethodInfo __toNullableIntFromBinData;
        private static readonly MethodInfo __toNullableIntFromBinDataWithOptions;
        private static readonly MethodInfo __toNullableLongFromBinData;
        private static readonly MethodInfo __toNullableLongFromBinDataWithOptions;
        private static readonly MethodInfo __toStringFromBinData;
        private static readonly MethodInfo __toStringFromBinDataWithOptions;

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
            __toBinDataFromDoubleWithOptions = ReflectionInfo.Method((double field, BsonBinarySubType subType, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, byteOrder, options));
            __toBinDataFromInt = ReflectionInfo.Method((int field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBsonBinaryData(field, subType, byteOrder));
            __toBinDataFromIntWithOptions = ReflectionInfo.Method((int field, BsonBinarySubType subType, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, byteOrder, options));
            __toBinDataFromLong = ReflectionInfo.Method((long field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBsonBinaryData(field, subType, byteOrder));
            __toBinDataFromLongWithOptions = ReflectionInfo.Method((long field, BsonBinarySubType subType, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, byteOrder, options));

            __toBinDataFromNullableDouble = ReflectionInfo.Method((double? field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBsonBinaryData(field, subType, byteOrder));
            __toBinDataFromNullableDoubleWithOptions = ReflectionInfo.Method((double? field, BsonBinarySubType subType, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, byteOrder, options));
            __toBinDataFromNullableInt = ReflectionInfo.Method((int? field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBsonBinaryData(field, subType, byteOrder));
            __toBinDataFromNullableIntWithOptions = ReflectionInfo.Method((int? field, BsonBinarySubType subType, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, byteOrder, options));
            __toBinDataFromNullableLong = ReflectionInfo.Method((long? field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBsonBinaryData(field, subType, byteOrder));
            __toBinDataFromNullableLongWithOptions = ReflectionInfo.Method((long? field, BsonBinarySubType subType, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, byteOrder, options));
            __toBinDataFromString = ReflectionInfo.Method((string field, BsonBinarySubType subType, string format) => Mql.ToBsonBinaryData(field, subType, format));
            __toBinDataFromStringWithOptions = ReflectionInfo.Method((string field, BsonBinarySubType subType, string format, ConvertOptions<BsonValue> options)
                => Mql.ToBsonBinaryData(field, subType, format, options));

            __toDoubleFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToDouble(field, byteOrder));
            __toDoubleFromBinDataWithOptions = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, ConvertOptions<double> options) => Mql.ToDouble(field, byteOrder, options));
            __toIntFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToInt(field, byteOrder));
            __toIntFromBinDataWithOptions = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, ConvertOptions<int> options) => Mql.ToInt(field, byteOrder, options));
            __toLongFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToLong(field, byteOrder));
            __toLongFromBinDataWithOptions = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, ConvertOptions<long> options) => Mql.ToLong(field, byteOrder, options));

            __toNullableDoubleFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToNullableDouble(field, byteOrder));
            __toNullableDoubleFromBinDataWithOptions = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, ConvertOptions<double?> options) => Mql.ToNullableDouble(field, byteOrder, options));
            __toNullableIntFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToNullableInt(field, byteOrder));
            __toNullableIntFromBinDataWithOptions = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, ConvertOptions<int?> options) => Mql.ToNullableInt(field, byteOrder, options));
            __toNullableLongFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToNullableLong(field, byteOrder));
            __toNullableLongFromBinDataWithOptions = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, ConvertOptions<long?> options) => Mql.ToNullableLong(field, byteOrder, options));
            __toStringFromBinData = ReflectionInfo.Method((BsonBinaryData field, string format) => Mql.ToString(field, format));
            __toStringFromBinDataWithOptions = ReflectionInfo.Method((BsonBinaryData field, string format, ConvertOptions<string> options) => Mql.ToString(field, format, options));
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
        public static MethodInfo ToBinDataFromDoubleWithOptions => __toBinDataFromDoubleWithOptions;
        public static MethodInfo ToBinDataFromInt => __toBinDataFromInt;
        public static MethodInfo ToBinDataFromIntWithOptions => __toBinDataFromIntWithOptions;
        public static MethodInfo ToBinDataFromLong => __toBinDataFromLong;
        public static MethodInfo ToBinDataFromLongWithOptions => __toBinDataFromLongWithOptions;
        public static MethodInfo ToBinDataFromNullableDouble => __toBinDataFromNullableDouble;
        public static MethodInfo ToBinDataFromNullableDoubleWithOptions => __toBinDataFromNullableDoubleWithOptions;
        public static MethodInfo ToBinDataFromNullableInt => __toBinDataFromNullableInt;
        public static MethodInfo ToBinDataFromNullableIntWithOptions => __toBinDataFromNullableIntWithOptions;
        public static MethodInfo ToBinDataFromNullableLong => __toBinDataFromNullableLong;
        public static MethodInfo ToBinDataFromNullableLongWithOptions => __toBinDataFromNullableLongWithOptions;
        public static MethodInfo ToBinDataFromString => __toBinDataFromString;
        public static MethodInfo ToDoubleFromBinData => __toDoubleFromBinData;
        public static MethodInfo ToDoubleFromBinDataWithOptions => __toDoubleFromBinDataWithOptions;
        public static MethodInfo ToIntFromBinData => __toIntFromBinData;
        public static MethodInfo ToIntFromBinDataWithOptions => __toIntFromBinDataWithOptions;
        public static MethodInfo ToLongFromBinData => __toLongFromBinData;
        public static MethodInfo ToLongFromBinDataWithOptions => __toLongFromBinDataWithOptions;
        public static MethodInfo ToBinDataFromStringWithOptions => __toBinDataFromStringWithOptions;
        public static MethodInfo ToNullableDoubleFromBinData => __toNullableDoubleFromBinData;
        public static MethodInfo ToNullableDoubleFromBinDataWithOptions => __toNullableDoubleFromBinDataWithOptions;
        public static MethodInfo ToNullableIntFromBinData => __toNullableIntFromBinData;
        public static MethodInfo ToNullableIntFromBinDataWithOptions => __toNullableIntFromBinDataWithOptions;
        public static MethodInfo ToNullableLongFromBinData => __toNullableLongFromBinData;
        public static MethodInfo ToNullableLongFromBinDataWithOptions => __toNullableLongFromBinDataWithOptions;
        public static MethodInfo ToStringFromBinData => __toStringFromBinData;
        public static MethodInfo ToStringFromBinDataWithOptions => __toStringFromBinDataWithOptions;
    }
}
