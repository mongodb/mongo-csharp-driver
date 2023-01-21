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
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Encryption
{
    public class AutoEncryptionOptionsTests
    {
        private static CollectionNamespace __keyVaultNamespace = CollectionNamespace.FromFullName("db.coll");

        [Fact]
        public void constructor_should_throw_when_keyVaultNamespace_is_null()
        {
            Record.Exception(() => new AutoEncryptionOptions(keyVaultNamespace: null, Mock.Of<IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>>()))
                .Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName
                .Should().Be("keyVaultNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_kmsProviders_is_null()
        {
            Record.Exception(() => new AutoEncryptionOptions(__keyVaultNamespace, kmsProviders: null))
                .Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName
                .Should().Be("kmsProviders");
        }

        [Fact]
        public void constructor_should_handle_empty_kmsProviderOptions_correctly()
        {
            var emptyAwsKmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>()
            {
                { "aws", new Dictionary<string, object>() }
            };
            _ = new AutoEncryptionOptions(
                keyVaultNamespace: __keyVaultNamespace,
                kmsProviders: emptyAwsKmsProviders);
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
        [InlineData("cryptSharedLibPath", "path", false)]
        [InlineData("cryptSharedLibPath", 1, true)]
        [InlineData("cryptSharedLibRequired", true, false)]
        [InlineData("cryptSharedLibRequired", 1, true)]
        [InlineData("test", "test", true)]
        public void constructor_should_handle_extraOptions_correctly(string key, object value, bool shouldFail)
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
        public void constructor_should_handle_kmsProviderOptions_type_correctly(Type optionType, bool shouldFail)
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

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("db.test", null, null)]
        [InlineData(null, "db.test", null)]
        [InlineData("db.test", "db.test1", null)]
        [InlineData("db.test", "db.test", "db.test")]
        [InlineData("db.test", "db.test;db.test1", "db.test")]
        [InlineData("db.test;db.test1", "db.test;db.test1", "db.test, db.test1")]
        [InlineData("db.test1;db.test", "db.test;db.test1", "db.test1, db.test")]
        public void constructor_should_handle_schemaMap_and_encryptedFieldsMap_type_correctly(string schemaMapKey, string encryptedFieldsMapKey, string errorMessage)
        {
            var schemaMap = CreateMap(schemaMapKey);
            var encryptedFieldsMap = CreateMap(encryptedFieldsMapKey);

            var exception = Record.Exception(
                () => new AutoEncryptionOptions(
                    keyVaultNamespace: __keyVaultNamespace,
                    kmsProviders: GetKmsProviders(),
                    schemaMap: schemaMap,
                    encryptedFieldsMap: encryptedFieldsMap));

            if (errorMessage != null)
            {
                exception.Should().BeOfType<ArgumentException>().Which.Message.Should().Contain($"SchemaMap and EncryptedFieldsMap cannot both contain the same collections: {errorMessage}.");
            }
            else
            {
                exception.Should().BeNull();
            }

            Dictionary<string, BsonDocument> CreateMap(string key)
            {
                var dummyMapValue = new BsonDocument();
                return key?.Split(';')?.ToDictionary(k => k, k => dummyMapValue) ?? new Dictionary<string, BsonDocument>();
            }
        }

        [Fact]
        public void constructor_should_handle_tlsSettings_correctly()
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
            // collectionNamespace
            Assert(
                CreateSubject(collectionNamespace: __keyVaultNamespace),
                CreateSubject(collectionNamespace: __keyVaultNamespace),
                expectedResult: true);

            Assert(
                CreateSubject(collectionNamespace: __keyVaultNamespace),
                CreateSubject(collectionNamespace: CollectionNamespace.FromFullName("db.temp")),
                expectedResult: false);

            // extraOptions
            Assert(
                CreateSubject(extraOptions: GetDictionary<object>(("mongocryptdURI", "key"))),
                CreateSubject(extraOptions: GetDictionary<object>(("mongocryptdSpawnPath", "key"))),
                expectedResult: false);

            Assert(
                CreateSubject(extraOptions: GetDictionary<object>(("mongocryptdURI", "key"))),
                CreateSubject(extraOptions: GetDictionary<object>(("mongocryptdURI", "key1"))),
                expectedResult: false);

            Assert(
                CreateSubject(extraOptions: GetDictionary<object>(("mongocryptdSpawnArgs", "key1"))),
                CreateSubject(extraOptions: GetDictionary<object>(("mongocryptdSpawnArgs", "key1"), ("mongocryptdSpawnPath", "key12"))),
                expectedResult: false);

            Assert(
                CreateSubject(extraOptions: GetDictionary<object>(("mongocryptdSpawnArgs", "key1"), ("mongocryptdSpawnPath", "key12"))),
                CreateSubject(extraOptions: GetDictionary<object>(("mongocryptdSpawnArgs", "key1"), ("mongocryptdSpawnPath", "key12"))),
                expectedResult: true);

            // schemaMap
            Assert(
                CreateSubject(schemaMap: GetDictionary(("coll1", new BsonDocument()))),
                CreateSubject(schemaMap: GetDictionary(("coll2", new BsonDocument()))),
                expectedResult: false);

            Assert(
                CreateSubject(schemaMap: GetDictionary(("coll1", new BsonDocument()))),
                CreateSubject(schemaMap: GetDictionary(("coll1", new BsonDocument("key", "value")))),
                expectedResult: false);

            Assert(
                CreateSubject(schemaMap: GetDictionary(("coll1", new BsonDocument()))),
                CreateSubject(schemaMap: GetDictionary(("coll1", new BsonDocument()), ("coll2", new BsonDocument("key", "value")))),
                expectedResult: false);

            Assert(
                CreateSubject(schemaMap: GetDictionary(("coll1", new BsonDocument()), ("coll2", new BsonDocument("key", "value")))),
                CreateSubject(schemaMap: GetDictionary(("coll1", new BsonDocument()), ("coll2", new BsonDocument("key", "value")))),
                expectedResult: true);

            // encryptedFieldsMap
            Assert(
                CreateSubject(encryptedFieldsMap: GetDictionary(("coll1", new BsonDocument()))),
                CreateSubject(encryptedFieldsMap: GetDictionary(("coll2", new BsonDocument()))),
                expectedResult: false);

            Assert(
                CreateSubject(encryptedFieldsMap: GetDictionary(("coll1", new BsonDocument()))),
                CreateSubject(encryptedFieldsMap: GetDictionary(("coll1", new BsonDocument("key", "value")))),
                expectedResult: false);

            Assert(
                CreateSubject(encryptedFieldsMap: GetDictionary(("coll1", new BsonDocument()))),
                CreateSubject(encryptedFieldsMap: GetDictionary(("coll1", new BsonDocument()), ("coll2", new BsonDocument("key", "value")))),
                expectedResult: false);

            Assert(
                CreateSubject(encryptedFieldsMap: GetDictionary(("coll1", new BsonDocument()), ("coll2", new BsonDocument("key", "value")))),
                CreateSubject(encryptedFieldsMap: GetDictionary(("coll1", new BsonDocument()), ("coll2", new BsonDocument("key", "value")))),
                expectedResult: true);

            // bypassQueryAnalysis
            Assert(
                CreateSubject(bypassQueryAnalysis: null),
                CreateSubject(bypassQueryAnalysis: null),
                expectedResult: true);

            Assert(
                CreateSubject(bypassQueryAnalysis: true),
                CreateSubject(bypassQueryAnalysis: true),
                expectedResult: true);

            Assert(
                CreateSubject(bypassQueryAnalysis: false),
                CreateSubject(bypassQueryAnalysis: true),
                expectedResult: false);

            void Assert(AutoEncryptionOptions subject1, AutoEncryptionOptions subject2, bool expectedResult) => subject1.Equals(subject2).Should().Be(expectedResult);
        }

        [Fact]
        public void Equals_with_tls_should_work_correctly()
        {
            var options1 = CreateSubject();
            var options2 = CreateSubject();
            options1.Equals(options2).Should().BeTrue();

            options1 = CreateSubject(tlsOptions: new SslSettings());
            options2 = CreateSubject(tlsOptions: new SslSettings());
            options1.Equals(options2).Should().BeTrue();

            options1 = CreateSubject(tlsOptions: new SslSettings());
            options2 = CreateSubject(tlsOptions: new SslSettings(), collectionNamespace: CollectionNamespace.FromFullName("d.c"));
            options1.Equals(options2).Should().BeFalse();

            options1 = CreateSubject(tlsOptions: new SslSettings(), tlsKey: "test1");
            options2 = CreateSubject(tlsOptions: new SslSettings());
            options1.Equals(options2).Should().BeFalse();

            options1 = CreateSubject(tlsOptions: new SslSettings() { EnabledSslProtocols = System.Security.Authentication.SslProtocols.None });
            options2 = CreateSubject(tlsOptions: new SslSettings());
            options1.Equals(options2).Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void ToCryptClientSettings_should_return_expected_result(
            [Values(false, true)] bool cryptSharedLibRequired,
            [Values(false, true)] bool bypassAutoEncryption)
        {
            var subject = CreateSubject(
                extraOptions: new Dictionary<string, object>
                {
                    { "cryptSharedLibPath", "cryptSharedLibPath" },
                    { "cryptSharedLibRequired", cryptSharedLibRequired }
                })
                .With(bypassAutoEncryption: bypassAutoEncryption);

            var result = subject.ToCryptClientSettings();

            result.CryptSharedLibPath.Should().Be("cryptSharedLibPath");
            result.CryptSharedLibSearchPath.Should().Be(bypassAutoEncryption ? null : "$SYSTEM");
            result.IsCryptSharedLibRequired.Should().Be(cryptSharedLibRequired);
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
            var encryptedFieldsMap = new Dictionary<string, BsonDocument>
            {
                {
                    "db.test",
                    BsonDocument.Parse("{ dummy : 'doc' }")
                }
            };

            var subject = new AutoEncryptionOptions(
                keyVaultNamespace: __keyVaultNamespace,
                kmsProviders: kmsProviders,
                bypassAutoEncryption: true,
                bypassQueryAnalysis: false,
                extraOptions: extraOptions,
                schemaMap: schemaMap,
                tlsOptions: tlsOptions,
                encryptedFieldsMap: encryptedFieldsMap);

            var result = subject.ToString();
            result.Should().Be("{ BypassAutoEncryption : True, BypassQueryAnalysis : False, KmsProviders : { \"provider1\" : { \"string\" : \"test\" }, \"provider2\" : { \"binary\" : { \"_t\" : \"System.Byte[]\", \"_v\" : new BinData(0, \"ABEiM0RVZneImaq7zN3u/w==\") } } }, KeyVaultNamespace : \"db.coll\", ExtraOptions : { \"mongocryptdURI\" : \"testURI\" }, SchemaMap : { \"coll1\" : { \"string\" : \"test\" }, \"coll2\" : { \"binary\" : UUID(\"00112233-4455-6677-8899-aabbccddeeff\") } }, TlsOptions : [{ \"local\" : \"<hidden>\" }], EncryptedFieldsMap : { \"db.test\" : { \"dummy\" : \"doc\" } } }");
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

        private AutoEncryptionOptions CreateSubject(
            SslSettings tlsOptions = null,
            string tlsKey = "test",
            CollectionNamespace collectionNamespace = null,
            Dictionary<string, BsonDocument> schemaMap = null,
            Dictionary<string, BsonDocument> encryptedFieldsMap = null,
            Dictionary<string, object> extraOptions = null,
            bool? bypassQueryAnalysis = null)
        {
            var autoEncryptionOptions = new AutoEncryptionOptions(
                bypassQueryAnalysis: bypassQueryAnalysis,
                keyVaultNamespace: collectionNamespace ?? __keyVaultNamespace,
                kmsProviders: GetKmsProviders(),
                schemaMap: schemaMap,
                encryptedFieldsMap: encryptedFieldsMap,
                extraOptions: extraOptions);
            if (tlsOptions != null)
            {
                autoEncryptionOptions = autoEncryptionOptions.With(tlsOptions: new Dictionary<string, SslSettings> { { tlsKey, tlsOptions } });
            }
            return autoEncryptionOptions;
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

        private Dictionary<string, TValue> GetDictionary<TValue>(params (string Key, TValue Document)[] map) => map.ToDictionary(k => k.Key, v => v.Document);
    }
}
