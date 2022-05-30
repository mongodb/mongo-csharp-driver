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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Core.Encryption;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Represents settings for a crypt client.
    /// </summary>
    public sealed class CryptClientSettings
    {
        /// <summary>
        /// Gets a value indicating whether query analysis should be bypassed.
        /// </summary>
        public bool? BypassQueryAnalysis { get; }

        /// <summary>
        /// Gets the csfle library path.
        /// </summary>
        public string CsfleLibPath { get; }

        /// <summary>
        /// Gets the csfle search path.
        /// </summary>
        public string CsfleSearchPath { get; }

        /// <summary>
        /// Gets the encrypted fields map.
        /// </summary>
        public IReadOnlyDictionary<string, BsonDocument> EncryptedFieldsMap { get; }

        /// <summary>
        /// Gets a value indicating whether csfle is required.
        /// </summary>
        public bool? IsCsfleRequired { get; }

        /// <summary>
        /// Gets the KMS providers.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> KmsProviders { get; }

        /// <summary>
        /// Gets the schema map.
        /// </summary>
        public IReadOnlyDictionary<string, BsonDocument> SchemaMap { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptClientSettings"/> class.
        /// </summary>
        /// <param name="bypassQueryAnalysis">The bypass query analysis.</param>
        /// <param name="csfleLibPath">The csfle library path.</param>
        /// <param name="csfleSearchPath">The csfle search path.</param>
        /// <param name="encryptedFieldsMap">The encrypted fields map.</param>
        /// <param name="isCsfleRequired">Value indicating whether csfle is required..</param>
        /// <param name="kmsProviders">The KMS providers.</param>
        /// <param name="schemaMap">The schema map.</param>
        public CryptClientSettings(
            bool? bypassQueryAnalysis,
            string csfleLibPath,
            string csfleSearchPath,
            IReadOnlyDictionary<string, BsonDocument> encryptedFieldsMap,
            bool? isCsfleRequired,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders,
            IReadOnlyDictionary<string, BsonDocument> schemaMap)
        {
            BypassQueryAnalysis = bypassQueryAnalysis;
            CsfleLibPath = csfleLibPath;
            CsfleSearchPath = csfleSearchPath;
            EncryptedFieldsMap = encryptedFieldsMap;
            IsCsfleRequired = isCsfleRequired;
            KmsProviders = kmsProviders;
            SchemaMap = schemaMap;
        }

        // methods
        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is CryptClientSettings rhs))
            {
                return false;
            }

            return
                BypassQueryAnalysis == rhs.BypassQueryAnalysis && // fail fast
                CsfleLibPath == rhs.CsfleLibPath &&
                CsfleSearchPath == rhs.CsfleSearchPath &&
                EncryptedFieldsMap.IsEquivalentTo(rhs.EncryptedFieldsMap, object.Equals) &&
                IsCsfleRequired == rhs.IsCsfleRequired &&
                KmsProvidersEqualityHelper.Equals(KmsProviders, rhs.KmsProviders) &&
                SchemaMap.IsEquivalentTo(rhs.SchemaMap, object.Equals);
        }
    }
}
