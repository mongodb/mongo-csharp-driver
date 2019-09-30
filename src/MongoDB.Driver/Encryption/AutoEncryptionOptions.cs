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
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// [Beta] Auto encryption options.
    /// </summary>
    public class AutoEncryptionOptions
    {
        // private fields
        private readonly bool _bypassAutoEncryption;
        private readonly IReadOnlyDictionary<string, object> _extraOptions;
        private readonly IMongoClient _keyVaultClient;
        private readonly CollectionNamespace _keyVaultNamespace;
        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> _kmsProviders;
        private readonly IReadOnlyDictionary<string, BsonDocument> _schemaMap;

        // constructors
        /// <summary>
        /// [Beta] Initializes a new instance of the <see cref="AutoEncryptionOptions"/> class.
        /// </summary>
        /// <param name="keyVaultNamespace">The keyVault namespace.</param>
        /// <param name="kmsProviders">The kms providers.</param>
        /// <param name="bypassAutoEncryption">The bypass auto encryption flag.</param>
        /// <param name="extraOptions">The extra options.</param>
        /// <param name="keyVaultClient">The keyVault client.</param>
        /// <param name="schemaMap">The schema map.</param>
        public AutoEncryptionOptions(
            CollectionNamespace keyVaultNamespace,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders,
            Optional<bool> bypassAutoEncryption = default,
            Optional<IReadOnlyDictionary<string, object>> extraOptions = default,
            Optional<IMongoClient> keyVaultClient = default,
            Optional<IReadOnlyDictionary<string, BsonDocument>> schemaMap = default) 
        {
            _keyVaultNamespace = Ensure.IsNotNull(keyVaultNamespace, nameof(keyVaultNamespace));
            _kmsProviders = Ensure.IsNotNull(kmsProviders, nameof(kmsProviders));
            _bypassAutoEncryption = bypassAutoEncryption.WithDefault(false);
            _extraOptions = extraOptions.WithDefault(null);
            _keyVaultClient = keyVaultClient.WithDefault(null);
            _schemaMap = schemaMap.WithDefault(null);

            EncryptionExtraOptionsValidator.EnsureThatExtraOptionsAreValid(_extraOptions);
            KmsProvidersHelper.EnsureKmsProvidersAreValid(_kmsProviders);
        }

        // public properties
        /// <summary>
        /// [Beta] Gets a value indicating whether to bypass automatic encryption.
        /// </summary>
        /// <value>
        ///   <c>true</c> if automatic encryption should be bypasssed; otherwise, <c>false</c>.
        /// </value>
        public bool BypassAutoEncryption => _bypassAutoEncryption;

        /// <summary>
        /// [Beta] Gets the extra options.
        /// </summary>
        /// <value>
        /// The extra options.
        /// </value>
        public IReadOnlyDictionary<string, object> ExtraOptions => _extraOptions;

        /// <summary>
        /// [Beta] Gets the key vault client.
        /// </summary>
        /// <value>
        /// The key vault client.
        /// </value>
        public IMongoClient KeyVaultClient => _keyVaultClient;

        /// <summary>
        /// [Beta] Gets the key vault namespace.
        /// </summary>
        /// <value>
        /// The key vault namespace.
        /// </value>
        public CollectionNamespace KeyVaultNamespace => _keyVaultNamespace;

        /// <summary>
        /// [Beta] Gets the KMS providers.
        /// </summary>
        /// <value>
        /// The KMS providers.
        /// </value>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> KmsProviders => _kmsProviders;

        /// <summary>
        /// [Beta] Gets the schema map.
        /// </summary>
        /// <value>
        /// The schema map.
        /// </value>
        public IReadOnlyDictionary<string, BsonDocument> SchemaMap => _schemaMap;

        /// <summary>
        /// [Beta] Returns a new instance of the <see cref="AutoEncryptionOptions"/> class.
        /// </summary>
        /// <param name="keyVaultNamespace">The keyVault namespace.</param>
        /// <param name="kmsProviders">The kms providers.</param>
        /// <param name="bypassAutoEncryption">The bypass auto encryption flag.</param>
        /// <param name="extraOptions">The extra options.</param>
        /// <param name="keyVaultClient">The keyVault client.</param>
        /// <param name="schemaMap">The schema map.</param>
        /// <returns>A new instance of <see cref="AutoEncryptionOptions"/>.</returns>
        public AutoEncryptionOptions With(
            Optional<CollectionNamespace> keyVaultNamespace = default,
            Optional<IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>> kmsProviders = default,
            Optional<bool> bypassAutoEncryption = default,
            Optional<IReadOnlyDictionary<string, object>> extraOptions = default,
            Optional<IMongoClient> keyVaultClient = default,
            Optional<IReadOnlyDictionary<string, BsonDocument>> schemaMap = default)
        {
            return new AutoEncryptionOptions(
                keyVaultNamespace.WithDefault(_keyVaultNamespace),
                kmsProviders.WithDefault(_kmsProviders),
                bypassAutoEncryption.WithDefault(_bypassAutoEncryption),
                Optional.Create(extraOptions.WithDefault(_extraOptions)),
                Optional.Create(keyVaultClient.WithDefault(_keyVaultClient)),
                Optional.Create(schemaMap.WithDefault(_schemaMap)));
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null) || GetType() != obj.GetType()) { return false; }
            var rhs = (AutoEncryptionOptions)obj;

            return
                _bypassAutoEncryption.Equals(rhs._bypassAutoEncryption) &&
                ExtraOptionsEquals(_extraOptions, rhs._extraOptions) &&
                object.ReferenceEquals(_keyVaultClient, rhs._keyVaultClient) &&
                _keyVaultNamespace.Equals(rhs._keyVaultNamespace) &&
                KmsProvidersHelper.Equals(_kmsProviders, rhs._kmsProviders) &&
                _schemaMap.IsEquivalentTo(rhs._schemaMap, object.Equals);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_bypassAutoEncryption)
                .HashElements(_extraOptions)
                .Hash(_keyVaultClient)
                .Hash(_keyVaultNamespace)
                .HashElements(_kmsProviders)
                .HashElements(_schemaMap)
                .GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{ ");
            sb.AppendFormat("BypassAutoEncryption : {0}, ", _bypassAutoEncryption);
            sb.AppendFormat("KmsProviders : {0}, ", _kmsProviders.ToJson());
            if (_keyVaultNamespace != null)
            {
                sb.AppendFormat("KeyVaultNamespace : \"{0}\", ", _keyVaultNamespace.FullName);
            }
            if (_extraOptions != null)
            {
                sb.AppendFormat("ExtraOptions : {0}, ", _extraOptions.ToJson());
            }
            if (_schemaMap != null)
            {
                sb.AppendFormat("SchemaMap : {0}, ", _schemaMap.ToJson());
            }
            sb.Remove(sb.Length - 2, 2);
            sb.Append(" }");
            return sb.ToString();
        }

        // private methods
        private bool ExtraOptionsEquals(IReadOnlyDictionary<string, object> x, IReadOnlyDictionary<string, object> y)
        {
            return x.IsEquivalentTo(y, ExtraOptionEquals);
        }

        private bool ExtraOptionEquals(object x, object y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            if (x is IEnumerable<string> enumerableX)
            {
                var enumerableY = (IEnumerable<string>)y;
                return enumerableX.SequenceEqual(enumerableY);
            }
            else
            {
                return x.Equals(y);
            }
        }
    }
}
