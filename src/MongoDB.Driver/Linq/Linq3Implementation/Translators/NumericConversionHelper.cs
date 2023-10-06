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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators
{
    /// <summary>
    /// Helper methods related to numeric conversions.
    /// </summary>
    internal static class NumericConversionHelper
    {
        public static IBsonSerializer CreateNumericConversionSerializer(
            Type sourceType,
            Type targetType,
            IBsonSerializer sourceSerializer)
        {
            if (ConvertHelper.IsWideningConvert(sourceType, targetType) &&
                sourceSerializer is IBsonNumericSerializer sourceNumericSerializer &&
                sourceNumericSerializer.Representation.IsNumericRepresentation())
            {
                return CreateWiderNumericSerializer(targetType, sourceNumericSerializer.Converter);
            }
            else
            {
                return ConvertingNumericSerializer.Create(sourceType, targetType, sourceSerializer);
            }
        }

        public static IBsonSerializer CreateWiderNumericSerializer(Type widerType, RepresentationConverter converter)
        {
            return
                Type.GetTypeCode(widerType) switch
                {
                    TypeCode.Char => new CharSerializer(BsonType.Int32, converter),
                    TypeCode.Decimal => new DecimalSerializer(BsonType.Decimal128, converter),
                    TypeCode.Double => new DoubleSerializer(BsonType.Double, converter),
                    TypeCode.Int16 => new Int16Serializer(BsonType.Int32, converter),
                    TypeCode.Int32 => new Int32Serializer(BsonType.Int32, converter),
                    TypeCode.Int64 => new Int64Serializer(BsonType.Int64, converter),
                    TypeCode.SByte => new SByteSerializer(BsonType.Int32, converter),
                    TypeCode.Single => new SingleSerializer(BsonType.Double, converter),
                    TypeCode.UInt16 => new UInt16Serializer(BsonType.Int32, converter),
                    TypeCode.UInt32 => new UInt32Serializer(BsonType.Int32, converter),
                    TypeCode.UInt64 => new UInt64Serializer(BsonType.Int64, converter),
                    _ => throw new ArgumentException($"Unexpected type code: {Type.GetTypeCode(widerType)}"),
                };
        }

        public static bool IsNumericConversion(Type sourceType, Type targetType)
        {
            return IsNumericType(sourceType) && IsNumericType(targetType);
        }

        public static bool IsNullableNumericConversion(Type sourceType, Type targetType, out Type underlyingSourceType, out Type underlyingTargetType)
        {
            if (sourceType.IsNullable(out underlyingSourceType) &&
                targetType.IsNullable(out underlyingTargetType) &&
                IsNumericConversion(underlyingSourceType, underlyingTargetType))
            {
                return true;
            }

            underlyingSourceType = null;
            underlyingTargetType = null;
            return false;
        }

        private static bool IsNumericRepresentation(this BsonType bsonType)
        {
            return bsonType switch
            {
                BsonType.Decimal128 or
                BsonType.Double or
                BsonType.Int32 or
                BsonType.Int64 => true,
                _ => false
            };
        }

        private static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;

                default:
                    return false;
            }
        }
    }
}
