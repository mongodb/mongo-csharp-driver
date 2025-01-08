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
        // Reconfigures a serializer recursively.
        // The reconfigure Func should return null if it does not apply to a given serializer.
        internal static IBsonSerializer ReconfigureSerializerRecursively(
            IBsonSerializer serializer,
            Func<IBsonSerializer, IBsonSerializer> reconfigure)
        {
            switch (serializer)
            {
                // check IMultipleChildSerializersConfigurableSerializer first because some serializers implement both interfaces
                case IMultipleChildSerializersConfigurable multipleChildSerializerConfigurable:
                {
                    var anyChildSerializerWasReconfigured = false;
                    var reconfiguredChildSerializers = new List<IBsonSerializer>();

                    foreach (var childSerializer in multipleChildSerializerConfigurable.ChildSerializers)
                    {
                        var reconfiguredChildSerializer = ReconfigureSerializerRecursively(childSerializer, reconfigure);
                        anyChildSerializerWasReconfigured |= reconfiguredChildSerializer != null;
                        reconfiguredChildSerializers.Add(reconfiguredChildSerializer ?? childSerializer);
                    }

                    return anyChildSerializerWasReconfigured ? multipleChildSerializerConfigurable.WithChildSerializers(reconfiguredChildSerializers.ToArray()) : null;
                }

                case IChildSerializerConfigurable childSerializerConfigurable:
                {
                    var childSerializer = childSerializerConfigurable.ChildSerializer;
                    var reconfiguredChildSerializer = ReconfigureSerializerRecursively(childSerializer, reconfigure);
                    return reconfiguredChildSerializer != null ? childSerializerConfigurable.WithChildSerializer(reconfiguredChildSerializer) : null;
                }

                default:
                    return reconfigure(serializer);
            }
        }
    }
}