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

using MongoDB.Driver.Core.Misc;
using System;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// Encryption options for explicit encryption.
    /// </summary>
    public class EncryptOptions
    {
        #region static
        private static string ConvertEnumAlgorithmToString(EncryptionAlgorithm encryptionAlgorithm) =>
            encryptionAlgorithm switch
            {
                EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic => "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic",
                EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random => "AEAD_AES_256_CBC_HMAC_SHA_512-Random",
                _ => encryptionAlgorithm.ToString(),
            };
        #endregion

        // private fields
        private readonly string _algorithm;
        private readonly string _alternateKeyName;
        private readonly long? _contentionFactor;
        private readonly Guid? _keyId;
        private readonly string _queryType;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptOptions"/> class.
        /// </summary>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="keyId">The key Id.</param>
        /// <param name="contentionFactor">[Beta] The contention factor.</param>
        /// <param name="queryType">[Beta] The query type.</param>
        public EncryptOptions(
            string algorithm,
            Optional<string> alternateKeyName = default,
            Optional<Guid?> keyId = default,
            Optional<long?> contentionFactor = default,
            Optional<string> queryType = default)
        {
            Ensure.IsNotNull(algorithm, nameof(algorithm));
            if (Enum.TryParse<EncryptionAlgorithm>(algorithm, out var @enum))
            {
                _algorithm = ConvertEnumAlgorithmToString(@enum);
            }
            else
            {
                _algorithm = algorithm;
            }

            _alternateKeyName = alternateKeyName.WithDefault(null);
            _contentionFactor = contentionFactor.WithDefault(null);
            _keyId = keyId.WithDefault(null);
            _queryType = queryType.WithDefault(null);
            EnsureThatOptionsAreValid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptOptions"/> class.
        /// </summary>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="keyId">The key Id.</param>
        /// <param name="contentionFactor">[Beta] The contention factor.</param>
        /// <param name="queryType">[Beta] The query type.</param>
        public EncryptOptions(
            EncryptionAlgorithm algorithm,
            Optional<string> alternateKeyName = default,
            Optional<Guid?> keyId = default,
            Optional<long?> contentionFactor = default,
            Optional<string> queryType = default)
            : this(
                  algorithm: ConvertEnumAlgorithmToString(algorithm),
                  alternateKeyName,
                  keyId,
                  contentionFactor,
                  queryType)
        {
        }

        // public properties
        /// <summary>
        /// Gets the algorithm.
        /// </summary>
        /// <value>
        /// The algorithm.
        /// </value>
        public string Algorithm => _algorithm;

        /// <summary>
        /// Gets the alternate key name.
        /// </summary>
        /// <value>
        /// The alternate key name.
        /// </value>
        public string AlternateKeyName => _alternateKeyName;

        /// <summary>
        /// [Beta] Gets the contention factor.
        /// </summary>
        /// <value>
        /// The contention factor.
        /// </value>
        public long? ContentionFactor => _contentionFactor;

        /// <summary>
        /// Gets the key identifier.
        /// </summary>
        /// <value>
        /// The key identifier.
        /// </value>
        public Guid? KeyId => _keyId;

        /// <summary>
        /// [Beta] Gets the query type.
        /// </summary>
        /// <value>
        /// The query type.
        /// </value>
        public string QueryType => _queryType;

        /// <summary>
        /// Returns a new EncryptOptions instance with some settings changed.
        /// </summary>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="keyId">The keyId.</param>
        /// <param name="contentionFactor">[Beta] The contention factor.</param>
        /// <param name="queryType">[Beta] The query type.</param>
        /// <returns>A new EncryptOptions instance.</returns>
        public EncryptOptions With(
            Optional<string> algorithm = default,
            Optional<string> alternateKeyName = default,
            Optional<Guid?> keyId = default,
            Optional<long?> contentionFactor = default,
            Optional<string> queryType = default)
        {
            return new EncryptOptions(
                algorithm: algorithm.WithDefault(_algorithm),
                alternateKeyName: alternateKeyName.WithDefault(_alternateKeyName),
                keyId: keyId.WithDefault(_keyId),
                contentionFactor: contentionFactor.WithDefault(_contentionFactor),
                queryType: queryType.WithDefault(_queryType));
        }

        // private methods
        private void EnsureThatOptionsAreValid()
        {
            Ensure.That(!(!_keyId.HasValue && _alternateKeyName == null), "Key Id and AlternateKeyName may not both be null.");
            Ensure.That(!(_keyId.HasValue && _alternateKeyName != null), "Key Id and AlternateKeyName may not both be set.");
            Ensure.That(!(_contentionFactor.HasValue && _algorithm != EncryptionAlgorithm.Indexed.ToString()), "ContentionFactor only applies for Indexed algorithm.");
            Ensure.That(!(_queryType != null && _algorithm != EncryptionAlgorithm.Indexed.ToString()), "QueryType only applies for Indexed algorithm.");
        }
    }
}
