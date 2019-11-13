
+++
date = "2019-09-30T20:38:42-04:00"
title = "Client-Side Encryption"
[menu.main]
  parent = "Reference Reading and Writing"
  identifier = "Client-Side Field Level Encryption"
  weight = 40
  pre = "<i class='fa fa-lock'></i>"
+++

# Client-Side Field Level Encryption

New in MongoDB 4.2, client-side field level encryption allows administrators and
developers to encrypt specific data fields in addition to other MongoDB
encryption features.

With client-side field level encryption, developers can encrypt fields
client-side without any server-side configuration or directives. Client-side
field level encryption supports workloads where applications must guarantee that
unauthorized parties, including server administrators, cannot read the encrypted
data.

{{% note class="important" %}} 
Client-side field level encryption is supported only on Windows.
{{% /note %}}

## mongocryptd configuration

Client-side field level encryption requires the `mongocryptd` daemon / process
to be running. If `mongocryptd` isn't running, the driver will atempt to spawn
an instance, utilizing the `PATH` environment variable. Alternatively, the path
to `mongocryptd` can be specified by setting `mongocryptdSpawnPath` in
`extraOptions`. A specific daemon / process URI can also be configured in the
`AutoEncryptionSettings` class by setting `mongocryptdURI` in `extraOptions`.

More information about `mongocryptd` will soon be available from the official
documentation.


## Examples

The following is a sample app that assumes the **key** and **schema** have
already been created in MongoDB. The example uses a local key, however using AWS
Key Management Service is also an option. The data in the `encryptedField` field
is automatically encrypted on the insert and decrypted when using find on the
client-side. The following example has been adapted from
[`ClientSideEncryptionExamples.cs`](https://github.com/mongodb/mongo-csharp-driver/blob/master/tests/MongoDB.Driver.Examples/ClientEncryptionExamples.cs), which can be found on GitHub along with the driver source. 

```csharp
using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver.Examples
{
    public class ClientEncryptionExamples
    {
        private const string LocalMasterKey = "Mng0NCt4ZHVUYUJCa1kxNkVyNUR1QURhZ2h2UzR2d2RrZzh0cFBwM3R6NmdWMDFBMUN3YkQ5aXRRMkhGRGdQV09wOGVNYUMxT2k3NjZKelhaQmRCZGJkTXVyZG9uSjFk";

        public static void Main(string[] args)
        {
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
            Console.WriteLine(result.ToJson());
        }
    }
}
```

{{% note %}}
Auto encryption is an **enterprise** only feature.
{{% /note %}}

The following example shows how to configure the `AutoEncryptionSettings`
instance to create a new key and how to set the json schema map. The following
example has been adapted from
[`ClientSideEncryptionExamples.cs`](https://github.com/mongodb/mongo-csharp-driver/blob/master/tests/MongoDB.Driver.Examples/ClientEncryptionExamples.cs),
which can be found on Github along with the driver source.

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver.Examples
{
    public class ClientEncryptionExamples
    {
        private const string LocalMasterKey = "Mng0NCt4ZHVUYUJCa1kxNkVyNUR1QURhZ2h2UzR2d2RrZzh0cFBwM3R6NmdWMDFBMUN3YkQ5aXRRMkhGRGdQV09wOGVNYUMxT2k3NjZKelhaQmRCZGJkTXVyZG9uSjFk";

        public static void Main(string[] args)
        {
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
            Console.WriteLine(result.ToJson());
        }
    }
}
```

**Coming soon:** An example using the community version and demonstrating explicit encryption/decryption.
