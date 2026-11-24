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
using System.Collections.ObjectModel;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver.Configuration;

/// <summary>
/// Configures automatic client-side field level encryption for a <see cref="MongoClient"/>.
/// </summary>
public sealed class AutoEncryptionBuilder
{
    private readonly Dictionary<string, BsonDocument> _encryptedFieldsMap = new();
    private readonly Dictionary<string, IReadOnlyDictionary<string, object>> _kmsProviders = new();
    private readonly Dictionary<string, BsonDocument> _schemaMap = new();
    private readonly Dictionary<string, KmsTlsOptions> _kmsTlsOptions = new();

    internal AutoEncryptionBuilder()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether to bypass automatic encryption. Decryption still occurs.
    /// The default value is <c>false</c>.
    /// </summary>
    public bool BypassAutoEncryption
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to bypass query analysis, which removes the need for
    /// mongocryptd or the crypt shared library. The default value is <c>null</c>.
    /// </summary>
    public bool? BypassQueryAnalysis
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets options passed through to libmongocrypt, such as the mongocryptd spawn arguments.
    /// The dictionary is copied when set. The default value is <c>null</c>.
    /// </summary>
    public IReadOnlyDictionary<string, object> ExtraOptions
    {
        get;
        set => field = value == null ? null : new ReadOnlyDictionary<string, object>(value.ToDictionary(o => o.Key, o => o.Value));
    }

    /// <summary>
    /// Gets or sets how long a data key may be cached before it is fetched from the key vault again.
    /// The default value is <c>null</c>, which uses the libmongocrypt default of 60 seconds.
    /// </summary>
    public TimeSpan? KeyExpiration
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the client used to access the key vault. The default value is <c>null</c>, which
    /// uses the client being built.
    /// </summary>
    public IMongoClient KeyVaultClient
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the collection holding the data keys. Required.
    /// </summary>
    public CollectionNamespace KeyVaultNamespace
    {
        get;
        set => field = Ensure.IsNotNull(value, nameof(KeyVaultNamespace));
    }

    /// <summary>
    /// Gets or sets the connector used to open connections to KMS hosts. The default value is
    /// <c>null</c>, which connects directly.
    /// </summary>
    public IKmsConnector KmsConnector
    {
        get;
        set;
    }

    /// <summary>
    /// Registers a KMS provider.
    /// </summary>
    /// <param name="name">The provider name.</param>
    /// <param name="options">The provider options.</param>
    public AutoEncryptionBuilder RegisterKmsProvider(
        string name,
        IReadOnlyDictionary<string, object> options)
        => RegisterKmsProvider(name, options, tls: null);

    /// <summary>
    /// Registers a KMS provider and the TLS options used to reach it.
    /// </summary>
    /// <param name="name">The provider name</param>
    /// <param name="options">The provider options.</param>
    /// <param name="tls">The TLS options used to reach the provider, or <c>null</c> for the defaults.</param>
    public AutoEncryptionBuilder RegisterKmsProvider(
        string name,
        IReadOnlyDictionary<string, object> options,
        KmsTlsOptions tls)
    {
        Ensure.IsNotNullOrEmpty(name, nameof(name));
        Ensure.IsNotNull(options, nameof(options));

        if (_kmsProviders.ContainsKey(name))
        {
            throw new InvalidOperationException($"KMS provider \"{name}\" is already registered.");
        }

        var optionsCopy = new Dictionary<string, object>();
        foreach (var option in options)
        {
            var value = Ensure.IsNotNull(option.Value, $"{nameof(options)}[\"{option.Key}\"]");
            optionsCopy.Add(option.Key, value switch
            {
                string => value,
                byte[] bytes => bytes.Clone(),
                _ => throw new ArgumentException(
                    $"Invalid KMS provider option type: {value.GetType().Name}. Must be a string or a byte[].",
                    nameof(options))
            });
        }

        _kmsProviders.Add(name, new ReadOnlyDictionary<string, object>(optionsCopy));
        if (tls != null)
        {
            _kmsTlsOptions.Add(name, new KmsTlsOptions(tls));
        }

        return this;
    }

    /// <summary>
    /// Registers the encrypted fields of a Queryable Encryption collection. A collection cannot have both
    /// encrypted fields and a schema.
    /// </summary>
    /// <param name="collectionNamespace">The collection.</param>
    /// <param name="encryptedFields">The encryptedFields document.</param>
    public AutoEncryptionBuilder RegisterEncryptedFields(CollectionNamespace collectionNamespace, BsonDocument encryptedFields)
    {
        Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
        Ensure.IsNotNull(encryptedFields, nameof(encryptedFields));

        var key = collectionNamespace.FullName;
        if (_schemaMap.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"Collection \"{key}\" already has a schema registered; a collection cannot have both a schema and encrypted fields.");
        }
        if (_encryptedFieldsMap.ContainsKey(key))
        {
            throw new InvalidOperationException($"Collection \"{key}\" already has encrypted fields registered.");
        }

        _encryptedFieldsMap.Add(key, (BsonDocument)encryptedFields.DeepClone());
        return this;
    }

    /// <summary>
    /// Registers the JSON schema of a client-side field level encryption collection. A collection cannot
    /// have both a schema and encrypted fields.
    /// </summary>
    /// <param name="collectionNamespace">The collection.</param>
    /// <param name="schema">The JSON schema document.</param>
    public AutoEncryptionBuilder RegisterSchema(CollectionNamespace collectionNamespace, BsonDocument schema)
    {
        Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
        Ensure.IsNotNull(schema, nameof(schema));

        var key = collectionNamespace.FullName;
        if (_encryptedFieldsMap.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"Collection \"{key}\" already has encrypted fields registered; a collection cannot have both a schema and encrypted fields.");
        }
        if (_schemaMap.ContainsKey(key))
        {
            throw new InvalidOperationException($"Collection \"{key}\" already has a schema registered.");
        }

        _schemaMap.Add(key, (BsonDocument)schema.DeepClone());
        return this;
    }

    // Returns null when automatic encryption has not been configured. The outer dictionaries are copied so that
    // later registrations on this builder do not reach a client that was already built; the values are copies
    // owned by this builder (see the Register* methods) that are never mutated, so sharing them is safe.
    // The exception is the SslSettings, which are mutable and are therefore created anew on every call.
    internal AutoEncryptionOptions ToAutoEncryptionOptions()
    {
        var isConfigured =
            KeyVaultNamespace != null ||
            _kmsProviders.Count > 0 ||
            _schemaMap.Count > 0 ||
            _encryptedFieldsMap.Count > 0;
        if (!isConfigured)
        {
            return null;
        }

        if (KeyVaultNamespace == null)
        {
            throw new InvalidOperationException($"{nameof(KeyVaultNamespace)} must be set when automatic encryption is configured.");
        }
        if (_kmsProviders.Count == 0)
        {
            throw new InvalidOperationException("At least one KMS provider must be registered when automatic encryption is configured.");
        }

        var options = new AutoEncryptionOptions(
            KeyVaultNamespace,
            new Dictionary<string, IReadOnlyDictionary<string, object>>(_kmsProviders),
            bypassAutoEncryption: BypassAutoEncryption,
            extraOptions: Optional.Create(ExtraOptions),
            keyVaultClient: Optional.Create(KeyVaultClient),
            schemaMap: Optional.Create<IReadOnlyDictionary<string, BsonDocument>>(_schemaMap.Count == 0 ? null : new Dictionary<string, BsonDocument>(_schemaMap)),
            tlsOptions: _kmsTlsOptions.ToDictionary(o => o.Key, o => o.Value.ToSslSettings()),
            encryptedFieldsMap: Optional.Create<IReadOnlyDictionary<string, BsonDocument>>(_encryptedFieldsMap.Count == 0 ? null : new Dictionary<string, BsonDocument>(_encryptedFieldsMap)),
            bypassQueryAnalysis: BypassQueryAnalysis,
            kmsConnector: Optional.Create(KmsConnector));
        options.SetKeyExpiration(KeyExpiration);
        return options;
    }
}
