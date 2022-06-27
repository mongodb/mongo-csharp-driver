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
using System.Threading;
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
        public void AddAlternateKeyName_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var guid = new Guid();

            using (var subject = CreateSubject())
            {
                Record.Exception(() => subject.AddAlternateKeyName(id: guid, alternateKeyName: null))
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("alternateKeyName");
                Record.Exception(() => subject.AddAlternateKeyNameAsync(id: guid, alternateKeyName: null).GetAwaiter().GetResult())
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("alternateKeyName");
            }
        }

        [SkippableFact]
        public void CreateDataKey_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                Record.Exception(() => subject.CreateDataKey(kmsProvider: null, new DataKeyOptions()))
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("kmsProvider");
                Record.Exception(() => subject.CreateDataKeyAsync(kmsProvider: null, new DataKeyOptions()).GetAwaiter().GetResult())
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("kmsProvider");

                _ = subject.CreateDataKey(kmsProvider: "local", dataKeyOptions: null);
                _ = subject.CreateDataKeyAsync(kmsProvider: "local", dataKeyOptions: null).GetAwaiter().GetResult();
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
        public void Decrypt_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                Record.Exception(() => subject.Decrypt(value: null))
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("encryptedValue");
                Record.Exception(() => subject.DecryptAsync(value: null).GetAwaiter().GetResult())
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("encryptedValue");
            }
        }

        [SkippableFact]
        public void Encrypt_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                Record.Exception(() => subject.Encrypt(value: "test", encryptOptions: null))
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("encryptOptions");
                Record.Exception(() => subject.EncryptAsync(value: "test", encryptOptions: null).GetAwaiter().GetResult())
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("encryptOptions");

                Record.Exception(() => subject.Encrypt(value: null, new EncryptOptions(EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic, alternateKeyName: "test")))
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("value");
                Record.Exception(() => subject.EncryptAsync(value: null, new EncryptOptions(EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic, alternateKeyName: "test")).GetAwaiter().GetResult())
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("value");
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Encryption_should_use_correct_binarySubType([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                var keyId = subject.CreateDataKey("local", new DataKeyOptions());

                var value = "hello";

                var encrypted = ExplicitEncrypt(subject, new EncryptOptions(EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic, keyId: keyId), value, async);
                encrypted.SubType.Should().Be(BsonBinarySubType.Encrypted);

                var decrypted = ExplicitDecrypt(subject, encrypted, async);

                decrypted.Should().Be(BsonValue.Create(value));
            }
        }

        [SkippableFact]
        public void GetKeyByAlternateKeyName_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                Record.Exception(() => subject.GetKeyByAlternateKeyName(alternateKeyName: null))
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("alternateKeyName");
                Record.Exception(() => subject.GetKeyByAlternateKeyNameAsync(alternateKeyName: null).GetAwaiter().GetResult())
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("alternateKeyName");
            }
        }

        [SkippableFact]
        public void RemoveAlternateKeyName_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            var guid = new Guid();

            using (var subject = CreateSubject())
            {
                Record.Exception(() => subject.RemoveAlternateKeyName(id: guid, alternateKeyName: null))
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("alternateKeyName");
                Record.Exception(() => subject.RemoveAlternateKeyNameAsync(id: guid, alternateKeyName: null).GetAwaiter().GetResult())
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("alternateKeyName");
            }
        }

        [SkippableFact]
        public void RewrapManyDataKey_should_correctly_handle_input_arguments()
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);

            using (var subject = CreateSubject())
            {
                Record.Exception(() => subject.RewrapManyDataKey(filter: null, options: new RewrapManyDataKeyOptions("local")))
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("filter");
                Record.Exception(() => subject.RewrapManyDataKeyAsync(filter: null, options: new RewrapManyDataKeyOptions("local")).GetAwaiter().GetResult())
                    .Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("filter");

                _ = subject.RewrapManyDataKey(filter: FilterDefinition<BsonDocument>.Empty, options: null);
                _ = subject.RewrapManyDataKeyAsync(filter: FilterDefinition<BsonDocument>.Empty, options: null).GetAwaiter().GetResult();
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

        private BsonValue ExplicitDecrypt(
            ClientEncryption clientEncryption,
            BsonBinaryData value,
            bool async)
        {
            BsonValue decryptedValue;
            if (async)
            {
                decryptedValue = clientEncryption
                    .DecryptAsync(
                        value,
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                decryptedValue = clientEncryption.Decrypt(
                    value,
                    CancellationToken.None);
            }

            return decryptedValue;
        }

        private BsonBinaryData ExplicitEncrypt(
            ClientEncryption clientEncryption,
            EncryptOptions encryptOptions,
            BsonValue value,
            bool async)
        {
            BsonBinaryData encryptedValue;
            if (async)
            {
                encryptedValue = clientEncryption
                    .EncryptAsync(
                        value,
                        encryptOptions,
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                encryptedValue = clientEncryption.Encrypt(
                    value,
                    encryptOptions,
                    CancellationToken.None);
            }

            return encryptedValue;
        }
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
