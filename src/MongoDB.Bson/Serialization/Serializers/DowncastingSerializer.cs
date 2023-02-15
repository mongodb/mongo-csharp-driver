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

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Static factory class for DowncastingSerializer.
    /// </summary>
    public static class DowncastingSerializer
    {
        /// <summary>
        /// Creates a new DowncastingSerializer.
        /// </summary>
        /// <param name="baseType">The base type.</param>
        /// <param name="derivedType">The derived type.</param>
        /// <param name="derivedTypeSerializer">The derived type serializer.</param>
        /// <returns></returns>
        public static IBsonSerializer Create(
            Type baseType,
            Type derivedType,
            IBsonSerializer derivedTypeSerializer)
        {
            var downcastingSerializerType = typeof(DowncastingSerializer<,>).MakeGenericType(baseType, derivedType);
            return (IBsonSerializer)Activator.CreateInstance(downcastingSerializerType, derivedTypeSerializer);
        }
    }

    /// <summary>
    /// A serializer for TBase where the actual values are of type TDerived.
    /// </summary>
    /// <typeparam name="TBase">The base type.</typeparam>
    /// <typeparam name="TDerived">The derived type.</typeparam>
    public class DowncastingSerializer<TBase, TDerived> : SerializerBase<TBase>
        where TDerived : TBase
    {
        private readonly IBsonSerializer<TDerived> _derivedSerializer;

        /// <summary>
        /// Initializes a new instance of DowncastingSerializer.
        /// </summary>
        /// <param name="derivedSerializer">The derived type serializer.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DowncastingSerializer(IBsonSerializer<TDerived> derivedSerializer)
        {
            _derivedSerializer = derivedSerializer ?? throw new ArgumentNullException(nameof(derivedSerializer));
        }

        /// <inheritdoc/>
        public override TBase Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return _derivedSerializer.Deserialize(context, args);
        }

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TBase value)
        {
            _derivedSerializer.Serialize(context, args, (TDerived)value);
        }
    }
}
