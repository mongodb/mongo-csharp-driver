/* Copyright 2010-present MongoDB Inc.
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
using System.Security.Authentication;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Configuration;
using MongoDB.Driver.Encryption;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Configuration;

public class AutoEncryptionBuilderTests
{
    private static readonly CollectionNamespace __keyVaultNamespace = CollectionNamespace.FromFullName("encryption.__keyVault");
    private static readonly CollectionNamespace __collectionNamespace = CollectionNamespace.FromFullName("medical.patients");

    [Fact]
    public void ToAutoEncryptionOptions_should_return_null_when_not_configured()
    {
        var subject = new AutoEncryptionBuilder();

        subject.ToAutoEncryptionOptions().Should().BeNull();
    }

    [Fact]
    public void ToAutoEncryptionOptions_should_throw_when_KeyVaultNamespace_is_missing()
    {
        var subject = new AutoEncryptionBuilder();
        subject.RegisterLocalKmsProvider(new byte[96]);

        var exception = Record.Exception(() => subject.ToAutoEncryptionOptions());

        exception.Should().BeOfType<InvalidOperationException>()
            .Subject.Message.Should().Contain(nameof(AutoEncryptionBuilder.KeyVaultNamespace));
    }

    [Fact]
    public void ToAutoEncryptionOptions_should_throw_when_no_kms_provider_is_registered()
    {
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace };

        var exception = Record.Exception(() => subject.ToAutoEncryptionOptions());

        exception.Should().BeOfType<InvalidOperationException>()
            .Subject.Message.Should().Contain("KMS provider");
    }

    [Fact]
    public void ToAutoEncryptionOptions_should_map_all_values()
    {
        var keyVaultClient = new MongoClient();
        var subject = new AutoEncryptionBuilder
        {
            BypassAutoEncryption = true,
            BypassQueryAnalysis = true,
            ExtraOptions = new Dictionary<string, object> { { "cryptSharedLibPath", "/lib/mongo_crypt_v1.so" } },
            KeyExpiration = TimeSpan.FromMinutes(5),
            KeyVaultClient = keyVaultClient,
            KeyVaultNamespace = __keyVaultNamespace
        };
        var tls = new KmsTlsOptions { CheckCertificateRevocation = true, AllowedProtocols = SslProtocols.Tls12 };
        subject.RegisterKmsProvider("kmip", new Dictionary<string, object> { { "endpoint", "kmip.example.com" } }, tls);
        subject.RegisterSchema(__collectionNamespace, new BsonDocument("bsonType", "object"));

        var result = subject.ToAutoEncryptionOptions();

        result.BypassAutoEncryption.Should().BeTrue();
        result.BypassQueryAnalysis.Should().BeTrue();
        result.ExtraOptions.Should().Equal(new Dictionary<string, object> { { "cryptSharedLibPath", "/lib/mongo_crypt_v1.so" } });
        result.KeyExpiration.Should().Be(TimeSpan.FromMinutes(5));
        result.KeyVaultClient.Should().BeSameAs(keyVaultClient);
        result.KeyVaultNamespace.Should().Be(__keyVaultNamespace);
        result.KmsProviders["kmip"].Should().Equal(new Dictionary<string, object> { { "endpoint", "kmip.example.com" } });
        result.TlsOptions["kmip"].CheckCertificateRevocation.Should().BeTrue();
        result.TlsOptions["kmip"].EnabledSslProtocols.Should().Be(SslProtocols.Tls12);
        result.SchemaMap[__collectionNamespace.FullName].Should().Be(new BsonDocument("bsonType", "object"));
        result.EncryptedFieldsMap.Should().BeNull();
    }

    [Fact]
    public void Register_methods_should_not_be_affected_by_later_changes_to_the_arguments()
    {
        var key = new byte[96];
        var kmsOptions = new Dictionary<string, object> { { "key", key } };
        var tls = new KmsTlsOptions();
        var schema = new BsonDocument("bsonType", "object");
        var extraOptions = new Dictionary<string, object> { { "cryptSharedLibPath", "/a" } };
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace, ExtraOptions = extraOptions };
        subject.RegisterKmsProvider("local", kmsOptions, tls);
        subject.RegisterSchema(__collectionNamespace, schema);

        key[0] = 1;
        kmsOptions["key"] = "not a byte array";
        kmsOptions.Add("extra", 42);
        tls.CheckCertificateRevocation = true;
        schema.Add("required", new BsonArray { "ssn" });
        extraOptions["cryptSharedLibPath"] = "/b";
        var result = subject.ToAutoEncryptionOptions();

        var registeredKey = result.KmsProviders["local"].Should().ContainSingle().Which.Value.Should().BeOfType<byte[]>().Subject;
        registeredKey.Should().OnlyContain(b => b == 0);
        result.TlsOptions["local"].CheckCertificateRevocation.Should().BeFalse();
        result.SchemaMap[__collectionNamespace.FullName].Should().Be(new BsonDocument("bsonType", "object"));
        result.ExtraOptions["cryptSharedLibPath"].Should().Be("/a");
    }

    [Fact]
    public void ToAutoEncryptionOptions_should_not_be_affected_by_later_changes_to_the_builder()
    {
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace };
        subject.RegisterLocalKmsProvider(new byte[96]);
        var result = subject.ToAutoEncryptionOptions();

        subject.RegisterAwsKmsProvider();
        subject.RegisterKmsProvider("kmip", new Dictionary<string, object> { { "endpoint", "kmip.example.com" } }, new KmsTlsOptions());
        subject.RegisterSchema(__collectionNamespace, new BsonDocument("bsonType", "object"));
        subject.RegisterEncryptedFields(CollectionNamespace.FromFullName("medical.visits"), new BsonDocument("fields", new BsonArray()));

        result.KmsProviders.Keys.Should().Equal("local");
        result.TlsOptions.Should().BeEmpty();
        result.SchemaMap.Should().BeNull();
        result.EncryptedFieldsMap.Should().BeNull();
    }

    [Fact]
    public void ToAutoEncryptionOptions_should_not_share_tls_settings_between_results()
    {
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace };
        subject.RegisterKmsProvider("kmip", new Dictionary<string, object> { { "endpoint", "kmip.example.com" } }, new KmsTlsOptions());

        var result1 = subject.ToAutoEncryptionOptions();
        result1.TlsOptions["kmip"].CheckCertificateRevocation = true;
        var result2 = subject.ToAutoEncryptionOptions();

        result2.TlsOptions["kmip"].CheckCertificateRevocation.Should().BeFalse();
    }

    [Fact]
    public void ToAutoEncryptionOptions_should_use_defaults_for_options_that_are_not_set()
    {
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace };
        subject.RegisterLocalKmsProvider(new byte[96]);

        var result = subject.ToAutoEncryptionOptions();

        result.BypassAutoEncryption.Should().BeFalse();
        result.BypassQueryAnalysis.Should().NotHaveValue();
        result.ExtraOptions.Should().BeNull();
        result.KeyExpiration.Should().NotHaveValue();
        result.KeyVaultClient.Should().BeNull();
        result.KmsConnector.Should().BeNull();
        result.SchemaMap.Should().BeNull();
        result.EncryptedFieldsMap.Should().BeNull();
        result.TlsOptions.Should().BeEmpty();
    }

    [Fact]
    public void ToAutoEncryptionOptions_should_map_encrypted_fields()
    {
        var encryptedFields = new BsonDocument("fields", new BsonArray { new BsonDocument("path", "ssn") });
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace };
        subject.RegisterLocalKmsProvider(new byte[96]);
        subject.RegisterEncryptedFields(__collectionNamespace, encryptedFields);

        var result = subject.ToAutoEncryptionOptions();

        result.EncryptedFieldsMap[__collectionNamespace.FullName].Should().Be(encryptedFields);
        result.SchemaMap.Should().BeNull();
    }

    [Fact]
    public void ToAutoEncryptionOptions_should_map_the_KmsConnector()
    {
        var kmsConnector = Mock.Of<IKmsConnector>();
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace, KmsConnector = kmsConnector };
        subject.RegisterLocalKmsProvider(new byte[96]);

        var result = subject.ToAutoEncryptionOptions();

        result.KmsConnector.Should().BeSameAs(kmsConnector);
    }

    [Fact]
    public void ToAutoEncryptionOptions_should_keep_a_zero_KeyExpiration()
    {
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace, KeyExpiration = TimeSpan.Zero };
        subject.RegisterLocalKmsProvider(new byte[96]);

        var result = subject.ToAutoEncryptionOptions();

        result.KeyExpiration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ToAutoEncryptionOptions_should_throw_when_only_a_schema_is_registered()
    {
        var subject = new AutoEncryptionBuilder();
        subject.RegisterSchema(__collectionNamespace, new BsonDocument("bsonType", "object"));

        var exception = Record.Exception(() => subject.ToAutoEncryptionOptions());

        exception.Should().BeOfType<InvalidOperationException>()
            .Subject.Message.Should().Contain(nameof(AutoEncryptionBuilder.KeyVaultNamespace));
    }

    [Fact]
    public void RegisterEncryptedFields_should_not_be_affected_by_later_changes_to_the_argument()
    {
        var encryptedFields = new BsonDocument("fields", new BsonArray());
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace };
        subject.RegisterLocalKmsProvider(new byte[96]);
        subject.RegisterEncryptedFields(__collectionNamespace, encryptedFields);

        encryptedFields["fields"].AsBsonArray.Add("added");
        var result = subject.ToAutoEncryptionOptions();

        result.EncryptedFieldsMap[__collectionNamespace.FullName].Should().Be(new BsonDocument("fields", new BsonArray()));
    }

    [Fact]
    public void RegisterKmsProvider_should_return_the_same_builder()
    {
        var subject = new AutoEncryptionBuilder();

        var result = subject.RegisterKmsProvider("local", new Dictionary<string, object> { { "key", new byte[96] } });

        result.Should().BeSameAs(subject);
    }

    [Fact]
    public void RegisterKmsProvider_should_accept_an_empty_dictionary_to_request_on_demand_credentials()
    {
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace };

        subject.RegisterKmsProvider("aws", new Dictionary<string, object>());

        subject.ToAutoEncryptionOptions().KmsProviders["aws"].Should().BeEmpty();
    }

    [Fact]
    public void RegisterKmsProvider_should_throw_when_the_name_is_already_registered()
    {
        var subject = new AutoEncryptionBuilder();
        subject.RegisterKmsProvider("local", new Dictionary<string, object> { { "key", new byte[96] } });

        var exception = Record.Exception(() => subject.RegisterKmsProvider("local", new Dictionary<string, object> { { "key", new byte[96] } }));

        exception.Should().BeOfType<InvalidOperationException>()
            .Subject.Message.Should().Contain("local");
    }

    [Fact]
    public void RegisterKmsProvider_should_allow_the_same_options_under_different_names()
    {
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace };
        var options = new Dictionary<string, object> { { "key", new byte[96] } };

        subject.RegisterKmsProvider("local", options).RegisterKmsProvider("local:name1", options);

        subject.ToAutoEncryptionOptions().KmsProviders.Keys.Should().BeEquivalentTo("local", "local:name1");
    }

    [Fact]
    public void RegisterKmsProvider_should_throw_when_the_name_is_null()
    {
        var subject = new AutoEncryptionBuilder();

        var exception = Record.Exception(() => subject.RegisterKmsProvider(null, new Dictionary<string, object>()));

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("name");
    }

    [Fact]
    public void RegisterKmsProvider_should_throw_when_the_name_is_empty()
    {
        var subject = new AutoEncryptionBuilder();

        var exception = Record.Exception(() => subject.RegisterKmsProvider("", new Dictionary<string, object>()));

        exception.Should().BeOfType<ArgumentException>()
            .Subject.ParamName.Should().Be("name");
    }

    [Fact]
    public void RegisterKmsProvider_should_throw_when_the_options_are_null()
    {
        var subject = new AutoEncryptionBuilder();

        var exception = Record.Exception(() => subject.RegisterKmsProvider("local", null));

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("options");
    }

    [Fact]
    public void RegisterKmsProvider_should_throw_when_an_option_value_is_null()
    {
        var subject = new AutoEncryptionBuilder();

        var exception = Record.Exception(() => subject.RegisterKmsProvider("local", new Dictionary<string, object> { { "key", null } }));

        exception.Should().BeOfType<ArgumentNullException>();
    }

    [Theory]
    [InlineData(42)]
    [InlineData(true)]
    public void RegisterKmsProvider_should_throw_when_an_option_value_is_neither_a_string_nor_a_byte_array(object value)
    {
        var subject = new AutoEncryptionBuilder();

        var exception = Record.Exception(() => subject.RegisterKmsProvider("local", new Dictionary<string, object> { { "key", value } }));

        exception.Should().BeOfType<ArgumentException>()
            .Subject.ParamName.Should().Be("options");
    }

    [Fact]
    public void RegisterKmsProvider_should_not_register_the_provider_when_the_options_are_invalid()
    {
        var subject = new AutoEncryptionBuilder();
        Record.Exception(() => subject.RegisterKmsProvider("local", new Dictionary<string, object> { { "key", 42 } }));

        var exception = Record.Exception(() => subject.RegisterKmsProvider("local", new Dictionary<string, object> { { "key", new byte[96] } }));

        exception.Should().BeNull();
    }

    [Fact]
    public void RegisterSchema_should_return_the_same_builder()
    {
        var subject = new AutoEncryptionBuilder();

        var result = subject.RegisterSchema(__collectionNamespace, new BsonDocument());

        result.Should().BeSameAs(subject);
    }

    [Fact]
    public void RegisterEncryptedFields_should_return_the_same_builder()
    {
        var subject = new AutoEncryptionBuilder();

        var result = subject.RegisterEncryptedFields(__collectionNamespace, new BsonDocument());

        result.Should().BeSameAs(subject);
    }

    [Fact]
    public void RegisterSchema_should_allow_different_collections()
    {
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace };
        subject.RegisterLocalKmsProvider(new byte[96]);

        subject
            .RegisterSchema(__collectionNamespace, new BsonDocument("a", 1))
            .RegisterSchema(CollectionNamespace.FromFullName("medical.visits"), new BsonDocument("b", 2));

        subject.ToAutoEncryptionOptions().SchemaMap.Keys.Should().BeEquivalentTo("medical.patients", "medical.visits");
    }

    [Fact]
    public void RegisterSchema_should_throw_when_the_collection_already_has_a_schema()
    {
        var subject = new AutoEncryptionBuilder();
        subject.RegisterSchema(__collectionNamespace, new BsonDocument());

        var exception = Record.Exception(() => subject.RegisterSchema(__collectionNamespace, new BsonDocument()));

        exception.Should().BeOfType<InvalidOperationException>()
            .Subject.Message.Should().Contain(__collectionNamespace.FullName);
    }

    [Fact]
    public void RegisterSchema_should_throw_when_the_collection_has_encrypted_fields()
    {
        var subject = new AutoEncryptionBuilder();
        subject.RegisterEncryptedFields(__collectionNamespace, new BsonDocument());

        var exception = Record.Exception(() => subject.RegisterSchema(__collectionNamespace, new BsonDocument()));

        exception.Should().BeOfType<InvalidOperationException>()
            .Subject.Message.Should().Contain("cannot have both");
    }

    [Fact]
    public void RegisterEncryptedFields_should_throw_when_the_collection_already_has_encrypted_fields()
    {
        var subject = new AutoEncryptionBuilder();
        subject.RegisterEncryptedFields(__collectionNamespace, new BsonDocument());

        var exception = Record.Exception(() => subject.RegisterEncryptedFields(__collectionNamespace, new BsonDocument()));

        exception.Should().BeOfType<InvalidOperationException>()
            .Subject.Message.Should().Contain(__collectionNamespace.FullName);
    }

    [Fact]
    public void RegisterEncryptedFields_should_throw_when_the_collection_has_a_schema()
    {
        var subject = new AutoEncryptionBuilder();
        subject.RegisterSchema(__collectionNamespace, new BsonDocument());

        var exception = Record.Exception(() => subject.RegisterEncryptedFields(__collectionNamespace, new BsonDocument()));

        exception.Should().BeOfType<InvalidOperationException>()
            .Subject.Message.Should().Contain("cannot have both");
    }

    [Fact]
    public void RegisterSchema_and_RegisterEncryptedFields_should_throw_for_null_arguments()
    {
        var subject = new AutoEncryptionBuilder();

        Record.Exception(() => subject.RegisterSchema(null, new BsonDocument()))
            .Should().BeOfType<ArgumentNullException>().Subject.ParamName.Should().Be("collectionNamespace");
        Record.Exception(() => subject.RegisterSchema(__collectionNamespace, null))
            .Should().BeOfType<ArgumentNullException>().Subject.ParamName.Should().Be("schema");
        Record.Exception(() => subject.RegisterEncryptedFields(null, new BsonDocument()))
            .Should().BeOfType<ArgumentNullException>().Subject.ParamName.Should().Be("collectionNamespace");
        Record.Exception(() => subject.RegisterEncryptedFields(__collectionNamespace, null))
            .Should().BeOfType<ArgumentNullException>().Subject.ParamName.Should().Be("encryptedFields");
    }
}
