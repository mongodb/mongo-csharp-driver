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

using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Core.Configuration
{
    public class CryptClientSettingsTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var encryptedFieldsMap = new Dictionary<string, BsonDocument>();
            var schemaMap = new Dictionary<string, BsonDocument>();
            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>();
            var subject = new CryptClientSettings(
                true,
                "cryptSharedLibPath",
                "cryptSharedLibSearchPath",
                encryptedFieldsMap,
                true,
                kmsProviders,
                schemaMap);

            subject.BypassQueryAnalysis.Should().Be(true);
            subject.CryptSharedLibPath.Should().Be("cryptSharedLibPath");
            subject.CryptSharedLibSearchPath.Should().Be("cryptSharedLibSearchPath");
            subject.IsCryptSharedLibRequired.Should().Be(true);
            subject.EncryptedFieldsMap.Should().BeSameAs(encryptedFieldsMap);
            subject.KmsProviders.Should().BeSameAs(kmsProviders);
            subject.SchemaMap.Should().BeSameAs(schemaMap);
        }

        [Fact]
        public void Equals_should_return_true_when_equals()
        {
            var encryptedFieldsMap1 = new Dictionary<string, BsonDocument>()
            {
                { "encryptedFieldsKey", new BsonDocument("key", "value") }
            };
            var encryptedFieldsMap2 = new Dictionary<string, BsonDocument>()
            {
                { "encryptedFieldsKey", new BsonDocument("key", "value") }
            };
            var schemaMap1 = new Dictionary<string, BsonDocument>()
            {
                { "schemaMapKey", new BsonDocument("key", "value") }
            };
            var schemaMap2 = new Dictionary<string, BsonDocument>()
            {
                { "schemaMapKey", new BsonDocument("key", "value") }
            };
            var kmsProviders1 = new Dictionary<string, IReadOnlyDictionary<string, object>>()
            {
                { "schemaMapKey", new Dictionary<string, object>() { { "kmsKey", "kmsValue" } } } 
            };
            var kmsProviders2 = new Dictionary<string, IReadOnlyDictionary<string, object>>()
            {
                { "schemaMapKey", new Dictionary<string, object>() { { "kmsKey", "kmsValue" } } }
            };

            var subject1 = new CryptClientSettings(
                true,
                "csfleLibPath",
                "csfleSearchPath",
                encryptedFieldsMap1,
                true,
                kmsProviders1,
                schemaMap1);

            var subject2 = new CryptClientSettings(
                true,
                "csfleLibPath",
                "csfleSearchPath",
                encryptedFieldsMap2,
                true,
                kmsProviders2,
                schemaMap2);

            subject1.Equals(subject2).Should().BeTrue();
        }

        [Fact]
        public void Equals_should_return_false_when_not_equals()
        {
            var encryptedFieldsMap1 = new Dictionary<string, BsonDocument>()
            {
                { "encryptedFieldsKey", new BsonDocument("key", "value") }
            };
            var encryptedFieldsMap2 = new Dictionary<string, BsonDocument>()
            {
                { "encryptedFieldsKey", new BsonDocument("key", "value") }
            };
            var schemaMap1 = new Dictionary<string, BsonDocument>()
            {
                { "schemaMapKey", new BsonDocument("key", "value") }
            };
            var schemaMap2 = new Dictionary<string, BsonDocument>()
            {
                { "schemaMapKey", new BsonDocument("key", "value") }
            };
            var kmsProviders1 = new Dictionary<string, IReadOnlyDictionary<string, object>>()
            {
                { "schemaMapKey", new Dictionary<string, object>() { { "kmsKey", "kmsValue1" } } }
            };
            var kmsProviders2 = new Dictionary<string, IReadOnlyDictionary<string, object>>()
            {
                { "schemaMapKey", new Dictionary<string, object>() { { "kmsKey", "kmsValue2" } } }
            };

            var subject1 = new CryptClientSettings(
                true,
                "csfleLibPath",
                "csfleSearchPath",
                encryptedFieldsMap1,
                true,
                kmsProviders1,
                schemaMap1);

            var subject2 = new CryptClientSettings(
                true,
                "csfleLibPath",
                "csfleSearchPath",
                encryptedFieldsMap2,
                true,
                kmsProviders2,
                schemaMap2);

            subject1.Equals(subject2).Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_false_when_null()
        {
            var subject = new CryptClientSettings(
               true,
               "csfleLibPath",
               "csfleSearchPath",
               null,
               true,
               null,
               null);

            subject.Equals(null).Should().BeFalse();
        }
    }
}
