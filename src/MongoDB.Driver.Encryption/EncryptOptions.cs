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
using System.Linq;
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
    /// Prefix options.
    /// </summary>
    /// <remarks>
    /// PrefixOptions is used with StringOptions (or the deprecated TextOptions) and provides further options to support "prefix" and "prefixPreview" queries.
    /// </remarks>
    public sealed class PrefixOptions
    {
        private readonly int _strMaxQueryLength;
        private readonly int _strMinQueryLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrefixOptions"/> class.
        /// </summary>
        /// <param name="strMaxQueryLength">The maximum allowed query length.</param>
        /// <param name="strMinQueryLength">The minimum allowed query length.</param>
        public PrefixOptions(int strMaxQueryLength, int strMinQueryLength)
        {
            Ensure.IsGreaterThanZero(strMaxQueryLength, nameof(strMaxQueryLength));
            Ensure.IsGreaterThanZero(strMinQueryLength, nameof(strMinQueryLength));
            Ensure.That(strMaxQueryLength >= strMinQueryLength,
                "strMaxQueryLength must be greater than or equal to strMinQueryLength");

            _strMaxQueryLength = strMaxQueryLength;
            _strMinQueryLength = strMinQueryLength;
        }

        /// <summary>
        /// Gets the maximum allowed query length.
        /// </summary>
        /// <remarks>
        /// Querying with a longer string will error.
        /// </remarks>
        public int StrMaxQueryLength => _strMaxQueryLength;

        /// <summary>
        /// Gets the minimum allowed query length.
        /// </summary>
        /// <remarks>
        /// Querying with a shorter string will error.
        /// </remarks>
        public int StrMinQueryLength => _strMinQueryLength;
    }

    /// <summary>
    /// Substring options.
    /// </summary>
    /// <remarks>
    /// SubstringOptions is used with StringOptions (or the deprecated TextOptions) and provides further options to support "substring" and "substringPreview" queries.
    /// </remarks>
    public sealed class SubstringOptions
    {
        private readonly int _strMaxLength;
        private readonly int _strMaxQueryLength;
        private readonly int _strMinQueryLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubstringOptions"/> class.
        /// </summary>
        /// <param name="strMaxLength">The maximum allowed length to insert.</param>
        /// <param name="strMaxQueryLength">The maximum allowed query length.</param>
        /// <param name="strMinQueryLength">The minimum allowed query length.</param>
        public SubstringOptions(int strMaxLength, int strMaxQueryLength, int strMinQueryLength)
        {
            Ensure.IsGreaterThanZero(strMaxLength, nameof(strMaxLength));
            Ensure.IsGreaterThanZero(strMaxQueryLength, nameof(strMaxQueryLength));
            Ensure.IsGreaterThanZero(strMinQueryLength, nameof(strMinQueryLength));

            Ensure.That(strMaxLength >= strMaxQueryLength,
                "strMaxLength must be greater than or equal to strMaxQueryLength");
            Ensure.That(strMaxQueryLength >= strMinQueryLength,
                "strMaxQueryLength must be greater than or equal to strMinQueryLength");

            _strMaxLength = strMaxLength;
            _strMaxQueryLength = strMaxQueryLength;
            _strMinQueryLength = strMinQueryLength;
        }

        /// <summary>
        /// Gets the maximum allowed length to insert.
        /// </summary>
        /// <remarks>
        /// Inserting longer strings will error.
        /// </remarks>
        public int StrMaxLength => _strMaxLength;

        /// <summary>
        /// Gets the maximum allowed query length.
        /// </summary>
        /// <remarks>
        /// Querying with a longer string will error.
        /// </remarks>
        public int StrMaxQueryLength => _strMaxQueryLength;

        /// <summary>
        /// Gets the minimum allowed query length.
        /// </summary>
        /// <remarks>
        /// Querying with a shorter string will error.
        /// </remarks>
        public int StrMinQueryLength => _strMinQueryLength;
    }

    /// <summary>
    /// Suffix options.
    /// </summary>
    /// <remarks>
    /// SuffixOptions is used with StringOptions (or the deprecated TextOptions) and provides further options to support "suffix" and "suffixPreview" queries.
    /// </remarks>
    public sealed class SuffixOptions
    {
        private readonly int _strMaxQueryLength;
        private readonly int _strMinQueryLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="SuffixOptions"/> class.
        /// </summary>
        /// <param name="strMaxQueryLength">The maximum allowed query length.</param>
        /// <param name="strMinQueryLength">The minimum allowed query length.</param>
        public SuffixOptions(int strMaxQueryLength, int strMinQueryLength)
        {
            Ensure.IsGreaterThanZero(strMaxQueryLength, nameof(strMaxQueryLength));
            Ensure.IsGreaterThanZero(strMinQueryLength, nameof(strMinQueryLength));
            Ensure.That(strMaxQueryLength >= strMinQueryLength,
                "strMaxQueryLength must be greater than or equal to strMinQueryLength");

            _strMaxQueryLength = strMaxQueryLength;
            _strMinQueryLength = strMinQueryLength;
        }

        /// <summary>
        /// Gets the maximum allowed query length.
        /// </summary>
        /// <remarks>
        /// Querying with a longer string will error.
        /// </remarks>
        public int StrMaxQueryLength => _strMaxQueryLength;

        /// <summary>
        /// Gets the minimum allowed query length.
        /// </summary>
        /// <remarks>
        /// Querying with a shorter string will error.
        /// </remarks>
        public int StrMinQueryLength => _strMinQueryLength;
    }

    /// <summary>
    /// String options.
    /// </summary>
    /// <remarks>
    /// StringOptions specifies options for a Queryable Encryption field that supports the "prefix", "prefixPreview", "substring", "substringPreview", "suffix", and "suffixPreview" query types.
    /// StringOptions only applies when the encryption algorithm is "String".
    /// </remarks>
    public sealed class StringOptions
    {
        private readonly bool _caseSensitive;
        private readonly bool _diacriticSensitive;
        private readonly PrefixOptions _prefixOptions;
        private readonly SubstringOptions _substringOptions;
        private readonly SuffixOptions _suffixOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringOptions"/> class.
        /// </summary>
        /// <param name="caseSensitive">The indicator of whether string indexes for this field are case-sensitive.</param>
        /// <param name="diacriticSensitive">The indicator of whether string indexes for this field are diacritic sensitive.</param>
        /// <param name="prefixOptions">The prefix options.</param>
        /// <param name="substringOptions">The substring options.</param>
        /// <param name="suffixOptions">The suffix options.</param>
        public StringOptions(
            bool caseSensitive,
            bool diacriticSensitive,
            Optional<PrefixOptions> prefixOptions = default,
            Optional<SubstringOptions> substringOptions = default,
            Optional<SuffixOptions> suffixOptions = default)
        {
            _caseSensitive = caseSensitive;
            _diacriticSensitive = diacriticSensitive;
            _prefixOptions = prefixOptions.WithDefault(null);
            _substringOptions = substringOptions.WithDefault(null);
            _suffixOptions = suffixOptions.WithDefault(null);
        }

        /// <summary>
        /// Gets whether string indexes for this field are case-sensitive.
        /// </summary>
        public bool CaseSensitive => _caseSensitive;

        /// <summary>
        /// Gets whether string indexes for this field are diacritic sensitive.
        /// </summary>
        public bool DiacriticSensitive => _diacriticSensitive;

        /// <summary>
        /// Gets the prefix options.
        /// </summary>
        public PrefixOptions PrefixOptions => _prefixOptions;

        /// <summary>
        /// Gets the substring options.
        /// </summary>
        public SubstringOptions SubstringOptions => _substringOptions;

        /// <summary>
        /// Gets the suffix options.
        /// </summary>
        public SuffixOptions SuffixOptions => _suffixOptions;
    }

    /// <summary>
    /// Text options.
    /// </summary>
    /// <remarks>
    /// This is a deprecated alias for <see cref="StringOptions"/>. Use <see cref="StringOptions"/> instead.
    /// </remarks>
    [Obsolete("Use StringOptions instead.")]
    public sealed class TextOptions
    {
        private readonly bool _caseSensitive;
        private readonly bool _diacriticSensitive;
        private readonly PrefixOptions _prefixOptions;
        private readonly SubstringOptions _substringOptions;
        private readonly SuffixOptions _suffixOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextOptions"/> class.
        /// </summary>
        /// <param name="caseSensitive">The indicator of whether text indexes for this field are case-sensitive.</param>
        /// <param name="diacriticSensitive">The indicator of whether text indexes for this field are diacritic sensitive.</param>
        /// <param name="prefixOptions">The prefix options.</param>
        /// <param name="substringOptions">The substring options.</param>
        /// <param name="suffixOptions">The suffix options.</param>
        public TextOptions(
            bool caseSensitive,
            bool diacriticSensitive,
            Optional<PrefixOptions> prefixOptions = default,
            Optional<SubstringOptions> substringOptions = default,
            Optional<SuffixOptions> suffixOptions = default)
        {
            _caseSensitive = caseSensitive;
            _diacriticSensitive = diacriticSensitive;
            _prefixOptions = prefixOptions.WithDefault(null);
            _substringOptions = substringOptions.WithDefault(null);
            _suffixOptions = suffixOptions.WithDefault(null);
        }

        /// <summary>
        /// Gets whether text indexes for this field are case-sensitive.
        /// </summary>
        public bool CaseSensitive => _caseSensitive;

        /// <summary>
        /// Gets whether text indexes for this field are diacritic sensitive.
        /// </summary>
        public bool DiacriticSensitive => _diacriticSensitive;

        /// <summary>
        /// Gets the prefix options.
        /// </summary>
        public PrefixOptions PrefixOptions => _prefixOptions;

        /// <summary>
        /// Gets the substring options.
        /// </summary>
        public SubstringOptions SubstringOptions => _substringOptions;

        /// <summary>
        /// Gets the suffix options.
        /// </summary>
        public SuffixOptions SuffixOptions => _suffixOptions;
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
#pragma warning disable CS0618 // TextPreview is a deprecated alias for String and is translated to "String".
                EncryptionAlgorithm.TextPreview => EncryptionAlgorithm.String.ToString(),
#pragma warning restore CS0618
                _ => encryptionAlgorithm.ToString(),
            };

        private static readonly string[] ValidStringQueryTypes = ["prefix", "prefixPreview", "substring", "substringPreview", "suffix", "suffixPreview"];
        #endregion

        // private fields
        private readonly string _algorithm;
        private readonly string _alternateKeyName;
        private readonly long? _contentionFactor;
        private readonly Guid? _keyId;
        private readonly RangeOptions _rangeOptions;
        private readonly StringOptions _stringOptions;
#pragma warning disable CS0618 // _textOptions is the deprecated alias for _stringOptions.
        private readonly TextOptions _textOptions;
#pragma warning restore CS0618
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
        /// <param name="stringOptions">The string options.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="contentionFactor">The contention factor.</param>
        /// <param name="keyId">The key Id.</param>
        /// <param name="queryType">The query type.</param>
        public EncryptOptions(
            string algorithm,
            StringOptions stringOptions,
            Optional<string> alternateKeyName = default,
            Optional<long?> contentionFactor = default,
            Optional<Guid?> keyId = default,
            Optional<string> queryType = default)
        {
            Ensure.IsNotNull(algorithm, nameof(algorithm));
            Ensure.IsNotNull(stringOptions, nameof(stringOptions));
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
            _stringOptions = stringOptions;
            _queryType = queryType.WithDefault(null);
            EnsureThatOptionsAreValid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptOptions"/> class.
        /// </summary>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="textOptions">The text options.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="contentionFactor">The contention factor.</param>
        /// <param name="keyId">The key Id.</param>
        /// <param name="queryType">The query type.</param>
        [Obsolete("Use the StringOptions overload instead.")]
        public EncryptOptions(
            string algorithm,
            TextOptions textOptions,
            Optional<string> alternateKeyName = default,
            Optional<long?> contentionFactor = default,
            Optional<Guid?> keyId = default,
            Optional<string> queryType = default)
        {
            Ensure.IsNotNull(algorithm, nameof(algorithm));
            Ensure.IsNotNull(textOptions, nameof(textOptions));
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
            _textOptions = textOptions;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptOptions"/> class.
        /// </summary>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="stringOptions">The string options.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="keyId">The key Id.</param>
        /// <param name="contentionFactor">The contention factor.</param>
        /// <param name="queryType">The query type.</param>
        public EncryptOptions(
            EncryptionAlgorithm algorithm,
            StringOptions stringOptions,
            Optional<string> alternateKeyName = default,
            Optional<Guid?> keyId = default,
            Optional<long?> contentionFactor = default,
            Optional<string> queryType = default)
            : this(
                algorithm: ConvertEnumAlgorithmToString(algorithm),
                stringOptions,
                alternateKeyName,
                contentionFactor,
                keyId,
                queryType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptOptions"/> class.
        /// </summary>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="textOptions">The text options.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="keyId">The key Id.</param>
        /// <param name="contentionFactor">The contention factor.</param>
        /// <param name="queryType">The query type.</param>
        [Obsolete("Use the StringOptions overload instead.")]
        public EncryptOptions(
            EncryptionAlgorithm algorithm,
            TextOptions textOptions,
            Optional<string> alternateKeyName = default,
            Optional<Guid?> keyId = default,
            Optional<long?> contentionFactor = default,
            Optional<string> queryType = default)
            : this(
                algorithm: ConvertEnumAlgorithmToString(algorithm),
                textOptions,
                alternateKeyName,
                contentionFactor,
                keyId,
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
        /// <remarks>
        /// Currently, we only support "equality", "range", "prefix", "prefixPreview", "substring", "substringPreview", "suffix" or "suffixPreview" queryTypes.
        /// </remarks>
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
        /// Gets the string options.
        /// </summary>
        /// <remarks>
        /// StringOptions specifies options for a Queryable Encryption field that supports the "prefix", "prefixPreview", "substring", "substringPreview", "suffix", and "suffixPreview" query types.
        /// StringOptions only applies when the encryption algorithm is "String".
        /// </remarks>
        public StringOptions StringOptions => _stringOptions;

        /// <summary>
        /// Gets the text options.
        /// </summary>
        /// <remarks>
        /// This is a deprecated alias for <see cref="StringOptions"/>. Use <see cref="StringOptions"/> instead.
        /// </remarks>
        [Obsolete("Use StringOptions instead.")]
        public TextOptions TextOptions => _textOptions;

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

        /// <summary>
        /// Returns a new EncryptOptions instance with some settings changed.
        /// </summary>
        /// <param name="stringOptions">The string options.</param>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="keyId">The keyId.</param>
        /// <param name="contentionFactor">The contention factor.</param>
        /// <param name="queryType">The query type.</param>
        /// <returns>A new EncryptOptions instance.</returns>
        public EncryptOptions With(
            StringOptions stringOptions,
            Optional<string> algorithm = default,
            Optional<string> alternateKeyName = default,
            Optional<Guid?> keyId = default,
            Optional<long?> contentionFactor = default,
            Optional<string> queryType = default)
        {
            return new EncryptOptions(
                algorithm: algorithm.WithDefault(_algorithm),
                alternateKeyName: alternateKeyName.WithDefault(_alternateKeyName),
                contentionFactor: contentionFactor.WithDefault(_contentionFactor),
                keyId: keyId.WithDefault(_keyId),
                queryType: queryType.WithDefault(_queryType),
                stringOptions: stringOptions);
        }

        /// <summary>
        /// Returns a new EncryptOptions instance with some settings changed.
        /// </summary>
        /// <param name="textOptions">The text options.</param>
        /// <param name="algorithm">The encryption algorithm.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="keyId">The keyId.</param>
        /// <param name="contentionFactor">The contention factor.</param>
        /// <param name="queryType">The query type.</param>
        /// <returns>A new EncryptOptions instance.</returns>
        [Obsolete("Use the StringOptions overload instead.")]
        public EncryptOptions With(
            TextOptions textOptions,
            Optional<string> algorithm = default,
            Optional<string> alternateKeyName = default,
            Optional<Guid?> keyId = default,
            Optional<long?> contentionFactor = default,
            Optional<string> queryType = default)
        {
            return new EncryptOptions(
                algorithm: algorithm.WithDefault(_algorithm),
                alternateKeyName: alternateKeyName.WithDefault(_alternateKeyName),
                contentionFactor: contentionFactor.WithDefault(_contentionFactor),
                keyId: keyId.WithDefault(_keyId),
                queryType: queryType.WithDefault(_queryType),
                textOptions: textOptions);
        }

        // internal methods
#pragma warning disable CS0618 // marshal whichever of StringOptions/TextOptions was set.
        internal BsonDocument GetStringOptionsDocument() => _stringOptions?.CreateDocument() ?? _textOptions?.CreateDocument();
#pragma warning restore CS0618

        // private methods
        private void EnsureThatOptionsAreValid()
        {
            Ensure.That(!(!_keyId.HasValue && _alternateKeyName == null), "Key Id and AlternateKeyName may not both be null.");
            Ensure.That(!(_keyId.HasValue && _alternateKeyName != null), "Key Id and AlternateKeyName may not both be set.");
            Ensure.That(!(_contentionFactor.HasValue && (_algorithm != EncryptionAlgorithm.Indexed.ToString() && _algorithm != EncryptionAlgorithm.Range.ToString() && _algorithm != EncryptionAlgorithm.String.ToString())), "ContentionFactor only applies for Indexed, Range, or String algorithm.");
            Ensure.That(!(_queryType != null && (_algorithm != EncryptionAlgorithm.Indexed.ToString() && _algorithm != EncryptionAlgorithm.Range.ToString() && _algorithm != EncryptionAlgorithm.String.ToString())), "QueryType only applies for Indexed, Range, or String algorithm.");
            Ensure.That(!(_rangeOptions != null && _algorithm != EncryptionAlgorithm.Range.ToString()), "RangeOptions only applies for Range algorithm.");
            Ensure.That(!(_stringOptions != null && _algorithm != EncryptionAlgorithm.String.ToString()), "StringOptions only applies for String algorithm.");
#pragma warning disable CS0618 // _textOptions is the deprecated alias; validate whichever options were set.
            Ensure.That(!(_textOptions != null && _algorithm != EncryptionAlgorithm.String.ToString()), "TextOptions only applies for String algorithm.");

            if (_algorithm == EncryptionAlgorithm.String.ToString() && _queryType != null)
            {
                Ensure.That(
                    ValidStringQueryTypes.Contains(_queryType),
                    $"QueryType '{_queryType}' is not valid for String algorithm. Use: {string.Join(", ", ValidStringQueryTypes)}.");
            }

            if ((_stringOptions != null || _textOptions != null) && _queryType != null)
            {
                Ensure.That(
                    !((_queryType == "prefix" || _queryType == "prefixPreview") && (_stringOptions?.PrefixOptions ?? _textOptions?.PrefixOptions) == null),
                    "PrefixOptions must be set when queryType is 'prefix' or 'prefixPreview'");
                Ensure.That(
                    !((_queryType == "substring" || _queryType == "substringPreview") && (_stringOptions?.SubstringOptions ?? _textOptions?.SubstringOptions) == null),
                    "SubstringOptions must be set when queryType is 'substring' or 'substringPreview'");
                Ensure.That(
                    !((_queryType == "suffix" || _queryType == "suffixPreview") && (_stringOptions?.SuffixOptions ?? _textOptions?.SuffixOptions) == null),
                    "SuffixOptions must be set when queryType is 'suffix' or 'suffixPreview'");
            }
#pragma warning restore CS0618
        }
    }
}
