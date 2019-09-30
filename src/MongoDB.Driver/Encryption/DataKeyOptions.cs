/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Bson;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// [Beta] Options for creating a data key.
    /// </summary>
    public class DataKeyOptions
    {
        // private fields
        private readonly IReadOnlyList<string> _alternateKeyNames;
        private readonly BsonDocument _masterKey;

        // constructors
        /// <summary>
        /// [Beta] Initializes a new instance of the <see cref="DataKeyOptions"/> class.
        /// </summary>
        /// <param name="alternateKeyNames">The alternate key names.</param>
        /// <param name="masterKey">The master key.</param>
        public DataKeyOptions(
            Optional<IReadOnlyList<string>> alternateKeyNames = default,
            Optional<BsonDocument> masterKey = default)
        {
            _alternateKeyNames = alternateKeyNames.WithDefault(null);
            _masterKey = masterKey.WithDefault(null);
        }

        /// <summary>
        /// [Beta] Gets the alternate key names.
        /// </summary>
        /// <value>
        /// The alternate key names.
        /// </value>
        public IReadOnlyList<string> AlternateKeyNames => _alternateKeyNames;

        // public properties
        /// <summary>
        /// [Beta] Gets the master key.
        /// </summary>
        /// <value>
        /// The master key.
        /// </value>
        public BsonDocument MasterKey => _masterKey;

        /// <summary>
        /// [Beta] Returns a new DataKeyOptions instance with some settings changed.
        /// </summary>
        /// <param name="alternateKeyNames">The alternate key names.</param>
        /// <param name="masterKey">The master key.</param>
        /// <returns>A new DataKeyOptions instance.</returns>
        public DataKeyOptions With(
            Optional<IReadOnlyList<string>> alternateKeyNames = default,
            Optional<BsonDocument> masterKey = default)
        {
            return new DataKeyOptions(
                alternateKeyNames: Optional.Create(alternateKeyNames.WithDefault(_alternateKeyNames)),
                masterKey: Optional.Create(masterKey.WithDefault(_masterKey)));
        }
    }
}
