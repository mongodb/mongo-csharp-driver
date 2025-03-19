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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Encryption;
using Xunit;

namespace MongoDB.Driver.Tests.Encryption
{
    public class CsfleSchemaBuilderTests
    {
        private readonly Guid _keyIdExample = Guid.Parse("6f4af470-00d1-401f-ac39-f45902a0c0c8");

        [Fact]
        public void TypedSchemaBuilder_Property_throws_when_path_is_null()
        {
            var exception = Record.Exception(() => new CsfleTypeSchemaBuilder<BsonDocument>().Property(null));

            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void TypedSchemaBuilder_PropertyWithConfigure_throws_when_path_is_null()
        {
            Action<CsfleTypeSchemaBuilder<BsonDocument>> configure = b => { };
            var exception = Record.Exception(() => new CsfleTypeSchemaBuilder<BsonDocument>().Property((FieldDefinition<BsonDocument>)null, configure));

            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void TypedSchemaBuilder_PropertyWithConfigure_throws_when_configure_is_null()
        {
            Action<CsfleTypeSchemaBuilder<BsonDocument>> configure = null;
            var exception = Record.Exception(() => new CsfleTypeSchemaBuilder<BsonDocument>().Property("path", configure));

            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void TypedSchemaBuilder_PatternProperty_throws_when_pattern_is_null()
        {
            var exception = Record.Exception(() => new CsfleTypeSchemaBuilder<BsonDocument>().PatternProperty(null));

            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
        }

        [Fact]
        public void TypedSchemaBuilder_PatternPropertyWithConfigure_throws_when_pattern_is_null()
        {
            Action<CsfleTypeSchemaBuilder<BsonDocument>> configure = b => { };
            var exception = Record.Exception(() => new CsfleTypeSchemaBuilder<BsonDocument>().PatternProperty((FieldDefinition<BsonDocument>)null, configure));

            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void TypedSchemaBuilder_PatternPropertyWithConfigure_throws_when_configure_is_null()
        {
            Action<CsfleTypeSchemaBuilder<BsonDocument>> configure = null;
            var exception = Record.Exception(() => new CsfleTypeSchemaBuilder<BsonDocument>().PatternProperty("path", configure));

            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void TypedSchemaBuilder_Property_works_as_expected()
        {
            var builder = new CsfleTypeSchemaBuilder<Patient>()
                .Property("bloodType", keyId: _keyIdExample,  bsonType: BsonType.String,
                    algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random);

            var expected = """
                           {
                                "bsonType": "object",
                                "properties": {
                                    "bloodType": {
                                        "encrypt": {
                                           "bsonType": "string",
                                           "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random"
                                           "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                                         }
                                    }
                                }
                           }
                           """;

            AssertOutcomeTypeBuilder(builder, expected);
        }

        [Fact]
        public void TypedSchemaBuilder_PropertyWithExpression_works_as_expected()
        {
            var builder = new CsfleTypeSchemaBuilder<Patient>()
                .Property(p => p.BloodType, keyId: _keyIdExample,  bsonType: BsonType.String,
                    algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random);

            var expected = """
                           {
                                "bsonType": "object",
                                "properties": {
                                    "bloodType": {
                                        "encrypt": {
                                           "bsonType": "string",
                                           "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random"
                                           "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                                         }
                                    }
                                }
                           }
                           """;

            AssertOutcomeTypeBuilder(builder, expected);
        }

        [Fact]
        public void TypedSchemaBuilder_PropertyWithConfigure_works_as_expected()
        {
            var builder = new CsfleTypeSchemaBuilder<Patient>()
                .Property<Insurance>("insurance", insurance => insurance
                    .Property("policyNumber", bsonType: BsonType.Int32,
                        algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic,
                        keyId: _keyIdExample));

            var expected = """
                           {
                                "bsonType": "object",
                                "properties": {
                                   "insurance": {
                                     "bsonType": "object",
                                     "properties": {
                                       "policyNumber": {
                                         "encrypt": {
                                           "bsonType": "int",
                                           "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic"
                                           "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                                         }
                                        }
                                       }
                                     }
                                }
                           }
                           """;

            AssertOutcomeTypeBuilder(builder, expected);
        }

        [Fact]
        public void TypedSchemaBuilder_PropertyWithExpressionAndConfigure_works_as_expected()
        {
            var builder = new CsfleTypeSchemaBuilder<Patient>()
                .Property(p => p.Insurance, insurance => insurance
                    .Property("policyNumber", bsonType: BsonType.Int32,
                        algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic,
                        keyId: _keyIdExample));

            var expected = """
                           {
                                "bsonType": "object",
                                "properties": {
                                   "insurance": {
                                     "bsonType": "object",
                                     "properties": {
                                       "policyNumber": {
                                         "encrypt": {
                                           "bsonType": "int",
                                           "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic"
                                           "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                                         }
                                        }
                                       }
                                     }
                                }
                           }
                           """;

            AssertOutcomeTypeBuilder(builder, expected);
        }

        [Fact]
        public void TypedSchemaBuilder_PatternProperty_works_as_expected()
        {
            var builder = new CsfleTypeSchemaBuilder<Patient>()
                .PatternProperty("_PIIString$", bsonType: BsonType.String,
                    algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic,
                    keyId: _keyIdExample);

            var expected = """
                           {
                                "bsonType": "object",
                                "patternProperties": {
                                    "_PIIString$": {
                                        "encrypt": {
                                           "bsonType": "string",
                                           "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic"
                                           "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                                         }
                                    }
                                }
                           }
                           """;

            AssertOutcomeTypeBuilder(builder, expected);
        }

        [Fact]
        public void TypedSchemaBuilder_PatternPropertyWithConfigure_works_as_expected()
        {
            var builder = new CsfleTypeSchemaBuilder<Patient>()
                .PatternProperty<Insurance>("insurance", builder => builder
                    .PatternProperty("_PIINumber$", bsonType: BsonType.Int32,
                        algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic));

            var expected = """
                           {
                                "bsonType": "object",
                                "patternProperties": {
                                    "insurance": {
                                      "bsonType": "object",
                                      "patternProperties": {
                                        "_PIINumber$": {
                                          "encrypt": {
                                            "bsonType": "int",
                                            "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic",
                                          },
                                        },
                                      },
                                    }
                                }
                           }
                           """;

            AssertOutcomeTypeBuilder(builder, expected);
        }

        [Fact]
        public void TypedSchemaBuilder_PatternPropertyWithExpressionAndConfigure_works_as_expected()
        {
            var builder = new CsfleTypeSchemaBuilder<Patient>()
                .PatternProperty(p=> p.Insurance, builder => builder
                    .PatternProperty("_PIINumber$", bsonType: BsonType.Int32,
                        algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic));

            var expected = """
                           {
                                "bsonType": "object",
                                "patternProperties": {
                                    "insurance": {
                                      "bsonType": "object",
                                      "patternProperties": {
                                        "_PIINumber$": {
                                          "encrypt": {
                                            "bsonType": "int",
                                            "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic",
                                          },
                                        },
                                      },
                                    }
                                }
                           }
                           """;

            AssertOutcomeTypeBuilder(builder, expected);
        }

        [Fact]
        public void TypedSchemaBuilder_EncryptMetadata_works_as_expected()
        {
            var builder = new CsfleTypeSchemaBuilder<Patient>()
                .EncryptMetadata(keyId: _keyIdExample,
                    algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic);

            var expected = """
                           {
                               "bsonType": "object",
                               "encryptMetadata": {
                                 "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic",
                                 "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                               },
                           }
                           """;

            AssertOutcomeTypeBuilder(builder, expected);
        }

        [Fact]
        public void SchemaBuilder_WithType_works_as_expected()
        {
            const string collectionName1 = "medicalRecords.patients";
            const string collectionName2 = "test.collection";

            var typedBuilder1 = new CsfleTypeSchemaBuilder<Patient>()
                .EncryptMetadata(keyId: _keyIdExample,
                    algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic);
            var typeBuilder2 = new CsfleTypeSchemaBuilder<BsonDocument>()
                .EncryptMetadata(algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random);

            var encryptionSchemaBuilder = new CsfleSchemaBuilder()
                .WithType(CollectionNamespace.FromFullName(collectionName1), typedBuilder1)
                .WithType(CollectionNamespace.FromFullName(collectionName2), typeBuilder2);

            var expected = new Dictionary<string, string>
            {
                [collectionName1] = """
                                   {
                                       "bsonType": "object",
                                       "encryptMetadata": {
                                         "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic",
                                         "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                                       },
                                   }
                                   """,
                [collectionName2] = """
                                    {
                                        "bsonType": "object",
                                        "encryptMetadata": {
                                          "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random",
                                        },
                                    }
                                    """
            };

            AssertOutcomeBuilder(encryptionSchemaBuilder, expected);
        }

        [Fact]
        public void SchemaBuilder_WithTypeAndConfigure_works_as_expected()
        {
            const string collectionName1 = "medicalRecords.patients";
            const string collectionName2 = "test.collection";

            var encryptionSchemaBuilder = new CsfleSchemaBuilder()
                .WithType<Patient>(CollectionNamespace.FromFullName(collectionName1), b => b.EncryptMetadata(keyId: _keyIdExample, algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic))
                .WithType<BsonDocument>(CollectionNamespace.FromFullName(collectionName2), b => b.EncryptMetadata(algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random));

            var expected = new Dictionary<string, string>
            {
                [collectionName1] = """
                                   {
                                       "bsonType": "object",
                                       "encryptMetadata": {
                                         "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic",
                                         "keyId": [{ "$binary" : { "base64" : "b0r0cADRQB+sOfRZAqDAyA==", "subType" : "04" } }]
                                       },
                                   }
                                   """,
                [collectionName2] = """
                                    {
                                        "bsonType": "object",
                                        "encryptMetadata": {
                                          "algorithm": "AEAD_AES_256_CBC_HMAC_SHA_512-Random",
                                        },
                                    }
                                    """
            };

            AssertOutcomeBuilder(encryptionSchemaBuilder, expected);
        }

        [Fact]
        public void SchemaBuilder_CompleteExampleWithBsonDocument_work_as_expected()
        {
            var collectionName = "medicalRecords.patients";

            var typedBuilder = CsfleSchemaBuilder.GetTypeBuilder<BsonDocument>()
                .EncryptMetadata(keyId: _keyIdExample)
                .Property<BsonDocument>("insurance", insurance => insurance
                    .Property("policyNumber", bsonType: BsonType.Int32,
                        algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic))
                .Property("medicalRecords", bsonType: BsonType.Array,
                    algorithm: CsfleEncryptionAlgorithm
                        .AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                .Property("bloodType", bsonType: BsonType.String,
                    algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                .Property("ssn", bsonType: BsonType.Int32,
                    algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic);

            var encryptionSchemaBuilder = new CsfleSchemaBuilder()
                .WithType(CollectionNamespace.FromFullName(collectionName), typedBuilder);

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

            AssertOutcomeBuilder(encryptionSchemaBuilder, expected);
        }

        [Fact]
        public void SchemaBuilder_CompleteExample_work_as_expected()
        {
            const string collectionName = "medicalRecords.patients";

            var typedBuilder = CsfleSchemaBuilder.GetTypeBuilder<Patient>()
                .EncryptMetadata(keyId: _keyIdExample)
                .Property(p => p.Insurance, insurance => insurance
                    .Property(i => i.PolicyNumber, bsonType: BsonType.Int32,
                        algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic))
                .Property(p => p.MedicalRecords, bsonType: BsonType.Array,
                    algorithm: CsfleEncryptionAlgorithm
                        .AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                .Property("bloodType", bsonType: BsonType.String,
                    algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                .Property(p => p.Ssn, bsonType: BsonType.Int32,
                    algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic);

            var encryptionSchemaBuilder = new CsfleSchemaBuilder()
                .WithType(CollectionNamespace.FromFullName(collectionName), typedBuilder);

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

            AssertOutcomeBuilder(encryptionSchemaBuilder, expected);
        }

        [Fact]
        public void SchemaBuilder_CompleteExampleWithPatternProperties_work_as_expected()
        {
            const string collectionName = "medicalRecords.patients";

            var typedBuilder = CsfleSchemaBuilder.GetTypeBuilder<Patient>()
                .PatternProperty("_PIIString$", bsonType: BsonType.String,
                    algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)
                .PatternProperty("_PIIArray$", bsonType: BsonType.Array,
                    algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                .PatternProperty(p => p.Insurance, builder => builder
                    .PatternProperty("_PIINumber$", bsonType: BsonType.Int32, algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)
                    .PatternProperty("_PIIString$", bsonType: BsonType.String, algorithm: CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)
                );

            var encryptionSchemaBuilder = new CsfleSchemaBuilder()
                .WithType(CollectionNamespace.FromFullName(collectionName), typedBuilder);

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

            AssertOutcomeBuilder(encryptionSchemaBuilder, expected);
        }

        private void AssertOutcomeTypeBuilder(CsfleTypeSchemaBuilder builder, string expected)
        {
            var parsedExpected = BsonDocument.Parse(expected);
            var builtSchema = builder.Build();

            Assert.Equal(parsedExpected, builtSchema);
        }

        private void AssertOutcomeBuilder(CsfleSchemaBuilder builder, Dictionary<string, string> expected)
        {
            var builtSchema = builder.Build();
            Assert.Equal(expected.Count, builtSchema.Count);
            foreach (var collectionNamespace in expected.Keys)
            {
                var parsed = BsonDocument.Parse(expected[collectionNamespace]);
                Assert.Equal(parsed, builtSchema[collectionNamespace]);
            }
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