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

using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// Rewrap many data keys options.
    /// </summary>
    public sealed class RewrapManyDataKeyOptions
    {
        // private fields
        private readonly string _provider;
        private readonly BsonDocument _masterKey;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RewrapManyDataKeyOptions"/> class.
        /// </summary>
        /// <param name="masterKey">The master key.</param>
        /// <param name="provider">The provider name.</param>
        public RewrapManyDataKeyOptions(
            string provider,
            Optional<BsonDocument> masterKey = default)
        {
            _provider = Ensure.IsNotNullOrEmpty(provider, nameof(provider));
            _masterKey = masterKey.WithDefault(null);
        }

        // public properties
        /// <summary>
        /// Gets the provider name.
        /// </summary>
        /// <value>
        /// The provider name.
        /// </value>
        public string Provider => _provider;

        /// <summary>
        /// Gets the master key.
        /// </summary>
        /// <value>
        /// The master key.
        /// </value>
        public BsonDocument MasterKey => _masterKey;

        /// <summary>
        /// Returns a new DataKeyOptions instance with some settings changed.
        /// </summary>
        /// <param name="masterKey">The master key.</param>
        /// <param name="provider">The provider name.</param>
        /// <returns>A new DataKeyOptions instance.</returns>
        public RewrapManyDataKeyOptions With(
            Optional<BsonDocument> masterKey = default,
            Optional<string> provider = default) =>
            new RewrapManyDataKeyOptions(
                masterKey: Optional.Create(masterKey.WithDefault(_masterKey)),
                provider: provider.WithDefault(_provider));
    }
}
