/*
 * Copyright 2019–present MongoDB, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Configuration;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// A factory for CryptClients.
    /// </summary>
    internal static class CryptClientFactory
    {
        // MUST be static fields since otherwise these callbacks can be collected via the garbage collector
        // regardless they're used by mongocrypt level or no
        private static Library.Delegates.CryptoCallback __cryptoAes256EcbEncryptCallback = new Library.Delegates.CryptoCallback(CipherCallbacks.EncryptEcb);
        private static Library.Delegates.CryptoCallback __cryptoAes256CbcDecryptCallback = new Library.Delegates.CryptoCallback(CipherCallbacks.DecryptCbc);
        private static Library.Delegates.CryptoCallback __cryptoAes256CbcEncryptCallback = new Library.Delegates.CryptoCallback(CipherCallbacks.EncryptCbc);
        private static Library.Delegates.HashCallback __cryptoHashCallback = new Library.Delegates.HashCallback(HashCallback.Hash);
        private static Library.Delegates.CryptoHmacCallback __cryptoHmacSha256Callback = new Library.Delegates.CryptoHmacCallback(HmacShaCallbacks.HmacSha256);
        private static Library.Delegates.CryptoHmacCallback __cryptoHmacSha512Callback = new Library.Delegates.CryptoHmacCallback(HmacShaCallbacks.HmacSha512);
        private static Library.Delegates.RandomCallback __randomCallback = new Library.Delegates.RandomCallback(SecureRandomCallback.GenerateRandom);
        private static Library.Delegates.CryptoHmacCallback __signRsaesPkcs1HmacCallback = new Library.Delegates.CryptoHmacCallback(SigningRSAESPKCSCallback.RsaSign);

        // mongocrypt_is_crypto_available is only available in libmongocrypt version >= 1.9
        private static readonly Version __mongocryptIsCryptoAvailableMinVersion = Version.Parse("1.9");

        public static CryptClient Create(CryptClientSettings cryptClientSettings)
        {
            List<KmsCredentials> kmsProviders = null;
            if (cryptClientSettings.KmsProviders?.Count > 0)
            {
                kmsProviders = new List<KmsCredentials>();
                foreach (var kmsProvider in cryptClientSettings.KmsProviders)
                {
#pragma warning disable CA1304
                    var kmsTypeDocumentKey = kmsProvider.Key.ToLower();
#pragma warning restore CA1304
                    var kmsProviderDocument = CreateProviderDocument(kmsTypeDocumentKey, kmsProvider.Value);
                    var kmsCredentials = new KmsCredentials(credentialsBytes: kmsProviderDocument.ToBson());
                    kmsProviders.Add(kmsCredentials);
                }
            }
            else
            {
                throw new ArgumentException("At least one kms provider must be specified");
            }

            byte[] schemaBytes = null;
            if (cryptClientSettings.SchemaMap != null)
            {
                schemaBytes = GetBytesFromMap(cryptClientSettings.SchemaMap);
            }

            byte[] encryptedFieldsBytes = null;
            if (cryptClientSettings.EncryptedFieldsMap != null)
            {
                encryptedFieldsBytes = GetBytesFromMap(cryptClientSettings.EncryptedFieldsMap);
            }

            var cryptOptions =  new CryptOptions(
                kmsProviders,
                encryptedFieldsMap: encryptedFieldsBytes,
                schema: schemaBytes,
                bypassQueryAnalysis: cryptClientSettings.BypassQueryAnalysis.GetValueOrDefault(false),
                cryptClientSettings.CryptSharedLibPath,
                cryptClientSettings.CryptSharedLibSearchPath,
                cryptClientSettings.IsCryptSharedLibRequired ?? false);

            return Create(cryptOptions);
        }

        /// <summary>Creates a CryptClient with the specified options.</summary>
        /// <param name="options">The options.</param>
        /// <returns>A CryptClient</returns>
        public static CryptClient Create(CryptOptions options)
        {
            MongoCryptSafeHandle handle = null;
            Status status = null;

            var cryptoAvailable = Version.Parse(Library.Version.Split('-', '+').First()) >= __mongocryptIsCryptoAvailableMinVersion && Library.mongocrypt_is_crypto_available();

            try
            {
                handle = Library.mongocrypt_new();
                status = new Status();

                if (!cryptoAvailable)
                {
                    handle.Check(
                        status,
                        Library.mongocrypt_setopt_crypto_hooks(
                            handle,
                            __cryptoAes256CbcEncryptCallback,
                            __cryptoAes256CbcDecryptCallback,
                            __randomCallback,
                            __cryptoHmacSha512Callback,
                            __cryptoHmacSha256Callback,
                            __cryptoHashCallback,
                            IntPtr.Zero));

                    handle.Check(
                        status,
                        Library.mongocrypt_setopt_crypto_hook_sign_rsaes_pkcs1_v1_5(
                            handle,
                            __signRsaesPkcs1HmacCallback,
                            IntPtr.Zero));

                    handle.Check(
                        status,
                        Library.mongocrypt_setopt_aes_256_ecb(
                            handle,
                            __cryptoAes256EcbEncryptCallback,
                            IntPtr.Zero));
                }

                foreach (var kmsCredentials in options.KmsCredentials)
                {
                    kmsCredentials.SetCredentials(handle, status);
                }

                if (options.Schema != null)
                {
                    PinnedBinary.RunAsPinnedBinary(handle, options.Schema, status, (h, pb) => Library.mongocrypt_setopt_schema_map(h, pb));
                }

                if (options.EncryptedFieldsMap != null)
                {
                    PinnedBinary.RunAsPinnedBinary(handle, options.EncryptedFieldsMap, status, (h, pb) => Library.mongocrypt_setopt_encrypted_field_config_map(h, pb));
                }

                if (options.BypassQueryAnalysis)
                {
                    Library.mongocrypt_setopt_bypass_query_analysis(handle);
                }

                if (options.CryptSharedLibPath != null)
                {
                    Library.mongocrypt_setopt_set_crypt_shared_lib_path_override(handle, options.CryptSharedLibPath);
                }

                if (options.CryptSharedLibSearchPath != null)
                {
                    Library.mongocrypt_setopt_append_crypt_shared_lib_search_path(handle, options.CryptSharedLibSearchPath);
                }

                Library.mongocrypt_setopt_use_need_kms_credentials_state(handle);

                Library.mongocrypt_init(handle);

                if (options.IsCryptSharedLibRequired)
                {
                    var versionPtr = Library.mongocrypt_crypt_shared_lib_version_string(handle, out _);
                    if (versionPtr == IntPtr.Zero)
                    {
                        throw new CryptException(Library.StatusType.MONGOCRYPT_STATUS_ERROR_CLIENT, uint.MaxValue, "CryptSharedLib is required, but was not found or not loaded.");
                    }
                }
            }
            catch
            {
                handle?.Dispose();
                status?.Dispose();
                throw;
            }

            return new CryptClient(handle, status);
        }

        private static BsonDocument CreateProviderDocument(string kmsType, IReadOnlyDictionary<string, object> data)
        {
            var providerContent = new BsonDocument();
            foreach (var record in data)
            {
                providerContent.Add(new BsonElement(record.Key, BsonValue.Create(record.Value)));
            }
            var providerDocument = new BsonDocument(kmsType, providerContent);
            return providerDocument;
        }

        private static byte[] GetBytesFromMap(IReadOnlyDictionary<string, BsonDocument> map)
        {
            var mapElements = map.Select(c => new BsonElement(c.Key, c.Value));
            var mapDocument = new BsonDocument(mapElements);
            var writerSettings = new BsonBinaryWriterSettings();
            return mapDocument.ToBson(writerSettings: writerSettings);
        }
    }
}
