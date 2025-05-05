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
        private const string _keyIdString = "6f4af470-00d1-401f-ac39-f45902a0c0c8";
        private static Guid _keyId = Guid.Parse(_keyIdString);

        [Fact]
        public void CsfleSchemaBuilder_works_as_expected()
        {
            const string collectionName = "medicalRecords.patients";

            var builder = CsfleSchemaBuilder.Create(schemaBuilder =>
            {
                schemaBuilder.Encrypt<Patient>(collectionName, builder =>
                {
                    builder
                        .EncryptMetadata(keyId: _keyId)
                        .Property(p => p.MedicalRecords, BsonType.Array,
                            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                        .Property("bloodType", BsonType.String,
                            algorithm: EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                        .Property(p => p.Ssn, BsonType.Int32,
                            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)
                        .Property(p => p.Insurance, innerBuilder =>
                        {
                            innerBuilder
                                .Property(i => i.PolicyNumber, BsonType.Int32,
                                    EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic);
                        })
                        .PatternProperty("_PIIString$", BsonType.String, EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)
                        .PatternProperty("_PIIArray$", BsonType.Array, EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                        .PatternProperty(p => p.Insurance, innerBuilder =>
                        {
                            innerBuilder
                                .PatternProperty("_PIIString$", BsonType.String,
                                    EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)
                                .PatternProperty("_PIINumber$", BsonType.Int32,
                                    algorithm: EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic);
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

            AssertOutcomeCsfleSchemaBuilder(builder, expected);
        }

        [Fact]
        public void CsfleSchemaBuilder_with_multiple_types_works_as_expected()
        {
            const string patientCollectionName = "medicalRecords.patients";
            const string testClassCollectionName = "test.class";

            var builder = CsfleSchemaBuilder.Create(schemaBuilder =>
            {
                schemaBuilder.Encrypt<Patient>(patientCollectionName, builder =>
                {
                    builder
                        .EncryptMetadata(keyId: _keyId)
                        .Property(p => p.MedicalRecords, BsonType.Array,
                            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random);
                });

                schemaBuilder.Encrypt<TestClass>(testClassCollectionName, builder =>
                {
                    builder.Property(t => t.TestString, BsonType.String);
                });
            });

            var expected = new Dictionary<string, string>
            {
                [patientCollectionName] = """
                                          {
                                            "bsonType": "object",
                                            "encryptMetadata": {
                                              "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                                            },
                                            "properties": {
                                              "medicalRecords": {
                                                "encrypt": {
                                                  "bsonType": "array",
                                                  "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random"
                                                }
                                              },
                                            },
                                          }
                                          """,
                [testClassCollectionName] = """
                                          {
                                            "bsonType": "object",
                                            "properties": {
                                              "TestString": {
                                                "encrypt": {
                                                  "bsonType": "string",
                                                }
                                              },
                                            }
                                          }
                                          """
            };

            AssertOutcomeCsfleSchemaBuilder(builder, expected);
        }

        [Theory]
        [InlineData(
            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
            null,
            """ "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random" """)]
        [InlineData(
            null,
            _keyIdString,
            """ "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }] """)]
        public void EncryptedCollection_Metadata_works_as_expected(EncryptionAlgorithm? algorithm, string keyString, string expectedContent)
        {
            Guid? keyId = keyString is null ? null : Guid.Parse(keyString);
            var builder = new EncryptedCollectionBuilder<Patient>();

            builder.EncryptMetadata(keyId, algorithm);

            var expected = $$"""
                             {
                               "bsonType": "object",
                               "encryptMetadata": {
                                       {{expectedContent}}
                               }
                             }
                             """;

            AssertOutcomeCollectionBuilder(builder, expected);
        }

        [Theory]
        [InlineData(BsonType.Array,
            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
            null,
            """ "bsonType": "array", "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random" """)]
        [InlineData(BsonType.Array,
            null,
            _keyIdString,
            """ "bsonType": "array", "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }] """)]
        [InlineData(BsonType.Array,
            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
            _keyIdString,
            """ "bsonType": "array", "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random", "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }] """)]
        public void EncryptedCollection_PatternProperty_works_as_expected(BsonType bsonType, EncryptionAlgorithm? algorithm, string keyString, string expectedContent)
        {
            Guid? keyId = keyString is null ? null : Guid.Parse(keyString);
            var builder = new EncryptedCollectionBuilder<Patient>();

            builder.PatternProperty("randomRegex*", bsonType, algorithm, keyId);

            var expected = $$"""
                             {
                               "bsonType": "object",
                               "patternProperties": {
                                 "randomRegex*": {
                                   "encrypt": {
                                       {{expectedContent}}
                                   }
                                 }
                               }
                             }
                             """;

            AssertOutcomeCollectionBuilder(builder, expected);
        }

        [Theory]
        [InlineData(new[] {BsonType.Array, BsonType.String},
            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
            null,
            """ "bsonType": ["array", "string"], "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random" """)]
        [InlineData(new[] {BsonType.Array, BsonType.String},
            null,
            _keyIdString,
            """ "bsonType": ["array", "string"], "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }] """)]
        [InlineData(new[] {BsonType.Array, BsonType.String},
            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
            _keyIdString,
            """ "bsonType": ["array", "string"], "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random", "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }] """)]
        public void EncryptedCollection_PatternProperty_with_multiple_bson_types_works_as_expected(IEnumerable<BsonType> bsonTypes, EncryptionAlgorithm? algorithm, string keyString, string expectedContent)
        {
            Guid? keyId = keyString is null ? null : Guid.Parse(keyString);
            var builder = new EncryptedCollectionBuilder<Patient>();

            builder.PatternProperty("randomRegex*", bsonTypes, algorithm, keyId);

            var expected = $$"""
                             {
                               "bsonType": "object",
                               "patternProperties": {
                                 "randomRegex*": {
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
        public void EncryptedCollection_PatternProperty_nested_works_as_expected()
        {
            Guid? keyId = Guid.Parse(_keyIdString);
            var builder = new EncryptedCollectionBuilder<Patient>();

            builder.PatternProperty(p => p.Insurance, innerBuilder =>
            {
                innerBuilder
                    .EncryptMetadata(keyId)
                    .Property("policyNumber", BsonType.Int32,
                        EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)
                    .PatternProperty("randomRegex*", BsonType.String,
                        EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random);
            });

            var expected = """
                           {
                               "bsonType": "object",
                               "patternProperties": {
                                   "insurance": {
                                       "bsonType": "object",
                                       "encryptMetadata": {
                                         "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                                       },
                                       "properties": {
                                           "policyNumber": {
                                               "encrypt": {
                                                   "bsonType": "int",
                                                   "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic"
                                               }
                                           }
                                       },
                                       "patternProperties": {
                                           "randomRegex*": {
                                               "encrypt": {
                                                   "bsonType": "string",
                                                   "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random"
                                               }
                                           }
                                       }
                                   }
                               }
                           }
                           """;

            AssertOutcomeCollectionBuilder(builder, expected);
        }

        [Fact]
        public void EncryptedCollection_PatternProperty_nested_with_string_works_as_expected()
        {
            Guid? keyId = Guid.Parse(_keyIdString);
            var builder = new EncryptedCollectionBuilder<Patient>();

            builder.PatternProperty<Insurance>("insurance", innerBuilder =>
            {
                innerBuilder
                    .EncryptMetadata(keyId)
                    .Property("policyNumber", BsonType.Int32,
                        EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)
                    .PatternProperty("randomRegex*", BsonType.String,
                        EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random);
            });

            var expected = """
                           {
                               "bsonType": "object",
                               "patternProperties": {
                                   "insurance": {
                                       "bsonType": "object",
                                       "encryptMetadata": {
                                         "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                                       },
                                       "properties": {
                                           "policyNumber": {
                                               "encrypt": {
                                                   "bsonType": "int",
                                                   "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic"
                                               }
                                           }
                                       },
                                       "patternProperties": {
                                           "randomRegex*": {
                                               "encrypt": {
                                                   "bsonType": "string",
                                                   "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random"
                                               }
                                           }
                                       }
                                   }
                               }
                           }
                           """;

            AssertOutcomeCollectionBuilder(builder, expected);
        }

        [Theory]
        [InlineData(BsonType.Array,
            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
            null,
            """ "bsonType": "array", "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random" """)]
        [InlineData(BsonType.Array,
            null,
            _keyIdString,
            """ "bsonType": "array", "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }] """)]
        [InlineData(BsonType.Array,
            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
            _keyIdString,
            """ "bsonType": "array", "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random", "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }] """)]
        public void EncryptedCollection_Property_with_expression_works_as_expected(BsonType bsonType, EncryptionAlgorithm? algorithm, string keyString, string expectedContent)
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

        [Theory]
        [InlineData(new[] {BsonType.Array, BsonType.String},
            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
            null,
            """ "bsonType": ["array", "string"], "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random" """)]
        [InlineData(new[] {BsonType.Array, BsonType.String},
            null,
            _keyIdString,
            """ "bsonType": ["array", "string"], "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }] """)]
        [InlineData(new[] {BsonType.Array, BsonType.String},
            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
            _keyIdString,
            """ "bsonType": ["array", "string"], "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random", "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }] """)]
        public void EncryptedCollection_Property_with_multiple_bson_types_works_as_expected(IEnumerable<BsonType> bsonTypes, EncryptionAlgorithm? algorithm, string keyString, string expectedContent)
        {
            Guid? keyId = keyString is null ? null : Guid.Parse(keyString);
            var builder = new EncryptedCollectionBuilder<Patient>();

            builder.Property(p => p.MedicalRecords, bsonTypes, algorithm, keyId);

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

        [Theory]
        [InlineData(BsonType.Array,
            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
            null,
            """ "bsonType": "array", "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random" """)]
        [InlineData(BsonType.Array,
            null,
            _keyIdString,
            """ "bsonType": "array", "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }] """)]
        [InlineData(BsonType.Array,
            EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random,
            _keyIdString,
            """ "bsonType": "array", "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random", "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }] """)]
        public void EncryptedCollection_Property_with_string_works_as_expected(BsonType bsonType, EncryptionAlgorithm? algorithm, string keyString, string expectedContent)
        {
            Guid? keyId = keyString is null ? null : Guid.Parse(keyString);
            var builder = new EncryptedCollectionBuilder<Patient>();

            builder.Property("medicalRecords", bsonType, algorithm, keyId);

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
        public void EncryptedCollection_Property_nested_works_as_expected()
        {
            Guid? keyId = Guid.Parse(_keyIdString);
            var builder = new EncryptedCollectionBuilder<Patient>();

            builder.Property(p => p.Insurance, innerBuilder =>
            {
                innerBuilder
                    .EncryptMetadata(keyId)
                    .Property("policyNumber", BsonType.Int32,
                        EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)
                    .PatternProperty("randomRegex*", BsonType.String,
                        EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random);
            });

            var expected = """
                           {
                               "bsonType": "object",
                               "properties": {
                                   "insurance": {
                                       "bsonType": "object",
                                       "encryptMetadata": {
                                         "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                                       },
                                       "properties": {
                                           "policyNumber": {
                                               "encrypt": {
                                                   "bsonType": "int",
                                                   "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic"
                                               }
                                           }
                                       },
                                       "patternProperties": {
                                           "randomRegex*": {
                                               "encrypt": {
                                                   "bsonType": "string",
                                                   "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random"
                                               }
                                           }
                                       }
                                   }
                               }
                           }
                           """;

            AssertOutcomeCollectionBuilder(builder, expected);
        }

        [Fact]
        public void EncryptedCollection_Property_nested_with_string_works_as_expected()
        {
            Guid? keyId = Guid.Parse(_keyIdString);
            var builder = new EncryptedCollectionBuilder<Patient>();

            builder.Property<Insurance>("insurance", innerBuilder =>
            {
                innerBuilder
                    .EncryptMetadata(keyId)
                    .Property("policyNumber", BsonType.Int32,
                        EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)
                    .PatternProperty("randomRegex*", BsonType.String,
                        EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random);
            });

            var expected = """
                           {
                               "bsonType": "object",
                               "properties": {
                                   "insurance": {
                                       "bsonType": "object",
                                       "encryptMetadata": {
                                         "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                                       },
                                       "properties": {
                                           "policyNumber": {
                                               "encrypt": {
                                                   "bsonType": "int",
                                                   "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic"
                                               }
                                           }
                                       },
                                       "patternProperties": {
                                           "randomRegex*": {
                                               "encrypt": {
                                                   "bsonType": "string",
                                                   "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random"
                                               }
                                           }
                                       }
                                   }
                               }
                           }
                           """;

            AssertOutcomeCollectionBuilder(builder, expected);
        }

        [Fact]
        public void EncryptedCollection_Property_with_empty_bson_types_throws()
        {
            var builder = new EncryptedCollectionBuilder<Patient>();

            var recordedException = Record.Exception(() => builder.Property("test", []));
            recordedException.Should().NotBeNull();
            recordedException.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void EncryptedCollection_Metadata_with_empty_algorithm_and_key_throws()
        {
            var builder = new EncryptedCollectionBuilder<Patient>();

            var recordedException = Record.Exception(() => builder.EncryptMetadata(null, null));
            recordedException.Should().NotBeNull();
            recordedException.Should().BeOfType<ArgumentException>();
        }

        private void AssertOutcomeCsfleSchemaBuilder(CsfleSchemaBuilder builder, Dictionary<string, string> expectedSchema)
        {
            var builtSchema = builder.Build();
            expectedSchema.Should().HaveCount(builtSchema.Count);
            foreach (var collectionNamespace in expectedSchema.Keys)
            {
                var parsed = BsonDocument.Parse(expectedSchema[collectionNamespace]);
                builtSchema[collectionNamespace].Should().BeEquivalentTo(parsed);
            }
        }

        private void AssertOutcomeCollectionBuilder<T>(EncryptedCollectionBuilder<T> builder, string expected)
        {
            var builtSchema = builder.Build();
            var expectedSchema = BsonDocument.Parse(expected);
            builtSchema.Should().BeEquivalentTo(expectedSchema);
        }

        internal class TestClass
        {
            public ObjectId Id { get; set; }

            public string TestString { get; set; }
        }

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