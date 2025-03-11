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
            var typedBuilder = CsfleSchemaBuilder.GetTypeBuilder<Patient>()
                .Encrypt("bloodType", bsonType: BsonType.String,
                    algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                .Encrypt(p => p.Ssn, bsonType: BsonType.Int32,
                    algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic);

            var expected = "{}";
            var parsedExpected = BsonDocument.Parse(expected);

            Assert.Equal(parsedExpected, typedBuilder.Build());
        }

        internal static void Example()
        {
            var myKeyId = Guid.NewGuid();

            var typedBuilder = CsfleSchemaBuilder.GetTypeBuilder<Patient>()
                .EncryptMetadata(keyId: myKeyId)
                .Encrypt("bloodType", bsonType: BsonType.String, algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random) //string field
                .Encrypt(p => p.Ssn, bsonType: BsonType.Int32, algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic) //expression field
                .Encrypt(p => p.MedicalRecords, bsonType: BsonType.Int32, algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)  //expression field with array
                .Encrypt(p => p.Insurance, insurance => insurance
                    .Encrypt(i => i.PolicyNumber)) //nested field
                .Encrypt<Insurance>("insurance", insurance => insurance
                    .Encrypt(i => i.PolicyNumber)) //nested field with string
                .PatternProperties("ins*", algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random); //with pattern

            var encryptionSchemaBuilder = new CsfleSchemaBuilder()
                .WithType(CollectionNamespace.FromFullName("db.coll1"), typedBuilder)  //with builder
                .WithType<Patient>(CollectionNamespace.FromFullName("db.coll2"), builder => builder //with configure
                    .Encrypt("bloodType", bsonType: BsonType.String,
                        algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                );

            var schema = encryptionSchemaBuilder.Build();  //This can be passed to AutoEncryptionOptions
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