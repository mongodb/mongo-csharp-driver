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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.Tests.Specifications.client_side_encryption;
using MongoDB.Libmongocrypt;
using Xunit;

namespace MongoDB.Driver.Tests.Encryption
{
    [Trait("Category", "CSFLE")]
    public class ClientEncryptionTests
    {
        #region static
        private static readonly CollectionNamespace __keyVaultCollectionNamespace = CollectionNamespace.FromFullName("datakeys.keyvault");
        #endregion

        [SkippableFact]
        public async Task AddAlternateKeyName_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var guid = new Guid();

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentException(Record.Exception(() => subject.AddAlternateKeyName(id: guid, alternateKeyName: null)), expectedParamName: "alternateKeyName");
                ShouldBeArgumentException(await Record.ExceptionAsync(() => subject.AddAlternateKeyNameAsync(id: guid, alternateKeyName: null)), expectedParamName: "alternateKeyName");
            }
        }

        [SkippableFact]
        public async Task CreateDataKey_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentException(Record.Exception(() => subject.CreateDataKey(kmsProvider: null, new DataKeyOptions())), expectedParamName: "kmsProvider");
                ShouldBeArgumentException(await Record.ExceptionAsync(() => subject.CreateDataKeyAsync(kmsProvider: null, new DataKeyOptions())), expectedParamName: "kmsProvider");

                _ = subject.CreateDataKey(kmsProvider: "local", dataKeyOptions: null);
                _ = await subject.CreateDataKeyAsync(kmsProvider: "local", dataKeyOptions: null);
            }
        }


        [SkippableFact]
        public void CryptClient_should_be_initialized()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                subject._cryptClient().Should().NotBeNull();
                subject._libMongoCryptController().Should().NotBeNull();
            }
        }

        [SkippableFact]
        public async Task Decrypt_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentException(Record.Exception(() => subject.Decrypt(value: null)), expectedParamName: "encryptedValue");
                ShouldBeArgumentException(await Record.ExceptionAsync(() => subject.DecryptAsync(value: null)), expectedParamName: "encryptedValue");
            }
        }

        [SkippableFact]
        public async Task Encrypt_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentException(Record.Exception(() => subject.Encrypt(value: "test", encryptOptions: null)), expectedParamName: "encryptOptions");
                ShouldBeArgumentException(await Record.ExceptionAsync(() => subject.EncryptAsync(value: "test", encryptOptions: null)), expectedParamName: "encryptOptions");

                ShouldBeArgumentException(Record.Exception(() => subject.Encrypt(value: null, new EncryptOptions(EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic, alternateKeyName: "test"))), expectedParamName: "value");
                ShouldBeArgumentException(await Record.ExceptionAsync(() => subject.EncryptAsync(value: null, new EncryptOptions(EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic, alternateKeyName: "test"))), expectedParamName: "value");
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public async Task Encryption_should_use_correct_binarySubType([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                var keyId = subject.CreateDataKey("local", new DataKeyOptions());

                var value = "hello";

                var encrypted = await ExplicitEncrypt(subject, new EncryptOptions(EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic, keyId: keyId), value, async);
                encrypted.SubType.Should().Be(BsonBinarySubType.Encrypted);

                var decrypted = await ExplicitDecrypt(subject, encrypted, async);

                decrypted.Should().Be(BsonValue.Create(value));
            }
        }

        [SkippableFact]
        public async Task GetKeyByAlternateKeyName_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentException(Record.Exception(() => subject.GetKeyByAlternateKeyName(alternateKeyName: null)), expectedParamName: "alternateKeyName");
                ShouldBeArgumentException(await Record.ExceptionAsync(() => subject.GetKeyByAlternateKeyNameAsync(alternateKeyName: null)), expectedParamName: "alternateKeyName");
            }
        }

        [SkippableFact]
        public async Task RemoveAlternateKeyName_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var guid = new Guid();

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentException(Record.Exception(() => subject.RemoveAlternateKeyName(id: guid, alternateKeyName: null)), expectedParamName: "alternateKeyName");
                ShouldBeArgumentException(await Record.ExceptionAsync(() => subject.RemoveAlternateKeyNameAsync(id: guid, alternateKeyName: null)), expectedParamName: "alternateKeyName");
            }
        }

        [SkippableFact]
        public async Task RewrapManyDataKey_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                ShouldBeArgumentException(Record.Exception(() => subject.RewrapManyDataKey(filter: null, options: new RewrapManyDataKeyOptions("local"))), expectedParamName: "filter");
                ShouldBeArgumentException(await Record.ExceptionAsync(() => subject.RewrapManyDataKeyAsync(filter: null, options: new RewrapManyDataKeyOptions("local"))), expectedParamName: "filter");

                _ = subject.RewrapManyDataKey(filter: FilterDefinition<BsonDocument>.Empty, options: null);
                _ = await subject.RewrapManyDataKeyAsync(filter: FilterDefinition<BsonDocument>.Empty, options: null);
            }
        }

        // private methods
        private ClientEncryption CreateSubject()
        {
            var clientEncryptionOptions = new ClientEncryptionOptions(
                DriverTestConfiguration.Client,
                __keyVaultCollectionNamespace,
                kmsProviders: EncryptionTestHelper.GetKmsProviders(filter: "local"));

            return new ClientEncryption(clientEncryptionOptions);
        }

        private async ValueTask<BsonValue> ExplicitDecrypt(ClientEncryption clientEncryption, BsonBinaryData value, bool async) =>
            async ? await clientEncryption.DecryptAsync(value) : clientEncryption.Decrypt(value);

        private async ValueTask<BsonBinaryData> ExplicitEncrypt(ClientEncryption clientEncryption, EncryptOptions encryptOptions, BsonValue value, bool async) =>
            async? await clientEncryption.EncryptAsync(value, encryptOptions) : clientEncryption.Encrypt(value, encryptOptions);

        private void ShouldBeArgumentException(Exception ex, string expectedParamName) => ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be(expectedParamName);
    }

    internal static class ClientEncryptionReflector
    {
        public static CryptClient _cryptClient(this ClientEncryption clientEncryption)
        {
            return (CryptClient)Reflector.GetFieldValue(clientEncryption, nameof(_cryptClient));
        }

        public static ExplicitEncryptionLibMongoCryptController _libMongoCryptController(this ClientEncryption clientEncryption)
        {
            return (ExplicitEncryptionLibMongoCryptController)Reflector.GetFieldValue(clientEncryption, nameof(_libMongoCryptController));
        }
    }
}
