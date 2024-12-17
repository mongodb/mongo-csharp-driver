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
        /// Reconfigures a serializer using the specified <paramref name="reconfigure"/> method if the result of <paramref name="testFunction"/> is true or the function is null.
        /// If the serializer implements <see cref="IChildSerializerConfigurable"/> and either:
        /// - is a collection serializer and <paramref name="shouldApplyToCollections"/> is true;
        /// - or is a <see cref="Nullable"/> serializer;
        /// the method traverses and applies the reconfiguration to its child serializers recursively.
        internal static IBsonSerializer ReconfigureSerializer<TSerializer>(IBsonSerializer serializer, Func<TSerializer, IBsonSerializer> reconfigure,
            Func<IBsonSerializer, bool> testFunction = null, bool shouldApplyToCollections = true)
        {
            switch (serializer)
            {
                case TSerializer typedSerializer when testFunction?.Invoke(serializer) ?? true:
                    return reconfigure(typedSerializer);
                case IChildSerializerConfigurable childSerializerConfigurable when
                    (shouldApplyToCollections && childSerializerConfigurable is IBsonArraySerializer)
                    || Nullable.GetUnderlyingType(serializer.ValueType) != null:
                {
                    if (childSerializerConfigurable is IKeyAndValueSerializerConfigurable keyAndValueSerializerConfigurable)
                    {
                        var keySerializer = keyAndValueSerializerConfigurable.KeySerializer;
                        var valueSerializer = keyAndValueSerializerConfigurable.ValueSerializer;

                        var reconfiguredKeySerializer = ReconfigureSerializer(keySerializer, reconfigure, testFunction,
                            shouldApplyToCollections);
                        var reconfiguredValueSerializer = ReconfigureSerializer(valueSerializer, reconfigure, testFunction,
                            shouldApplyToCollections);

                        return keyAndValueSerializerConfigurable.WithKeyAndValueSerializers(
                            reconfiguredKeySerializer ?? keySerializer, reconfiguredValueSerializer ?? valueSerializer);
                    }
                    
                    var childSerializer = childSerializerConfigurable.ChildSerializer;
                    var reconfiguredChildSerializer = ReconfigureSerializer(childSerializer, reconfigure, testFunction, shouldApplyToCollections);
                    return reconfiguredChildSerializer != null? childSerializerConfigurable.WithChildSerializer(reconfiguredChildSerializer) : null;
                }
                default:
                    return null;
            }
        }
    }
}