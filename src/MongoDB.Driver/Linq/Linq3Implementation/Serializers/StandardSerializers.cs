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

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers
{
    internal static class StandardSerializers
    {
        private static readonly IBsonSerializer<bool> __booleanSerializer;
        private static readonly IBsonSerializer<byte> __byteSerializer;
        private static readonly IBsonSerializer<char> __charSerializer;
        private static readonly IBsonSerializer<decimal> __decimalSerializer;
        private static readonly IBsonSerializer<double> __doubleSerializer;
        private static readonly IBsonSerializer<short> __int16Serializer;
        private static readonly IBsonSerializer<int> __int32Serializer;
        private static readonly IBsonSerializer<long> __int64Serializer;
        private static readonly IBsonSerializer<sbyte> __sbyteSerializer;
        private static readonly IBsonSerializer<float> __singleSerializer;
        private static readonly IBsonSerializer<ushort> __uint16Serializer;
        private static readonly IBsonSerializer<uint> __uint32Serializer;
        private static readonly IBsonSerializer<ulong> __uint64Serializer;

        static StandardSerializers()
        {
            var strictConverter = new RepresentationConverter(allowOverflow: false, allowTruncation: false);
            var allowTruncationConverter = new RepresentationConverter(allowOverflow: false, allowTruncation: true);

            __booleanSerializer = new BooleanSerializer(BsonType.Boolean);
            __byteSerializer = new ByteSerializer(BsonType.Int32);
            __charSerializer = new CharSerializer(BsonType.Int32);
            __decimalSerializer = new DecimalSerializer(BsonType.Decimal128, allowTruncationConverter);
            __doubleSerializer = new DoubleSerializer(BsonType.Double, strictConverter);
            __int16Serializer = new Int16Serializer(BsonType.Int32, strictConverter);
            __int32Serializer = new Int32Serializer(BsonType.Int32, strictConverter);
            __int64Serializer = new Int64Serializer(BsonType.Int64, strictConverter);
            __sbyteSerializer = new SByteSerializer(BsonType.Int32);
            __singleSerializer = new SingleSerializer(BsonType.Double, allowTruncationConverter);
            __uint16Serializer = new UInt16Serializer(BsonType.Int32, strictConverter);
            __uint32Serializer = new UInt32Serializer(BsonType.Int32, strictConverter);
            __uint64Serializer = new UInt64Serializer(BsonType.Int64, strictConverter);
        }

        public static IBsonSerializer<bool> BooleanSerializer => __booleanSerializer;
        public static IBsonSerializer<byte> ByteSerializer => __byteSerializer;
        public static IBsonSerializer<char> CharSerializer => __charSerializer;
        public static IBsonSerializer<decimal> DecimalSerializer => __decimalSerializer;
        public static IBsonSerializer<double> DoubleSerializer => __doubleSerializer;
        public static IBsonSerializer<short> Int16Serializer => __int16Serializer;
        public static IBsonSerializer<int> Int32Serializer => __int32Serializer;
        public static IBsonSerializer<long> Int64Serializer => __int64Serializer;
        public static IBsonSerializer<sbyte> SByteSerializer => __sbyteSerializer;
        public static IBsonSerializer<float> SingleSerializer => __singleSerializer;
        public static IBsonSerializer<ushort> UInt16Serializer => __uint16Serializer;
        public static IBsonSerializer<uint> UInt32Serializer => __uint32Serializer;
        public static IBsonSerializer<ulong> UInt64Serializer => __uint64Serializer;

        public static IBsonSerializer GetSerializer(Type type)
            => type switch
            {
                _ when type == typeof(bool) => __booleanSerializer,
                _ when type == typeof(byte) => __byteSerializer,
                _ when type == typeof(char) => __charSerializer,
                _ when type == typeof(decimal) => __decimalSerializer,
                _ when type == typeof(double) => __doubleSerializer,
                _ when type == typeof(short) => __int16Serializer,
                _ when type == typeof(int) => __int32Serializer,
                _ when type == typeof(long) => __int64Serializer,
                _ when type == typeof(sbyte) => __sbyteSerializer,
                _ when type == typeof(float) => __singleSerializer,
                _ when type == typeof(ushort) => __uint16Serializer,
                _ when type == typeof(uint) => __uint32Serializer,
                _ when type == typeof(ulong) => __uint64Serializer,
                _ when type.IsNullable(out var valueType) => NullableSerializer.Create(GetSerializer(valueType)),
                _ => throw new ArgumentException($"{type} is not a standard type,", nameof(type))
            };
    }
}
