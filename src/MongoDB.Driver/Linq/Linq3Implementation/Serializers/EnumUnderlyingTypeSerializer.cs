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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers
{
    internal class EnumUnderlyingTypeSerializer<TEnum, TEnumUnderlyingType> : StructSerializerBase<TEnumUnderlyingType> 
        where TEnum : Enum 
        where TEnumUnderlyingType : struct
    {
        // private fields 
        private readonly IBsonSerializer<TEnum> _enumSerializer;

        // constructors
        public EnumUnderlyingTypeSerializer(IBsonSerializer<TEnum> enumSerializer)
        {
            if (typeof(TEnumUnderlyingType) != Enum.GetUnderlyingType(typeof(TEnum)))
            {
                throw new ArgumentException($"{typeof(TEnumUnderlyingType).FullName} is not the underlying type of {typeof(TEnum).FullName}.");
            }
            _enumSerializer = Ensure.IsNotNull(enumSerializer, nameof(enumSerializer));
        }

        // public methods
        public override TEnumUnderlyingType Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var enumValue = _enumSerializer.Deserialize(context);
            return (TEnumUnderlyingType)(object)enumValue;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEnumUnderlyingType value)
        {
            var enumValue = (TEnum)(object)value;
            _enumSerializer.Serialize(context, enumValue);
        }
    }

    internal static class EnumUnderlyingTypeSerializer
    {
        public static IBsonSerializer Create(IBsonSerializer enumSerializer)
        {
            var enumType = enumSerializer.ValueType;
            var enumUnderlyingType = enumType.GetEnumUnderlyingType();
            var serializerType = typeof(EnumUnderlyingTypeSerializer<,>).MakeGenericType(enumType, enumUnderlyingType);
            return (IBsonSerializer)Activator.CreateInstance(serializerType, enumSerializer);
        }
    }
}
