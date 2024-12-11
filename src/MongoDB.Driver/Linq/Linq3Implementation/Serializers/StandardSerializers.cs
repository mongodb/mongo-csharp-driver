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
using System.Collections.Generic;
using System.Linq;
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
        private static readonly Dictionary<Type, IBsonSerializer> __standardSerializers = new();

        static StandardSerializers()
        {
            var strictConverter = new RepresentationConverter(allowOverflow: false, allowTruncation: false);
            var allowTruncationConverter = new RepresentationConverter(allowOverflow: false, allowTruncation: true);

            __standardSerializers.Add(typeof(bool), new BooleanSerializer(representation: BsonType.Boolean));
            __standardSerializers.Add(typeof(byte), new ByteSerializer(representation: BsonType.Int32));
            __standardSerializers.Add(typeof(char), new CharSerializer(representation: BsonType.Int32));
            __standardSerializers.Add(typeof(decimal), new DecimalSerializer(representation: BsonType.Decimal128, allowTruncationConverter));
            __standardSerializers.Add(typeof(double), new DoubleSerializer(representation: BsonType.Double, strictConverter));
            __standardSerializers.Add(typeof(short), new Int16Serializer(representation: BsonType.Int32, strictConverter));
            __standardSerializers.Add(typeof(int), new Int32Serializer(representation: BsonType.Int32, strictConverter));
            __standardSerializers.Add(typeof(long), new Int64Serializer(representation: BsonType.Int64, strictConverter));
            __standardSerializers.Add(typeof(sbyte), new SByteSerializer(representation: BsonType.Int32));
            __standardSerializers.Add(typeof(float), new SingleSerializer(representation: BsonType.Double, allowTruncationConverter));
            __standardSerializers.Add(typeof(ushort), new UInt16Serializer(representation: BsonType.Int32, strictConverter));
            __standardSerializers.Add(typeof(uint), new UInt32Serializer(representation: BsonType.Int32, strictConverter));
            __standardSerializers.Add(typeof(ulong), new UInt64Serializer(representation: BsonType.Int64, strictConverter));

            var standardTypes = __standardSerializers.Keys.ToArray(); // call ToArray to make a copy of the current Keys

            foreach (var standardType in standardTypes)
            {
                var standardSerializer = __standardSerializers[standardType];

                var nullableType = typeof(Nullable<>).MakeGenericType(standardType);
                var nullableSerializer = NullableSerializer.Create(valueSerializer: standardSerializer);
                __standardSerializers.Add(nullableType, nullableSerializer);

                var arrayType = standardType.MakeArrayType();
                var arraySerializer = ArraySerializerHelper.CreateSerializer(itemSerializer: standardSerializer);
                __standardSerializers.Add(arrayType, arraySerializer);

                var arrayOfNullableType = nullableType.MakeArrayType();
                var arrayOfNullableSerializer = ArraySerializerHelper.CreateSerializer(itemSerializer: nullableSerializer);
                __standardSerializers.Add(arrayOfNullableType, arrayOfNullableSerializer);
            }

            __booleanSerializer = (IBsonSerializer<bool>)__standardSerializers[typeof(bool)];
        }

        public static IBsonSerializer<bool> BooleanSerializer => __booleanSerializer;

        public static IBsonSerializer GetSerializer(Type type)
            => TryGetSerializer(type, out var serializer) ? serializer : throw new ArgumentException($"{type} is not a standard type,", nameof(type));

        public static bool TryGetSerializer(Type type, out IBsonSerializer serializer)
            => __standardSerializers.TryGetValue(type, out serializer);
    }
}
