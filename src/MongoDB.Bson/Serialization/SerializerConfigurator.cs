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

namespace MongoDB.Bson.Serialization
{
    internal static class SerializerConfigurator
    {
        /// Reconfigures a serializer using the specified <paramref name="reconfigure"/> method if the result of <paramref name="testFunction"/> is true or the function is null.
        /// If the serializer implements <see cref="IChildSerializerConfigurable"/> and either:
        /// - <paramref name="topLevelOnly"/> is false;
        /// - or is a <see cref="Nullable"/> serializer;
        /// the method traverses and applies the reconfiguration to its child serializers recursively.
        internal static IBsonSerializer ReconfigureSerializer<TSerializer>(IBsonSerializer serializer, Func<TSerializer, IBsonSerializer> reconfigure,
            Func<IBsonSerializer, bool> testFunction = null, bool topLevelOnly = false)
        {
            switch (serializer)
            {
                case TSerializer typedSerializer when testFunction?.Invoke(serializer) ?? true:
                    return reconfigure(typedSerializer);
                case IMultipleChildrenSerializerConfigurableSerializer multipleChildrenSerializerConfigurable when !topLevelOnly:
                {
                    var newSerializers = new List<IBsonSerializer>();

                    foreach (var childSerializer in multipleChildrenSerializerConfigurable.ChildrenSerializers)
                    {
                        var reconfiguredChildSerializer = ReconfigureSerializer(childSerializer, reconfigure, testFunction,
                            false);

                        newSerializers.Add(reconfiguredChildSerializer ?? childSerializer);
                    }

                    return multipleChildrenSerializerConfigurable.WithChildrenSerializers(newSerializers.ToArray());
                }
                case IChildSerializerConfigurable childSerializerConfigurable when
                    !topLevelOnly || Nullable.GetUnderlyingType(serializer.ValueType) != null:
                {
                    var childSerializer = childSerializerConfigurable.ChildSerializer;
                    var reconfiguredChildSerializer = ReconfigureSerializer(childSerializer, reconfigure, testFunction, topLevelOnly);
                    return reconfiguredChildSerializer != null? childSerializerConfigurable.WithChildSerializer(reconfiguredChildSerializer) : null;
                }

                default:
                    return null;
            }
        }
    }
}