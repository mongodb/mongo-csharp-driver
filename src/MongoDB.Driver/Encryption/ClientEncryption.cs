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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Libmongocrypt;

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
            _cryptClient = CryptClientCreator.CreateCryptClient(
                kmsProviders: clientEncryptionOptions.KmsProviders,
                schemaMap: null);
            _libMongoCryptController = new ExplicitEncryptionLibMongoCryptController(
                _cryptClient,
                clientEncryptionOptions);
        }

        // public methods
        /// <summary>
        /// Creates a data key.
        /// </summary>
        /// <param name="kmsProvider">The kms provider.</param>
        /// <param name="dataKeyOptions">The data key options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A data key.</returns>
        public Guid CreateDataKey(string kmsProvider, DataKeyOptions dataKeyOptions, CancellationToken cancellationToken)
        {
            return _libMongoCryptController.CreateDataKey(
                kmsProvider,
                dataKeyOptions.AlternateKeyNames,
                dataKeyOptions.MasterKey,
                cancellationToken);
        }

        /// <summary>
        /// Creates a data key.
        /// </summary>
        /// <param name="kmsProvider">The kms provider.</param>
        /// <param name="dataKeyOptions">The data key options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A data key.</returns>
        public Task<Guid> CreateDataKeyAsync(string kmsProvider, DataKeyOptions dataKeyOptions, CancellationToken cancellationToken)
        {
            return _libMongoCryptController.CreateDataKeyAsync(
                kmsProvider,
                dataKeyOptions.AlternateKeyNames,
                dataKeyOptions.MasterKey,
                cancellationToken);
        }

        /// <summary>
        /// Decrypts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The decrypted value.</returns>
        public BsonValue Decrypt(BsonBinaryData value, CancellationToken cancellationToken)
        {
            return _libMongoCryptController.DecryptField(value, cancellationToken);
        }

        /// <summary>
        /// Decrypts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The decrypted value.</returns>
        public Task<BsonValue> DecryptAsync(BsonBinaryData value, CancellationToken cancellationToken)
        {
            return _libMongoCryptController.DecryptFieldAsync(value, cancellationToken);
        }

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
        public BsonBinaryData Encrypt(BsonValue value, EncryptOptions encryptOptions, CancellationToken cancellationToken)
        {
            var algorithm = (EncryptionAlgorithm)Enum.Parse(typeof(EncryptionAlgorithm), encryptOptions.Algorithm);
            return _libMongoCryptController.EncryptField(
                value,
                encryptOptions.KeyId,
                encryptOptions.AlternateKeyName,
                algorithm,
                cancellationToken);
        }

        /// <summary>
        /// Encrypts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="encryptOptions">The encrypt options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The encrypted value.</returns>
        public Task<BsonBinaryData> EncryptAsync(BsonValue value, EncryptOptions encryptOptions, CancellationToken cancellationToken)
        {
            var algorithm = (EncryptionAlgorithm)Enum.Parse(typeof(EncryptionAlgorithm), encryptOptions.Algorithm);
            return _libMongoCryptController.EncryptFieldAsync(
                value,
                encryptOptions.KeyId,
                encryptOptions.AlternateKeyName,
                algorithm,
                cancellationToken);
        }
    }
}
