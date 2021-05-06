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
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers
{
    internal interface IGroupByKeyElementSerializer : IBsonSerializer
    {
        IBsonSerializer KeySerializer { get; }
        IBsonSerializer ElementSerializer { get; }
    }

    internal static class GroupByKeyElementSerializer
    {
        public static IGroupByKeyElementSerializer Create(IBsonSerializer keySerializer, IBsonSerializer elementSerializer)
        {
            var serializerType = typeof(GroupByKeyElementSerializer<,>).MakeGenericType(keySerializer.ValueType, elementSerializer.ValueType);
            return (IGroupByKeyElementSerializer)Activator.CreateInstance(serializerType, keySerializer, elementSerializer);
        }
    }

    internal class GroupByKeyElementSerializer<TKey, TElement> : SerializerBase<GroupByKeyElement<TKey, TElement>>, IGroupByKeyElementSerializer, IBsonDocumentSerializer
    {
        // private fields
        private readonly IBsonSerializer<TKey> _keySerializer;
        private readonly IBsonSerializer<TElement> _elementSerializer;

        // constructors
        public GroupByKeyElementSerializer(IBsonSerializer<TKey> keySerializer, IBsonSerializer<TElement> elementSerializer)
        {
            _keySerializer = Ensure.IsNotNull(keySerializer, nameof(keySerializer));
            _elementSerializer = Ensure.IsNotNull(elementSerializer, nameof(elementSerializer));
        }

        // public properties
        public IBsonSerializer KeySerializer => _keySerializer;
        public IBsonSerializer ElementSerializer => _elementSerializer;

        // public methods
        public override GroupByKeyElement<TKey, TElement> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            reader.ReadStartDocument();
            reader.ReadName("_key");
            var key = _keySerializer.Deserialize(context);
            reader.ReadName("_element");
            var element = _elementSerializer.Deserialize(context);
            reader.ReadEndDocument();

            return new GroupByKeyElement<TKey, TElement>(key, element);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, GroupByKeyElement<TKey, TElement> value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            writer.WriteName("_key");
            _keySerializer.Serialize(context, value.Key);
            writer.WriteName("_element");
            _elementSerializer.Serialize(context, value.Element);
            writer.WriteEndDocument();
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            switch (memberName)
            {
                case "Key":
                    serializationInfo = new BsonSerializationInfo("_key", _keySerializer, typeof(TKey));
                    return true;

                case "Element":
                    serializationInfo = new BsonSerializationInfo("_element", _elementSerializer, typeof(TElement));
                    return true;

                default:
                    serializationInfo = null;
                    return false;
            }
        }
    }
}
