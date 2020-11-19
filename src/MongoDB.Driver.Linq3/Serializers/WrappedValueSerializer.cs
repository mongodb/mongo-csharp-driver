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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq3.Serializers
{
    public interface IWrappedValueSerializer
    {
        string FieldName { get; }
        IBsonSerializer ValueSerializer { get; }
    }

    public class WrappedValueSerializer<TValue> : SerializerBase<TValue>, IWrappedValueSerializer, IBsonArraySerializer, IBsonDocumentSerializer
    {
        // private fields
        private readonly string _fieldName;
        private readonly IBsonSerializer<TValue> _valueSerializer;

        // constructors
        public WrappedValueSerializer(IBsonSerializer<TValue> valueSerializer)
            : this("_v", valueSerializer)
        {
        }

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
            var reader = context.Reader;
            reader.ReadStartDocument();
            reader.ReadName(_fieldName);
            var value = _valueSerializer.Deserialize(context);
            reader.ReadEndDocument();
            return value;
        }

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
            if (_valueSerializer is IBsonArraySerializer arraySerializer)
            {
                return arraySerializer.TryGetItemSerializationInfo(out serializationInfo);
            }

            serializationInfo = null;
            return false;
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            if (_valueSerializer is IBsonDocumentSerializer documentSerializer)
            {
                if (documentSerializer.TryGetMemberSerializationInfo(memberName, out serializationInfo))
                {
                    var wrappedElementName = $"{_fieldName}.{serializationInfo.ElementName}";
                    serializationInfo = new BsonSerializationInfo(wrappedElementName, serializationInfo.Serializer, serializationInfo.NominalType);
                    return true;
                }
            }

            serializationInfo = null;
            return false;
        }
    }

    public static class WrappedValueSerializer
    {
        public static IBsonSerializer Create(IBsonSerializer valueSerializer)
        {
            return Create("_v", valueSerializer);
        }

        public static IBsonSerializer Create(string fieldName, IBsonSerializer valueSerializer)
        {
            var valueType = valueSerializer.ValueType;
            var wrappedValueSerializerType = typeof(WrappedValueSerializer<>).MakeGenericType(valueType);
            return (IBsonSerializer)Activator.CreateInstance(wrappedValueSerializerType, fieldName, valueSerializer);
        }
    }
}
