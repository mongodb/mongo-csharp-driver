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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Encryption
{
    internal sealed class ExplicitEncryptionLibMongoCryptController : LibMongoCryptControllerBase
    {
        private const int BufferOverhead = 32; // fixed overhead added when serializing BsonDocument with BsonBinaryData Object

        // constructors
        public ExplicitEncryptionLibMongoCryptController(
            CryptClient cryptClient,
            ClientEncryptionOptions clientEncryptionOptions)
            : base(cryptClient,
                  Ensure.IsNotNull(Ensure.IsNotNull(clientEncryptionOptions, nameof(clientEncryptionOptions)).KeyVaultClient, nameof(clientEncryptionOptions.KeyVaultClient)),
                  clientEncryptionOptions.KeyVaultNamespace, clientEncryptionOptions.KmsProviders, clientEncryptionOptions.TlsOptions)
        {
        }

        // public methods
        public BsonDocument AddAlternateKeyName(Guid id, string alternateKeyName, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(alternateKeyName, nameof(alternateKeyName));

            try
            {
                var previousRecord = _keyVaultCollection.Value.FindOneAndUpdate(
                    CreateFilterById(id),
                    new UpdateDefinitionBuilder<BsonDocument>().AddToSet("keyAltNames", alternateKeyName),
                    new FindOneAndUpdateOptions<BsonDocument, BsonDocument>()
                    {
                        ReturnDocument = ReturnDocument.Before
                    },
                    cancellationToken);

                return previousRecord;
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<BsonDocument> AddAlternateKeyNameAsync(Guid id, string alternateKeyName, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(alternateKeyName, nameof(alternateKeyName));

            try
            {
                var previousRecord = await _keyVaultCollection.Value
                    .FindOneAndUpdateAsync(
                        CreateFilterById(id),
                        new UpdateDefinitionBuilder<BsonDocument>().AddToSet("keyAltNames", alternateKeyName),
                        new FindOneAndUpdateOptions<BsonDocument, BsonDocument>()
                        {
                            ReturnDocument = ReturnDocument.Before
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                return previousRecord;
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public Guid CreateDataKey(
            string kmsProvider,
            DataKeyOptions dataKeyOptions,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(kmsProvider, nameof(kmsProvider));

            try
            {
                var kmsKeyId = GetKmsKeyId(kmsProvider, dataKeyOptions);

                using (var context = _cryptClient.StartCreateDataKeyContext(kmsKeyId))
                {
                    var wrappedKeyBytes = ProcessStates(context, _keyVaultNamespace.DatabaseNamespace.DatabaseName, cancellationToken);

                    var wrappedKeyDocument = BsonSerializer.Deserialize<BsonDocument>(wrappedKeyBytes);
                    var keyId = UnwrapKeyId(wrappedKeyDocument);

                    _keyVaultCollection.Value.InsertOne(wrappedKeyDocument, cancellationToken: cancellationToken);

                    return keyId;
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<Guid> CreateDataKeyAsync(
            string kmsProvider,
            DataKeyOptions dataKeyOptions,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(kmsProvider, nameof(kmsProvider));

            try
            {
                var kmsKeyId = GetKmsKeyId(kmsProvider, dataKeyOptions);

                using (var context = _cryptClient.StartCreateDataKeyContext(kmsKeyId))
                {
                    var wrappedKeyBytes = await ProcessStatesAsync(context, _keyVaultNamespace.DatabaseNamespace.DatabaseName, cancellationToken).ConfigureAwait(false);

                    var wrappedKeyDocument = BsonSerializer.Deserialize<BsonDocument>(wrappedKeyBytes);
                    var keyId = UnwrapKeyId(wrappedKeyDocument);

                    await _keyVaultCollection.Value.InsertOneAsync(wrappedKeyDocument, cancellationToken: cancellationToken).ConfigureAwait(false);

                    return keyId;
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public BsonValue DecryptField(BsonBinaryData encryptedValue, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(encryptedValue, nameof(encryptedValue));

            try
            {
                var wrappedValueBytes = GetWrappedValueBytes(encryptedValue);

                using (var context = _cryptClient.StartExplicitDecryptionContext(wrappedValueBytes))
                {
                    var wrappedBytes = ProcessStates(context, databaseName: null, cancellationToken);
                    return UnwrapValue(wrappedBytes);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<BsonValue> DecryptFieldAsync(BsonBinaryData encryptedValue, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(encryptedValue, nameof(encryptedValue));

            try
            {
                var wrappedValueBytes = GetWrappedValueBytes(encryptedValue);

                using (var context = _cryptClient.StartExplicitDecryptionContext(wrappedValueBytes))
                {
                    var wrappedBytes = await ProcessStatesAsync(context, databaseName: null, cancellationToken).ConfigureAwait(false);
                    return UnwrapValue(wrappedBytes);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public DeleteResult DeleteKey(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var filter = CreateFilterById(id);
                return _keyVaultCollection.Value.DeleteOne(filter, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<DeleteResult> DeleteKeyAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var filter = CreateFilterById(id);
                return await _keyVaultCollection.Value.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public BsonValue EncryptField(
            BsonValue value,
            EncryptOptions encryptOptions,
            bool isExpressionMode,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(value, nameof(value));
            Ensure.IsNotNull(encryptOptions, nameof(encryptOptions));

            try
            {
                var wrappedValueBytes = GetWrappedValueBytes(value);

                var context = _cryptClient.StartExplicitEncryptionContext(
                    keyId: encryptOptions.KeyId.HasValue ? GuidConverter.ToBytes(encryptOptions.KeyId.Value, GuidRepresentation.Standard) : null,
                    keyAltName: GetWrappedAlternateKeyNameBytes(encryptOptions.AlternateKeyName),
                    queryType: encryptOptions.QueryType,
                    contentionFactor: encryptOptions.ContentionFactor,
                    encryptOptions.Algorithm,
                    wrappedValueBytes,
                    ToBsonIfNotNull(encryptOptions.RangeOptions?.CreateDocument()),
                    isExpressionMode);

                using (context)
                {
                    var wrappedBytes = ProcessStates(context, databaseName: null, cancellationToken);
                    return UnwrapValue(wrappedBytes);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<BsonValue> EncryptFieldAsync(
            BsonValue value,
            EncryptOptions encryptOptions,
            bool isExpressionMode,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(value, nameof(value));
            Ensure.IsNotNull(encryptOptions, nameof(encryptOptions));

            try
            {
                var wrappedValueBytes = GetWrappedValueBytes(value);

                var context = _cryptClient.StartExplicitEncryptionContext(
                    keyId: encryptOptions.KeyId.HasValue ? GuidConverter.ToBytes(encryptOptions.KeyId.Value, GuidRepresentation.Standard) : null,
                    keyAltName: GetWrappedAlternateKeyNameBytes(encryptOptions.AlternateKeyName),
                    queryType: encryptOptions.QueryType,
                    contentionFactor: encryptOptions.ContentionFactor,
                    encryptOptions.Algorithm,
                    wrappedValueBytes,
                    ToBsonIfNotNull(encryptOptions.RangeOptions?.CreateDocument()),
                    isExpressionMode);

                using (context)
                {
                    var wrappedBytes = await ProcessStatesAsync(context, databaseName: null, cancellationToken).ConfigureAwait(false);
                    return UnwrapValue(wrappedBytes);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public BsonDocument GetKey(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var filter = CreateFilterById(id);
                var cursor = _keyVaultCollection.Value.FindSync(filter, cancellationToken: cancellationToken);
                return cursor.FirstOrDefault(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<BsonDocument> GetKeyAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var filter = CreateFilterById(id);
                var cursor = await _keyVaultCollection.Value.FindAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
                return await cursor.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public BsonDocument GetKeyByAlternateKeyName(string alternateKeyName, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(alternateKeyName, nameof(alternateKeyName));

            try
            {
                var filter = CreateFilter(new BsonDocument("keyAltNames", alternateKeyName));
                var cursor = _keyVaultCollection.Value.FindSync(filter, cancellationToken: cancellationToken);
                // keyVault collection is supposed to have a unique index on keyAltName
                return cursor.FirstOrDefault(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<BsonDocument> GetKeyByAlternateKeyNameAsync(string alternateKeyName, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(alternateKeyName, nameof(alternateKeyName));

            try
            {
                var filter = CreateFilter(new BsonDocument("keyAltNames", alternateKeyName));
                var cursor = await _keyVaultCollection.Value.FindAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
                // keyVault collection is supposed to have a unique index on keyAltName
                return await cursor.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public IReadOnlyList<BsonDocument> GetKeys(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = _keyVaultCollection.Value.FindSync(FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
                return cursor.ToList(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<IReadOnlyList<BsonDocument>> GetKeysAsync(CancellationToken cancellationToken)
        {
            try
            {
                var cursor = await _keyVaultCollection.Value.FindAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken).ConfigureAwait(false);
                return await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public BsonDocument RemoveAlternateKeyName(Guid id, string alternateKeyName, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(alternateKeyName, nameof(alternateKeyName));

            try
            {
                var result = _keyVaultCollection.Value.FindOneAndUpdate(
                    CreateFilterById(id),
                    CreateRemoveAlternateKeyNameUpdatePipeline(alternateKeyName),
                    new FindOneAndUpdateOptions<BsonDocument, BsonDocument>
                    {
                        ReturnDocument = ReturnDocument.Before
                    },
                    cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<BsonDocument> RemoveAlternateKeyNameAsync(Guid id, string alternateKeyName, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(alternateKeyName, nameof(alternateKeyName));

            try
            {
                var filter = CreateFilterById(id);
                var updatePipeline = CreateRemoveAlternateKeyNameUpdatePipeline(alternateKeyName);
                var result = await _keyVaultCollection.Value
                    .FindOneAndUpdateAsync(
                        filter,
                        updatePipeline,
                        options: new FindOneAndUpdateOptions<BsonDocument, BsonDocument>
                        {
                            ReturnDocument = ReturnDocument.Before
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public RewrapManyDataKeyResult RewrapManyDataKey(FilterDefinition<BsonDocument> filter, RewrapManyDataKeyOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(filter, nameof(filter));

            try
            {
                var renderedFilter = RenderFilter(filter);
                var kmsKey = GetKmsKeyId(options?.Provider, new DataKeyOptions(masterKey: options?.MasterKey));
                using (var context = _cryptClient.StartRewrapMultipleDataKeysContext(kmsKey, ToBsonIfNotNull(renderedFilter)))
                {
                    var wrappedBytes = ProcessStates(context, databaseName: null, cancellationToken);
                    if (wrappedBytes == null)
                    {
                        return new RewrapManyDataKeyResult();
                    }

                    var bulkResult = _keyVaultCollection.Value.BulkWrite(requests: CreateRewrapManyDataKeysBulkUpdateRequests(wrappedBytes));

                    return new RewrapManyDataKeyResult(bulkResult);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<RewrapManyDataKeyResult> RewrapManyDataKeyAsync(FilterDefinition<BsonDocument> filter, RewrapManyDataKeyOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(filter, nameof(filter));

            try
            {
                var renderedFilter = RenderFilter(filter);
                var kmsKey = GetKmsKeyId(options?.Provider, new DataKeyOptions(masterKey: options?.MasterKey));
                using (var context = _cryptClient.StartRewrapMultipleDataKeysContext(kmsKey, ToBsonIfNotNull(renderedFilter)))
                {
                    var wrappedBytes = await ProcessStatesAsync(context, databaseName: null, cancellationToken).ConfigureAwait(false);
                    if (wrappedBytes == null)
                    {
                        return new RewrapManyDataKeyResult();
                    }

                    var bulkResult = await _keyVaultCollection.Value.BulkWriteAsync(requests: CreateRewrapManyDataKeysBulkUpdateRequests(wrappedBytes)).ConfigureAwait(false);

                    return new RewrapManyDataKeyResult(bulkResult);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        // private methods
        private static FilterDefinition<BsonDocument> CreateFilter(BsonDocument filter) => new BsonDocumentFilterDefinition<BsonDocument>(filter);
        private static FilterDefinition<BsonDocument> CreateFilterById(Guid id) => CreateFilterById(new BsonBinaryData(GuidConverter.ToBytes(id, GuidRepresentation.Standard), BsonBinarySubType.UuidStandard));
        private static FilterDefinition<BsonDocument> CreateFilterById(BsonBinaryData id) => CreateFilter(new BsonDocument("_id", id));
        private static UpdateDefinition<BsonDocument> CreateRemoveAlternateKeyNameUpdatePipeline(string keyAlterName) =>
            new EmptyPipelineDefinition<BsonDocument>()
                .AppendStage<BsonDocument, BsonDocument, BsonDocument>(
                    // Better to have this spec defined $set query in a raw string form for better visibility
                    BsonDocument.Parse(@$"
                    {{
                        ""$set"" :
                        {{
                            ""keyAltNames"" :
                            {{
                                ""$cond"":
                                [
                                    {{  ""$eq"": [ ""$keyAltNames"", [ ""{keyAlterName}"" ] ] }},
                                    ""$$REMOVE"",
                                    {{
                                        ""$filter"":
                                        {{
                                            ""input"": ""$keyAltNames"",
                                            ""cond"": {{ ""$ne"": [ ""$$this"", ""{keyAlterName}"" ] }}
                                        }}
                                    }}
                                ]
                            }}
                        }}
                    }}"));
#pragma warning disable CA1822
        private IEnumerable<UpdateOneModel<BsonDocument>> CreateRewrapManyDataKeysBulkUpdateRequests(byte[] rewrappedDocumentBytes) =>
            UnwrapValue(rewrappedDocumentBytes)
                .AsBsonArray
                .Cast<BsonDocument>()
                .Select(document =>
                    new UpdateOneModel<BsonDocument>(
                        filter: CreateFilterById(document["_id"].AsBsonBinaryData),
                        update: new UpdateDefinitionBuilder<BsonDocument>()
                            .CurrentDate("updateDate") // update date
                            .Set("keyMaterial", document["keyMaterial"]) // update new fields
                            .Set("masterKey", document["masterKey"])));
#pragma warning restore CA1822

        private KmsKeyId GetKmsKeyId(string kmsProvider, DataKeyOptions dataKeyOptions)
        {
            var wrappedAlternateKeyNamesBytes = dataKeyOptions?.AlternateKeyNames?.Select(GetWrappedAlternateKeyNameBytes);

            BsonDocument dataKeyDocument = null;
            if (kmsProvider != null)
            {
#pragma warning disable CA1308
                dataKeyDocument = new BsonDocument("provider", kmsProvider.ToLowerInvariant());
#pragma warning restore CA1308
                if (dataKeyOptions?.MasterKey != null)
                {
                    dataKeyDocument.AddRange(dataKeyOptions.MasterKey.Elements);
                }
            }

            BsonDocument keyMaterial = null;
            if (dataKeyOptions?.KeyMaterial != null)
            {
                keyMaterial = new BsonDocument("keyMaterial",  dataKeyOptions.KeyMaterial);
            }
            return new KmsKeyId(
                dataKeyOptionsBytes: ToBsonIfNotNull(dataKeyDocument),
                alternateKeyNameBytes: wrappedAlternateKeyNamesBytes,
                keyMaterialBytes: ToBsonIfNotNull(keyMaterial));
        }

        private byte[] GetWrappedAlternateKeyNameBytes(string value) => !string.IsNullOrWhiteSpace(value) ? ToBsonIfNotNull(new BsonDocument("keyAltName", value)) : null;

        private byte[] GetWrappedValueBytes(BsonValue value)
        {
            var estimatedSize = (value is BsonBinaryData binaryData) ? binaryData.Bytes.Length + BufferOverhead : 0;
            return ToBsonIfNotNull(new BsonDocument("v", value), estimatedSize);
        }

        private static BsonValue RenderFilter(FilterDefinition<BsonDocument> filter)
        {
            var registry = BsonSerializer.SerializerRegistry;
            var serializer = registry.GetSerializer<BsonDocument>();
            return filter.Render(new(serializer, registry));
        }

        private static Guid UnwrapKeyId(BsonDocument wrappedKeyDocument)
        {
            var keyId = wrappedKeyDocument["_id"].AsBsonBinaryData;
            if (keyId.SubType != BsonBinarySubType.UuidStandard)
            {
                throw new InvalidOperationException($"KeyId sub type must be UuidStandard, not: {keyId.SubType}.");
            }
            return GuidConverter.FromBytes(keyId.Bytes, GuidRepresentation.Standard);
        }

        private static BsonValue UnwrapValue(byte[] encryptedWrappedBytes)
        {
            var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(encryptedWrappedBytes);
            return bsonDocument["v"];
        }
    }
}
