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
            __toBinDataFromDouble = ReflectionInfo.Method((double? field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBinData(field, subType, byteOrder));
            __toBinDataFromDoubleWithOnErrorAndOnNull = ReflectionInfo.Method((double? field, BsonBinarySubType subType, ByteOrder byteOrder, BsonBinaryData onError, BsonBinaryData onNull)
                => Mql.ToBinData(field, subType, byteOrder, onError, onNull));
            __toBinDataFromInt = ReflectionInfo.Method((int? field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBinData(field, subType, byteOrder));
            __toBinDataFromIntWithOnErrorAndOnNull = ReflectionInfo.Method((int? field, BsonBinarySubType subType, ByteOrder byteOrder, BsonBinaryData onError, BsonBinaryData onNull)
                => Mql.ToBinData(field, subType, byteOrder, onError, onNull));
            __toBinDataFromLong = ReflectionInfo.Method((long? field, BsonBinarySubType subType, ByteOrder byteOrder) => Mql.ToBinData(field, subType, byteOrder));
            __toBinDataFromLongWithOnErrorAndOnNull = ReflectionInfo.Method((long? field, BsonBinarySubType subType, ByteOrder byteOrder, BsonBinaryData onError, BsonBinaryData onNull)
                => Mql.ToBinData(field, subType, byteOrder, onError, onNull));
            __toBinDataFromString = ReflectionInfo.Method((string field, BsonBinarySubType subType, string format) => Mql.ToBinData(field, subType, format));
            __toBinDataFromStringWithOnErrorAndOnNull = ReflectionInfo.Method((string field, BsonBinarySubType subType, string format, BsonBinaryData onError, BsonBinaryData onNull)
                => Mql.ToBinData(field, subType, format, onError, onNull));

            __toNullableDoubleFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToNullableDouble(field, byteOrder));
            __toNullableDoubleFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, double? onError, double? onNull) => Mql.ToNullableDouble(field, byteOrder, onError, onNull));

            __toNullableIntFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToNullableInt(field, byteOrder));
            __toNullableIntFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, int? onError, int? onNull) => Mql.ToNullableInt(field, byteOrder, onError, onNull));

            __toNullableLongFromBinData = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder) => Mql.ToNullableLong(field, byteOrder));
            __toNullableLongFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, ByteOrder byteOrder, long? onError, long? onNull) => Mql.ToNullableLong(field, byteOrder, onError, onNull));

            __toStringFromBinData = ReflectionInfo.Method((BsonBinaryData field, string format) => Mql.ToString(field, format));
            __toStringFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, string format, string onError, string onNull) => Mql.ToString(field, format, onError, onNull));
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
