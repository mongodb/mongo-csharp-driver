/* Copyright 2020–present MongoDB Inc.
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
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Encryption;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AutoEncryptionOptionsTests
    {
        private static CollectionNamespace __keyVaultNamespace = CollectionNamespace.FromFullName("db.coll");

        [Fact]
        public void Ctor_should_throw_when_keyVaultNamespace_is_null()
        {
            Record.Exception(() => new AutoEncryptionOptions(keyVaultNamespace: null, Mock.Of<IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>>()))
                .Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName
                .Should().Be("keyVaultNamespace");
        }

        [Fact]
        public void Ctor_should_throw_when_kmsProviders_is_null()
        {
            Record.Exception(() => new AutoEncryptionOptions(__keyVaultNamespace, kmsProviders: null))
                .Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName
                .Should().Be("kmsProviders");
        }

        [Theory]
        [InlineData("mongocryptdURI", "test", false)]
        [InlineData("mongocryptdURI", 1, true)]
        [InlineData("mongocryptdBypassSpawn", true, false)]
        [InlineData("mongocryptdBypassSpawn", 1, true)]
        [InlineData("mongocryptdSpawnPath", "test", false)]
        [InlineData("mongocryptdSpawnPath", 1, true)]
        [InlineData("mongocryptdSpawnArgs", "test", false)]
        [InlineData("mongocryptdSpawnArgs", new[] { "test" }, false)]
        [InlineData("mongocryptdSpawnArgs", 1, true)]
        [InlineData("test", "test", true)]
        public void Ctor_should_handle_extraOptions_correctly(string key, object value, bool shouldFail)
        {
            IReadOnlyDictionary<string, object> extraOptions = new Dictionary<string, object>()
            {
                { key, value }
            };

            var exception = Record.Exception(() => new AutoEncryptionOptions(
                keyVaultNamespace: __keyVaultNamespace,
                kmsProviders: GetKmsProviders(),
                extraOptions: Optional.Create(extraOptions)));

            if (shouldFail)
            {
                exception.Should().NotBeNull();
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Theory]
        [InlineData(typeof(byte[]), false)]
        [InlineData(typeof(string), false)]
        [InlineData(typeof(int), true)]
        public void Ctor_should_handle_kmsProviderOptions_type_correctly(Type optionType, bool shouldFail)
        {
            var exception = Record.Exception(() => new AutoEncryptionOptions(
                keyVaultNamespace: __keyVaultNamespace,
                kmsProviders: GetKmsProviders(optionType)));

            if (shouldFail)
            {
                exception.Should().NotBeNull();
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Fact]
        public void Ctor_should_handle_tlsSettings_correctly()
        {
            NegativeTestCase(new SslSettings() { ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback((a, b, c, d) => true) });

            void NegativeTestCase(SslSettings settings) =>
                Record.Exception(() => new AutoEncryptionOptions(
                    keyVaultNamespace: __keyVaultNamespace,
                    kmsProviders: GetKmsProviders(),
                    tlsOptions: new Dictionary<string, SslSettings>() { { "test", settings } }))
                .Should().BeOfType<ArgumentException>()
                .Subject.Message
                .Should().Be("Insecure TLS options prohibited.");
        }

        [Fact]
        public void Equals_should_work_correctly()
        {
            var options1 = CreateAutoEncryptionOptions();
            var options2 = CreateAutoEncryptionOptions();
            options1.Equals(options2).Should().BeTrue();

            options1 = CreateAutoEncryptionOptions(tlsOptions: new SslSettings());
            options2 = CreateAutoEncryptionOptions(tlsOptions: new SslSettings());
            options1.Equals(options2).Should().BeTrue();

            options1 = CreateAutoEncryptionOptions(tlsOptions: new SslSettings());
            options2 = CreateAutoEncryptionOptions(tlsOptions: new SslSettings(), collectionNamespace: CollectionNamespace.FromFullName("d.c"));
            options1.Equals(options2).Should().BeFalse();

            options1 = CreateAutoEncryptionOptions(tlsOptions: new SslSettings(), tlsKey: "test1");
            options2 = CreateAutoEncryptionOptions(tlsOptions: new SslSettings());
            options1.Equals(options2).Should().BeFalse();

            options1 = CreateAutoEncryptionOptions(tlsOptions: new SslSettings() { EnabledSslProtocols = System.Security.Authentication.SslProtocols.None });
            options2 = CreateAutoEncryptionOptions(tlsOptions: new SslSettings());
            options1.Equals(options2).Should().BeFalse();

            AutoEncryptionOptions CreateAutoEncryptionOptions(SslSettings tlsOptions = null, string tlsKey = "test", CollectionNamespace collectionNamespace = null)
            {
                var autoEncryptionOptions = new AutoEncryptionOptions(
                    keyVaultNamespace: collectionNamespace ?? __keyVaultNamespace,
                    kmsProviders: GetKmsProviders());
                if (tlsOptions != null)
                {
                    autoEncryptionOptions = autoEncryptionOptions.With(tlsOptions: new Dictionary<string, SslSettings> { { tlsKey, tlsOptions } });
                }
                return autoEncryptionOptions;
            }
        }

        [Fact]
        public void ToString_should_return_expected_result()
        {
            var guid = new Guid("00112233445566778899aabbccddeeff");
            var guidBytes = GuidConverter.ToBytes(guid, GuidRepresentation.Standard);
            var binary = new BsonBinaryData(guidBytes, BsonBinarySubType.UuidStandard);

            var extraOptions = new Dictionary<string, object>()
            {
                { "mongocryptdURI", "testURI" },
            };
            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>()
            {
                { "provider1", new Dictionary<string, object>() { { "string", "test" } } },
                { "provider2", new Dictionary<string, object>() { { "binary", binary.Bytes } } }
            };
            var schemaMap = new Dictionary<string, BsonDocument>()
            {
                { "coll1", new BsonDocument("string", "test") },
                { "coll2", new BsonDocument("binary", binary) },
            };
            var tlsOptions = new Dictionary<string, SslSettings>
            {
                { "local", new SslSettings { ClientCertificates = new [] { Mock.Of<X509Certificate2>() } } }
            };

            var subject = new AutoEncryptionOptions(
                keyVaultNamespace: __keyVaultNamespace,
                kmsProviders: kmsProviders,
                bypassAutoEncryption: true,
                extraOptions: extraOptions,
                schemaMap: schemaMap,
                tlsOptions: tlsOptions);

            var result = subject.ToString();
            result.Should().Be("{ BypassAutoEncryption : True, KmsProviders : { \"provider1\" : { \"string\" : \"test\" }, \"provider2\" : { \"binary\" : { \"_t\" : \"System.Byte[]\", \"_v\" : new BinData(0, \"ABEiM0RVZneImaq7zN3u/w==\") } } }, KeyVaultNamespace : \"db.coll\", ExtraOptions : { \"mongocryptdURI\" : \"testURI\" }, SchemaMap : { \"coll1\" : { \"string\" : \"test\" }, \"coll2\" : { \"binary\" : UUID(\"00112233-4455-6677-8899-aabbccddeeff\") } }, TlsOptions: [{ \"local\" : \"<hidden>\"  }");
        }

        // private methods
        private object CreateInstance(Type type)
        {
            if (type.IsArray)
            {
                return Array.CreateInstance(type.GetElementType(), 0);
            }
            else
            {
                return type == typeof(string) ? string.Empty : Activator.CreateInstance(type);
            }
        }

        private Dictionary<string, IReadOnlyDictionary<string, object>> GetKmsProviders(Type optionType = null)
        {
            var localOptions = new Dictionary<string, object>
            {
                { "key", optionType != null ? CreateInstance(optionType) : new byte []{ 1, 2 } }
            };
            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>()
            {
                { "local", localOptions }
            };
            return kmsProviders;
        }
    }
}
