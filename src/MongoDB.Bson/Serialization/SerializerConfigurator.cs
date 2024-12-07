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

namespace MongoDB.Bson.Serialization
{
    internal static class SerializerConfigurator
    {
        /// <summary>
        /// Reconfigures a serializer using the specified <paramref name="reconfigure"/> method.
        /// If the serializer implements <see cref="IChildSerializerConfigurable"/>,
        /// the method traverses and applies the reconfiguration to its child serializers recursively until an appropriate leaf serializer is found.
        /// </summary>
        /// <param name="serializer">The input serializer to be reconfigured.</param>
        /// <param name="reconfigure">A function that defines how the serializer of type <typeparamref name="TSerializer"/> should be reconfigured.</param>
        /// <typeparam name="TSerializer">The input type for the reconfigure method.</typeparam>
        /// <returns>
        /// The reconfigured serializer, or <c>null</c> if no leaf serializer could be reconfigured.
        /// </returns>
        internal static IBsonSerializer ReconfigureSerializer<TSerializer>(IBsonSerializer serializer, Func<TSerializer, IBsonSerializer> reconfigure)
        {
            switch (serializer)
            {
                case IChildSerializerConfigurable childSerializerConfigurable:
                    var childSerializer = childSerializerConfigurable.ChildSerializer;
                    var reconfiguredChildSerializer = ReconfigureSerializer(childSerializer, reconfigure);
                    return reconfiguredChildSerializer != null? childSerializerConfigurable.WithChildSerializer(reconfiguredChildSerializer) : null;

                case TSerializer typedSerializer:
                    return reconfigure(typedSerializer);

                default:
                    return null;
            }
        }
    }
}