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
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq3.Serializers
{
    public interface IGroupByKeyValueSerializer : IBsonSerializer
    {
        IBsonSerializer KeySerializer { get; }
        IBsonSerializer ValueSerializer { get; }
    }

    public static class GroupByKeyValueSerializer
    {
        public static IGroupByKeyValueSerializer Create(IBsonSerializer keySerializer, IBsonSerializer valueSerializer)
        {
            var serializerType = typeof(GroupByKeyValueSerializer<,>).MakeGenericType(keySerializer.ValueType, valueSerializer.ValueType);
            return (IGroupByKeyValueSerializer)Activator.CreateInstance(serializerType, keySerializer, valueSerializer);
        }
    }

    public class GroupByKeyValueSerializer<TKey, TValue> : SerializerBase<GroupByKeyValue<TKey, TValue>>, IGroupByKeyValueSerializer, IBsonDocumentSerializer
    {
        // private fields
        private readonly IBsonSerializer<TKey> _keySerializer;
        private readonly IBsonSerializer<TValue> _valueSerializer;

        // constructors
        public GroupByKeyValueSerializer(IBsonSerializer<TKey> keySerializer, IBsonSerializer<TValue> valueSerializer)
        {
            _keySerializer = Throw.IfNull(keySerializer, nameof(keySerializer));
            _valueSerializer = Throw.IfNull(valueSerializer, nameof(valueSerializer));
        }

        // public properties
        public IBsonSerializer KeySerializer => _keySerializer;
        public IBsonSerializer ValueSerializer => _valueSerializer;

        // public methods
        public override GroupByKeyValue<TKey, TValue> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            reader.ReadStartDocument();
            reader.ReadName("_key");
            var key = _keySerializer.Deserialize(context);
            reader.ReadName("_v");
            var value = _valueSerializer.Deserialize(context);
            reader.ReadEndDocument();

            return new GroupByKeyValue<TKey, TValue>(key, value);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, GroupByKeyValue<TKey, TValue> value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            writer.WriteName("_key");
            _keySerializer.Serialize(context, value.Key);
            writer.WriteName("_v");
            _valueSerializer.Serialize(context, value);
            writer.WriteEndDocument();
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            switch (memberName)
            {
                case "Key":
                    serializationInfo = new BsonSerializationInfo("_key", _keySerializer, typeof(TKey));
                    return true;

                case "Value":
                    serializationInfo = new BsonSerializationInfo("_v", _valueSerializer, typeof(TValue));
                    return true;

                default:
                    serializationInfo = null;
                    return false;
            }
        }
    }
}
