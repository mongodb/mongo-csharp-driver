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
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers
{
    internal static class ConvertingNumericSerializer
    {
        public static IBsonSerializer Create(
            Type sourceType,
            Type targetType,
            IBsonSerializer sourceSerializer)
        {
            var numericConversionSerializerType = typeof(ConvertingNumericSerializer<,>).MakeGenericType(sourceType, targetType);
            return (IBsonSerializer)Activator.CreateInstance(numericConversionSerializerType, sourceSerializer);
        }
    }

    internal class ConvertingNumericSerializer<TSource, TTarget> : SerializerBase<TTarget>
    {
        private readonly IBsonSerializer<TSource> _sourceSerializer;

        public ConvertingNumericSerializer(IBsonSerializer<TSource> sourceSerializer)
        {
            _sourceSerializer = sourceSerializer ?? throw new ArgumentNullException(nameof(sourceSerializer));
        }

        public override TTarget Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var sourceValue = _sourceSerializer.Deserialize(context, args);
            return ConvertChecked<TSource, TTarget>(sourceValue);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TTarget value)
        {
            var sourceValue = ConvertChecked<TTarget, TSource>(value);
            _sourceSerializer.Serialize(context, args, sourceValue);
        }

        private TTo ConvertChecked<TFrom, TTo>(TFrom value)
        {
            var fromTypeCode = Type.GetTypeCode(typeof(TFrom));
            var toTypeCode = Type.GetTypeCode(typeof(TTo));

            var converted = (TTo)ConvertUnchecked(fromTypeCode, toTypeCode, value);
            var convertedBack = ConvertUnchecked(toTypeCode, fromTypeCode, converted);
            if (convertedBack.Equals(value))
            {
                return converted;
            }

            throw new BsonSerializationException($"The constant {value} could not be converted to {GetTypeCodeName(toTypeCode)} in order to be serialized using the proper serializer.");
        }

        private object ConvertUnchecked(TypeCode fromTypeCode, TypeCode toTypeCode, object value)
        {
            unchecked
            {
                return (fromTypeCode, toTypeCode) switch
                {
                    (TypeCode.Byte, TypeCode.Char) => (char)(byte)value,
                    (TypeCode.Byte, TypeCode.Decimal) => (decimal)(byte)value,
                    (TypeCode.Byte, TypeCode.Double) => (double)(byte)value,
                    (TypeCode.Byte, TypeCode.Int16) => (short)(byte)value,
                    (TypeCode.Byte, TypeCode.Int32) => (int)(byte)value,
                    (TypeCode.Byte, TypeCode.Int64) => (long)(byte)value,
                    (TypeCode.Byte, TypeCode.SByte) => (sbyte)(byte)value,
                    (TypeCode.Byte, TypeCode.Single) => (float)(byte)value,
                    (TypeCode.Byte, TypeCode.UInt16) => (ushort)(byte)value,
                    (TypeCode.Byte, TypeCode.UInt32) => (uint)(byte)value,
                    (TypeCode.Byte, TypeCode.UInt64) => (ulong)(byte)value,
                    (TypeCode.Char, TypeCode.Byte) => (byte)(char)value,
                    (TypeCode.Char, TypeCode.Decimal) => (decimal)(char)value,
                    (TypeCode.Char, TypeCode.Double) => (double)(char)value,
                    (TypeCode.Char, TypeCode.Int16) => (short)(char)value,
                    (TypeCode.Char, TypeCode.Int32) => (int)(char)value,
                    (TypeCode.Char, TypeCode.Int64) => (long)(char)value,
                    (TypeCode.Char, TypeCode.SByte) => (sbyte)(char)value,
                    (TypeCode.Char, TypeCode.Single) => (float)(char)value,
                    (TypeCode.Char, TypeCode.UInt16) => (ushort)(char)value,
                    (TypeCode.Char, TypeCode.UInt32) => (uint)(char)value,
                    (TypeCode.Char, TypeCode.UInt64) => (ulong)(char)value,
                    (TypeCode.Decimal, TypeCode.Byte) => (byte)(decimal)value,
                    (TypeCode.Decimal, TypeCode.Char) => (char)(decimal)value,
                    (TypeCode.Decimal, TypeCode.Double) => (double)(decimal)value,
                    (TypeCode.Decimal, TypeCode.Int16) => (short)(decimal)value,
                    (TypeCode.Decimal, TypeCode.Int32) => (int)(decimal)value,
                    (TypeCode.Decimal, TypeCode.Int64) => (long)(decimal)value,
                    (TypeCode.Decimal, TypeCode.SByte) => (sbyte)(decimal)value,
                    (TypeCode.Decimal, TypeCode.Single) => (float)(decimal)value,
                    (TypeCode.Decimal, TypeCode.UInt16) => (ushort)(decimal)value,
                    (TypeCode.Decimal, TypeCode.UInt32) => (uint)(decimal)value,
                    (TypeCode.Decimal, TypeCode.UInt64) => (ulong)(decimal)value,
                    (TypeCode.Double, TypeCode.Byte) => (byte)(double)value,
                    (TypeCode.Double, TypeCode.Char) => (char)(double)value,
                    (TypeCode.Double, TypeCode.Decimal) => (decimal)(double)value,
                    (TypeCode.Double, TypeCode.Int16) => (short)(double)value,
                    (TypeCode.Double, TypeCode.Int32) => (int)(double)value,
                    (TypeCode.Double, TypeCode.Int64) => (long)(double)value,
                    (TypeCode.Double, TypeCode.SByte) => (sbyte)(double)value,
                    (TypeCode.Double, TypeCode.Single) => (float)(double)value,
                    (TypeCode.Double, TypeCode.UInt16) => (ushort)(double)value,
                    (TypeCode.Double, TypeCode.UInt32) => (uint)(double)value,
                    (TypeCode.Double, TypeCode.UInt64) => (ulong)(double)value,
                    (TypeCode.Int16, TypeCode.Byte) => (byte)(short)value,
                    (TypeCode.Int16, TypeCode.Char) => (char)(short)value,
                    (TypeCode.Int16, TypeCode.Decimal) => (decimal)(short)value,
                    (TypeCode.Int16, TypeCode.Double) => (double)(short)value,
                    (TypeCode.Int16, TypeCode.Int32) => (int)(short)value,
                    (TypeCode.Int16, TypeCode.Int64) => (long)(short)value,
                    (TypeCode.Int16, TypeCode.SByte) => (sbyte)(short)value,
                    (TypeCode.Int16, TypeCode.Single) => (float)(short)value,
                    (TypeCode.Int16, TypeCode.UInt16) => (ushort)(short)value,
                    (TypeCode.Int16, TypeCode.UInt32) => (uint)(short)value,
                    (TypeCode.Int16, TypeCode.UInt64) => (ulong)(short)value,
                    (TypeCode.Int32, TypeCode.Byte) => (byte)(int)value,
                    (TypeCode.Int32, TypeCode.Char) => (char)(int)value,
                    (TypeCode.Int32, TypeCode.Decimal) => (decimal)(int)value,
                    (TypeCode.Int32, TypeCode.Double) => (double)(int)value,
                    (TypeCode.Int32, TypeCode.Int16) => (short)(int)value,
                    (TypeCode.Int32, TypeCode.Int64) => (long)(int)value,
                    (TypeCode.Int32, TypeCode.SByte) => (sbyte)(int)value,
                    (TypeCode.Int32, TypeCode.Single) => (float)(int)value,
                    (TypeCode.Int32, TypeCode.UInt16) => (ushort)(int)value,
                    (TypeCode.Int32, TypeCode.UInt32) => (uint)(int)value,
                    (TypeCode.Int32, TypeCode.UInt64) => (ulong)(int)value,
                    (TypeCode.Int64, TypeCode.Byte) => (byte)(long)value,
                    (TypeCode.Int64, TypeCode.Char) => (char)(long)value,
                    (TypeCode.Int64, TypeCode.Decimal) => (decimal)(long)value,
                    (TypeCode.Int64, TypeCode.Double) => (double)(long)value,
                    (TypeCode.Int64, TypeCode.Int16) => (short)(long)value,
                    (TypeCode.Int64, TypeCode.Int32) => (int)(long)value,
                    (TypeCode.Int64, TypeCode.SByte) => (sbyte)(long)value,
                    (TypeCode.Int64, TypeCode.Single) => (float)(long)value,
                    (TypeCode.Int64, TypeCode.UInt16) => (ushort)(long)value,
                    (TypeCode.Int64, TypeCode.UInt32) => (uint)(long)value,
                    (TypeCode.Int64, TypeCode.UInt64) => (ulong)(long)value,
                    (TypeCode.SByte, TypeCode.Byte) => (byte)(sbyte)value,
                    (TypeCode.SByte, TypeCode.Char) => (char)(sbyte)value,
                    (TypeCode.SByte, TypeCode.Decimal) => (decimal)(sbyte)value,
                    (TypeCode.SByte, TypeCode.Double) => (double)(sbyte)value,
                    (TypeCode.SByte, TypeCode.Int16) => (short)(sbyte)value,
                    (TypeCode.SByte, TypeCode.Int32) => (int)(sbyte)value,
                    (TypeCode.SByte, TypeCode.Int64) => (long)(sbyte)value,
                    (TypeCode.SByte, TypeCode.Single) => (float)(sbyte)value,
                    (TypeCode.SByte, TypeCode.UInt16) => (ushort)(sbyte)value,
                    (TypeCode.SByte, TypeCode.UInt32) => (uint)(sbyte)value,
                    (TypeCode.SByte, TypeCode.UInt64) => (ulong)(sbyte)value,
                    (TypeCode.Single, TypeCode.Byte) => (byte)(float)value,
                    (TypeCode.Single, TypeCode.Char) => (char)(float)value,
                    (TypeCode.Single, TypeCode.Decimal) => (decimal)(float)value,
                    (TypeCode.Single, TypeCode.Double) => (double)(float)value,
                    (TypeCode.Single, TypeCode.Int16) => (short)(float)value,
                    (TypeCode.Single, TypeCode.Int32) => (int)(float)value,
                    (TypeCode.Single, TypeCode.Int64) => (long)(float)value,
                    (TypeCode.Single, TypeCode.SByte) => (sbyte)(float)value,
                    (TypeCode.Single, TypeCode.UInt16) => (ushort)(float)value,
                    (TypeCode.Single, TypeCode.UInt32) => (uint)(float)value,
                    (TypeCode.Single, TypeCode.UInt64) => (ulong)(float)value,
                    (TypeCode.UInt16, TypeCode.Char) => (char)(ushort)value,
                    (TypeCode.UInt16, TypeCode.Decimal) => (decimal)(ushort)value,
                    (TypeCode.UInt16, TypeCode.Double) => (double)(ushort)value,
                    (TypeCode.UInt16, TypeCode.Int16) => (short)(ushort)value,
                    (TypeCode.UInt16, TypeCode.Int32) => (int)(ushort)value,
                    (TypeCode.UInt16, TypeCode.Int64) => (long)(ushort)value,
                    (TypeCode.UInt16, TypeCode.SByte) => (sbyte)(ushort)value,
                    (TypeCode.UInt16, TypeCode.Single) => (float)(ushort)value,
                    (TypeCode.UInt16, TypeCode.UInt32) => (uint)(ushort)value,
                    (TypeCode.UInt16, TypeCode.UInt64) => (ulong)(ushort)value,
                    (TypeCode.UInt32, TypeCode.Byte) => (byte)(uint)value,
                    (TypeCode.UInt32, TypeCode.Char) => (char)(uint)value,
                    (TypeCode.UInt32, TypeCode.Decimal) => (decimal)(uint)value,
                    (TypeCode.UInt32, TypeCode.Double) => (double)(uint)value,
                    (TypeCode.UInt32, TypeCode.Int16) => (short)(uint)value,
                    (TypeCode.UInt32, TypeCode.Int32) => (int)(uint)value,
                    (TypeCode.UInt32, TypeCode.Int64) => (long)(uint)value,
                    (TypeCode.UInt32, TypeCode.SByte) => (sbyte)(uint)value,
                    (TypeCode.UInt32, TypeCode.Single) => (float)(uint)value,
                    (TypeCode.UInt32, TypeCode.UInt16) => (ushort)(uint)value,
                    (TypeCode.UInt32, TypeCode.UInt64) => (ulong)(uint)value,
                    (TypeCode.UInt64, TypeCode.Byte) => (byte)(ulong)value,
                    (TypeCode.UInt64, TypeCode.Char) => (char)(ulong)value,
                    (TypeCode.UInt64, TypeCode.Int16) => (short)(ulong)value,
                    (TypeCode.UInt64, TypeCode.Decimal) => (decimal)(ulong)value,
                    (TypeCode.UInt64, TypeCode.Double) => (double)(ulong)value,
                    (TypeCode.UInt64, TypeCode.Int32) => (int)(ulong)value,
                    (TypeCode.UInt64, TypeCode.Int64) => (long)(ulong)value,
                    (TypeCode.UInt64, TypeCode.SByte) => (sbyte)(ulong)value,
                    (TypeCode.UInt64, TypeCode.Single) => (float)(ulong)value,
                    (TypeCode.UInt64, TypeCode.UInt16) => (ushort)(ulong)value,
                    (TypeCode.UInt64, TypeCode.UInt32) => (uint)(ulong)value,
                    _ => throw new Exception($"Invalid numeric conversion: {GetTypeCodeName(fromTypeCode)} to {GetTypeCodeName(toTypeCode)}.")
                };
            }
        }

        private string GetTypeCodeName(TypeCode typeCode)
        {
            return Enum.GetName(typeof(TypeCode), typeCode);
        }
    }
}
