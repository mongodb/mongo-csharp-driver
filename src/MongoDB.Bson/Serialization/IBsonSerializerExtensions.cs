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
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Extensions methods for IBsonSerializer.
    /// </summary>
    public static class IBsonSerializerExtensions
    {
        // methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="context">The deserialization context.</param>
        /// <returns>A deserialized value.</returns>
        public static object Deserialize(this IBsonSerializer serializer, BsonDeserializationContext context)
        {
            var args = new BsonDeserializationArgs { NominalType = serializer.ValueType };
            return serializer.Deserialize(context, args);
        }

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <typeparam name="TValue">The type that this serializer knows how to serialize.</typeparam>
        /// <param name="serializer">The serializer.</param>
        /// <param name="context">The deserialization context.</param>
        /// <returns>A deserialized value.</returns>
        public static TValue Deserialize<TValue>(this IBsonSerializer<TValue> serializer, BsonDeserializationContext context)
        {
            var args = new BsonDeserializationArgs { NominalType = serializer.ValueType };
            return serializer.Deserialize(context, args);
        }

        /// <summary>
        /// Gets the discriminator convention for a serializer.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <returns>The discriminator convention.</returns>
        public static IDiscriminatorConvention GetDiscriminatorConvention(this IBsonSerializer serializer) =>
            serializer is IHasDiscriminatorConvention hasDiscriminatorConvention
                ? hasDiscriminatorConvention.DiscriminatorConvention
                : BsonSerializer.LookupDiscriminatorConvention(serializer.ValueType);

        /// <summary>
        /// Reconfigures a serializer using the specified <paramref name="reconfigure"/> method.
        /// If the serializer implements <see cref="IChildSerializerConfigurable"/>,
        /// the method traverses and applies the reconfiguration to its child serializers recursively until an appropriate leaf serializer is found.
        /// </summary>
        /// <param name="serializer">The input serializer to be reconfigured.</param>
        /// <param name="reconfigure">A function that defines how the serializer of type <typeparamref name="T"/> should be reconfigured.</param>
        /// <param name="shouldReconfigure">
        /// An optional predicate to determine if the reconfiguration should be applied to the current serializer.
        /// </param>
        /// <typeparam name="T">The specific type of serializer to be reconfigured.</typeparam>
        /// <returns>
        /// The reconfigured serializer, or <c>null</c> if no leaf serializer could be reconfigured.
        /// </returns>
        internal static IBsonSerializer GetReconfigured<T>(this IBsonSerializer serializer, Func<T, IBsonSerializer> reconfigure, Func<IBsonSerializer, bool> shouldReconfigure = null)
        {
            switch (serializer)
            {
                case IChildSerializerConfigurable childSerializerConfigurable:
                {
                    var childSerializer = childSerializerConfigurable.ChildSerializer;
                    var reconfiguredChildSerializer = childSerializer.GetReconfigured(reconfigure, shouldReconfigure);
                    return reconfiguredChildSerializer != null? childSerializerConfigurable.WithChildSerializer(reconfiguredChildSerializer) : null;
                }
                case T typedSerializer when shouldReconfigure?.Invoke(serializer) ?? true:
                    return reconfigure(typedSerializer);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        public static void Serialize(this IBsonSerializer serializer, BsonSerializationContext context, object value)
        {
            var args = new BsonSerializationArgs { NominalType = serializer.ValueType };
            serializer.Serialize(context, args, value);
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <typeparam name="TValue">The type that this serializer knows how to serialize.</typeparam>
        /// <param name="serializer">The serializer.</param>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        public static void Serialize<TValue>(this IBsonSerializer<TValue> serializer, BsonSerializationContext context, TValue value)
        {
            var args = new BsonSerializationArgs { NominalType = serializer.ValueType };
            serializer.Serialize(context, args, value);
        }

        /// <summary>
        /// Converts a value to a BsonValue by serializing it.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="value">The value.</param>
        /// <returns>The serialized value.</returns>
        public static BsonValue ToBsonValue(this IBsonSerializer serializer, object value)
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                writer.WriteStartDocument();
                writer.WriteName("x");
                serializer.Serialize(context, value);
                writer.WriteEndDocument();
            }
            return document[0];
        }

        /// <summary>
        /// Converts a value to a BsonValue by serializing it.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="serializer">The serializer.</param>
        /// <param name="value">The value.</param>
        /// <returns>The serialized value.</returns>
        public static BsonValue ToBsonValue<TValue>(this IBsonSerializer<TValue> serializer, TValue value)
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                writer.WriteStartDocument();
                writer.WriteName("x");
                serializer.Serialize(context, value);
                writer.WriteEndDocument();
            }
            return document[0];
        }
    }
}
