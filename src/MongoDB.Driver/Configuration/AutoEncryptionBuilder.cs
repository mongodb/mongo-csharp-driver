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
/// <remarks>
/// <para>
/// Automatic encryption is enabled as soon as <see cref="KeyVaultNamespace"/> is set or a KMS provider, schema
/// or encrypted fields are registered. <see cref="KeyVaultNamespace"/> is then required, as is at least one KMS
/// provider, registered with one of the provider specific extension methods in
/// <see cref="MongoClientBuilderExtensions"/> (such as <c>RegisterAwsKmsProvider</c>) or with
/// <see cref="RegisterKmsProvider(string, IReadOnlyDictionary{string, object}, SslSettings)"/>. Both are
/// verified when the client is built.
/// </para>
/// <para>
/// Everything passed to this builder is copied when it is set or registered, so later changes to the
/// caller's objects have no effect, and clients that were already built are not affected by later changes
/// to this builder.
/// </para>
/// </remarks>
public sealed class AutoEncryptionBuilder
{
    private readonly Dictionary<string, BsonDocument> _encryptedFieldsMap = new();
    private readonly Dictionary<string, IReadOnlyDictionary<string, object>> _kmsProviders = new();
    private readonly Dictionary<string, BsonDocument> _schemaMap = new();
    private readonly Dictionary<string, SslSettings> _tlsOptions = new();

    internal AutoEncryptionBuilder()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether to bypass automatic encryption. Decryption still occurs.
    /// The default value is <c>false</c>.
    /// </summary>
    public bool BypassAutoEncryption { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to bypass query analysis, which removes the need for
    /// mongocryptd or the crypt shared library. The default value is <c>null</c>.
    /// </summary>
    public bool? BypassQueryAnalysis { get; set; }

    /// <summary>
    /// Gets or sets options passed through to libmongocrypt, such as the mongocryptd spawn arguments.
    /// The dictionary is copied when set. The default value is <c>null</c>.
    /// </summary>
    public IReadOnlyDictionary<string, object> ExtraOptions
    {
        get;
        // a shallow copy: a mutable value such as a mongocryptdSpawnArgs list is still shared with the caller
        set => field = value == null ? null : new ReadOnlyDictionary<string, object>(value.ToDictionary(o => o.Key, o => o.Value));
    }

    /// <summary>
    /// Gets or sets how long a data key may be cached before it is fetched from the key vault again.
    /// The default value is <c>null</c>, which uses the libmongocrypt default of 60 seconds.
    /// </summary>
    public TimeSpan? KeyExpiration { get; set; }

    /// <summary>
    /// Gets or sets the client used to access the key vault. The default value is <c>null</c>, which
    /// uses the client being built.
    /// </summary>
    public IMongoClient KeyVaultClient { get; set; }

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
    public IKmsConnector KmsConnector { get; set; }

    // TODO: reevaluate taking SslSettings here and on the per-provider KMS options (AwsKmsProviderOptions etc.).
    // Cluster TLS is flattened into TlsBuilder, so KMS TLS is the only remaining place SslSettings appears on
    // the new builder surface, which keeps the type alive.
    // Options: keep it (per-KMS-host TLS is a genuinely different concern from cluster TLS), or introduce
    // a small dedicated per-provider TLS type.

    // NOTE: do not merge these overloads into one method with optional parameters. Optional parameter
    // defaults are compiled into the caller's assembly, so adding a parameter later would break already
    // compiled callers, and adding an overload would make existing call sites ambiguous. Keeping every
    // parameter required leaves room to add one more overload without breaking anything.
    /// <summary>
    /// Registers a KMS provider.
    /// </summary>
    /// <param name="name">
    /// The provider name, either a bare provider such as <c>"aws"</c> or a named provider such as
    /// <c>"aws:name1"</c>. The name is opaque to the driver and is interpreted by libmongocrypt.
    /// </param>
    /// <param name="options">
    /// The provider options. Every value must be a <see cref="string"/> or a <c>byte[]</c>.
    /// An empty dictionary requests on-demand credentials for providers that support them.
    /// </param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public AutoEncryptionBuilder RegisterKmsProvider(
        string name,
        IReadOnlyDictionary<string, object> options)
        => RegisterKmsProvider(name, options, tlsSettings: null);

    /// <summary>
    /// Registers a KMS provider and the TLS settings used to reach it.
    /// </summary>
    /// <param name="name">
    /// The provider name, either a bare provider such as <c>"aws"</c> or a named provider such as
    /// <c>"aws:name1"</c>. The name is opaque to the driver and is interpreted by libmongocrypt.
    /// </param>
    /// <param name="options">
    /// The provider options. Every value must be a <see cref="string"/> or a <c>byte[]</c>.
    /// An empty dictionary requests on-demand credentials for providers that support them.
    /// </param>
    /// <param name="tlsSettings">The TLS settings used to reach the provider, or <c>null</c> for the defaults.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public AutoEncryptionBuilder RegisterKmsProvider(
        string name,
        IReadOnlyDictionary<string, object> options,
        SslSettings tlsSettings)
    {
        Ensure.IsNotNullOrEmpty(name, nameof(name));
        Ensure.IsNotNull(options, nameof(options));

        if (_kmsProviders.ContainsKey(name))
        {
            throw new InvalidOperationException($"KMS provider \"{name}\" is already registered.");
        }

        // copy before validating, so the caller cannot change the values after they have been checked
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

        var tlsSettingsCopy = tlsSettings?.Clone().Freeze();
        if (tlsSettingsCopy != null && tlsSettingsCopy.ServerCertificateValidationCallback != null)
        {
            throw new ArgumentException("Insecure TLS options prohibited.", nameof(tlsSettings));
        }

        _kmsProviders.Add(name, new ReadOnlyDictionary<string, object>(optionsCopy));
        if (tlsSettingsCopy != null)
        {
            _tlsOptions.Add(name, tlsSettingsCopy);
        }

        return this;
    }

    /// <summary>
    /// Registers the encrypted fields of a Queryable Encryption collection. A collection cannot have both
    /// encrypted fields and a schema.
    /// </summary>
    /// <param name="collectionNamespace">The collection.</param>
    /// <param name="encryptedFields">The encryptedFields document.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
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
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
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
            tlsOptions: new Dictionary<string, SslSettings>(_tlsOptions),
            encryptedFieldsMap: Optional.Create<IReadOnlyDictionary<string, BsonDocument>>(_encryptedFieldsMap.Count == 0 ? null : new Dictionary<string, BsonDocument>(_encryptedFieldsMap)),
            bypassQueryAnalysis: BypassQueryAnalysis,
            kmsConnector: Optional.Create(KmsConnector));
        options.SetKeyExpiration(KeyExpiration);
        return options;
    }
}
