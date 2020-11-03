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
        /// <param name="kmsProviders">The kms providers.</param>
        /// <param name="schemaMap">The schema map.</param>
        /// <returns>The CryptClient instance.</returns>
        public static CryptClient CreateCryptClient(
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders,
            IReadOnlyDictionary<string, BsonDocument> schemaMap)
        {
            var helper = new CryptClientCreator(kmsProviders, schemaMap);
            var cryptOptions = helper.CreateCryptOptions();
            return helper.CreateCryptClient(cryptOptions);
        }
#pragma warning restore
        #endregion

        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> _kmsProviders;
        private readonly IReadOnlyDictionary<string, BsonDocument> _schemaMap;

        private CryptClientCreator(
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders,
            IReadOnlyDictionary<string, BsonDocument> schemaMap)
        {
            _kmsProviders = Ensure.IsNotNull(kmsProviders, nameof(kmsProviders));
            _schemaMap = schemaMap;
        }

        private CryptClient CreateCryptClient(CryptOptions options)
        {
            return CryptClientFactory.Create(options);
        }

        private CryptOptions CreateCryptOptions()
        {
            List<KmsCredentials> kmsProviders = null;
            if (_kmsProviders != null && _kmsProviders.Count > 0)
            {
                kmsProviders = new List<KmsCredentials>();
                foreach (var kmsProvider in _kmsProviders)
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
            if (_schemaMap != null)
            {
                var schemaMapElements = _schemaMap.Select(c => new BsonElement(c.Key, c.Value));
                var schemaDocument = new BsonDocument(schemaMapElements);
#pragma warning disable 618
                var writerSettings = new BsonBinaryWriterSettings();
                if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
                {
                    writerSettings.GuidRepresentation = GuidRepresentation.Unspecified;
                }
#pragma warning restore 618
                schemaBytes = schemaDocument.ToBson(writerSettings: writerSettings);
            }

            return new CryptOptions(kmsProviders, schemaBytes);
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
    }
}
