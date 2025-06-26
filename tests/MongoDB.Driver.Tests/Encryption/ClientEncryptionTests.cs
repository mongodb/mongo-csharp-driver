/* Copyright 2010–present MongoDB Inc.
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests.Specifications.client_side_encryption;
using MongoDB.Driver.Encryption;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Encryption
{
    [Trait("Category", "CSFLE")]
    [Trait("Category", "Integration")]
    public class ClientEncryptionTests
    {
        #region static
        private static readonly ClusterId __clusterId = new ClusterId();
        private static readonly EndPoint __endPoint = new DnsEndPoint("localhost", 27017);
        private static readonly CollectionNamespace __keyVaultCollectionNamespace = CollectionNamespace.FromFullName("datakeys.keyvault");
        #endregion

        [Fact]
        public async Task AddAlternateKeyName_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var guid = new Guid();

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentNullException(Record.Exception(() => subject.AddAlternateKeyName(id: guid, alternateKeyName: null)), expectedParamName: "alternateKeyName");
                ShouldBeArgumentNullException(await Record.ExceptionAsync(() => subject.AddAlternateKeyNameAsync(id: guid, alternateKeyName: null)), expectedParamName: "alternateKeyName");
            }
        }

        [Fact]
        public async Task CreateDataKey_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentNullException(Record.Exception(() => subject.CreateDataKey(kmsProvider: null, new DataKeyOptions())), expectedParamName: "kmsProvider");
                ShouldBeArgumentNullException(await Record.ExceptionAsync(() => subject.CreateDataKeyAsync(kmsProvider: null, new DataKeyOptions())), expectedParamName: "kmsProvider");

                _ = subject.CreateDataKey(kmsProvider: "local", dataKeyOptions: null);
                _ = await subject.CreateDataKeyAsync(kmsProvider: "local", dataKeyOptions: null);
            }
        }

        [Fact]
        public async Task CreateEncryptedCollection_should_handle_input_arguments()
        {
            const string kmsProvider = "local";
            const string collectionName = "collName";
            var createCollectionOptions = new CreateCollectionOptions();
            var database = Mock.Of<IMongoDatabase>();

            var masterKey = new BsonDocument();

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentNullException(Record.Exception(() => subject.CreateEncryptedCollection(database: null, collectionName, createCollectionOptions, kmsProvider, masterKey)), expectedParamName: "database");
                ShouldBeArgumentNullException(await Record.ExceptionAsync(() => subject.CreateEncryptedCollectionAsync(database: null, collectionName, createCollectionOptions, kmsProvider, masterKey)), expectedParamName: "database");

                ShouldBeArgumentNullException(Record.Exception(() => subject.CreateEncryptedCollection(database, collectionName: null, createCollectionOptions, kmsProvider, masterKey)), expectedParamName: "collectionName");
                ShouldBeArgumentNullException(await Record.ExceptionAsync(() => subject.CreateEncryptedCollectionAsync(database, collectionName: null, createCollectionOptions, kmsProvider, masterKey)), expectedParamName: "collectionName");

                ShouldBeArgumentNullException(Record.Exception(() => subject.CreateEncryptedCollection(database, collectionName: collectionName, createCollectionOptions: null, kmsProvider, masterKey)), expectedParamName: "createCollectionOptions");
                ShouldBeArgumentNullException(await Record.ExceptionAsync(() => subject.CreateEncryptedCollectionAsync(database, collectionName, createCollectionOptions: null, kmsProvider, masterKey)), expectedParamName: "createCollectionOptions");

                ShouldBeArgumentNullException(Record.Exception(() => subject.CreateEncryptedCollection(database, collectionName: collectionName, createCollectionOptions, kmsProvider: null, masterKey)), expectedParamName: "kmsProvider");
                ShouldBeArgumentNullException(await Record.ExceptionAsync(() => subject.CreateEncryptedCollectionAsync(database, collectionName, createCollectionOptions, kmsProvider: null, masterKey)), expectedParamName: "kmsProvider");

                var invalidDataKeyOptions = new DataKeyOptions(alternateKeyNames: Optional.Create(Mock.Of<IReadOnlyList<string>>()));
#pragma warning disable CS0618 // Type or member is obsolete
                Record.Exception(() => subject.CreateEncryptedCollection(database, collectionName: collectionName, createCollectionOptions, kmsProvider, dataKeyOptions: invalidDataKeyOptions))
                    .Should().BeOfType<ArgumentException>().Which.Message.Should().Be("CreateEncryptedCollection supports only MasterKey in DataKeyOptions.");
                (await Record.ExceptionAsync(() => subject.CreateEncryptedCollectionAsync(database, collectionName, createCollectionOptions, kmsProvider, dataKeyOptions: invalidDataKeyOptions)))
                    .Should().BeOfType<ArgumentException>().Which.Message.Should().Be("CreateEncryptedCollection supports only MasterKey in DataKeyOptions.");

                invalidDataKeyOptions = new DataKeyOptions(keyMaterial: new BsonBinaryData(new byte[0]));
                Record.Exception(() => subject.CreateEncryptedCollection(database, collectionName: collectionName, createCollectionOptions, kmsProvider, dataKeyOptions: invalidDataKeyOptions))
                    .Should().BeOfType<ArgumentException>().Which.Message.Should().Be("CreateEncryptedCollection supports only MasterKey in DataKeyOptions.");
                (await Record.ExceptionAsync(() => subject.CreateEncryptedCollectionAsync(database, collectionName, createCollectionOptions, kmsProvider, dataKeyOptions: invalidDataKeyOptions)))
                    .Should().BeOfType<ArgumentException>().Which.Message.Should().Be("CreateEncryptedCollection supports only MasterKey in DataKeyOptions.");
#pragma warning restore CS0618 // Type or member is obsolete
            }

        }

        [Fact]
        public async Task CreateEncryptedCollection_should_handle_generated_key_when_second_key_failed()
        {
            const string kmsProvider = "local";
            const string collectionName = "collName";
            const string encryptedFieldsStr = "{ fields : [{ keyId : null }, { keyId : null }] }";
            var serverId = new ServerId(__clusterId, __endPoint);
            var serverDescription = new ServerDescription(serverId, __endPoint, wireVersionRange: new Range<int>(20, 21), type: ServerType.ReplicaSetPrimary);
            var mockCluster = new Mock<IClusterInternal>();

            var clusterDescription = new ClusterDescription(
                __clusterId,
                false,
                null,
                ClusterType.ReplicaSet,
                [serverDescription]);

            mockCluster.SetupGet(c => c.Description).Returns(clusterDescription);
            var mockServer = new Mock<IServer>();
            mockServer.SetupGet(s => s.Description).Returns(serverDescription);
            var connection = Mock.Of<IConnectionHandle>(c => c.Description == new ConnectionDescription(new ConnectionId(serverId), new HelloResult(new BsonDocument("maxWireVersion", serverDescription.WireVersionRange.Max))));
            mockServer.Setup(s => s.GetConnection(It.IsAny<OperationContext>())).Returns(connection);
            mockServer.Setup(s => s.GetConnectionAsync(It.IsAny<OperationContext>())).ReturnsAsync(connection);

            mockCluster
                .Setup(m => m.SelectServer(It.IsAny<OperationContext>(), It.IsAny<IServerSelector>()))
                .Returns((mockServer.Object, TimeSpan.FromMilliseconds(42)));
            mockCluster
                .Setup(m => m.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<IServerSelector>()))
                .ReturnsAsync((mockServer.Object, TimeSpan.FromMilliseconds(42)));

            var database = Mock.Of<IMongoDatabase>(d =>
                d.DatabaseNamespace == new DatabaseNamespace("db") &&
                d.Client == Mock.Of<IMongoClient>(c => c.Cluster == mockCluster.Object));

            var masterKey = new BsonDocument();

            var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
            mockCollection
                .SetupSequence(c => c.InsertOne(It.IsAny<BsonDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
                .Pass()
                .Throws(new Exception("test"));
            mockCollection
                .SetupSequence(c => c.InsertOneAsync(It.IsAny<BsonDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Throws(new Exception("test"));
            var mockDatabase = new Mock<IMongoDatabase>();
            mockDatabase.Setup(c => c.GetCollection<BsonDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>())).Returns(mockCollection.Object);
            var client = new Mock<IMongoClient>();
            client.Setup(c => c.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>())).Returns(mockDatabase.Object);

            using (var subject = CreateSubject(client.Object))
            {
                var createCollectionOptions = new CreateCollectionOptions() { EncryptedFields = BsonDocument.Parse(encryptedFieldsStr) };
                var exception = Record.Exception(() => subject.CreateEncryptedCollection(database, collectionName, createCollectionOptions, kmsProvider, masterKey));
                AssertResults(exception);

                exception = await Record.ExceptionAsync(() => subject.CreateEncryptedCollectionAsync(database, collectionName, createCollectionOptions, kmsProvider, masterKey));
                AssertResults(exception);
            }

            void AssertResults(Exception ex)
            {
                var createCollectionException = ex.Should().BeOfType<MongoEncryptionCreateCollectionException>().Subject;
                createCollectionException
                    .InnerException
                    .Should().BeOfType<MongoEncryptionException>().Subject.InnerException
                    .Should().BeOfType<Exception>().Which.Message
                    .Should().Be("test");
                var fields = createCollectionException.EncryptedFields["fields"].AsBsonArray;
                fields[0].AsBsonDocument["keyId"].Should().BeOfType<BsonBinaryData>(); // pass
                /*
                    - If generating `D` resulted in an error `E`, the entire
                    `CreateEncryptedCollection` must now fail with error `E`. Return the
                    partially-formed `EF'` with the error so that the caller may know what
                    datakeys have already been created by the helper.
                 */
                fields[1].AsBsonDocument["keyId"].Should().BeOfType<BsonNull>(); // throw
            }
        }

        [Theory]
        [InlineData(null, "There are no encrypted fields defined for the collection.")]
        [InlineData("{}", "{}")]
        [InlineData("{ a : 1 }", "{ a : 1 }")]
        [InlineData("{ fields : { } }", "{ fields: { } }")]
        [InlineData("{ fields : [] }", "{ fields: [] }")]
        [InlineData("{ fields : [{ a : 1 }] }", "{ fields: [{ a : 1 }] }")]
        [InlineData("{ fields : [{ keyId : 1 }] }", "{ fields: [{ keyId : 1 }] }")]
        [InlineData("{ fields : [{ keyId : null }] }", "{ fields: [{ keyId : '#binary_generated#' }] }")]
        [InlineData("{ fields : [{ keyId : null }, { keyId : null }] }", "{ fields: [{ keyId : '#binary_generated#' }, { keyId : '#binary_generated#' }] }")]
        [InlineData("{ fields : [{ keyId : 3 }, { keyId : null }] }", "{ fields: [{ keyId : 3 }, { keyId : '#binary_generated#' }] }")]
        public async Task CreateEncryptedCollection_should_handle_various_encryptedFields(string encryptedFieldsStr, string expectedResult)
        {
            const string kmsProvider = "local";
            const string collectionName = "collName";
            var serverId = new ServerId(__clusterId, __endPoint);
            var serverDescription = new ServerDescription(serverId, __endPoint, wireVersionRange: new Range<int>(20, 21), type: ServerType.ReplicaSetPrimary);
            var mockCluster = new Mock<IClusterInternal>();
            var clusterDescription = new ClusterDescription(
                __clusterId,
                false,
                null,
                ClusterType.ReplicaSet,
                [serverDescription]);

            mockCluster.SetupGet(c => c.Description).Returns(clusterDescription);
            var mockServer = new Mock<IServer>();
            mockServer.SetupGet(s => s.Description).Returns(serverDescription);
            var connection = Mock.Of<IConnectionHandle>(c => c.Description == new ConnectionDescription(new ConnectionId(serverId), new HelloResult(new BsonDocument("maxWireVersion", serverDescription.WireVersionRange.Max))));
            mockServer.Setup(s => s.GetConnection(It.IsAny<OperationContext>())).Returns(connection);
            mockServer.Setup(s => s.GetConnectionAsync(It.IsAny<OperationContext>())).ReturnsAsync(connection);

            mockCluster
                .Setup(m => m.SelectServer(It.IsAny<OperationContext>(), It.IsAny<IServerSelector>()))
                .Returns((mockServer.Object, TimeSpan.FromMilliseconds(42)));
            mockCluster
                .Setup(m => m.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<IServerSelector>()))
                .ReturnsAsync((mockServer.Object, TimeSpan.FromMilliseconds(42)));

            var database = Mock.Of<IMongoDatabase>(d =>
                d.DatabaseNamespace == new DatabaseNamespace("db") &&
                d.Client == Mock.Of<IMongoClient>(c => c.Cluster == mockCluster.Object));

            var masterKey = new BsonDocument();

            using (var subject = CreateSubject())
            {
                var createCollectionOptions = new CreateCollectionOptions() { EncryptedFields = encryptedFieldsStr != null ? BsonDocument.Parse(encryptedFieldsStr) : null };

                if (BsonDocument.TryParse(expectedResult, out var encryptedFields))
                {
                    var createCollectionResult = subject.CreateEncryptedCollection(database, collectionName, createCollectionOptions, kmsProvider, masterKey);
                    createCollectionResult.EncryptedFields.WithComparer(new EncryptedFieldsComparer()).Should().Be(encryptedFields.DeepClone());

                    createCollectionResult = await subject.CreateEncryptedCollectionAsync(database, collectionName, createCollectionOptions, kmsProvider, masterKey);
                    createCollectionResult.EncryptedFields.WithComparer(new EncryptedFieldsComparer()).Should().Be(encryptedFields.DeepClone());
                }
                else
                {
                    AssertInvalidOperationException(Record.Exception(() => subject.CreateEncryptedCollection(database, collectionName, createCollectionOptions, kmsProvider, masterKey)), expectedResult);
                    AssertInvalidOperationException(await Record.ExceptionAsync(() => subject.CreateEncryptedCollectionAsync(database, collectionName, createCollectionOptions, kmsProvider, masterKey)), expectedResult);
                }
            }

            void AssertInvalidOperationException(Exception ex, string message) =>
                ex
                .Should().BeOfType<MongoEncryptionCreateCollectionException>().Subject.InnerException
                .Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be(message);
        }

        private sealed class EncryptedFieldsComparer : IEqualityComparer<BsonDocument>
        {
            public bool Equals(BsonDocument x, BsonDocument y) =>
                BsonValueEquivalencyComparer.Compare(
                    x, y,
                    massageAction: (a, b) =>
                    {
                        if (a is BsonDocument aDocument && aDocument.TryGetValue("keyId", out var aKeyId) && aKeyId.IsBsonBinaryData &&
                            b is BsonDocument bDocument && bDocument.TryGetValue("keyId", out var bKeyId) && bKeyId == "#binary_generated#")
                        {
                            bDocument["keyId"] = aDocument["keyId"];
                        }
                    });

            public int GetHashCode(BsonDocument obj) => obj.GetHashCode();
        }

        [Fact]
        public void CryptClient_should_be_initialized()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                subject._cryptClient().Should().NotBeNull();
                subject._libMongoCryptController().Should().NotBeNull();
            }
        }

        [Fact]
        public async Task Decrypt_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentNullException(Record.Exception(() => subject.Decrypt(value: null)), expectedParamName: "encryptedValue");
                ShouldBeArgumentNullException(await Record.ExceptionAsync(() => subject.DecryptAsync(value: null)), expectedParamName: "encryptedValue");
            }
        }

        [Fact]
        public async Task Encrypt_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentNullException(Record.Exception(() => subject.Encrypt(value: "test", encryptOptions: null)), expectedParamName: "encryptOptions");
                ShouldBeArgumentNullException(await Record.ExceptionAsync(() => subject.EncryptAsync(value: "test", encryptOptions: null)), expectedParamName: "encryptOptions");

                ShouldBeArgumentNullException(Record.Exception(() => subject.Encrypt(value: null, new EncryptOptions(EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic, alternateKeyName: "test"))), expectedParamName: "value");
                ShouldBeArgumentNullException(await Record.ExceptionAsync(() => subject.EncryptAsync(value: null, new EncryptOptions(EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic, alternateKeyName: "test"))), expectedParamName: "value");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Encryption_should_use_correct_binarySubType([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                var keyId = subject.CreateDataKey("local", new DataKeyOptions());

                var value = "hello";

                var encrypted = await ExplicitEncryptAsync(subject, new EncryptOptions(EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic, keyId: keyId), value, async);
                encrypted.SubType.Should().Be(BsonBinarySubType.Encrypted);

                var decrypted = await ExplicitDecryptAsync(subject, encrypted, async);

                decrypted.Should().Be(BsonValue.Create(value));
            }
        }

        [Fact]
        public async Task GetKeyByAlternateKeyName_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentNullException(Record.Exception(() => subject.GetKeyByAlternateKeyName(alternateKeyName: null)), expectedParamName: "alternateKeyName");
                ShouldBeArgumentNullException(await Record.ExceptionAsync(() => subject.GetKeyByAlternateKeyNameAsync(alternateKeyName: null)), expectedParamName: "alternateKeyName");
            }
        }

        [Fact]
        public async Task RemoveAlternateKeyName_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var guid = new Guid();

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentNullException(Record.Exception(() => subject.RemoveAlternateKeyName(id: guid, alternateKeyName: null)), expectedParamName: "alternateKeyName");
                ShouldBeArgumentNullException(await Record.ExceptionAsync(() => subject.RemoveAlternateKeyNameAsync(id: guid, alternateKeyName: null)), expectedParamName: "alternateKeyName");
            }
        }

        [Fact]
        public async Task RewrapManyDataKey_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentNullException(Record.Exception(() => subject.RewrapManyDataKey(filter: null, options: new RewrapManyDataKeyOptions("local"))), expectedParamName: "filter");
                ShouldBeArgumentNullException(await Record.ExceptionAsync(() => subject.RewrapManyDataKeyAsync(filter: null, options: new RewrapManyDataKeyOptions("local"))), expectedParamName: "filter");

                _ = subject.RewrapManyDataKey(filter: FilterDefinition<BsonDocument>.Empty, options: null);
                _ = await subject.RewrapManyDataKeyAsync(filter: FilterDefinition<BsonDocument>.Empty, options: null);
            }
        }

        // private methods
        private ClientEncryption CreateSubject(IMongoClient client = null)
        {
            var clientEncryptionOptions = new ClientEncryptionOptions(
                client ?? DriverTestConfiguration.Client,
                __keyVaultCollectionNamespace,
                kmsProviders: EncryptionTestHelper.GetKmsProviders(filter: "local"));

            return new ClientEncryption(clientEncryptionOptions);
        }

        private async ValueTask<BsonValue> ExplicitDecryptAsync(ClientEncryption clientEncryption, BsonBinaryData value, bool async) =>
            async ? await clientEncryption.DecryptAsync(value) : clientEncryption.Decrypt(value);

        private async ValueTask<BsonBinaryData> ExplicitEncryptAsync(ClientEncryption clientEncryption, EncryptOptions encryptOptions, BsonValue value, bool async) =>
            async? await clientEncryption.EncryptAsync(value, encryptOptions) : clientEncryption.Encrypt(value, encryptOptions);

        private void ShouldBeArgumentNullException(Exception ex, string expectedParamName) => ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be(expectedParamName);
    }

    internal static class ClientEncryptionReflector
    {
        public static object _cryptClient(this ClientEncryption clientEncryption)
        {
            return Reflector.GetFieldValue(clientEncryption, nameof(_cryptClient));
        }

        public static ExplicitEncryptionLibMongoCryptController _libMongoCryptController(this ClientEncryption clientEncryption)
        {
            return (ExplicitEncryptionLibMongoCryptController)Reflector.GetFieldValue(clientEncryption, nameof(_libMongoCryptController));
        }
    }
}
