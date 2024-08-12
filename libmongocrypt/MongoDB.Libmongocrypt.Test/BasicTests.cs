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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using System.Text;
using FluentAssertions;
using Xunit.Abstractions;
using MongoDB.Libmongocrypt.Test.Callbacks;

namespace MongoDB.Libmongocrypt.Test
{
    public class BasicTests
    {
        private static ITestOutputHelper _output;
        private const string AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic = "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic";
        private const string AEAD_AES_256_CBC_HMAC_SHA_512_Random = "AEAD_AES_256_CBC_HMAC_SHA_512-Random";

        public BasicTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void EncryptQuery()
        {
            using (var cryptClient = CryptClientFactory.Create(CreateOptions()))
            using (var context =
                cryptClient.StartEncryptionContext("test", command: BsonUtil.ToBytes(ReadJsonTestFile("cmd.json"))))
            {
                var (_, bsonCommand) = ProcessContextToCompletion(context);
                bsonCommand.Should().Equal((ReadJsonTestFile("encrypted-command.json")));
            }
        }

        [Fact]
        public void EncryptQueryStepwise()
        {
            using (var cryptClient = CryptClientFactory.Create(CreateOptions()))
            using (var context = cryptClient.StartEncryptionContext("test", command: BsonUtil.ToBytes(ReadJsonTestFile("cmd.json"))))
            {
                var (state, _, operationSent) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_COLLINFO);
                operationSent.Should().Equal((ReadJsonTestFile("list-collections-filter.json")));

                (state, _, operationSent) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_MARKINGS);
                operationSent.Should().Equal(ReadJsonTestFile("mongocryptd-command.json"));

                (state, _, operationSent) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_KEYS);
                operationSent.Should().Equal(ReadJsonTestFile("key-filter.json"));

                (state, _, _) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_NEED_KMS);
                // kms fluent assertions inside ProcessState()

                (state, _, operationSent) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_READY);
                operationSent.Should().Equal((ReadJsonTestFile("encrypted-command.json")));
            }
        }

        [Fact]
        public void EncryptQueryWithSchemaStepwise()
        {
            var listCollectionsReply = ReadJsonTestFile("collection-info.json");
            var schema = new BsonDocument("test.test", listCollectionsReply["options"]["validator"]["$jsonSchema"]);

            var options = new CryptOptions(
                new[] { CreateKmsCredentials("aws") },
                BsonUtil.ToBytes(schema));

            using (var cryptClient = CryptClientFactory.Create(options))
            using (var context =
                cryptClient.StartEncryptionContext(
                    db: "test",
                    command: BsonUtil.ToBytes(ReadJsonTestFile("cmd.json"))))
            {

                var (state, _, operationSent) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_MARKINGS);
                var mongoCryptdCommand = ReadJsonTestFile("mongocryptd-command.json");
                mongoCryptdCommand["isRemoteSchema"] = false;
                operationSent.Should().Equal(mongoCryptdCommand);

                (state, _, operationSent) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_KEYS);
                operationSent.Should().Equal(ReadJsonTestFile("key-filter.json"));

                (state, _, _) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_NEED_KMS);
                // kms fluent assertions inside ProcessState()

                (state, _, operationSent) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_READY);
                operationSent.Should().Equal((ReadJsonTestFile("encrypted-command.json")));
            }
        }

        [Fact]
        public void DecryptQuery()
        {
            using (var cryptClient = CryptClientFactory.Create(CreateOptions()))
            using (var context =
                cryptClient.StartDecryptionContext(BsonUtil.ToBytes(ReadJsonTestFile("encrypted-command-reply.json"))))
            {
                var (_, bsonCommand) = ProcessContextToCompletion(context);
                bsonCommand.Should().Equal(ReadJsonTestFile("command-reply.json"));
            }
        }

        [Fact]
        public void DecryptQueryStepwise()
        {
            using (var cryptClient = CryptClientFactory.Create(CreateOptions()))
            using (var context = cryptClient.StartDecryptionContext(BsonUtil.ToBytes(ReadJsonTestFile("encrypted-command-reply.json"))))
            {
                var (state, _, operationProduced) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_KEYS);
                operationProduced.Should().Equal(ReadJsonTestFile("key-filter.json"));

                (state, _, _) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_NEED_KMS);
                // kms fluent assertions inside ProcessState()

                (state, _, operationProduced) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_READY);
                operationProduced.Should().Equal(ReadJsonTestFile("command-reply.json"));

                (state, _, _) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_DONE);
            }
        }

        [Fact]
        public void EncryptBadBson()
        {
            using (var cryptClient = CryptClientFactory.Create(CreateOptions()))
            {
                Func<CryptContext> startEncryptionContext = () =>
                    cryptClient.StartEncryptionContext("test", command: new byte[] { 0x1, 0x2, 0x3 });

                // Ensure if we encrypt non-sense, it throws an exception demonstrating our exception code is good
                var exception = Record.Exception(startEncryptionContext);

                exception.Should().BeOfType<CryptException>();
            }
        }

        [Fact]
        public void EncryptExplicit()
        {
            var keyDoc = ReadJsonTestFile("key-document.json");
            var keyId = keyDoc["_id"].AsBsonBinaryData.Bytes;

            BsonDocument doc = new BsonDocument()
            {
                {  "v" , "hello" },
            };

            var testData = BsonUtil.ToBytes(doc);

            byte[] encryptedBytes;
            using (var cryptClient = CryptClientFactory.Create(CreateOptions()))
            using (var context = StartExplicitEncryptionContextWithKeyId(cryptClient, keyId, AEAD_AES_256_CBC_HMAC_SHA_512_Random, testData))
            {
                var (encryptedBinary, encryptedDocument) = ProcessContextToCompletion(context);
                encryptedBytes = encryptedBinary.ToArray(); // need to copy bytes out before the context gets destroyed
            }

            using (var cryptClient = CryptClientFactory.Create(CreateOptions()))
            using (var context = cryptClient.StartExplicitDecryptionContext(encryptedBytes))
            {
                var (decryptedResult, _) = ProcessContextToCompletion(context);

                decryptedResult.ToArray().Should().Equal(testData);
            }
        }

        [Fact]
        public void EncryptExplicitStepwise()
        {
            var keyDoc = ReadJsonTestFile("key-document.json");
            var keyId = keyDoc["_id"].AsBsonBinaryData.Bytes;

            var doc = new BsonDocument("v", "hello");

            var testData = BsonUtil.ToBytes(doc);

            using (var cryptClient = CryptClientFactory.Create(CreateOptions()))
            {
                byte[] encryptedResult;
                using (var context = StartExplicitEncryptionContextWithKeyId(
                    cryptClient,
                    keyId: keyId,
                    encryptionAlgorithm: AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic,
                    message: testData))
                {
                    var (state, binaryProduced, operationProduced) = ProcessState(context);
                    state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_KEYS);
                    operationProduced.Should().Equal(ReadJsonTestFile("key-filter.json"));

                    (state, _, _) = ProcessState(context);
                    state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_NEED_KMS);
                    // kms fluent assertions inside ProcessState()

                    (state, binaryProduced, operationProduced) = ProcessState(context);
                    state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_READY);
                    operationProduced.Should().Equal(ReadJsonTestFile("encrypted-value.json"));
                    encryptedResult = binaryProduced.ToArray(); // need to copy bytes out before the context gets destroyed

                    (state, _, _) = ProcessState(context);
                    state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_DONE);
                }

                using (var context = cryptClient.StartExplicitDecryptionContext(encryptedResult))
                {
                    var (state, decryptedBinary, _) = ProcessState(context);
                    state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_READY);
                    decryptedBinary.ToArray().Should().Equal(testData);

                    (state, _, _) = ProcessState(context);
                    state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_DONE);
                }
            }
        }

        [Fact]
        public void TestAwsKeyCreationWithEndPoint()
        {
            var endpoint = "kms.us-east-1.amazonaws.com";
            var keyId = CreateKmsKeyId("aws", endpoint);
            var key = CreateKmsCredentials("aws");

            using (var cryptClient = CryptClientFactory.Create(new CryptOptions(new[] { key })))
            using (var context = cryptClient.StartCreateDataKeyContext(keyId))
            {
                var (_, dataKeyDocument) = ProcessContextToCompletion(context, isKmsDecrypt: false);
                dataKeyDocument["masterKey"]["endpoint"].Should().Be(endpoint);
            }
        }

        [Fact]
        public void TestAwsKeyCreationWithEndpointStepwise()
        {
            var endpoint = "kms.us-east-1.amazonaws.com";
            var keyId = CreateKmsKeyId("aws", endpoint);
            var key = CreateKmsCredentials("aws");

            using (var cryptClient = CryptClientFactory.Create(new CryptOptions(new[] { key })))
            using (var context = cryptClient.StartCreateDataKeyContext(keyId))
            {
                BsonDocument dataKeyDocument;
                var (state, _, _) = ProcessState(context, isKmsDecrypt: false);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_NEED_KMS);

                (state, _, dataKeyDocument) = ProcessState(context, isKmsDecrypt: false);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_READY);
                dataKeyDocument["masterKey"]["endpoint"].Should().Be(endpoint);

                (state, _, _) = ProcessState(context, isKmsDecrypt: false);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_DONE);
            }
        }

        [Fact]
        public void TestAwsKeyCreationWithkeyAltNames()
        {
            var keyAltNames = new[] { "KeyMaker", "Architect" };
            var keyAltNameDocuments = keyAltNames.Select(name => new BsonDocument("keyAltName", name));
            var keyAltNameBuffers = keyAltNameDocuments.Select(BsonUtil.ToBytes);
            var keyId = CreateKmsKeyId("aws", keyAltNameBuffers: keyAltNameBuffers);
            var key = CreateKmsCredentials("aws");

            using (var cryptClient = CryptClientFactory.Create(new CryptOptions(new[] { key })))
            using (var context = cryptClient.StartCreateDataKeyContext(keyId))
            {
                var (_, dataKeyDocument) = ProcessContextToCompletion(context, isKmsDecrypt: false);
                dataKeyDocument.Should().NotBeNull();
                var actualKeyAltNames = dataKeyDocument["keyAltNames"].AsBsonArray.Select(x => x.AsString);
                var expectedKeyAltNames = keyAltNames.Reverse(); // https://jira.mongodb.org/browse/CDRIVER-3277?
                actualKeyAltNames.Should().BeEquivalentTo(expectedKeyAltNames);
            }
        }

        [Fact]
        public void TestAwsKeyCreationWithkeyAltNamesStepwise()
        {
            var keyAltNames = new[] { "KeyMaker", "Architect" };
            var keyAltNameDocuments = keyAltNames.Select(name => new BsonDocument("keyAltName", name));
            var keyAltNameBuffers = keyAltNameDocuments.Select(BsonUtil.ToBytes);
            var keyId = CreateKmsKeyId("aws", keyAltNameBuffers: keyAltNameBuffers);
            var key = CreateKmsCredentials("aws");

            using (var cryptClient = CryptClientFactory.Create(new CryptOptions(new[] { key })))
            using (var context =
                cryptClient.StartCreateDataKeyContext(keyId))
            {
                BsonDocument dataKeyDocument;
                var (state, _, _) = ProcessState(context, isKmsDecrypt: false);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_NEED_KMS);

                (state, _, dataKeyDocument) = ProcessState(context, isKmsDecrypt: false);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_READY);
                dataKeyDocument.Should().NotBeNull();
                var actualKeyAltNames = dataKeyDocument["keyAltNames"].AsBsonArray.Select(x => x.AsString);
                var expectedKeyAltNames = keyAltNames.Reverse(); // https://jira.mongodb.org/browse/CDRIVER-3277?
                actualKeyAltNames.Should().BeEquivalentTo(expectedKeyAltNames);

                (state, _, _) = ProcessState(context, isKmsDecrypt: false);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_DONE);
            }
        }

        [Fact]
        public void TestLocalKeyCreationWithkeyAltNames()
        {
            var keyAltNames = new[] { "KeyMaker", "Architect" };
            var keyAltNameDocuments = keyAltNames.Select(name => new BsonDocument("keyAltName", name));
            var keyAltNameBuffers = keyAltNameDocuments.Select(BsonUtil.ToBytes);
            var key = CreateKmsCredentials("local");
            var keyId = CreateKmsKeyId("local", keyAltNameBuffers: keyAltNameBuffers);
            var cryptOptions = new CryptOptions(new[] { key });

            using (var cryptClient = CryptClientFactory.Create(cryptOptions))
            using (var context =
                cryptClient.StartCreateDataKeyContext(keyId))
            {
                var (_, dataKeyDocument) = ProcessContextToCompletion(context);
                dataKeyDocument.Should().NotBeNull();
                var actualKeyAltNames = dataKeyDocument["keyAltNames"].AsBsonArray.Select(x => x.AsString);
                var expectedKeyAltNames = keyAltNames.Reverse(); // https://jira.mongodb.org/browse/CDRIVER-3277?
                actualKeyAltNames.Should().BeEquivalentTo(expectedKeyAltNames);
            }
        }

        [Fact]
        public void TestLocalKeyCreationWithkeyAltNamesStepwise()
        {
            var keyAltNames = new[] { "KeyMaker", "Architect" };
            var keyAltNameDocuments = keyAltNames.Select(name => new BsonDocument("keyAltName", name));
            var keyAltNameBuffers = keyAltNameDocuments.Select(BsonUtil.ToBytes);
            var key = CreateKmsCredentials("local");
            var keyId = CreateKmsKeyId("local", keyAltNameBuffers: keyAltNameBuffers);
            var cryptOptions = new CryptOptions(new[] { key });

            using (var cryptClient = CryptClientFactory.Create(cryptOptions))
            using (var context =
                cryptClient.StartCreateDataKeyContext(keyId))
            {
                var (state, _, dataKeyDocument) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_READY);
                dataKeyDocument.Should().NotBeNull();
                var actualKeyAltNames = dataKeyDocument["keyAltNames"].AsBsonArray.Select(x => x.AsString);
                var expectedKeyAltNames = keyAltNames.Reverse(); // https://jira.mongodb.org/browse/CDRIVER-3277?
                actualKeyAltNames.Should().BeEquivalentTo(expectedKeyAltNames);

                (state, _, _) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_DONE);
            }
        }

        [Fact]
        public void TestLocalKeyCreation()
        {
            var key = CreateKmsCredentials("local");
            var keyId = CreateKmsKeyId("local");
            var cryptOptions = new CryptOptions(new[] { key });

            using (var cryptClient = CryptClientFactory.Create(cryptOptions))
            using (var context =
                cryptClient.StartCreateDataKeyContext(keyId))
            {
                var (_, dataKeyDocument) = ProcessContextToCompletion(context);
                dataKeyDocument.Should().NotBeNull();
            }
        }


        [Fact]
        public void TestLocalKeyCreationStepwise()
        {
            var key = CreateKmsCredentials("local");
            var keyId = CreateKmsKeyId("local");
            var cryptOptions = new CryptOptions(new[] { key });

            using (var cryptClient = CryptClientFactory.Create(cryptOptions))
            using (var context =
                cryptClient.StartCreateDataKeyContext(keyId))
            {
                var (state, _, dataKeyDocument) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_READY);
                dataKeyDocument.Should().NotBeNull();

                (state, _, _) = ProcessState(context);
                state.Should().Be(CryptContext.StateCode.MONGOCRYPT_CTX_DONE);
            }
        }

        [Theory]
        [InlineData("aws")]
        [InlineData("azure")]
#if NETCOREAPP3_0
        [InlineData("gcp")]
#endif
        [InlineData("kmip")]
        public void TestGetKmsProviderName(string kmsName)
        {
            var key = CreateKmsCredentials(kmsName);
            var keyId = CreateKmsKeyId(kmsName);
            var cryptOptions = new CryptOptions(new[] { key });

            using (var cryptClient = CryptClientFactory.Create(cryptOptions))
            using (var context = cryptClient.StartCreateDataKeyContext(keyId))
            {
                var request = context.GetKmsMessageRequests().Single();
                request.KmsProvider.Should().Be(kmsName);
            }
        }

        // private methods
        private static KmsCredentials CreateKmsCredentials(string kmsName)
        {
            BsonDocument kmsCredentialsDocument;
            switch (kmsName)
            {
                case "local":
                    kmsCredentialsDocument = new BsonDocument
                    {
                        {
                            "local",
                            new BsonDocument
                            {
                                { "key", new BsonBinaryData(new byte[96]) }
                            }
                        }
                    };
                    break;
                case "aws":
                    kmsCredentialsDocument = new BsonDocument
                    {
                        {
                            "aws",
                            new BsonDocument
                            {
                                { "secretAccessKey", "dummy" },
                                { "accessKeyId", "dummy" }
                            }
                        }
                    };
                    break;
                case "azure":
                    kmsCredentialsDocument = new BsonDocument
                    {
                        {
                            "azure",
                            new BsonDocument
                            {
                                { "tenantId", "dummy" },
                                { "clientId", "dummy" },
                                { "clientSecret", "dummy" }
                            }
                        }
                    };
                    break;
                case "gcp":
                    kmsCredentialsDocument = new BsonDocument
                    {
                        {
                            "gcp",
                            new BsonDocument
                            {
                                { "email", "dummy" },
                                { "privateKey", SigningRSAESPKCSCallbackTests.PrivateKey }
                            }
                        }
                    };
                    break;
                case "kmip":
                    kmsCredentialsDocument = new BsonDocument
                    {
                        {
                            "kmip", 
                            new BsonDocument
                            {
                                { "endpoint", "dummy" }
                            }
                        }
                    };
                    break;
                default: throw new Exception($"Unsupported kms {kmsName}.");
            }
            return new KmsCredentials(kmsCredentialsDocument.ToBson());
        }

        private static KmsKeyId CreateKmsKeyId(string kmsName, string endPoint = null, IEnumerable<byte[]> keyAltNameBuffers = null)
        {
            BsonDocument datakeyOptionsDocument;
            switch (kmsName)
            {
                case "local":
                    datakeyOptionsDocument = new BsonDocument
                    {
                        { "provider", "local" },
                    };
                    break;
                case "aws":
                    datakeyOptionsDocument = new BsonDocument
                    {
                        { "provider", "aws" },
                        { "key", "cmk" },
                        { "region", "us-east-1" },
                        { "endpoint", () => endPoint, endPoint != null }
                    };
                    break;
                case "azure":
                    datakeyOptionsDocument = new BsonDocument
                    {
                        { "provider", "azure" },
                        { "keyName", "dummy" },
                        { "keyVaultEndpoint", endPoint ?? "dummy.azure.net" }
                    };
                    break;
                case "gcp":
                    datakeyOptionsDocument = new BsonDocument
                    {
                        { "provider", "gcp" },
                        { "projectId", "dummy" },
                        { "location", "dummy" },
                        { "keyRing", "dummy" },
                        { "keyName", "dummy" },
                        { "endpoint", () => endPoint, endPoint != null }
                    };
                    break;
                case "kmip":
                    datakeyOptionsDocument = new BsonDocument
                    {
                        { "provider", "kmip" }
                    };
                    break;
                default: throw new Exception($"Unsupported kms {kmsName}.");
            }
            return new KmsKeyId(datakeyOptionsDocument.ToBson(), keyAltNameBuffers);
        }

        private CryptOptions CreateOptions()
        {
            return new CryptOptions(
                new[] 
                {
                    CreateKmsCredentials("aws"),
                    CreateKmsCredentials("local")
                });
        }

        private (Binary binarySent, BsonDocument document) ProcessContextToCompletion(CryptContext context, bool isKmsDecrypt = true)
        {
            BsonDocument document = null;
            Binary binary = null;

            while (!context.IsDone)
            {
                (_, binary, document) = ProcessState(context, isKmsDecrypt);
            }

            return (binary, document);
        }

        /// <summary>
        /// Processes the current state, simulating the execution the operation/post requests needed to reach the next state
        /// Returns (stateProcessed, binaryOperationProduced, bsonOperationProduced)
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private (CryptContext.StateCode stateProcessed, Binary binaryProduced, BsonDocument bsonOperationProduced) ProcessState(CryptContext context, bool isKmsDecrypt = true)
        {
            _output.WriteLine("\n----------------------------------\nState:" + context.State);
            switch (context.State)
            {
                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_COLLINFO:
                    {
                        var binary = context.GetOperation();
                        var doc = BsonUtil.ToDocument(binary);
                        _output.WriteLine("ListCollections: " + doc);
                        var reply = ReadJsonTestFile("collection-info.json");
                        _output.WriteLine("Reply:" + reply);
                        context.Feed(BsonUtil.ToBytes(reply));
                        context.MarkDone();
                        return (CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_COLLINFO, binary, doc);
                    }

                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_MARKINGS:
                    {
                        var binary = context.GetOperation();
                        var doc = BsonUtil.ToDocument(binary);
                        _output.WriteLine("Markings: " + doc);
                        var reply = ReadJsonTestFile("mongocryptd-reply.json");
                        _output.WriteLine("Reply:" + reply);
                        context.Feed(BsonUtil.ToBytes(reply));
                        context.MarkDone();
                        return (CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_MARKINGS, binary, doc);
                    }

                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_KEYS:
                    {
                        var binary = context.GetOperation();
                        var doc = BsonUtil.ToDocument(binary);
                        _output.WriteLine("Key Document: " + doc);
                        var reply = ReadJsonTestFile("key-document.json");
                        _output.WriteLine("Reply:" + reply);
                        context.Feed(BsonUtil.ToBytes(reply));
                        context.MarkDone();
                        return (CryptContext.StateCode.MONGOCRYPT_CTX_NEED_MONGO_KEYS, binary, doc);
                    }

                case CryptContext.StateCode.MONGOCRYPT_CTX_NEED_KMS:
                    {
                        var requests = context.GetKmsMessageRequests();
                        foreach (var req in requests)
                        {
                            var binary = req.Message;
                            _output.WriteLine("Key Document: " + binary);
                            var postRequest = binary.ToString();
                            // TODO: add different hosts handling
                            postRequest.Should().Contain("Host:kms.us-east-1.amazonaws.com"); // only AWS

                            var reply = ReadHttpTestFile(isKmsDecrypt ? "kms-decrypt-reply.txt" : "kms-encrypt-reply.txt");
                            _output.WriteLine("Reply: " + reply);
                            req.Feed(Encoding.UTF8.GetBytes(reply));
                            req.BytesNeeded.Should().Be(0);
                        }

                        requests.MarkDone();
                        return (CryptContext.StateCode.MONGOCRYPT_CTX_NEED_KMS, null, null);
                    }

                case CryptContext.StateCode.MONGOCRYPT_CTX_READY:
                    {
                        Binary binary = context.FinalizeForEncryption();
                        _output.WriteLine("Buffer:" + binary.ToArray());
                        var document = BsonUtil.ToDocument(binary);
                        _output.WriteLine("Document:" + document);
                        return (CryptContext.StateCode.MONGOCRYPT_CTX_READY, binary, document);
                    }

                case CryptContext.StateCode.MONGOCRYPT_CTX_DONE:
                    {
                        _output.WriteLine("DONE!!");
                        return (CryptContext.StateCode.MONGOCRYPT_CTX_DONE, null, null);
                    }

                case CryptContext.StateCode.MONGOCRYPT_CTX_ERROR:
                    {
                        // We expect exceptions are thrown before we get to this state
                        throw new NotImplementedException();
                    }
            }

            throw new NotImplementedException();
        }

        public CryptContext StartExplicitEncryptionContextWithKeyId(CryptClient client, byte[] keyId, string encryptionAlgorithm, byte[] message)
        {
            return client.StartExplicitEncryptionContext(keyId, keyAltName: null, queryType: null, contentionFactor: null, encryptionAlgorithm, message, rangeOptions: null);
        }

        static IEnumerable<string> FindTestDirectories()
        {
            string[] searchPaths = new[] { Path.Combine("..", "test", "example"), Path.Combine("..", "test", "data") };
            var assemblyLocation = Path.GetDirectoryName(typeof(BasicTests).GetTypeInfo().Assembly.Location);
            string cwd = Directory.GetCurrentDirectory(); // Assume we are in a child directory of the repo
            var searchDirectory = assemblyLocation ?? cwd;
            var testDirs = Enumerable.Range(1, 10)
                .Select(i => Enumerable.Repeat("..", i))
                .Select(dotsSeq => dotsSeq.Aggregate(Path.Combine))
                .SelectMany(previousDirectories =>
                    searchPaths.Select(searchPath => Path.Combine(searchDirectory, previousDirectories, searchPath)))
                .Where(Directory.Exists)
                .ToArray();

            if (!testDirs.Any())
            {
                throw new DirectoryNotFoundException("test/example");
            }

            return testDirs;
        }

        static string ReadHttpTestFile(string file)
        {
            // The HTTP tests assume \r\n
            // And git strips \r on Unix machines by default so fix up the files

            var text = ReadTestFile(file);

            StringBuilder builder = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n' && text[i - 1] != '\r')
                    builder.Append('\r');
                builder.Append(text[i]);
            }
            return builder.ToString();
        }

        static BsonDocument ReadJsonTestFile(string file)
        {
            var text = ReadTestFile(file);

            if (text == null)
            {
                throw new FileNotFoundException(file);
            }

            // Work around C# drivers and C driver have different extended json support
            text = text.Replace("\"$numberLong\"", "$numberLong");

            return BsonUtil.FromJSON(text);
        }

        static string ReadTestFile(string fileName)
        {
            return FindTestDirectories()
                .Select(directory => Path.Combine(directory, fileName))
                .Where(File.Exists)
                .Select(File.ReadAllText)
                .FirstOrDefault();
        }
    }
}
