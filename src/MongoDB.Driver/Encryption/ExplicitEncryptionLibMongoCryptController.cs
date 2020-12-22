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
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Misc;
using MongoDB.Libmongocrypt;

namespace MongoDB.Driver.Encryption
{
    internal sealed class ExplicitEncryptionLibMongoCryptController : LibMongoCryptControllerBase
    {
        // constructors
        public ExplicitEncryptionLibMongoCryptController(
            CryptClient cryptClient,
            ClientEncryptionOptions clientEncryptionOptions)
            : base(
                  Ensure.IsNotNull(cryptClient, nameof(cryptClient)),
                  Ensure.IsNotNull(Ensure.IsNotNull(clientEncryptionOptions, nameof(clientEncryptionOptions)).KeyVaultClient, nameof(clientEncryptionOptions.KeyVaultClient)),
                  Ensure.IsNotNull(Ensure.IsNotNull(clientEncryptionOptions, nameof(clientEncryptionOptions)).KeyVaultNamespace, nameof(clientEncryptionOptions.KeyVaultNamespace)))
        {
        }

        // public methods
        public Guid CreateDataKey(
            string kmsProvider,
            IReadOnlyList<string> alternateKeyNames,
            BsonDocument masterKey,
            CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfUnsupportedPlatform();

                var kmsKeyId = GetKmsKeyId(kmsProvider, alternateKeyNames, masterKey);

                using (var context = _cryptClient.StartCreateDataKeyContext(kmsKeyId))
                {
                    var wrappedKeyBytes = ProcessStates(context, _keyVaultNamespace.DatabaseNamespace.DatabaseName, cancellationToken);

                    var wrappedKeyDocument = new RawBsonDocument(wrappedKeyBytes);
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
            IReadOnlyList<string> alternateKeyNames,
            BsonDocument masterKey,
            CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfUnsupportedPlatform();

                var kmsKeyId = GetKmsKeyId(kmsProvider, alternateKeyNames, masterKey);

                using (var context = _cryptClient.StartCreateDataKeyContext(kmsKeyId))
                {
                    var wrappedKeyBytes = await ProcessStatesAsync(context, _keyVaultNamespace.DatabaseNamespace.DatabaseName, cancellationToken).ConfigureAwait(false);

                    var wrappedKeyDocument = new RawBsonDocument(wrappedKeyBytes);
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
            try
            {
                ThrowIfUnsupportedPlatform();

                var wrappedValueBytes = GetWrappedValueBytes(encryptedValue);

                using (var context = _cryptClient.StartExplicitDecryptionContext(wrappedValueBytes))
                {
                    var wrappedBytes = ProcessStates(context, databaseName: null, cancellationToken);
                    return UnwrapDecryptedValue(wrappedBytes);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<BsonValue> DecryptFieldAsync(BsonBinaryData wrappedBinaryValue, CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfUnsupportedPlatform();

                var wrappedValueBytes = GetWrappedValueBytes(wrappedBinaryValue);

                using (var context = _cryptClient.StartExplicitDecryptionContext(wrappedValueBytes))
                {
                    var wrappedBytes = await ProcessStatesAsync(context, databaseName: null, cancellationToken).ConfigureAwait(false);
                    return UnwrapDecryptedValue(wrappedBytes);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public BsonBinaryData EncryptField(
            BsonValue value,
            Guid? keyId,
            string alternateKeyName,
            EncryptionAlgorithm encryptionAlgorithm,
            CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfUnsupportedPlatform();

                var wrappedValueBytes = GetWrappedValueBytes(value);

                CryptContext context;
                if (keyId.HasValue && alternateKeyName != null)
                {
                    throw new ArgumentException("keyId and alternateKeyName cannot both be provided.");
                }
                else if (keyId.HasValue)
                {
                    var keyBytes = GuidConverter.ToBytes(keyId.Value, GuidRepresentation.Standard);
                    context = _cryptClient.StartExplicitEncryptionContextWithKeyId(keyBytes, encryptionAlgorithm, wrappedValueBytes);
                }
                else if (alternateKeyName != null)
                {
                    var wrappedAlternateKeyNameBytes = GetWrappedAlternateKeyNameBytes(alternateKeyName);
                    context = _cryptClient.StartExplicitEncryptionContextWithKeyAltName(wrappedAlternateKeyNameBytes, encryptionAlgorithm, wrappedValueBytes);
                }
                else
                {
                    throw new ArgumentException("Either keyId or alternateKeyName must be provided.");
                }

                using (context)
                {
                    var wrappedBytes = ProcessStates(context, databaseName: null, cancellationToken);
                    return UnwrapEncryptedValue(wrappedBytes);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        public async Task<BsonBinaryData> EncryptFieldAsync(
            BsonValue value,
            Guid? keyId,
            string alternateKeyName,
            EncryptionAlgorithm encryptionAlgorithm,
            CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfUnsupportedPlatform();

                var wrappedValueBytes = GetWrappedValueBytes(value);

                CryptContext context;
                if (keyId.HasValue && alternateKeyName != null)
                {
                    throw new ArgumentException("keyId and alternateKeyName cannot both be provided.");
                }
                else if (keyId.HasValue)
                {
                    var bytes = GuidConverter.ToBytes(keyId.Value, GuidRepresentation.Standard);
                    context = _cryptClient.StartExplicitEncryptionContextWithKeyId(bytes, encryptionAlgorithm, wrappedValueBytes);
                }
                else if (alternateKeyName != null)
                {
                    var wrappedAlternateKeyNameBytes = GetWrappedAlternateKeyNameBytes(alternateKeyName);
                    context = _cryptClient.StartExplicitEncryptionContextWithKeyAltName(wrappedAlternateKeyNameBytes, encryptionAlgorithm, wrappedValueBytes);
                }
                else
                {
                    throw new ArgumentException("Either keyId or alternateKeyName must be provided.");
                }

                using (context)
                {
                    var wrappedBytes = await ProcessStatesAsync(context, databaseName: null, cancellationToken).ConfigureAwait(false);
                    return UnwrapEncryptedValue(wrappedBytes);
                }
            }
            catch (Exception ex)
            {
                throw new MongoEncryptionException(ex);
            }
        }

        // private methods
        private KmsKeyId GetKmsKeyId(string kmsProvider, IReadOnlyList<string> alternateKeyNames, BsonDocument masterKey)
        {
            IEnumerable<byte[]> wrappedAlternateKeyNamesBytes = null;
            if (alternateKeyNames != null)
            {
                wrappedAlternateKeyNamesBytes = alternateKeyNames.Select(GetWrappedAlternateKeyNameBytes);
            }

            var dataKeyDocument = new BsonDocument("provider", kmsProvider.ToLower());
            if (masterKey != null)
            {
                dataKeyDocument.AddRange(masterKey.Elements);
            }
            return new KmsKeyId(dataKeyDocument.ToBson(), wrappedAlternateKeyNamesBytes);
        }

        private byte[] GetWrappedAlternateKeyNameBytes(string value)
        {
            return
               !string.IsNullOrWhiteSpace(value)
                   ? new BsonDocument("keyAltName", value).ToBson()
                   : null;
        }

        private byte[] GetWrappedValueBytes(BsonValue value)
        {
            var wrappedValue = new BsonDocument("v", value);
            var writerSettings = BsonBinaryWriterSettings.Defaults.Clone();
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                writerSettings.GuidRepresentation = GuidRepresentation.Unspecified;
            }
#pragma warning restore 618
            return wrappedValue.ToBson(writerSettings: writerSettings);
        }

        private BsonValue UnwrapDecryptedValue(byte[] wrappedBytes)
        {
            var wrappedDocument = new RawBsonDocument(wrappedBytes);
            return wrappedDocument["v"];
        }

        private BsonBinaryData UnwrapEncryptedValue(byte[] encryptedWrappedBytes)
        {
            var wrappedDocument = new RawBsonDocument(encryptedWrappedBytes);
            return wrappedDocument["v"].AsBsonBinaryData;
        }

        private Guid UnwrapKeyId(RawBsonDocument wrappedKeyDocument)
        {
            var keyId = wrappedKeyDocument["_id"].AsBsonBinaryData;
            if (keyId.SubType != BsonBinarySubType.UuidStandard)
            {
                throw new InvalidOperationException($"KeyId sub type must be UuidStandard, not: {keyId.SubType}.");
            }
            return GuidConverter.FromBytes(keyId.Bytes, GuidRepresentation.Standard);
        }
    }
}
