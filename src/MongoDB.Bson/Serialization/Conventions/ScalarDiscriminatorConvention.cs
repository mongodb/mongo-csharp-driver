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

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// Represents a discriminator convention where the discriminator is provided by the class map of the actual type.
    /// </summary>
    public class ScalarDiscriminatorConvention : StandardDiscriminatorConvention, IScalarDiscriminatorConvention
    {
        private readonly ConcurrentDictionary<Type, BsonValue[]> _cachedTypeAndSubTypeDiscriminators = new();

        // constructors
        /// <summary>
        /// Initializes a new instance of the ScalarDiscriminatorConvention class.
        /// </summary>
        /// <param name="elementName">The element name.</param>
        public ScalarDiscriminatorConvention(string elementName)
            : base(elementName)
        {
        }

        // public methods
        /// <summary>
        /// Gets the discriminator value for an actual type.
        /// </summary>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="actualType">The actual type.</param>
        /// <returns>The discriminator value.</returns>
        public override BsonValue GetDiscriminator(Type nominalType, Type actualType) =>
            GetDiscriminator(nominalType, actualType, BsonSerializer.DefaultSerializationDomain);

        /// <inheritdoc />
        public override BsonValue GetDiscriminator(Type nominalType, Type actualType, IBsonSerializationDomain domain)
        {
            // TODO: this isn't quite right, not all classes are serialized using a class map serializer
            var classMap = domain.BsonClassMap.LookupClassMap(actualType);
            if (actualType != nominalType || classMap.DiscriminatorIsRequired)
            {
                return classMap.Discriminator;
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public BsonValue[] GetDiscriminatorsForTypeAndSubTypes(Type type)
        {
            return _cachedTypeAndSubTypeDiscriminators.GetOrAdd(type, BsonSerializer.GetDiscriminatorsForTypeAndSubTypes);
        }
    }
}
