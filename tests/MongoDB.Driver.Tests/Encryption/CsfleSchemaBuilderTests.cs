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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Encryption;
using Xunit;

namespace MongoDB.Driver.Tests.Encryption
{
    public class CsfleSchemaBuilderTests
    {
        private static Guid _keyIdExample = Guid.Parse("6f4af470-00d1-401f-ac39-f45902a0c0c8");
        private const string _keyIdString = "6f4af470-00d1-401f-ac39-f45902a0c0c8";

        [Theory]
        [InlineData(BsonType.Array,
            CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
            null,
            """ "bsonType": "array", "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random" """)]
        [InlineData(BsonType.Array,
            null,
            _keyIdString,
            """ "bsonType": "array", "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }] """)]
        [InlineData(BsonType.Array,
            CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
            _keyIdString,
            """ "bsonType": "array", "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random", "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }] """)]
        public void EncryptedCollection_PropertyWithExpression_works_as_expected(BsonType bsonType, CsfleEncryptionAlgorithm? algorithm, string keyString, string expectedContent)
        {
            Guid? keyId = keyString is null ? null : Guid.Parse(keyString);
            var builder = new EncryptedCollectionBuilder<Patient>();

            builder.Property(p => p.MedicalRecords, bsonType, algorithm, keyId);

            var expected = $$"""
                              {
                                "bsonType": "object",
                                "properties": {
                                  "medicalRecords": {
                                    "encrypt": {
                                        {{expectedContent}}
                                    }
                                  }
                                }
                              }
                              """;

            AssertOutcomeCollectionBuilder(builder, expected);
        }

        [Fact]
        public void BasicPropertyTest()
        {
            const string collectionName = "medicalRecords.patients";

            var builder = CsfleSchemaBuilder.Create(schemaBuilder =>
            {
                schemaBuilder.Encrypt<Patient>(collectionName, builder =>
                {
                    builder
                        .EncryptMetadata(keyId: _keyIdExample)
                        .Property(p => p.MedicalRecords, BsonType.Array,
                            CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                        .Property("bloodType", BsonType.String,
                            algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                        .Property(p => p.Ssn, BsonType.Int32,
                            CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)
                        .Property(p => p.Insurance, innerBuilder =>
                        {
                            innerBuilder
                                .Property(i => i.PolicyNumber, BsonType.Int32,
                                    CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic);
                        });
                } );
            });

            var expected = new Dictionary<string, string>
            {
                [collectionName] = """
                                   {
                                     "bsonType": "object",
                                     "encryptMetadata": {
                                       "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                                     },
                                     "properties": {
                                       "insurance": {
                                         "bsonType": "object",
                                         "properties": {
                                           "policyNumber": {
                                             "encrypt": {
                                               "bsonType": "int",
                                               "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic"
                                             }
                                           }
                                         }
                                       },
                                       "medicalRecords": {
                                         "encrypt": {
                                           "bsonType": "array",
                                           "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random"
                                         }
                                       },
                                       "bloodType": {
                                         "encrypt": {
                                           "bsonType": "string",
                                           "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random"
                                         }
                                       },
                                       "ssn": {
                                         "encrypt": {
                                           "bsonType": "int",
                                           "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic"
                                         }
                                       }
                                     },
                                   }
                                   """
            };

            AssertOutcomeBuilder(builder, expected);
        }

        [Fact]
        public void BasicPatternTest()
        {
            const string collectionName = "medicalRecords.patients";

            var builder = CsfleSchemaBuilder.Create(schemaBuilder =>
            {
                schemaBuilder.Encrypt<Patient>(collectionName, builder =>
                {
                    builder
                        .PatternProperty("_PIIString$", BsonType.String, CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)
                        .PatternProperty("_PIIArray$", BsonType.Array, CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                        .PatternProperty(p => p.Insurance, innerBuilder =>
                        {
                            innerBuilder
                                .PatternProperty("_PIIString$", BsonType.String,
                                    CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)
                                .PatternProperty("_PIINumber$", BsonType.Int32,
                                    algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic);
                        });
                } );
            });

            var expected = new Dictionary<string, string>
            {
                [collectionName] = """
                                   {
                                   "bsonType": "object",
                                   "patternProperties": {
                                     "_PIIString$": {
                                       "encrypt": {
                                         "bsonType": "string",
                                         "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic",
                                       },
                                     },
                                     "_PIIArray$": {
                                       "encrypt": {
                                         "bsonType": "array",
                                         "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random",
                                       },
                                     },
                                     "insurance": {
                                       "bsonType": "object",
                                       "patternProperties": {
                                         "_PIINumber$": {
                                           "encrypt": {
                                             "bsonType": "int",
                                             "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic",
                                           },
                                         },
                                         "_PIIString$": {
                                           "encrypt": {
                                             "bsonType": "string",
                                             "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic",
                                           },
                                         },
                                       },
                                     },
                                   },
                                   }
                                   """
            };

            AssertOutcomeBuilder(builder, expected);
        }

        private void AssertOutcomeBuilder(CsfleSchemaBuilder builder, Dictionary<string, string> expected)
        {
            var builtSchema = builder.Build();
            expected.Should().HaveCount(builtSchema.Count);
            foreach (var collectionNamespace in expected.Keys)
            {
                var parsed = BsonDocument.Parse(expected[collectionNamespace]);
                parsed.Should().BeEquivalentTo(builtSchema[collectionNamespace]);
            }
        }

        //TODO Give better name...
        private void AssertOutcomeCollectionBuilder<T>(EncryptedCollectionBuilder<T> builder, string expected)
        {
            var builtSchema = builder.Build();
            var expectedSchema = BsonDocument.Parse(expected);
            expectedSchema.Should().BeEquivalentTo(builtSchema);
        }

        /** To test:
         * - Metadata
         * - Property with expression
         * - Property with string
         * - Property with single bsonType
         * - Property with multiple bsonType
         * - Nested property with expression
         * - Nested property with string
         * - Pattern property with string
         * - Pattern property with multiple bsonType
         * - Nested pattern property with expression
         * - Nested pattern property with string
         * - Multiple types in schema
         * - Property and pattern property together
         */

        internal class Patient
        {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonElement("name")]
            public string Name { get; set; }

            [BsonElement("ssn")]
            public int Ssn { get; set; }

            [BsonElement("bloodType")]
            public string BloodType { get; set; }

            [BsonElement("medicalRecords")]
            public List<MedicalRecord> MedicalRecords { get; set; }

            [BsonElement("insurance")]
            public Insurance Insurance { get; set; }
        }

        internal class MedicalRecord
        {
            [BsonElement("weight")]
            public int Weight { get; set; }

            [BsonElement("bloodPressure")]
            public string BloodPressure { get; set; }
        }

        internal class Insurance
        {
            [BsonElement("provider")]
            public string Provider { get; set; }

            [BsonElement("policyNumber")]
            public int PolicyNumber { get; set; }
        }
    }
}