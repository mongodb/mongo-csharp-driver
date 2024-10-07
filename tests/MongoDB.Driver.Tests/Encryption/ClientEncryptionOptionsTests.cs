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
using FluentAssertions;
using MongoDB.Driver.Encryption;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Encryption
{
    public class ClientEncryptionOptionsTests
    {
        private static CollectionNamespace __keyVaultNamespace = CollectionNamespace.FromFullName("db.coll");

        [Fact]
        public void Ctor_should_throw_when_keyVaultClient_is_null()
        {
            Record.Exception(() => new ClientEncryptionOptions(keyVaultClient: null, keyVaultNamespace: __keyVaultNamespace, Mock.Of<IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>>()))
                .Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName
                .Should().Be("keyVaultClient");
        }

        [Fact]
        public void Ctor_should_throw_when_keyVaultNamespace_is_null()
        {
            Record.Exception(() => new ClientEncryptionOptions(Mock.Of<IMongoClient>(), keyVaultNamespace: null, Mock.Of<IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>>()))
                .Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName
                .Should().Be("keyVaultNamespace");
        }

        [Fact]
        public void Ctor_should_throw_when_kmsProviders_is_null()
        {
            Record.Exception(() => new ClientEncryptionOptions(Mock.Of<IMongoClient>(), __keyVaultNamespace, kmsProviders: null))
                .Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName
                .Should().Be("kmsProviders");
        }

        [Theory]
        [InlineData(typeof(byte[]), false)]
        [InlineData(typeof(string), false)]
        [InlineData(typeof(int), true)]
        public void Ctor_should_handle_kmsProviderOptions_type_correctly(Type optionType, bool shouldFail)
        {
            var exception = Record.Exception(() => new ClientEncryptionOptions(
                Mock.Of<IMongoClient>(),
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
                Record.Exception(() => new ClientEncryptionOptions(
                    keyVaultClient: Mock.Of<IMongoClient>(),
                    keyVaultNamespace: __keyVaultNamespace,
                    kmsProviders: GetKmsProviders(),
                    tlsOptions: new Dictionary<string, SslSettings>() { { "test", settings } }))
                .Should().BeOfType<ArgumentException>()
                .Subject.Message
                .Should().Be("Insecure TLS options prohibited.");
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
