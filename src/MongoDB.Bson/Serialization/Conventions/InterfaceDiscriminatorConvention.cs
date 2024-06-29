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

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// Represents a discriminator convention where the discriminator is an array of all the discriminators provided by the class maps of the root class down to the actual type.
    /// </summary>
    public class InterfaceDiscriminatorConvention<TInterface> : StandardDiscriminatorConvention
    {
        private readonly IDictionary<Type, BsonValue> _discriminators = new Dictionary<Type, BsonValue>();
        // constructors
        /// <summary>
        /// Initializes a new instance of the HierarchicalDiscriminatorConvention class.
        /// </summary>
        /// <param name="elementName">The element name.</param>
        public InterfaceDiscriminatorConvention(string elementName)
            : base(elementName)
        {
            PrecomputeDiscriminators();
        }

        // public methods
        /// <summary>
        /// Gets the discriminator value for an actual type.
        /// </summary>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="actualType">The actual type.</param>
        /// <returns>The discriminator value.</returns>
        public override BsonValue GetDiscriminator(Type nominalType, Type actualType)
        {
            if (nominalType != typeof(TInterface))
            {
                return null;
            }

            return _discriminators.TryGetValue(actualType, out var discriminator) ? discriminator : null;
        }

        private void PrecomputeDiscriminators()
        {
            var interfaceType = typeof(TInterface);

            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException("<TInterface> must be an interface", nameof(TInterface));
            }

            var dependents = interfaceType.Assembly.GetTypes().Where(x => interfaceType.IsAssignableFrom(x));

            foreach (var dependent in dependents)
            {
                var interfaces = OrderInterfaces(dependent.GetInterfaces().ToList());
                var discriminator = new BsonArray(interfaces.Select(x => x.Name))
                {
                    dependent.Name
                };

                _discriminators.Add(dependent, discriminator);
            }
        }

        private IEnumerable<Type> OrderInterfaces(List<Type> interfaces)
        {
            var sorted = new List<Type>();
            while (interfaces.Any())
            {
                var allParentInterfaces = interfaces.SelectMany(t => t.GetInterfaces()).ToList();

                foreach (var interfaceType in interfaces)
                {
                    var newInterfaces = new List<Type>();

                    if (allParentInterfaces.Contains(interfaceType))
                    {
                        newInterfaces.Add(interfaceType);
                    }
                    else
                    {
                        sorted.Add(interfaceType);
                    }

                    interfaces = newInterfaces;
                }
            }

            sorted.Reverse();

            return sorted;
        }
    }
}
