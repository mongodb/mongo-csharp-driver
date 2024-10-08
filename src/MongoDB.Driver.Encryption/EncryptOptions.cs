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

using System;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// Range options.
    /// </summary>
    /// <remarks>
    /// The Range algorithm is experimental only. It is not intended for public use.
    /// RangeOpts specifies index options for a Queryable Encryption field supporting "range" queries.
    /// min, max, sparsity, and range must match the values set in the encryptedFields of the destination collection.
    /// For double and decimal128, min/max/precision must all be set, or all be unset.
    /// RangeOptions only applies when algorithm is "range".
    /// </remarks>
    public sealed class RangeOptions
    {
        private readonly BsonValue _max;
        private readonly BsonValue _min;
        private readonly int? _precision;
        private readonly long? _sparsity;
        private readonly int? _trimFactor;

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeOptions"/> class.
        /// </summary>
        /// <param name="min">The min range.</param>
        /// <param name="max">The max range.</param>
        /// <param name="precision">The precision range.</param>
        /// <param name="sparsity">The sparsity.</param>
        /// <param name="trimFactor">The trim factor.</param>
        public RangeOptions(
            Optional<BsonValue> min = default,
            Optional<BsonValue> max = default,
            Optional<int?> precision = default,
            Optional<long?> sparsity = default,
            Optional<int?> trimFactor = default)
        {
            _min = min.WithDefault(null);
            _max = max.WithDefault(null);
            _precision = precision.WithDefault(null);
            _sparsity = sparsity.WithDefault(null);
            _trimFactor = trimFactor.WithDefault(null);
        }

        // public properties
        /// <summary>
        /// Minimum value.
        /// </summary>
        /// <remarks>Min is required if precision is set.</remarks>
        public BsonValue Min => _min;

        /// <summary>
        /// Maximum value.
        /// </summary>
        /// <remarks>Max is required if precision is set.</remarks>
        public BsonValue Max => _max;

        /// <summary>
        /// Gets the trim factor.
        /// </summary>
        public int? TrimFactor => _trimFactor;

        /// <summary>
        /// Gets the precision.
        /// </summary>
        /// <remarks>
        /// Precision may only be set for double or decimal128.
        /// </remarks>
        public int? Precision => _precision;

        /// <summary>
        /// Gets the sparsity.
        /// </summary>
        public long? Sparsity => _sparsity;
    }

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
        private readonly RangeOptions _rangeOptions;
        private readonly string _queryType;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptOptions"/> class.
        /// </summary>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="keyId">The key Id.</param>
        /// <param name="contentionFactor">The contention factor.</param>
        /// <param name="queryType">The query type.</param>
        /// <param name="rangeOptions">The range options.</param>
        public EncryptOptions(
            string algorithm,
            Optional<string> alternateKeyName = default,
            Optional<Guid?> keyId = default,
            Optional<long?> contentionFactor = default,
            Optional<string> queryType = default,
            Optional<RangeOptions> rangeOptions = default)
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
            _rangeOptions = rangeOptions.WithDefault(null);
            _queryType = queryType.WithDefault(null);
            EnsureThatOptionsAreValid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptOptions"/> class.
        /// </summary>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="keyId">The key Id.</param>
        /// <param name="contentionFactor">The contention factor.</param>
        /// <param name="queryType">The query type.</param>
        /// <param name="rangeOptions">The range options.</param>
        public EncryptOptions(
            EncryptionAlgorithm algorithm,
            Optional<string> alternateKeyName = default,
            Optional<Guid?> keyId = default,
            Optional<long?> contentionFactor = default,
            Optional<string> queryType = default,
            Optional<RangeOptions> rangeOptions = default)
            : this(
                  algorithm: ConvertEnumAlgorithmToString(algorithm),
                  alternateKeyName,
                  keyId,
                  contentionFactor,
                  queryType,
                  rangeOptions)
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
        /// Gets the contention factor.
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
        /// Gets the query type.
        /// </summary>
        /// <value>
        /// The query type.
        /// </value>
        public string QueryType => _queryType;

        /// <summary>
        /// Gets the range options.
        /// </summary>
        /// <value>
        /// The range options.
        /// </value>
        /// <remarks>
        /// The Range algorithm is experimental only. It is not intended for public use.
        /// RangeOpts specifies index options for a Queryable Encryption field supporting "range" queries.
        /// RangeOptions only applies when algorithm is "range".
        /// </remarks>
        public RangeOptions RangeOptions => _rangeOptions;

        /// <summary>
        /// Returns a new EncryptOptions instance with some settings changed.
        /// </summary>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="keyId">The keyId.</param>
        /// <param name="contentionFactor">The contention factor.</param>
        /// <param name="queryType">The query type.</param>
        /// <param name="rangeOptions">The range options.</param>
        /// <returns>A new EncryptOptions instance.</returns>
        public EncryptOptions With(
            Optional<string> algorithm = default,
            Optional<string> alternateKeyName = default,
            Optional<Guid?> keyId = default,
            Optional<long?> contentionFactor = default,
            Optional<string> queryType = default,
            Optional<RangeOptions> rangeOptions = default)
        {
            return new EncryptOptions(
                algorithm: algorithm.WithDefault(_algorithm),
                alternateKeyName: alternateKeyName.WithDefault(_alternateKeyName),
                keyId: keyId.WithDefault(_keyId),
                contentionFactor: contentionFactor.WithDefault(_contentionFactor),
                queryType: queryType.WithDefault(_queryType),
                rangeOptions: rangeOptions.WithDefault(_rangeOptions));
        }

        // private methods
        private void EnsureThatOptionsAreValid()
        {
            Ensure.That(!(!_keyId.HasValue && _alternateKeyName == null), "Key Id and AlternateKeyName may not both be null.");
            Ensure.That(!(_keyId.HasValue && _alternateKeyName != null), "Key Id and AlternateKeyName may not both be set.");
            Ensure.That(!(_contentionFactor.HasValue && (_algorithm != EncryptionAlgorithm.Indexed.ToString() && _algorithm != EncryptionAlgorithm.Range.ToString())), "ContentionFactor only applies for Indexed or Range algorithm.");
            Ensure.That(!(_queryType != null && (_algorithm != EncryptionAlgorithm.Indexed.ToString() && _algorithm != EncryptionAlgorithm.Range.ToString())), "QueryType only applies for Indexed or Range algorithm.");
            Ensure.That(!(_rangeOptions != null && _algorithm != EncryptionAlgorithm.Range.ToString()), "RangeOptions only applies for Range algorithm.");
        }
    }
}
