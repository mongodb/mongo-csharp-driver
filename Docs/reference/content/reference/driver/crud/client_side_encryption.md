
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

### Automatic client-side encryption

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

            Guid dataKeyId;
            using (var clientEncryption = new ClientEncryption(clientEncryptionSettings))
            {
                dataKeyId = clientEncryption.CreateDataKey("local", new DataKeyOptions(), CancellationToken.None);
            }

            var base64DataKeyId = Convert.ToBase64String(GuidConverter.ToBytes(dataKeyId, GuidRepresentation.Standard));
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

### Explicit Encryption and Decryption

Explicit encryption and decryption is a **MongoDB Community Server** feature and does not use the `mongocryptd` process. Explicit encryption is provided by the `ClientEncryption` class. The following example has been adapted from [`ExplicitEncryptionExamples.cs`](https://github.com/mongodb/mongo-csharp-driver/blob/master/tests/MongoDB.Driver.Examples/ExplicitEncryptionExamples.cs):

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using MongoDB.Driver.Encryption;
using MongoDB.Libmongocrypt;

namespace MongoDB.Driver.Examples
{
    public class ExplicitEncryptionExamples
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
            var keyVaultClient = new MongoClient("mongodb://localhost");
            var keyVaultDatabase = keyVaultClient.GetDatabase(keyVaultNamespace.DatabaseNamespace.DatabaseName);
            keyVaultDatabase.DropCollection(keyVaultNamespace.CollectionName);

            // Create the ClientEncryption instance
            var clientEncryptionSettings = new ClientEncryptionOptions(
                keyVaultClient,
                keyVaultNamespace,
                kmsProviders);
            using (var clientEncryption = new ClientEncryption(clientEncryptionSettings))
            {
                var dataKeyId = clientEncryption.CreateDataKey(
                    "local",
                    new DataKeyOptions(),
                    CancellationToken.None);

                var originalString = "123456789";
                Console.WriteLine($"Original string {originalString}.");

                // Explicitly encrypt a field
                var encryptOptions = new EncryptOptions(
                    EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic.ToString(),
                    keyId: dataKeyId);
                var encryptedFieldValue = clientEncryption.Encrypt(
                    originalString,
                    encryptOptions,
                    CancellationToken.None);
                Console.WriteLine($"Encrypted value {encryptedFieldValue}.");

                // Explicitly decrypt the field
                var decryptedValue = clientEncryption.Decrypt(encryptedFieldValue, CancellationToken.None);
                Console.WriteLine($"Decrypted value {decryptedValue}.");
            }
        }
    }
}
```

### Explicit Encryption and Auto Decryption

Although automatic encryption requires MongoDB 4.2 Enterprise Server or a MongoDB 4.2 Atlas cluster, automatic decryption is supported for all users. To configure automatic decryption without automatic encryption set `bypassAutoEncryption=true`. The following example has been adapted from [`ExplicitEncryptionExamples.cs`](https://github.com/mongodb/mongo-csharp-driver/blob/master/tests/MongoDB.Driver.Examples/ExplicitEncryptionExamples.cs):

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver.Encryption;
using MongoDB.Libmongocrypt;

namespace MongoDB.Driver.Examples
{
    public class ExplicitEncryptionAndAutoDecryptionExamples
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
            var collectionNamespace = CollectionNamespace.FromFullName("test.coll");
            var autoEncryptionOptions = new AutoEncryptionOptions(
                keyVaultNamespace,
                kmsProviders,
                bypassAutoEncryption: true);
            var clientSettings = MongoClientSettings.FromConnectionString("mongodb://localhost");
            clientSettings.AutoEncryptionOptions = autoEncryptionOptions;
            var mongoClient = new MongoClient(clientSettings);
            var database = mongoClient.GetDatabase(collectionNamespace.DatabaseNamespace.DatabaseName);
            database.DropCollection(collectionNamespace.CollectionName);
            var collection = database.GetCollection<BsonDocument>(collectionNamespace.CollectionName);

            var keyVaultClient = new MongoClient("mongodb://localhost");
            var keyVaultDatabase = keyVaultClient.GetDatabase(keyVaultNamespace.DatabaseNamespace.DatabaseName);
            keyVaultDatabase.DropCollection(keyVaultNamespace.CollectionName);

            // Create the ClientEncryption instance
            var clientEncryptionSettings = new ClientEncryptionOptions(
                keyVaultClient,
                keyVaultNamespace,
                kmsProviders);
            using (var clientEncryption = new ClientEncryption(clientEncryptionSettings))
            {
                var dataKeyId = clientEncryption.CreateDataKey(
                    "local",
                    new DataKeyOptions(),
                    CancellationToken.None);

                var originalString = "123456789";
                Console.WriteLine($"Original string {originalString}.");

                // Explicitly encrypt a field
                var encryptOptions = new EncryptOptions(
                    EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic.ToString(),
                    keyId: dataKeyId);
                var encryptedFieldValue = clientEncryption.Encrypt(
                    originalString,
                    encryptOptions,
                    CancellationToken.None);
                Console.WriteLine($"Encrypted value {encryptedFieldValue}.");

                collection.InsertOne(new BsonDocument("encryptedField", encryptedFieldValue));

                // Automatically decrypts the encrypted field.
                var decryptedValue = collection.Find(FilterDefinition<BsonDocument>.Empty).First();
                Console.WriteLine($"Decrypted document {decryptedValue.ToJson()}.");
            }
        }
    }
}
```