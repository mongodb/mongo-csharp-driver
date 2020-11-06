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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Encryption;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AutoEncryptionOptionsTests
    {
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

            var subject = new AutoEncryptionOptions(
                keyVaultNamespace: CollectionNamespace.FromFullName("db.coll"),
                kmsProviders: kmsProviders,
                bypassAutoEncryption: true,
                extraOptions: extraOptions,
                schemaMap: schemaMap);

            var result = subject.ToString();
            result.Should().Be("{ BypassAutoEncryption : True, KmsProviders : { \"provider1\" : { \"string\" : \"test\" }, \"provider2\" : { \"binary\" : { \"_t\" : \"System.Byte[]\", \"_v\" : new BinData(0, \"ABEiM0RVZneImaq7zN3u/w==\") } } }, KeyVaultNamespace : \"db.coll\", ExtraOptions : { \"mongocryptdURI\" : \"testURI\" }, SchemaMap : { \"coll1\" : { \"string\" : \"test\" }, \"coll2\" : { \"binary\" : UUID(\"00112233-4455-6677-8899-aabbccddeeff\") } } }");
        }
    }
}
