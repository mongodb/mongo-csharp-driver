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

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers
{
    internal static class UpcastingSerializer
    {
        public static IBsonSerializer Create(
            Type baseType,
            Type derivedType,
            IBsonSerializer baseTypeSerializer)
        {
            var upcastingSerializerType = typeof(UpcastingSerializer<,>).MakeGenericType(baseType, derivedType);
            return (IBsonSerializer)Activator.CreateInstance(upcastingSerializerType, baseTypeSerializer);
        }
    }

    internal sealed class UpcastingSerializer<TBase, TDerived> : SerializerBase<TDerived>, IBsonArraySerializer, IBsonDocumentSerializer
        where TDerived : TBase
    {
        private readonly IBsonSerializer<TBase> _baseTypeSerializer;

        public UpcastingSerializer(IBsonSerializer<TBase> baseTypeSerializer)
        {
            _baseTypeSerializer = baseTypeSerializer ?? throw new ArgumentNullException(nameof(baseTypeSerializer));
        }

        public Type BaseType => typeof(TBase);

        public IBsonSerializer<TBase> BaseTypeSerializer => _baseTypeSerializer;

        public Type DerivedType => typeof(TDerived);

        public override TDerived Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return (TDerived)_baseTypeSerializer.Deserialize(context);
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is UpcastingSerializer<TBase, TDerived> other &&
                object.Equals(_baseTypeSerializer, other._baseTypeSerializer);
        }

        public override int GetHashCode() => 0;

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TDerived value)
        {
            _baseTypeSerializer.Serialize(context, value);
        }

        public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
        {
            if (_baseTypeSerializer is not IBsonArraySerializer arraySerializer)
            {
                throw new NotSupportedException($"The class {_baseTypeSerializer.GetType().FullName} does not implement IBsonArraySerializer.");
            }

            return arraySerializer.TryGetItemSerializationInfo(out serializationInfo);
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            if (_baseTypeSerializer is not IBsonDocumentSerializer documentSerializer)
            {
                throw new NotSupportedException($"The class {_baseTypeSerializer.GetType().FullName} does not implement IBsonDocumentSerializer.");
            }

            return documentSerializer.TryGetMemberSerializationInfo(memberName, out serializationInfo);
        }
    }
}
