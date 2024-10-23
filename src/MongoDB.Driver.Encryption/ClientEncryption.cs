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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// Explicit client encryption.
    /// </summary>
    public sealed class ClientEncryption : IDisposable
    {
        // private fields
        private readonly CryptClient _cryptClient;
        private bool _disposed;
        private readonly ExplicitEncryptionLibMongoCryptController _libMongoCryptController;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientEncryption"/> class.
        /// </summary>
        /// <param name="clientEncryptionOptions">The client encryption options.</param>
        public ClientEncryption(ClientEncryptionOptions clientEncryptionOptions)
        {
            var cryptClientSettings = new CryptClientSettings(
                bypassQueryAnalysis: null,
                cryptSharedLibPath: null,
                cryptSharedLibSearchPath: null,
                encryptedFieldsMap: null,
                isCryptSharedLibRequired: null,
                kmsProviders: clientEncryptionOptions.KmsProviders,
                schemaMap: null);

            _cryptClient = CryptClientFactory.Create(cryptClientSettings);

            _libMongoCryptController = new ExplicitEncryptionLibMongoCryptController(
                _cryptClient,
                clientEncryptionOptions);
        }

        // public methods
        /// <summary>
        /// Adds an alternate key name to the keyAltNames array of the key document in the key vault collection with the given UUID (BSON binary subtype 0x04).
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns the previous version of the key document.</returns>
        public BsonDocument AddAlternateKeyName(Guid id, string alternateKeyName, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.AddAlternateKeyName(id, alternateKeyName, cancellationToken);

        /// <summary>
        /// Adds an alternate key name to the keyAltNames array of the key document in the key vault collection with the given UUID (BSON binary subtype 0x04).
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="alternateKeyName">The key alter name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns the previous version of the key document.</returns>
        public Task<BsonDocument> AddAlternateKeyNameAsync(Guid id, string alternateKeyName, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.AddAlternateKeyNameAsync(id, alternateKeyName, cancellationToken);

        /// <summary>
        /// Create encrypted collection.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="createCollectionOptions">The create collection options.</param>
        /// <param name="kmsProvider">The kms provider.</param>
        /// <param name="dataKeyOptions">The datakey options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The operation result.</returns>
        /// <remarks>
        /// If EncryptionFields contains a keyId with a null value, a data key will be automatically generated and returned in <see cref="CreateEncryptedCollectionResult.EncryptedFields"/>.
        /// </remarks>
        [Obsolete("Use the overload with masterKey instead.")]
        public CreateEncryptedCollectionResult CreateEncryptedCollection(IMongoDatabase database, string collectionName, CreateCollectionOptions createCollectionOptions, string kmsProvider, DataKeyOptions dataKeyOptions, CancellationToken cancellationToken = default)
        {
            Ensure.That(dataKeyOptions?.AlternateKeyNames == null && dataKeyOptions?.KeyMaterial == null, $"{nameof(CreateEncryptedCollection)} supports only {nameof(dataKeyOptions.MasterKey)} in {nameof(DataKeyOptions)}.");

            return CreateEncryptedCollection(database, collectionName, createCollectionOptions, kmsProvider, dataKeyOptions?.MasterKey, cancellationToken);
        }

        /// <summary>
        /// Create encrypted collection.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="createCollectionOptions">The create collection options.</param>
        /// <param name="kmsProvider">The kms provider.</param>
        /// <param name="masterKey">The master key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The operation result.</returns>
        /// <remarks>
        /// If EncryptionFields contains a keyId with a null value, a data key will be automatically generated and returned in <see cref="CreateEncryptedCollectionResult.EncryptedFields"/>.
        /// </remarks>
        public CreateEncryptedCollectionResult CreateEncryptedCollection(IMongoDatabase database, string collectionName, CreateCollectionOptions createCollectionOptions, string kmsProvider, BsonDocument masterKey, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(database, nameof(database));
            Ensure.IsNotNull(collectionName, nameof(collectionName));
            Ensure.IsNotNull(createCollectionOptions, nameof(createCollectionOptions));
            Ensure.IsNotNull(kmsProvider, nameof(kmsProvider));
            Feature.Csfle2QEv2.ThrowIfNotSupported(database.Client, cancellationToken);

            var encryptedFields = createCollectionOptions.EncryptedFields?.DeepClone()?.AsBsonDocument;
            try
            {
                foreach (var fieldDocument in IterateEmptyKeyIds(encryptedFields))
                {
                    var dataKey = CreateDataKey(kmsProvider, new DataKeyOptions(masterKey: masterKey), cancellationToken);
                    ModifyEncryptedFields(fieldDocument, dataKey);
                }

                var effectiveCreateEncryptionOptions = createCollectionOptions.Clone();
                effectiveCreateEncryptionOptions.EncryptedFields = encryptedFields;
                database.CreateCollection(collectionName, effectiveCreateEncryptionOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionCreateCollectionException(ex, encryptedFields);
            }

            return new CreateEncryptedCollectionResult(encryptedFields);
        }

        /// <summary>
        /// Create encrypted collection.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="createCollectionOptions">The create collection options.</param>
        /// <param name="kmsProvider">The kms provider.</param>
        /// <param name="dataKeyOptions">The datakey options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The operation result.</returns>
        /// <remarks>
        /// If EncryptionFields contains a keyId with a null value, a data key will be automatically generated and returned in <see cref="CreateEncryptedCollectionResult.EncryptedFields"/>.
        /// </remarks>
        [Obsolete("Use the overload with masterKey instead.")]
        public Task<CreateEncryptedCollectionResult> CreateEncryptedCollectionAsync(IMongoDatabase database, string collectionName, CreateCollectionOptions createCollectionOptions, string kmsProvider, DataKeyOptions dataKeyOptions, CancellationToken cancellationToken = default)
        {
            Ensure.That(dataKeyOptions?.AlternateKeyNames == null && dataKeyOptions?.KeyMaterial == null, $"{nameof(CreateEncryptedCollection)} supports only {nameof(dataKeyOptions.MasterKey)} in {nameof(DataKeyOptions)}.");

            return CreateEncryptedCollectionAsync(database, collectionName, createCollectionOptions, kmsProvider, dataKeyOptions?.MasterKey, cancellationToken);
        }

        /// <summary>
        /// Create encrypted collection.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="createCollectionOptions">The create collection options.</param>
        /// <param name="kmsProvider">The kms provider.</param>
        /// <param name="masterKey">The master key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The operation result.</returns>
        /// <remarks>
        /// If EncryptionFields contains a keyId with a null value, a data key will be automatically generated and returned in <see cref="CreateEncryptedCollectionResult.EncryptedFields"/>.
        /// </remarks>
        public async Task<CreateEncryptedCollectionResult> CreateEncryptedCollectionAsync(IMongoDatabase database, string collectionName, CreateCollectionOptions createCollectionOptions, string kmsProvider, BsonDocument masterKey, CancellationToken cancellationToken = default)
        {
            Ensure.IsNotNull(database, nameof(database));
            Ensure.IsNotNull(collectionName, nameof(collectionName));
            Ensure.IsNotNull(createCollectionOptions, nameof(createCollectionOptions));
            Ensure.IsNotNull(kmsProvider, nameof(kmsProvider));
            await Feature.Csfle2QEv2.ThrowIfNotSupportedAsync(database.Client, cancellationToken).ConfigureAwait(false);

            var encryptedFields = createCollectionOptions.EncryptedFields?.DeepClone()?.AsBsonDocument;
            try
            {
                foreach (var fieldDocument in IterateEmptyKeyIds(encryptedFields))
                {
                    var dataKey = await CreateDataKeyAsync(kmsProvider, new DataKeyOptions(masterKey: masterKey), cancellationToken).ConfigureAwait(false);
                    ModifyEncryptedFields(fieldDocument, dataKey);
                }

                var effectiveCreateEncryptionOptions = createCollectionOptions.Clone();
                effectiveCreateEncryptionOptions.EncryptedFields = encryptedFields;
                await database.CreateCollectionAsync(collectionName, effectiveCreateEncryptionOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionCreateCollectionException(ex, encryptedFields);
            }

            return new CreateEncryptedCollectionResult(encryptedFields);
        }

        /// <summary>
        /// An alias function equivalent to createKey.
        /// </summary>
        /// <param name="kmsProvider">The kms provider.</param>
        /// <param name="dataKeyOptions">The data key options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A data key.</returns>
        public Guid CreateDataKey(string kmsProvider, DataKeyOptions dataKeyOptions, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.CreateDataKey(
                kmsProvider,
                dataKeyOptions,
                cancellationToken);

        /// <summary>
        /// An alias function equivalent to createKey.
        /// </summary>
        /// <param name="kmsProvider">The kms provider.</param>
        /// <param name="dataKeyOptions">The data key options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A data key.</returns>
        public Task<Guid> CreateDataKeyAsync(string kmsProvider, DataKeyOptions dataKeyOptions, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.CreateDataKeyAsync(
                kmsProvider,
                dataKeyOptions,
                cancellationToken);

        /// <summary>
        /// Decrypts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The decrypted value.</returns>
        public BsonValue Decrypt(BsonBinaryData value, CancellationToken cancellationToken = default) => _libMongoCryptController.DecryptField(value, cancellationToken);

        /// <summary>
        /// Decrypts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The decrypted value.</returns>
        public Task<BsonValue> DecryptAsync(BsonBinaryData value, CancellationToken cancellationToken = default) => _libMongoCryptController.DecryptFieldAsync(value, cancellationToken);

        /// <summary>
        /// Removes the key document with the given UUID (BSON binary subtype 0x04) from the key vault collection.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns the result of the internal deleteOne() operation on the key vault collection.</returns>
        public DeleteResult DeleteKey(Guid id, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.DeleteKey(id, cancellationToken);

        /// <summary>
        /// Removes the key document with the given UUID (BSON binary subtype 0x04) from the key vault collection.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns the result of the internal deleteOne() operation on the key vault collection.</returns>
        public Task<DeleteResult> DeleteKeyAsync(Guid id, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.DeleteKeyAsync(id, cancellationToken);

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _cryptClient.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Encrypts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="encryptOptions">The encrypt options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The encrypted value.</returns>
        public BsonBinaryData Encrypt(BsonValue value, EncryptOptions encryptOptions, CancellationToken cancellationToken = default) =>
            EnsureEncryptedData<BsonBinaryData>(_libMongoCryptController.EncryptField(value, encryptOptions, isExpressionMode: false, cancellationToken));

        /// <summary>
        /// Encrypts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="encryptOptions">The encrypt options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The encrypted value.</returns>
        public async Task<BsonBinaryData> EncryptAsync(BsonValue value, EncryptOptions encryptOptions, CancellationToken cancellationToken = default) =>
            EnsureEncryptedData<BsonBinaryData>(await _libMongoCryptController.EncryptFieldAsync(value, encryptOptions, isExpressionMode: false, cancellationToken).ConfigureAwait(false));

        /// <summary>
        /// Encrypts a Match Expression or Aggregate Expression to query a range index.
        /// </summary>
        /// <param name="expression">The expression that is expected to be a BSON document of one of the following forms:
        /// 1. A Match Expression of this form:
        ///   {$and: [{"field": {$gt: "value1"}}, {"field": {$lt: "value2" }}]}
        /// 2. An Aggregate Expression of this form:
        ///   {$and: [{$gt: ["fieldpath", "value1"]}, {$lt: ["fieldpath", "value2"]}]
        /// $gt may also be $gte. $lt may also be $lte.
        /// </param>
        /// <param name="encryptOptions">The encryption options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The encrypted expression.</returns>
        /// <remarks>
        /// Only supported for queryType "range"
        /// The Range algorithm is experimental only. It is not intended for public use. It is subject to breaking changes.
        /// </remarks>
        public BsonDocument EncryptExpression(BsonDocument expression, EncryptOptions encryptOptions, CancellationToken cancellationToken = default) =>
            EnsureEncryptedData<BsonDocument>(_libMongoCryptController.EncryptField(expression, encryptOptions, isExpressionMode: true, cancellationToken));

        /// <summary>
        /// Encrypts a Match Expression or Aggregate Expression to query a range index.
        /// </summary>
        /// <param name="expression">The expression that is expected to be a BSON document of one of the following forms:
        /// 1. A Match Expression of this form:
        ///   {$and: [{"field": {$gt: "value1"}}, {"field": {$lt: "value2" }}]}
        /// 2. An Aggregate Expression of this form:
        ///   {$and: [{$gt: ["fieldpath", "value1"]}, {$lt: ["fieldpath", "value2"]}]
        /// $gt may also be $gte. $lt may also be $lte.
        /// </param>
        /// <param name="encryptOptions">The encryption options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>the encrypted expression.</returns>
        /// <remarks>
        /// Only supported for queryType "range"
        /// The Range algorithm is experimental only. It is not intended for public use. It is subject to breaking changes.
        /// </remarks>
        public async Task<BsonDocument> EncryptExpressionAsync(BsonDocument expression, EncryptOptions encryptOptions, CancellationToken cancellationToken = default) =>
            EnsureEncryptedData<BsonDocument>(await _libMongoCryptController.EncryptFieldAsync(expression, encryptOptions, isExpressionMode: true, cancellationToken).ConfigureAwait(false));

        /// <summary>
        /// Finds a single key document with the given UUID (BSON binary subtype 0x04).
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns the result of the internal find() operation on the key vault collection.</returns>
        public BsonDocument GetKey(Guid id, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.GetKey(id, cancellationToken);

        /// <summary>
        /// Finds a single key document with the given UUID (BSON binary subtype 0x04).
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns the result of the internal find() operation on the key vault collection.</returns>
        public Task<BsonDocument> GetKeyAsync(Guid id, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.GetKeyAsync(id, cancellationToken);

        /// <summary>
        /// Finds a single key document with the given alter name.
        /// </summary>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns a key document in the key vault collection with the given alternateKeyName.</returns>
        public BsonDocument GetKeyByAlternateKeyName(string alternateKeyName, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.GetKeyByAlternateKeyName(alternateKeyName, cancellationToken);

        /// <summary>
        /// Finds a single key document with the given UUID (BSON binary subtype 0x04).
        /// </summary>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns a key document in the key vault collection with the given alternateKeyName.</returns>
        public Task<BsonDocument> GetKeyByAlternateKeyNameAsync(string alternateKeyName, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.GetKeyByAlternateKeyNameAsync(alternateKeyName, cancellationToken);

        /// <summary>
        /// Finds all documents in the key vault collection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns the result of the internal find() operation on the key vault collection.</returns>
        public IReadOnlyList<BsonDocument> GetKeys(CancellationToken cancellationToken = default) =>
            _libMongoCryptController.GetKeys(cancellationToken);

        /// <summary>
        /// Finds all documents in the key vault collection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns the result of the internal find() operation on the key vault collection.</returns>
        public Task<IReadOnlyList<BsonDocument>> GetKeysAsync(CancellationToken cancellationToken = default) =>
            _libMongoCryptController.GetKeysAsync(cancellationToken);

        /// <summary>
        /// Removes an alternateKeyName from the keyAltNames array of the key document in the key vault collection with the given UUID (BSON binary subtype 0x04).
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns the previous version of the key document.</returns>
        public BsonDocument RemoveAlternateKeyName(Guid id, string alternateKeyName, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.RemoveAlternateKeyName(id, alternateKeyName, cancellationToken);

        /// <summary>
        /// Removes an alternateKeyName from the keyAltNames array of the key document in the key vault collection with the given UUID (BSON binary subtype 0x04).
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="alternateKeyName">The alternate key name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns the previous version of the key document.</returns>
        public Task<BsonDocument> RemoveAlternateKeyNameAsync(Guid id, string alternateKeyName, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.RemoveAlternateKeyNameAsync(id, alternateKeyName, cancellationToken);

        /// <summary>
        /// Decrypts multiple data keys and (re-)encrypts them with a new masterKey, or with their current masterKey if a new one is not given.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result.</returns>
        public RewrapManyDataKeyResult RewrapManyDataKey(FilterDefinition<BsonDocument> filter, RewrapManyDataKeyOptions options, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.RewrapManyDataKey(filter, options, cancellationToken);

        /// <summary>
        /// Decrypts multiple data keys and (re-)encrypts them with a new masterKey, or with their current masterKey if a new one is not given.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result.</returns>
        public Task<RewrapManyDataKeyResult> RewrapManyDataKeyAsync(FilterDefinition<BsonDocument> filter, RewrapManyDataKeyOptions options, CancellationToken cancellationToken = default) =>
            _libMongoCryptController.RewrapManyDataKeyAsync(filter, options, cancellationToken);

        // private methods
        private TEncryptedValue EnsureEncryptedData<TEncryptedValue>(BsonValue encryptedValue) where TEncryptedValue : BsonValue
        {
            if (encryptedValue is TEncryptedValue convertedValue)
            {
                return convertedValue;
            }
            else
            {
                // should not be reached
                throw new InvalidOperationException($"The encrypted data must be {typeof(TEncryptedValue).Name}, but was {encryptedValue?.GetType()?.Name ?? "null"}.");
            }
        }

        private static IEnumerable<BsonDocument> IterateEmptyKeyIds(BsonDocument encryptedFields)
        {
            if (encryptedFields == null)
            {
                throw new InvalidOperationException("There are no encrypted fields defined for the collection.");
            }

            if (encryptedFields.TryGetValue("fields", out var fields) && fields is BsonArray fieldsArray)
            {
                foreach (var field in fieldsArray.OfType<BsonDocument>()) // If `F` is not a document element, skip it.
                {
                    if (field.TryGetElement("keyId", out var keyId) && keyId.Value == BsonNull.Value)
                    {
                        yield return field;
                    }
                }
            }
        }

        private static void ModifyEncryptedFields(BsonDocument fieldDocument, Guid dataKey)
        {
            fieldDocument["keyId"] = new BsonBinaryData(dataKey, GuidRepresentation.Standard);
        }
    }
}
