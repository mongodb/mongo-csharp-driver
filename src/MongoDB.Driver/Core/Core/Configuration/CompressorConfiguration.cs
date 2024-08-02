/* Copyright 2019–present MongoDB Inc.
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

using System.Collections.Generic;
using MongoDB.Driver.Core.Compression;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Represents a compressor configuration.
    /// </summary>
    public sealed class CompressorConfiguration
    {
        // private fields
        private readonly IDictionary<string, object> _properties;
        private readonly CompressorType _type;

        // constructors
        /// <summary>
        /// Initializes an instance of <see cref="CompressorConfiguration"/>.
        /// </summary>
        /// <param name="type">The compressor type.</param>
        public CompressorConfiguration(CompressorType type)
        {
            _type = type;
            _properties = new Dictionary<string, object>();
        }

        // public properties
        /// <summary>
        /// Gets the compression properties.
        /// </summary>
        public IDictionary<string, object> Properties => _properties;

        /// <summary>
        /// Gets the compressor type.
        /// </summary>
        public CompressorType Type => _type;

        // public methods
        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, this))
            {
                return true;
            }

            if (object.ReferenceEquals(obj, null) || obj.GetType() != typeof(CompressorConfiguration))
            {
                return false;
            }

            var rhs = (CompressorConfiguration)obj;
            return
                _type == rhs._type &&
                IsEquivalentTo(_properties, rhs._properties);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_type)
                .HashElements(_properties.Keys) // keep it cheap
                .GetHashCode();
        }

        // private methods
        private bool IsEquivalentTo(IDictionary<string, object> x, IDictionary<string, object> y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
            {
                return false;
            }

            if (x.Count != y.Count)
            {
                return false;
            }

            foreach (var keyValuePair in x)
            {
                var key = keyValuePair.Key;
                var xValue = keyValuePair.Value;
                if (!y.TryGetValue(key, out var yValue) || !object.Equals(xValue, yValue))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
