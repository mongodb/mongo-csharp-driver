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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers.KnownSerializers
{
    internal class KnownSerializersNode
    {
        // private fields
        private readonly Dictionary<Type, HashSet<IBsonSerializer>> _knownSerializers = new Dictionary<Type, HashSet<IBsonSerializer>>();
        private readonly KnownSerializersNode _parent;

        // constructors
        public KnownSerializersNode(KnownSerializersNode parent)
        {
            _parent = parent; // will be null for the root node
        }

        // public properties
        public Dictionary<Type, HashSet<IBsonSerializer>> KnownSerializers => _knownSerializers;
        public KnownSerializersNode Parent => _parent;

        // public methods
        public void AddKnownSerializer(Type type, IBsonSerializer serializer)
        {
            if (!_knownSerializers.TryGetValue(type, out var set))
            {
                set = new HashSet<IBsonSerializer>();
                _knownSerializers.Add(type, set);
            }

            set.Add(serializer);

            _parent?.AddKnownSerializer(type, serializer);
        }

        public HashSet<IBsonSerializer> GetPossibleSerializers(Type type)
        {
            var possibleSerializers = GetPossibleSerializersAtThisLevel(type);
            if (possibleSerializers.Count > 0)
            {
                return possibleSerializers;
            }

            if (_parent != null)
            {
                return _parent.GetPossibleSerializers(type);
            }

            return new HashSet<IBsonSerializer>();
        }

        // private methods
        private HashSet<IBsonSerializer> GetPossibleSerializersAtThisLevel(Type type)
        {
            if (_knownSerializers.TryGetValue(type, out var knownSerializers))
            {
                return knownSerializers;
            }

            Type itemType = null;
            if (type.TryGetIEnumerableGenericInterface(out var ienumerableGenericInterface))
            {
                itemType = ienumerableGenericInterface.GetGenericArguments()[0];
            }

            var possibleSerializers = new HashSet<IBsonSerializer>();
            foreach (var serializer in _knownSerializers.Values.SelectMany(hashset => hashset))
            {
                var valueType = serializer.ValueType;
                if (valueType == type || valueType.IsEnum() && Enum.GetUnderlyingType(valueType) == type)
                {
                    possibleSerializers.Add(serializer);
                }

                if (serializer is IBsonArraySerializer arraySerializer && arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo))
                {
                    var itemSerializer = itemSerializationInfo.Serializer;
                    if (itemSerializer.ValueType == type)
                    {
                        possibleSerializers.Add(itemSerializer);
                    }
                }

                if (valueType == itemType)
                {
                    var ienumerableSerializer = IEnumerableSerializer.Create(serializer);
                    possibleSerializers.Add(ienumerableSerializer);
                }
            }

            return possibleSerializers;
        }
    }
}
