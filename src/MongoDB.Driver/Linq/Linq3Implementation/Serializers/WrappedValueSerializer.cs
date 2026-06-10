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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers
{
    internal interface IWrappedValueSerializer : IBsonSerializer
    {
        string FieldName { get; }
        IBsonSerializer ValueSerializer { get; }
    }

    internal class WrappedValueSerializer<TValue> : SerializerBase<TValue>, IWrappedValueSerializer, IBsonArraySerializer, IBsonDocumentSerializer
    {
        // private fields
        private readonly string _fieldName;
        private readonly IBsonSerializer<TValue> _valueSerializer;

        // constructors
        public WrappedValueSerializer(string fieldName, IBsonSerializer<TValue> valueSerializer)
        {
            _fieldName = Ensure.IsNotNull(fieldName, nameof(fieldName));
            _valueSerializer = Ensure.IsNotNull(valueSerializer, nameof(valueSerializer));
        }

        // public properties
        public string FieldName => _fieldName;

        public IBsonSerializer<TValue> ValueSerializer => _valueSerializer;

        IBsonSerializer IWrappedValueSerializer.ValueSerializer => _valueSerializer;

        // public methods
        public override TValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            TValue value;
            var reader = context.Reader;
            reader.ReadStartDocument();
            if (reader.ReadBsonType() == BsonType.EndOfDocument)
            {
                value = default;
            }
            else
            {
                reader.ReadName(_fieldName);
                value = _valueSerializer.Deserialize(context);
            }
            reader.ReadEndDocument();
            return value;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is WrappedValueSerializer<TValue> other &&
                object.Equals(_fieldName, other._fieldName) &&
                object.Equals(_valueSerializer, other._valueSerializer);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TValue value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            writer.WriteName(_fieldName);
            _valueSerializer.Serialize(context, value);
            writer.WriteEndDocument();
        }

        public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            if (_valueSerializer is IBsonDocumentSerializer documentSerializer)
            {
                if (documentSerializer.TryGetMemberSerializationInfo(memberName, out serializationInfo))
                {
                    serializationInfo = BsonSerializationInfo.CreateWithPath(
                        [_fieldName, serializationInfo.ElementName],
                        serializationInfo.Serializer,
                        serializationInfo.NominalType);
                    return true;
                }

                return false;
            }

            throw new InvalidOperationException();
        }
    }

    internal static class WrappedValueSerializer
    {
        public static IWrappedValueSerializer Create(string fieldName, IBsonSerializer valueSerializer)
        {
            var valueType = valueSerializer.ValueType;
            var wrappedValueSerializerType = typeof(WrappedValueSerializer<>).MakeGenericType(valueType);
            return (IWrappedValueSerializer)Activator.CreateInstance(wrappedValueSerializerType, fieldName, valueSerializer);
        }
    }
}
