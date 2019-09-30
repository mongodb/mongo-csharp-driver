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

using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using System;
using System.Collections.Generic;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver.Encryption;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Examples
{
    public class ClientEncryptionExamples
    {
        private const string LocalMasterKey = "Mng0NCt4ZHVUYUJCa1kxNkVyNUR1QURhZ2h2UzR2d2RrZzh0cFBwM3R6NmdWMDFBMUN3YkQ5aXRRMkhGRGdQV09wOGVNYUMxT2k3NjZKelhaQmRCZGJkTXVyZG9uSjFk";

        private readonly ITestOutputHelper _output;

        public ClientEncryptionExamples(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ClientSideEncryptionSimpleTour()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var localMasterKey = Convert.FromBase64String(LocalMasterKey);

            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();
            var localKey = new Dictionary<string, object>
            {
                { "key", localMasterKey }
            };
            kmsProviders.Add("local", localKey);

            var keyVaultNamespace = CollectionNamespace.FromFullName("admin.datakeys");
            var autoEncryptionOptions = new AutoEncryptionOptions(keyVaultNamespace, kmsProviders);

            var mongoClientSettings = new MongoClientSettings
            {
                AutoEncryptionOptions = autoEncryptionOptions
            };
            var client = new MongoClient(mongoClientSettings);
            var database = client.GetDatabase("test");
            database.DropCollection("coll");
            var collection = database.GetCollection<BsonDocument>("coll");

            collection.InsertOne(new BsonDocument("encryptedField", "123456789"));

            var result = collection.Find(FilterDefinition<BsonDocument>.Empty).First();
            _output.WriteLine(result.ToJson());
        }

        [Fact]
        public void ClientSideEncryptionAutoEncryptionSettingsTour()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var localMasterKey = Convert.FromBase64String(LocalMasterKey);

            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();
            var localKey = new Dictionary<string, object>
            {
                { "key", localMasterKey }
            };
            kmsProviders.Add("local", localKey);

            var keyVaultNamespace = CollectionNamespace.FromFullName("admin.datakeys");
            var keyVaultMongoClient = new MongoClient();
            var clientEncryptionSettings = new ClientEncryptionOptions(
                keyVaultMongoClient,
                keyVaultNamespace,
                kmsProviders);

            var clientEncryption = new ClientEncryption(clientEncryptionSettings);
            var dataKeyId = clientEncryption.CreateDataKey("local", new DataKeyOptions(), CancellationToken.None);
            var base64DataKeyId = Convert.ToBase64String(GuidConverter.ToBytes(dataKeyId, GuidRepresentation.Standard));
            clientEncryption.Dispose();

            var collectionNamespace = CollectionNamespace.FromFullName("test.coll");

            var schemaMap = $@"{{
                properties: {{
                    encryptedField: {{
                        encrypt: {{
                            keyId: [{{
                                '$binary' : {{
                                    'base64' : '{base64DataKeyId}',
                                    'subType' : '04'
                                }}
                            }}],
                        bsonType: 'string',
                        algorithm: 'AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic'
                        }}
                    }}
                }},
                'bsonType': 'object'
            }}";
            var autoEncryptionSettings = new AutoEncryptionOptions(
                keyVaultNamespace,
                kmsProviders,
                schemaMap: new Dictionary<string, BsonDocument>()
                {
                    { collectionNamespace.ToString(), BsonDocument.Parse(schemaMap) }
                });
            var clientSettings = new MongoClientSettings
            {
                AutoEncryptionOptions = autoEncryptionSettings
            };
            var client = new MongoClient(clientSettings);
            var database = client.GetDatabase("test");
            database.DropCollection("coll");
            var collection = database.GetCollection<BsonDocument>("coll");

            collection.InsertOne(new BsonDocument("encryptedField", "123456789"));

            var result = collection.Find(FilterDefinition<BsonDocument>.Empty).First();
            _output.WriteLine(result.ToJson());
        }
    }
}
