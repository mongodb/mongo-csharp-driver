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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Configuration;
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
        var tlsSettings = new SslSettings { CheckCertificateRevocation = true };
        subject.RegisterKmsProvider("kmip", new Dictionary<string, object> { { "endpoint", "kmip.example.com" } }, tlsSettings);
        subject.RegisterSchema(__collectionNamespace, new BsonDocument("bsonType", "object"));

        var result = subject.ToAutoEncryptionOptions();

        result.BypassAutoEncryption.Should().BeTrue();
        result.BypassQueryAnalysis.Should().BeTrue();
        result.ExtraOptions.Should().Equal(new Dictionary<string, object> { { "cryptSharedLibPath", "/lib/mongo_crypt_v1.so" } });
        result.KeyExpiration.Should().Be(TimeSpan.FromMinutes(5));
        result.KeyVaultClient.Should().BeSameAs(keyVaultClient);
        result.KeyVaultNamespace.Should().Be(__keyVaultNamespace);
        result.KmsProviders["kmip"].Should().Equal(new Dictionary<string, object> { { "endpoint", "kmip.example.com" } });
        result.TlsOptions["kmip"].Should().Be(tlsSettings);
        result.SchemaMap[__collectionNamespace.FullName].Should().Be(new BsonDocument("bsonType", "object"));
        result.EncryptedFieldsMap.Should().BeNull();
    }

    [Fact]
    public void Register_methods_should_not_be_affected_by_later_changes_to_the_arguments()
    {
        var key = new byte[96];
        var kmsOptions = new Dictionary<string, object> { { "key", key } };
        var tlsSettings = new SslSettings();
        var schema = new BsonDocument("bsonType", "object");
        var extraOptions = new Dictionary<string, object> { { "cryptSharedLibPath", "/a" } };
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace, ExtraOptions = extraOptions };
        subject.RegisterKmsProvider("local", kmsOptions, tlsSettings);
        subject.RegisterSchema(__collectionNamespace, schema);

        key[0] = 1;
        kmsOptions["key"] = "not a byte array";
        kmsOptions.Add("extra", 42);
        tlsSettings.CheckCertificateRevocation = true;
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
        subject.RegisterKmsProvider("kmip", new Dictionary<string, object> { { "endpoint", "kmip.example.com" } }, new SslSettings());
        subject.RegisterSchema(__collectionNamespace, new BsonDocument("bsonType", "object"));
        subject.RegisterEncryptedFields(CollectionNamespace.FromFullName("medical.visits"), new BsonDocument("fields", new BsonArray()));

        result.KmsProviders.Keys.Should().Equal("local");
        result.TlsOptions.Should().BeEmpty();
        result.SchemaMap.Should().BeNull();
        result.EncryptedFieldsMap.Should().BeNull();
    }

    [Fact]
    public void RegisterKmsProvider_should_freeze_the_registered_tls_settings()
    {
        var subject = new AutoEncryptionBuilder { KeyVaultNamespace = __keyVaultNamespace };
        subject.RegisterKmsProvider("kmip", new Dictionary<string, object> { { "endpoint", "kmip.example.com" } }, new SslSettings());

        var result = subject.ToAutoEncryptionOptions();

        var exception = Record.Exception(() => result.TlsOptions["kmip"].CheckCertificateRevocation = true);

        exception.Should().BeOfType<InvalidOperationException>();
    }
}
