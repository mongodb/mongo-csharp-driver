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

        private static readonly MethodInfo __convertToBinDataFromDouble;
        private static readonly MethodInfo __convertToBinDataFromDoubleWithOnErrorAndOnNull;
        private static readonly MethodInfo __convertToBinDataFromInt;
        private static readonly MethodInfo __convertToBinDataFromIntWithOnErrorAndOnNull;
        private static readonly MethodInfo __convertToBinDataFromLong;
        private static readonly MethodInfo __convertToBinDataFromLongWithOnErrorAndOnNull;
        private static readonly MethodInfo __convertToBinDataFromString;
        private static readonly MethodInfo __convertToBinDataFromStringWithOnErrorAndOnNull;
        private static readonly MethodInfo __convertToNullableDoubleFromBinData;
        private static readonly MethodInfo __convertToNullableDoubleFromBinDataWithOnErrorAndOnNull;
        private static readonly MethodInfo __convertToNullableIntFromBinData;
        private static readonly MethodInfo __convertToNullableIntFromBinDataWithOnErrorAndOnNull;
        private static readonly MethodInfo __convertToNullableLongFromBinData;
        private static readonly MethodInfo __convertToNullableLongFromBinDataWithOnErrorAndOnNull;
        private static readonly MethodInfo __convertToStringFromBinData;
        private static readonly MethodInfo __convertToStringFromBinDataWithOnErrorAndOnNull;

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
            __convertToBinDataFromDouble = ReflectionInfo.Method((double? field, BsonBinarySubType subType, Mql.ByteOrder byteOrder) => Mql.ConvertToBinData(field, subType, byteOrder));
            __convertToBinDataFromDoubleWithOnErrorAndOnNull = ReflectionInfo.Method((double? field, BsonBinarySubType subType, Mql.ByteOrder byteOrder, BsonBinaryData onError, BsonBinaryData onNull)
                => Mql.ConvertToBinData(field, subType, byteOrder, onError, onNull));
            __convertToBinDataFromInt = ReflectionInfo.Method((int? field, BsonBinarySubType subType, Mql.ByteOrder byteOrder) => Mql.ConvertToBinData(field, subType, byteOrder));
            __convertToBinDataFromIntWithOnErrorAndOnNull = ReflectionInfo.Method((int? field, BsonBinarySubType subType, Mql.ByteOrder byteOrder, BsonBinaryData onError, BsonBinaryData onNull)
                => Mql.ConvertToBinData(field, subType, byteOrder, onError, onNull));
            __convertToBinDataFromLong = ReflectionInfo.Method((long? field, BsonBinarySubType subType, Mql.ByteOrder byteOrder) => Mql.ConvertToBinData(field, subType, byteOrder));
            __convertToBinDataFromLongWithOnErrorAndOnNull = ReflectionInfo.Method((long? field, BsonBinarySubType subType, Mql.ByteOrder byteOrder, BsonBinaryData onError, BsonBinaryData onNull)
                => Mql.ConvertToBinData(field, subType, byteOrder, onError, onNull));
            __convertToBinDataFromString = ReflectionInfo.Method((string field, BsonBinarySubType subType, string format) => Mql.ConvertToBinData(field, subType, format));
            __convertToBinDataFromStringWithOnErrorAndOnNull = ReflectionInfo.Method((string field, BsonBinarySubType subType, string format, BsonBinaryData onError, BsonBinaryData onNull)
                => Mql.ConvertToBinData(field, subType, format, onError, onNull));

            __convertToNullableDoubleFromBinData = ReflectionInfo.Method((BsonBinaryData field, Mql.ByteOrder byteOrder) => Mql.ConvertToNullableDouble(field, byteOrder));
            __convertToNullableDoubleFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, Mql.ByteOrder byteOrder, double? onError, double? onNull) => Mql.ConvertToNullableDouble(field, byteOrder, onError, onNull));

            __convertToNullableIntFromBinData = ReflectionInfo.Method((BsonBinaryData field, Mql.ByteOrder byteOrder) => Mql.ConvertToNullableInt(field, byteOrder));
            __convertToNullableIntFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, Mql.ByteOrder byteOrder, int? onError, int? onNull) => Mql.ConvertToNullableInt(field, byteOrder, onError, onNull));

            __convertToNullableLongFromBinData = ReflectionInfo.Method((BsonBinaryData field, Mql.ByteOrder byteOrder) => Mql.ConvertToNullableLong(field, byteOrder));
            __convertToNullableLongFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, Mql.ByteOrder byteOrder, long? onError, long? onNull) => Mql.ConvertToNullableLong(field, byteOrder, onError, onNull));

            __convertToStringFromBinData = ReflectionInfo.Method((BsonBinaryData field, string format) => Mql.ConvertToString(field, format));
            __convertToStringFromBinDataWithOnErrorAndOnNull = ReflectionInfo.Method((BsonBinaryData field, string format, string onError, string onNull) => Mql.ConvertToString(field, format, onError, onNull));
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
        public static MethodInfo ConvertToBinDataFromDouble => __convertToBinDataFromDouble;
        public static MethodInfo ConvertToBinDataFromDoubleWithOnErrorAndOnNull => __convertToBinDataFromDoubleWithOnErrorAndOnNull;
        public static MethodInfo ConvertToBinDataFromInt => __convertToBinDataFromInt;
        public static MethodInfo ConvertToBinDataFromIntWithOnErrorAndOnNull => __convertToBinDataFromIntWithOnErrorAndOnNull;
        public static MethodInfo ConvertToBinDataFromLong => __convertToBinDataFromLong;
        public static MethodInfo ConvertToBinDataFromLongWithOnErrorAndOnNull => __convertToBinDataFromLongWithOnErrorAndOnNull;
        public static MethodInfo ConvertToBinDataFromString => __convertToBinDataFromString;
        public static MethodInfo ConvertToBinDataFromStringWithOnErrorAndOnNull => __convertToBinDataFromStringWithOnErrorAndOnNull;
        public static MethodInfo ConvertToNullableDoubleFromBinData => __convertToNullableDoubleFromBinData;
        public static MethodInfo ConvertToNullableDoubleFromBinDataWithOnErrorAndOnNull => __convertToNullableDoubleFromBinDataWithOnErrorAndOnNull;
        public static MethodInfo ConvertToNullableIntFromBinData => __convertToNullableIntFromBinData;
        public static MethodInfo ConvertToNullableIntFromBinDataWithOnErrorAndOnNull => __convertToNullableIntFromBinDataWithOnErrorAndOnNull;
        public static MethodInfo ConvertToNullableLongFromBinData => __convertToNullableLongFromBinData;
        public static MethodInfo ConvertToNullableLongFromBinDataWithOnErrorAndOnNull => __convertToNullableLongFromBinDataWithOnErrorAndOnNull;
        public static MethodInfo ConvertToStringFromBinData => __convertToStringFromBinData;
        public static MethodInfo ConvertToStringFromBinDataWithOnErrorAndOnNull => __convertToStringFromBinDataWithOnErrorAndOnNull;
    }
}
