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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers
{
    internal interface ISetWindowFieldsPartitionSerializer
    {
        IBsonSerializer InputSerializer { get; }
    }

    internal class ISetWindowFieldsPartitionSerializer<TInput> : IBsonSerializer<ISetWindowFieldsPartition<TInput>>, ISetWindowFieldsPartitionSerializer
    {
        private readonly IBsonSerializer<TInput> _inputSerializer;

        public ISetWindowFieldsPartitionSerializer(IBsonSerializer<TInput> inputSerializer)
        {
            _inputSerializer = Ensure.IsNotNull(inputSerializer, nameof(inputSerializer));
        }

        IBsonSerializer ISetWindowFieldsPartitionSerializer.InputSerializer => _inputSerializer;
        public IBsonSerializer<TInput> InputSerializer => _inputSerializer;
        public Type ValueType => typeof(ISetWindowFieldsPartition<TInput>);

        public ISetWindowFieldsPartition<TInput> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            throw new InvalidOperationException("This serializer is not intended to be used.");
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                GetType().Equals(obj.GetType()) &&
                obj is ISetWindowFieldsPartitionSerializer<TInput> other &&
                object.Equals(_inputSerializer, other._inputSerializer);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ISetWindowFieldsPartition<TInput> value)
        {
            throw new InvalidOperationException("This serializer is not intended to be used.");
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            throw new InvalidOperationException("This serializer is not intended to be used.");
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            throw new InvalidOperationException("This serializer is not intended to be used.");
        }
    }
}
