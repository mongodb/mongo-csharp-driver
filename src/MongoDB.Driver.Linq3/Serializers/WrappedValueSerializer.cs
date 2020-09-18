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
        IBsonSerializer ValueSerializer { get; }
    }

    public class WrappedValueSerializer<TValue> : SerializerBase<TValue>, IWrappedValueSerializer
    {
        // private fields
        private readonly IBsonSerializer<TValue> _valueSerializer;

        // constructors
        public WrappedValueSerializer(IBsonSerializer<TValue> valueSerializer)
        {
            _valueSerializer = Ensure.IsNotNull(valueSerializer, nameof(valueSerializer));
        }

        // public properties
        public IBsonSerializer<TValue> ValueSerializer => _valueSerializer;

        IBsonSerializer IWrappedValueSerializer.ValueSerializer => _valueSerializer;

        // public methods
        public override TValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            reader.ReadStartDocument();
            reader.ReadName("_v");
            var value = _valueSerializer.Deserialize(context);
            reader.ReadEndDocument();
            return value;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TValue value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            writer.WriteName("_v");
            _valueSerializer.Serialize(context, value);
            writer.WriteEndDocument();
        }
    }

    public static class WrappedValueSerializer
    {
        public static IBsonSerializer Create(IBsonSerializer valueSerializer)
        {
            var valueType = valueSerializer.ValueType;
            var factoryType = typeof(WrappedValueSerializerFactory<>).MakeGenericType(valueType);
            var factory = (WrappedValueSerializerFactory)Activator.CreateInstance(factoryType);
            return factory.Create(valueSerializer);
        }
    }

    public abstract class WrappedValueSerializerFactory
    {
        public abstract IBsonSerializer Create(IBsonSerializer valueSerializer);
    }

    public class WrappedValueSerializerFactory<TValue> : WrappedValueSerializerFactory
    {
        public override IBsonSerializer Create(IBsonSerializer valueSerializer)
        {
            return new WrappedValueSerializer<TValue>((IBsonSerializer<TValue>)valueSerializer);
        }
    }
}
