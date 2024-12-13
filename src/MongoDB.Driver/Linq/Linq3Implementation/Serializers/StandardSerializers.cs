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

            AddStandardSerializers(new BooleanSerializer(representation: BsonType.Boolean));
            AddStandardSerializers(new ByteSerializer(representation: BsonType.Int32));
            AddStandardSerializers(new CharSerializer(representation: BsonType.Int32));
            AddStandardSerializers(new DecimalSerializer(representation: BsonType.Decimal128, allowTruncationConverter));
            AddStandardSerializers(new DoubleSerializer(representation: BsonType.Double, strictConverter));
            AddStandardSerializers(new Int16Serializer(representation: BsonType.Int32, strictConverter));
            AddStandardSerializers(new Int32Serializer(representation: BsonType.Int32, strictConverter));
            AddStandardSerializers(new Int64Serializer(representation: BsonType.Int64, strictConverter));
            AddStandardSerializers(new SByteSerializer(representation: BsonType.Int32));
            AddStandardSerializers(new SingleSerializer(representation: BsonType.Double, allowTruncationConverter));
            AddStandardSerializers(new UInt16Serializer(representation: BsonType.Int32, strictConverter));
            AddStandardSerializers(new UInt32Serializer(representation: BsonType.Int32, strictConverter));
            AddStandardSerializers(new UInt64Serializer(representation: BsonType.Int64, strictConverter));

            __booleanSerializer = (IBsonSerializer<bool>)__standardSerializers[typeof(bool)];

            static void AddStandardSerializers(IBsonSerializer standardSerializer)
            {
                __standardSerializers.Add(standardSerializer.ValueType, standardSerializer);

                var nullableSerializer = NullableSerializer.Create(valueSerializer: standardSerializer);
                __standardSerializers.Add(nullableSerializer.ValueType, nullableSerializer);

                var arraySerializer = standardSerializer.ValueType == typeof(byte) ?
                    new ByteArraySerializer() : // use this custom serializer for byte arrays
                    ArraySerializerHelper.CreateSerializer(itemSerializer: standardSerializer);
                __standardSerializers.Add(arraySerializer.ValueType, arraySerializer);

                var arrayOfNullableSerializer = ArraySerializerHelper.CreateSerializer(itemSerializer: nullableSerializer);
                __standardSerializers.Add(arrayOfNullableSerializer.ValueType, arrayOfNullableSerializer);
            }
        }

        public static IBsonSerializer<bool> BooleanSerializer => __booleanSerializer;

        public static IBsonSerializer GetSerializer(Type type)
            => TryGetSerializer(type, out var serializer) ? serializer : throw new ArgumentException($"{type} is not a standard type,", nameof(type));

        public static bool TryGetSerializer(Type type, out IBsonSerializer serializer)
            => __standardSerializers.TryGetValue(type, out serializer);
    }
}
