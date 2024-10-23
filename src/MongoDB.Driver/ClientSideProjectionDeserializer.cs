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
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    internal interface IClientSideProjectionDeserializer
    {
    }

    internal static class ClientSideProjectionDeserializer
    {
        public static IBsonSerializer Create(
            IBsonSerializer inputSerializer,
            LambdaExpression projector)
        {
            var inputType = inputSerializer.ValueType;
            var projectionType = projector.ReturnType;
            var serializerType = typeof(ClientSideProjectionDeserializer<,>).MakeGenericType(inputType, projectionType);
            var projectorDelegate = projector.Compile();
            return (IBsonSerializer)Activator.CreateInstance(serializerType, inputSerializer, projectorDelegate);
        }
    }

    /// <summary>
    /// A deserializer for doing client side projections.
    /// </summary>
    /// <typeparam name="TInput">The type of the input.</typeparam>
    /// <typeparam name="TProjection">The type of the projection.</typeparam>
    public sealed class ClientSideProjectionDeserializer<TInput, TProjection> : SerializerBase<TProjection>, IClientSideProjectionDeserializer
    {
        private readonly Func<TInput, TProjection> _projector;
        private readonly IBsonSerializer<TInput> _inputSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSideProjectionDeserializer{TInput, TProjection}"/> class.
        /// </summary>
        /// <param name="inputSerializer">The input serializer.</param>
        /// <param name="projector">The client side projector.</param>
        public ClientSideProjectionDeserializer(
            IBsonSerializer<TInput> inputSerializer,
            Func<TInput, TProjection> projector)
        {
            _inputSerializer = Ensure.IsNotNull(inputSerializer, nameof(inputSerializer));
            _projector = Ensure.IsNotNull(projector, nameof(projector));
        }

        /// <inheritdoc/>
        public override TProjection Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var document = _inputSerializer.Deserialize(context);
            return _projector(document);
        }
    }
}
