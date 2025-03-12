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
        [Fact]
        public void Test1()
        {
            var myKeyId = Guid.Parse("6f4af470-00d1-401f-ac39-f45902a0c0c8");
            var collectionName = "medicalRecords.patients";

            var typedBuilder = CsfleSchemaBuilder.GetTypeBuilder<Patient>()
                .EncryptMetadata(keyId: myKeyId)
                .Encrypt(p => p.Insurance, insurance => insurance
                    .Encrypt(i => i.PolicyNumber, bsonType: BsonType.Int32,
                        algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic))
                .Encrypt(p => p.MedicalRecords, bsonType: BsonType.Array,
                    algorithm: CsfleEncyptionAlgorithm
                        .AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                .Encrypt("bloodType", bsonType: BsonType.String,
                    algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                .Encrypt(p => p.Ssn, bsonType: BsonType.Int32,
                    algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic);

            var encryptionSchemaBuilder = new CsfleSchemaBuilder()
                .WithType(CollectionNamespace.FromFullName(collectionName), typedBuilder);

            const string expected = """
                                    {
                                      "medicalRecords.patients": {
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
                                        }
                                      }
                                    }
                                    """;
            var parsedExpected = BsonDocument.Parse(expected);

            var builtSchema = encryptionSchemaBuilder.Build();
            Assert.Equal(parsedExpected.Count(), builtSchema.Count);
            foreach (var name in parsedExpected.Names)
            {
                var builtSchemaForName = builtSchema[name];
                var parseExpectedForName = parsedExpected[name];
                Assert.Equal(parsedExpected[name].AsBsonDocument, builtSchema[name]);
            }
        }

        // Taken from the docs, just to have an example case
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