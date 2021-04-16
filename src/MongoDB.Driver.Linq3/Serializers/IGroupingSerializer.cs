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
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq3.Serializers
{
    public class IGroupingSerializer<TKey, TElement> : SerializerBase<IGrouping<TKey, TElement>>, IBsonArraySerializer, IBsonDocumentSerializer, IWrappedEnumerableSerializer
    {
        // private fields
        private readonly IBsonSerializer<TElement> _elementSerializer;
        private readonly IBsonSerializer<TKey> _keySerializer;

        // constructors
        public IGroupingSerializer(IBsonSerializer<TKey> keySerializer, IBsonSerializer<TElement> elementSerializer)
        {
            _keySerializer = Ensure.IsNotNull(keySerializer, nameof(keySerializer));
            _elementSerializer = Ensure.IsNotNull(elementSerializer, nameof(elementSerializer));
        }

        // public properties
        public string EnumerableFieldName => "_elements";

        public IBsonSerializer EnumerableElementSerializer => _elementSerializer;

        // public methods
        public override IGrouping<TKey, TElement> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            reader.ReadStartDocument();
            reader.ReadName("_id");
            var key = _keySerializer.Deserialize(context);
            reader.ReadName("_elements");
            reader.ReadStartArray();
            var elements = new List<TElement>();
            while (reader.ReadBsonType() != 0)
            {
                var element = _elementSerializer.Deserialize(context);
                elements.Add(element);
            }
            reader.ReadEndArray();
            reader.ReadEndDocument();

            return new Grouping<TKey, TElement>(key, elements);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IGrouping<TKey, TElement> value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            writer.WriteName("_id");
            _keySerializer.Serialize(context, value.Key);
            writer.WriteName("_elements");
            writer.WriteStartArray();
            foreach (var element in value)
            {
                _elementSerializer.Serialize(context, element);
            }
            writer.WriteEndArray();
            writer.WriteEndDocument();
        }

        public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = new BsonSerializationInfo(elementName: null, serializer: _elementSerializer, nominalType: typeof(TElement));
            return true;
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = null;

            if (memberName == "Key")
            {
                serializationInfo = new BsonSerializationInfo("_id", _keySerializer, typeof(TKey));
                return true;
            }

            return false;
        }
    }

    public static class IGroupingSerializer
    {
        public static IBsonSerializer Create(IBsonSerializer keySerializer, IBsonSerializer elementSerializer)
        {
            var factoryType = typeof(IGroupingSerializerFactory<,>).MakeGenericType(keySerializer.ValueType, elementSerializer.ValueType);
            var factory = (IGroupingSerializerFactory)Activator.CreateInstance(factoryType);
            return factory.Create(keySerializer, elementSerializer);
        }
    }

    public abstract class IGroupingSerializerFactory
    {
        public abstract IBsonSerializer Create(IBsonSerializer keySerializer, IBsonSerializer elementSerializer);
    }

    public class IGroupingSerializerFactory<TKey, TElement> : IGroupingSerializerFactory
    {
        public override IBsonSerializer Create(IBsonSerializer keySerializer, IBsonSerializer elementSerializer)
        {
            return new IGroupingSerializer<TKey, TElement>((IBsonSerializer<TKey>)keySerializer, (IBsonSerializer<TElement>)elementSerializer);
        }
    }
}
