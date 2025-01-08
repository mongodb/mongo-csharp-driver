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

namespace MongoDB.Driver
{
    internal static class ClientSideProjectionSnippetsDeserializer
    {
        public static IBsonSerializer Create(
            Type projectionType,
            IBsonSerializer[] snippetDeserializers,
            Delegate projector)
        {
            var deserializerType = typeof(ClientSideProjectionSnippetsDeserializer<>).MakeGenericType(projectionType);
            return (IBsonSerializer)Activator.CreateInstance(deserializerType, [snippetDeserializers, projector]);
        }
    }

    internal sealed class ClientSideProjectionSnippetsDeserializer<TProjection> : SerializerBase<TProjection>, IClientSideProjectionDeserializer
    {
        private readonly IBsonSerializer[] _snippetDeserializers;
        private readonly Func<object[], TProjection> _projector;

        public ClientSideProjectionSnippetsDeserializer(IBsonSerializer[] snippetDeserializers, Func<object[], TProjection> projector)
        {
            _snippetDeserializers = snippetDeserializers;
            _projector = projector;
        }

        public override TProjection Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var snippets = DeserializeSnippets(context);
            return _projector(snippets);
        }

        private object[] DeserializeSnippets(BsonDeserializationContext context)
        {
            var reader = context.Reader;

            reader.ReadStartDocument();
            reader.ReadName("_snippets");
            reader.ReadStartArray();
            var snippets = new object[_snippetDeserializers.Length];
            var i = 0;
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                if (i >= _snippetDeserializers.Length)
                {
                    throw new BsonSerializationException($"Expected {_snippetDeserializers.Length} snippets but found more than that.");
                }
                snippets[i] = _snippetDeserializers[i].Deserialize(context);
                i++;
            }
            if (i != _snippetDeserializers.Length)
            {
                throw new BsonSerializationException($"Expected {_snippetDeserializers.Length} snippets but found {i}.");
            }
            reader.ReadEndArray();
            reader.ReadEndDocument();

            return snippets;
        }
    }
}
