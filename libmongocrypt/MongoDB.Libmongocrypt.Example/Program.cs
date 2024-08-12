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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Libmongocrypt;
using MongoDB.Driver;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace drivertest
{
    class BsonUtil
    {
        public static BsonDocument ToDocument(Binary bin)
        {
            MemoryStream stream = new MemoryStream(bin.ToArray());
            using (var jsonReader = new BsonBinaryReader(stream))
            {
                var context = BsonDeserializationContext.CreateRoot(jsonReader);
                return BsonDocumentSerializer.Instance.Deserialize(context);
            }
        }

        public static byte[] ToBytes(BsonDocument doc)
        {
            BsonBinaryWriterSettings settings = new BsonBinaryWriterSettings()
            {
                // C# driver "magically" changes UUIDs underneath by default so tell it not to
                GuidRepresentation = GuidRepresentation.Standard
            };
            return doc.ToBson(null, settings);
        }

        public static BsonDocument Concat(BsonDocument doc1, BsonDocument doc2)
        {
            BsonDocument dest = new BsonDocument();
            BsonDocumentWriter writer = new BsonDocumentWriter(dest);
            var context = BsonSerializationContext.CreateRoot(writer);

            writer.WriteStartDocument();

            foreach (var field in doc1)
            {
                writer.WriteName(field.Name);
                BsonValueSerializer.Instance.Serialize(context, field.Value);
            }

            foreach (var field in doc2)
            {
                writer.WriteName(field.Name);
                BsonValueSerializer.Instance.Serialize(context, field.Value);
            }

            writer.WriteEndDocument();
            return writer.Document;
        }


        public static BsonDocument FromJSON(string str)
        {
            using (var jsonReader = new JsonReader(str))
            {
                var context = BsonDeserializationContext.CreateRoot(jsonReader);
                return BsonDocumentSerializer.Instance.Deserialize(context);
            }
        }
    }

    class MongoCryptDController
    {
        MongoClient _clientCryptD;
        IMongoCollection<BsonDocument> _keyVault;
        Uri _kmsEndpoint;

        public MongoCryptDController(MongoUrl urlCryptD, IMongoCollection<BsonDocument> keyVault, Uri kmsEndpoint)
        {
            _clientCryptD = new MongoClient(urlCryptD);
            _keyVault = keyVault;
            _kmsEndpoint = kmsEndpoint;
        }

        public Guid GenerateKey(KmsCredentials credentials, KmsKeyId kmsKeyId)
        {
            var options = new CryptOptions(new[] { credentials });

            BsonDocument key = null;

            using (var cryptClient = CryptClientFactory.Create(options))
            using (var context = cryptClient.StartCreateDataKeyContext(kmsKeyId))
            {
                key = ProcessState(context, _keyVault.Database, null);
            }

            _keyVault.InsertOne(key);
            Guid g = key["_id"].AsGuid;
            return g;
        }

        public BsonDocument EncryptCommand(KmsCredentials credentials, IMongoCollection<BsonDocument> coll, BsonDocument cmd)
        {
            var options = new CryptOptions(new[] { credentials });

            using (var cryptClient = CryptClientFactory.Create(options))
            using (var context = cryptClient.StartEncryptionContext(coll.Database.DatabaseNamespace.DatabaseName, command: BsonUtil.ToBytes(cmd)))
            {
                return ProcessState(context, coll.Database, cmd);
            }
        }

        public BsonDocument DecryptCommand(KmsCredentials credentials, IMongoDatabase db, BsonDocument doc)
        {
            var options = new CryptOptions(new[] { credentials });

            using (var cryptClient = CryptClientFactory.Create(options))
            using (var context = cryptClient.StartDecryptionContext(BsonUtil.ToBytes(doc)))
            {
                return ProcessState(context, db, null);
            }
        }

        public static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Ignore certificate errors when testing against localhost
            return true;
        }

        void DoKmsRequest(KmsRequest request)
        {
            TcpClient tcpClient = new TcpClient();

            Console.WriteLine("KMS: " + request.Endpoint);

            // change me to use the mock
            if (_kmsEndpoint != null)
            {
                tcpClient.Connect(_kmsEndpoint.DnsSafeHost, _kmsEndpoint.Port);
            }
            else
            {
                tcpClient.Connect(request.Endpoint, 443);
            }
            SslStream stream = new SslStream(tcpClient.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate));

            stream.AuthenticateAsClient("localhost");

            Binary bin = request.Message;
            stream.Write(bin.ToArray());

            byte[] buffer = new byte[4096];
            while (request.BytesNeeded > 0)
            {
                MemoryStream memBuffer = new MemoryStream();
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    memBuffer.Write(buffer, 0, read);
                }
                request.Feed(memBuffer.ToArray());
            }
        }

        private BsonDocument ProcessState(CryptContext context, IMongoDatabase db, BsonDocument cmd)
        {
            BsonDocument ret = cmd;

            while (!context.IsDone)
            {
                Console.WriteLine("\n----------------------------------\nState:" + context.State);
                switch (context.State)
                {
                    case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_COLLINFO:
                        {
                            var binary = context.GetOperation();
                            var doc = BsonUtil.ToDocument(binary);

                            Console.WriteLine("ListCollections Query: " + doc);

                            ListCollectionsOptions opts = new ListCollectionsOptions()
                            {
                                Filter = new BsonDocumentFilterDefinition<BsonDocument>(doc)
                            };

                            var reply = db.ListCollections(opts);

                            var replyDocs = reply.ToList<BsonDocument>();
                            Console.WriteLine("ListCollections Reply: " + replyDocs);

                            foreach (var replyDoc in replyDocs)
                            {
                                Console.WriteLine("ListCollections Reply: " + replyDoc);
                                context.Feed(BsonUtil.ToBytes(replyDoc));
                            }
                            context.MarkDone();

                            break;
                        }
                    case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_MARKINGS:
                        {
                            var binary = context.GetOperation();
                            var commandWithSchema = BsonUtil.ToDocument(binary);
                            Console.WriteLine("MongoCryptD Query: " + commandWithSchema);

                            var cryptDB = _clientCryptD.GetDatabase(db.DatabaseNamespace.DatabaseName);

                            var reply = cryptDB.RunCommand(new BsonDocumentCommand<BsonDocument>(commandWithSchema));
                            Console.WriteLine("MongoCryptD Reply: " + reply);

                            context.Feed(BsonUtil.ToBytes(reply));
                            context.MarkDone();

                            break;
                        }
                    case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_KEYS:
                        {
                            var binary = context.GetOperation();
                            Console.WriteLine("Buffer:" + BitConverter.ToString(binary.ToArray()));

                            var doc = BsonUtil.ToDocument(binary);

                            Console.WriteLine("GetKeys Query: " + doc);

                            var reply = _keyVault.Find(new BsonDocumentFilterDefinition<BsonDocument>(doc));

                            var replyDocs = reply.ToList<BsonDocument>();
                            Console.WriteLine("GetKeys Reply: " + replyDocs);

                            foreach (var replyDoc in replyDocs)
                            {
                                context.Feed(BsonUtil.ToBytes(replyDoc));
                            }

                            context.MarkDone();

                            break;
                        }
                    case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_KMS:
                        {
                            var requests = context.GetKmsMessageRequests();
                            foreach (var req in requests)
                            {
                                DoKmsRequest(req);
                            }
                            requests.MarkDone();
                            break;
                        }
                    case CryptContext.StateCode.MONGOCRYPT_CTX_READY:
                        {
                            Binary b = context.FinalizeForEncryption();
                            Console.WriteLine("Buffer:" + BitConverter.ToString(b.ToArray()));
                            ret = BsonUtil.ToDocument(b);
                            break;
                        }
                    case CryptContext.StateCode.MONGOCRYPT_CTX_DONE:
                        {
                            Console.WriteLine("DONE!!");
                            return ret;
                        }
                    case CryptContext.StateCode.MONGOCRYPT_CTX_ERROR:
                        {
                            throw new NotImplementedException();
                        }
                }
            }

            return ret;
        }
    }

    class Program
    {
        static IMongoCollection<BsonDocument> SetupKeyStore(MongoClient client)
        {
            var dbAdmin = client.GetDatabase("admin");
            var collKeyVault = dbAdmin.GetCollection<BsonDocument>("datakeys");

            // Clear the key vault
            collKeyVault.DeleteMany(new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument()));

            return collKeyVault;
        }

        static IMongoCollection<BsonDocument> SetupTestCollection(MongoClient client, Guid keyID)
        {
            var database = client.GetDatabase("test");

            // Reset state
            database.DropCollection("test");

            var s = new BsonDocument
            {
                {  "$jsonSchema" ,
                    new BsonDocument
                    {
                        {  "type", "object" },
                        { "properties" , new BsonDocument
                        {
                            { "ssn" , new BsonDocument
                            {

                                { "encrypt" , new BsonDocument
                                    {
                                    { "keyId" , new BsonArray( new BsonValue[] { keyID } ) },
                                    {  "bsonType" , "string"},
                                    { "algorithm" , "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic" },
                                    }
                                }
                            }
                            }
                        }
                        }
                    }
                }
            };

            database.CreateCollection("test", new CreateCollectionOptions<BsonDocument>() { Validator = new BsonDocumentFilterDefinition<BsonDocument>(s) });

            return database.GetCollection<BsonDocument>("test");

        }

        static string GetEnvironmenVariabletOrValue(string env, string def)
        {
            string value = Environment.GetEnvironmentVariable(env);
            if (value != null)
            {
                return value;
            }
            return def;
        }

        static void Main(string[] args)
        {
            // The C# driver transmutes data unless you specify this stupid line!
            BsonDefaults.GuidRepresentation = GuidRepresentation.Standard;

            Console.WriteLine("Using url: " + args);
            // or change me to use the mock
            Uri kmsURL = Environment.GetEnvironmentVariable("FLE_AWS_SECRET_ACCESS_KEY") != null ? null : new Uri("https://localhost:8000");

            var cryptDUrl = new MongoUrl("mongodb://localhost:27020");
            var client = new MongoClient("mongodb://localhost:27017");

            IMongoCollection<BsonDocument> collKeyVault = SetupKeyStore(client);

            var controller = new MongoCryptDController(cryptDUrl, collKeyVault, kmsURL);

            var awsKeyId = new KmsKeyId(
                new BsonDocument
                {
                    { "provider", "aws" },
                    { "region", "us-east-1" },
                    { "key", "arn:aws:kms:us-east-1:579766882180:key/0689eb07-d588-4bbf-a83e-42157a92576b" }
                }.ToBson());

            var kmsCredentials = new KmsCredentials(
                new BsonDocument
                {
                    {  "aws",
                        new BsonDocument
                        {
                            { "secretAccessKey",  GetEnvironmenVariabletOrValue("FLE_AWS_SECRET_ACCESS_KEY", "us-east-1") },
                            { "accessKeyId", GetEnvironmenVariabletOrValue("FLE_AWS_ACCESS_KEY_ID", "us-east-1") }
                        }
                    }
                }.ToBson());


            Guid keyID = controller.GenerateKey(kmsCredentials, awsKeyId);

            IMongoCollection<BsonDocument> collection = SetupTestCollection(client, keyID);
            var database = collection.Database;

            // Insert a document with SSN
            var insertDoc = new BsonDocument
            {
                {  "ssn" , "123-45-6789" },
            };

            var insertDocCmd = new BsonDocument
            {
                { "insert" , "test" },
                { "documents", new BsonArray(new BsonValue[] { insertDoc }) }
            };

            var insertEncryptedDoc = new BsonDocument(controller.EncryptCommand(kmsCredentials, collection, insertDocCmd));

            Console.WriteLine("Insert Doc: " + insertEncryptedDoc);

            insertEncryptedDoc.Remove("$db");
            database.RunCommand(new BsonDocumentCommand<BsonDocument>(insertEncryptedDoc));


            var findDoc = BsonUtil.FromJSON(@"{
'find': 'test',
'filter' :  { '$or': [{ '_id': 1},{ 'ssn': '123-45-6789'}]},
        }");


            var findCmd = new BsonDocumentCommand<BsonDocument>(controller.EncryptCommand(kmsCredentials, collection, findDoc));

            Console.WriteLine("Find CMD: " + findCmd.Document);

            findCmd.Document.Remove("$db");

            var commandResult = database.RunCommand(findCmd);

            Console.WriteLine("Find Result: " + commandResult);

            var decryptedDocument = controller.DecryptCommand(kmsCredentials, database, commandResult);

            Console.WriteLine("Find Result (DECRYPTED): " + decryptedDocument);
        }
    }
}
