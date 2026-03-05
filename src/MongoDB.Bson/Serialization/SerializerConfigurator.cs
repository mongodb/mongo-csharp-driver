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
using System.Linq;

namespace MongoDB.Bson.Serialization
{
    internal static class SerializerConfigurator
    {
        // Reconfigures a serializer recursively.
        // The reconfigure Func should return null if it does not apply to a given serializer.
        internal static IBsonSerializer ReconfigureSerializerRecursively(
            IBsonSerializer serializer,
            Func<IBsonSerializer, IBsonSerializer> reconfigure,
            Type classMapType)
        {
            return ReconfigureSerializerRecursively(serializer, reconfigure, new HashSet<Type>([classMapType]));
        }

        private static IBsonSerializer ReconfigureSerializerRecursively(
            IBsonSerializer serializer,
            Func<IBsonSerializer, IBsonSerializer> reconfigure,
            ISet<Type> appliedTypes)
        {
            switch (serializer)
            {
                // check IMultipleChildSerializersConfigurableSerializer first because some serializers implement both interfaces
                case IMultipleChildSerializersConfigurable multipleChildSerializerConfigurable:
                {
                    var anyChildSerializerWasReconfigured = false;
                    var reconfiguredChildSerializers = new List<IBsonSerializer>();

                    if (multipleChildSerializerConfigurable.ChildSerializerTypes.Any(appliedTypes.Contains))
                    {
                        // at least one child type was already applied to, break out to avoid causing re-entrancy bug with serializer initialization
                        return null;
                    }

                    foreach (var childSerializer in multipleChildSerializerConfigurable.ChildSerializers)
                    {
                        var reconfiguredChildSerializer = ReconfigureSerializerRecursively(childSerializer, reconfigure, appliedTypes);
                        anyChildSerializerWasReconfigured |= reconfiguredChildSerializer != null;
                        reconfiguredChildSerializers.Add(reconfiguredChildSerializer ?? childSerializer);
                    }

                    return anyChildSerializerWasReconfigured ? multipleChildSerializerConfigurable.WithChildSerializers(reconfiguredChildSerializers.ToArray()) : null;
                }

                case IChildSerializerConfigurable childSerializerConfigurable:
                {
                    if (!appliedTypes.Add(childSerializerConfigurable.ChildSerializerType))
                    {
                        // type was already applied to, break out to avoid causing re-entrancy bug with serializer initialization
                        return null;
                    }

                    var childSerializer = childSerializerConfigurable.ChildSerializer;
                    var reconfiguredChildSerializer = ReconfigureSerializerRecursively(childSerializer, reconfigure, appliedTypes);
                    return reconfiguredChildSerializer != null ? childSerializerConfigurable.WithChildSerializer(reconfiguredChildSerializer) : null;
                }

                default:
                    return reconfigure(serializer);
            }
        }
    }
}
