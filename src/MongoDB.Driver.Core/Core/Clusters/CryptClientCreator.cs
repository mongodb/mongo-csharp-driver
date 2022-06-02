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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Libmongocrypt;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents a creator for CryptClient.
    /// </summary>
    public sealed class CryptClientCreator
    {
        #region static
#pragma warning disable 3002
        /// <summary>
        /// Create a CryptClient instance.
        /// </summary>
        /// <param name="cryptClientSettings">Crypt client settings.</param>
        /// <returns>The CryptClient instance.</returns>
        public static CryptClient CreateCryptClient(CryptClientSettings cryptClientSettings)
        {
            var helper = new CryptClientCreator(cryptClientSettings);
            var cryptOptions = helper.CreateCryptOptions();
            return helper.CreateCryptClient(cryptOptions);
        }
#pragma warning restore
        #endregion

        private readonly CryptClientSettings _cryptClientSettings;

        private CryptClientCreator(CryptClientSettings cryptClientSettings)
        {
            _cryptClientSettings = Ensure.IsNotNull(cryptClientSettings, nameof(cryptClientSettings));
        }

        private CryptClient CreateCryptClient(CryptOptions options) =>
            CryptClientFactory.Create(options);

        private CryptOptions CreateCryptOptions()
        {
            List<KmsCredentials> kmsProviders = null;
            if (_cryptClientSettings.KmsProviders?.Count > 0)
            {
                kmsProviders = new List<KmsCredentials>();
                foreach (var kmsProvider in _cryptClientSettings.KmsProviders)
                {
                    var kmsTypeDocumentKey = kmsProvider.Key.ToLower();
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
            if (_cryptClientSettings.SchemaMap != null)
            {
                schemaBytes = GetBytesFromMap(_cryptClientSettings.SchemaMap);
            }

            byte[] encryptedFieldsBytes = null;
            if (_cryptClientSettings.EncryptedFieldsMap != null)
            {
                encryptedFieldsBytes = GetBytesFromMap(_cryptClientSettings.EncryptedFieldsMap);
            }

            return new CryptOptions(
                kmsProviders,
                encryptedFieldsMap: encryptedFieldsBytes,
                schema: schemaBytes,
                bypassQueryAnalysis: _cryptClientSettings.BypassQueryAnalysis.GetValueOrDefault(false),
                _cryptClientSettings.CryptSharedLibPath,
                _cryptClientSettings.CryptSharedLibSearchPath,
                _cryptClientSettings.IsCryptSharedLibRequired ?? false);
        }

        private BsonDocument CreateProviderDocument(string kmsType, IReadOnlyDictionary<string, object> data)
        {
            var providerContent = new BsonDocument();
            foreach (var record in data)
            {
                providerContent.Add(new BsonElement(record.Key, BsonValue.Create(record.Value)));
            }
            var providerDocument = new BsonDocument(kmsType, providerContent);
            return providerDocument;
        }

        private byte[] GetBytesFromMap(IReadOnlyDictionary<string, BsonDocument> map)
        {
            var mapElements = map.Select(c => new BsonElement(c.Key, c.Value));
            var mapDocument = new BsonDocument(mapElements);
#pragma warning disable 618
            var writerSettings = new BsonBinaryWriterSettings();
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                writerSettings.GuidRepresentation = GuidRepresentation.Unspecified;
            }
#pragma warning restore 618
            return mapDocument.ToBson(writerSettings: writerSettings);
        }
    }
}
