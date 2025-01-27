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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// A scalar discriminator convention for class mapped types.
    /// </summary>
    public class BsonClassMapScalarDiscriminatorConvention : StandardDiscriminatorConvention, IScalarDiscriminatorConvention
    {
        private readonly BsonClassMap _classMap;

        // cached map
        private readonly ConcurrentDictionary<Type, BsonValue[]> _typeToDiscriminatorsForTypeAndSubTypesMap = new();

        /// <summary>
        /// Gets the class map.
        /// </summary>
        public BsonClassMap ClassMap => _classMap;

        /// <summary>
        /// Initializes a new instance of BsonClassMapScalarDiscriminatorConvention.
        /// </summary>
        /// <param name="elementName">The discriminator element name.</param>
        /// <param name="classMap">The class map.</param>
        public BsonClassMapScalarDiscriminatorConvention(string elementName, BsonClassMap classMap)
            : base(elementName)
        {
            _classMap = classMap ?? throw new ArgumentNullException(nameof(classMap));
        }

        /// <summary>
        /// Gets the actual type.
        /// </summary>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="discriminator">The discriminator value.</param>
        /// <returns>The actual type.</returns>
        protected override Type GetActualType(Type nominalType, BsonValue discriminator)
        {
            if (_classMap.DiscriminatorToTypeMap.TryGetValue(discriminator, out Type actualType))
            {
                return actualType;
            }

            if (nominalType == typeof(object) && discriminator.IsString)
            {
                actualType = TypeNameDiscriminator.GetActualType(discriminator.AsString);
                if (actualType != null)
                {
                    return actualType;
                }
            }

            throw new BsonSerializationException($"No type found for discriminator value: {discriminator}.");
        }

        /// <summary>
        /// Gets the discriminator value.
        /// </summary>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="actualType">The actual type.</param>
        /// <returns>The discriminator value.</returns>
        public override BsonValue GetDiscriminator(Type nominalType, Type actualType)
        {
            if (actualType == nominalType && !_classMap.DiscriminatorIsRequired)
            {
                return null;
            }

            if (_classMap.TypeToDiscriminatorMap.TryGetValue(actualType, out BsonValue discriminator))
            {
                return discriminator;
            }

            throw new BsonSerializationException($"No discriminator value found for type: {actualType}.");
        }

        /// <summary>
        /// Gets the discriminator values for a type and all of its sub types.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The discriminator values for a type and all of its sub types.</returns>
        public BsonValue[] GetDiscriminatorsForTypeAndSubTypes(Type type)
        {
            return _typeToDiscriminatorsForTypeAndSubTypesMap.GetOrAdd(type, MapTypeToDiscriminatorsForTypeAndSubTypes);
        }

        private BsonValue[] MapTypeToDiscriminatorsForTypeAndSubTypes(Type type)
        {
            var discriminators = new List<BsonValue>();
            foreach (var entry in _classMap.TypeToDiscriminatorMap)
            {
                var discriminatedType = entry.Key;
                if (type.IsAssignableFrom(discriminatedType))
                {
                    var discriminator = entry.Value;
                    discriminators.Add(discriminator);
                }
            }

            return discriminators.OrderBy(x => x).ToArray();
        }
    }
}
