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
    internal interface IConvertEnumToIntegralTypeSerializer
    {
        IBsonSerializer IntegralTypeSerializer { get; }
    }

    internal class ConvertEnumToIntegralTypeSerializer<TEnum, TEnumUnderlyingType, TIntegralType> : StructSerializerBase<TEnum>, IConvertEnumToIntegralTypeSerializer
        where TEnum : struct, Enum
        where TEnumUnderlyingType : struct
        where TIntegralType : struct
    {
        // private fields
        private readonly IBsonSerializer<TIntegralType> _integralTypeSerializer;

        // constructors
        public ConvertEnumToIntegralTypeSerializer(IBsonSerializer<TIntegralType> integralTypeSerializer)
        {
            if (typeof(TEnumUnderlyingType) != Enum.GetUnderlyingType(typeof(TEnum)))
            {
                throw new ArgumentException($"{typeof(TEnumUnderlyingType).FullName} is not the underlying type of {typeof(TEnum).FullName}.");
            }
            if (!typeof(TIntegralType).IsIntegral())
            {
                throw new ArgumentException($"{typeof(TIntegralType).FullName} is not an integral type.");
            }
            _integralTypeSerializer = Ensure.IsNotNull(integralTypeSerializer, nameof(integralTypeSerializer));
        }

        // public properties
        public IBsonSerializer<TIntegralType> IntegralTypeSerializer => _integralTypeSerializer;

        // explicitly implemented properties
        IBsonSerializer IConvertEnumToIntegralTypeSerializer.IntegralTypeSerializer => IntegralTypeSerializer;

        // public methods
        public override TEnum Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var integralTypeValue = _integralTypeSerializer.Deserialize(context);
            var enumUnderlyingTypeValue = Convert.ChangeType(integralTypeValue, typeof(TEnumUnderlyingType));
            return (TEnum)(object)enumUnderlyingTypeValue;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is ConvertEnumToIntegralTypeSerializer<TEnum, TEnumUnderlyingType, TIntegralType> other &&
                object.Equals(_integralTypeSerializer, other._integralTypeSerializer);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => _integralTypeSerializer.GetHashCode();

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEnum value)
        {
            var enumUnderlyingTypeValue = (TEnumUnderlyingType)(object)value;
            var integralTypeValue = Convert.ChangeType(enumUnderlyingTypeValue, typeof(TIntegralType));
            _integralTypeSerializer.Serialize(context, integralTypeValue);
        }
    }

    internal static class ConvertEnumToIntegralTypeSerializer
    {
        public static IBsonSerializer Create(Type enumType, IBsonSerializer integralTypeSerializer)
        {
            var enumUnderlyingType = Enum.GetUnderlyingType(enumType);
            var integralType = integralTypeSerializer.ValueType;
            var convertEnumToIntegralTypeSerializerType = typeof(ConvertEnumToIntegralTypeSerializer<,,>).MakeGenericType(enumType, enumUnderlyingType, integralType);
            return (IBsonSerializer)Activator.CreateInstance(convertEnumToIntegralTypeSerializerType, integralTypeSerializer);
        }
    }
}
