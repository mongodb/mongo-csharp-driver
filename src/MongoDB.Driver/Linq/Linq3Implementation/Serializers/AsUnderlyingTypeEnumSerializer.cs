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

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers
{
    internal interface IAsUnderlyingTypeEnumSerializer
    {
        IBsonSerializer UnderlyingTypeSerializer { get; }
    }

    internal class AsUnderlyingTypeEnumSerializer<TEnum, TUnderlyingType> : SerializerBase<TEnum>, IAsUnderlyingTypeEnumSerializer
        where TEnum : Enum
        where TUnderlyingType : struct
    {
        // private fields
        private readonly IBsonSerializer<TUnderlyingType> _underlyingTypeSerializer;

        // constructors
        public AsUnderlyingTypeEnumSerializer(IBsonSerializer<TUnderlyingType> underlyingTypeSerializer)
        {
            if (typeof(TUnderlyingType) != Enum.GetUnderlyingType(typeof(TEnum)))
            {
                throw new ArgumentException($"{typeof(TUnderlyingType).FullName} is not the underlying type of {typeof(TEnum).FullName}.");
            }
            _underlyingTypeSerializer = Ensure.IsNotNull(underlyingTypeSerializer, nameof(underlyingTypeSerializer));
        }

        // public properties
        public IBsonSerializer<TUnderlyingType> UnderlyingTypeSerializer => _underlyingTypeSerializer;

        // explicitly implemented properties
        IBsonSerializer IAsUnderlyingTypeEnumSerializer.UnderlyingTypeSerializer => UnderlyingTypeSerializer;

        // public methods
        public override TEnum Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var underlyingTypeValue = _underlyingTypeSerializer.Deserialize(context);
            return (TEnum)(object)underlyingTypeValue;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is AsUnderlyingTypeEnumSerializer<TEnum, TUnderlyingType> other &&
                object.Equals(_underlyingTypeSerializer, other._underlyingTypeSerializer);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => _underlyingTypeSerializer.GetHashCode();

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEnum value)
        {
            var underlyingTypeValue = (TUnderlyingType)(object)value;
            _underlyingTypeSerializer.Serialize(context, underlyingTypeValue);
        }
    }

    internal static class AsUnderlyingTypeEnumSerializer
    {
        public static IBsonSerializer Create(Type enumType, IBsonSerializer underlyingTypeSerializer)
        {
            var underlyingType = Enum.GetUnderlyingType(enumType);
            var asUnderlyingTypeEnumSerializerType = typeof(AsUnderlyingTypeEnumSerializer<,>).MakeGenericType(enumType, underlyingType);
            return (IBsonSerializer)Activator.CreateInstance(asUnderlyingTypeEnumSerializerType, underlyingTypeSerializer);
        }
    }
}
