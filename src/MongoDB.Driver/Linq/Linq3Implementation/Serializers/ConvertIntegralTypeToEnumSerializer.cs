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
    internal interface IConvertIntegralTypeToEnumSerializer
    {
        IBsonSerializer EnumSerializer { get; }
    }

    internal class ConvertIntegralTypeToEnumSerializer<TIntegralType, TEnumUnderlyingType, TEnum> : StructSerializerBase<TIntegralType>, IConvertIntegralTypeToEnumSerializer
        where TIntegralType : struct
        where TEnumUnderlyingType : struct
        where TEnum : Enum
    {
        // private fields
        private readonly IBsonSerializer<TEnum> _enumSerializer;

        // constructors
        public ConvertIntegralTypeToEnumSerializer(IBsonSerializer<TEnum> enumSerializer)
        {
            if (typeof(TEnumUnderlyingType) != Enum.GetUnderlyingType(typeof(TEnum)))
            {
                throw new ArgumentException($"{typeof(TEnumUnderlyingType).FullName} is not the underlying type of {typeof(TEnum).FullName}.");
            }
            if (!typeof(TIntegralType).IsIntegral())
            {
                throw new ArgumentException($"{typeof(TIntegralType).FullName} is not an integral type.");
            }
            _enumSerializer = Ensure.IsNotNull(enumSerializer, nameof(enumSerializer));
        }

        // public properties
        public IBsonSerializer<TEnum> EnumSerializer => _enumSerializer;

        // explicitly implemented properties
        IBsonSerializer IConvertIntegralTypeToEnumSerializer.EnumSerializer => EnumSerializer;

        // public methods
        public override TIntegralType Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var enumValue = _enumSerializer.Deserialize(context);
            var enumUnderlyingTypeValue = (TEnumUnderlyingType)(object)enumValue;
            return (TIntegralType)Convert.ChangeType(enumUnderlyingTypeValue, typeof(TIntegralType));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is ConvertIntegralTypeToEnumSerializer<TIntegralType, TEnumUnderlyingType, TEnum> other &&
                object.Equals(_enumSerializer, other._enumSerializer);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => _enumSerializer.GetHashCode();

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TIntegralType value)
        {
            var underlyingTypeValue = (TEnumUnderlyingType)Convert.ChangeType(value, typeof(TEnumUnderlyingType));
            var enumValue = (TEnum)(object)underlyingTypeValue;
            _enumSerializer.Serialize(context, enumValue);
        }
    }

    internal static class ConvertIntegralTypeToEnumSerializer
    {
        public static IBsonSerializer Create(Type integralType, IBsonSerializer enumSerializer)
        {
            var enumType = enumSerializer.ValueType;
            var enumUnderlyingType = Enum.GetUnderlyingType(enumType);
            var convertIntegralTypeToEnumSerializerType = typeof(ConvertIntegralTypeToEnumSerializer<,,>).MakeGenericType(integralType, enumUnderlyingType, enumType);
            return (IBsonSerializer)Activator.CreateInstance(convertIntegralTypeToEnumSerializerType, enumSerializer);
        }
    }
}
