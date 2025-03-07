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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver.Encryption
{
    //TODO Need to specify this is for CSFLE (add the name everywhere ...?)
    //TODO Do we need to do local validation of the schema?
    internal class EncryptionSchemaBuilder
    {
        public static TypedEncryptionSchemaBuilder<T> GetTypedBuilder<T>()
        {
            return new TypedEncryptionSchemaBuilder<T>();
        }

        public EncryptionSchemaBuilder WithType<T>(CollectionNamespace collectionNamespace, TypedEncryptionSchemaBuilder<T> typedBuilder)
        {
            return this;
        }

        public EncryptionSchemaBuilder WithType<T>(CollectionNamespace collectionNamespace, Action<TypedEncryptionSchemaBuilder<T>> configure)
        {
            return this;
        }

        public IReadOnlyDictionary<string, BsonDocument> Build()
        {
            return null;
        }
    }

    internal class TypedEncryptionSchemaBuilder<TDocument>
    {
        public TypedEncryptionSchemaBuilder<TDocument> WithField(FieldDefinition<TDocument> path, Guid? keyId = null, CsfleEncyptionAlgorithm? algorithm = null, BsonType? bsonType = null)
        {
            return this;
        }

        public TypedEncryptionSchemaBuilder<TDocument> WithField<TField>(Expression<Func<TDocument, TField>> path, Guid? keyId = null, CsfleEncyptionAlgorithm? algorithm = null, BsonType? bsonType = null)
        {
            return this;
        }

        public TypedEncryptionSchemaBuilder<TDocument> WithNestedField<TField>(FieldDefinition<TDocument> path, Action<TypedEncryptionSchemaBuilder<TField>> configure)
        {
            return this;
        }

        public TypedEncryptionSchemaBuilder<TDocument> WithNestedField<TField>(Expression<Func<TDocument, TField>> path, Action<TypedEncryptionSchemaBuilder<TField>> configure)
        {
            return this;
        }

        public TypedEncryptionSchemaBuilder<TDocument> WithPattern(string pattern, Guid? keyId = null, CsfleEncyptionAlgorithm? algorithm = null, BsonType? bsonType = null)
        {
            return this;
        }

        public TypedEncryptionSchemaBuilder<TDocument> WithMetadata(Guid? keyId = null, CsfleEncyptionAlgorithm? algorithm = null )
        {
            return this;
        }

        public BsonDocument Build()
        {
            return null;
        }

        public static void Example()
        {
            var myKeyId = Guid.NewGuid();

            var typedBuilder = EncryptionSchemaBuilder.GetTypedBuilder<Patient>()
                .WithMetadata(keyId: myKeyId)
                .WithField("bloodType", bsonType: BsonType.String, algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random) //string field
                .WithField(p => p.Ssn, bsonType: BsonType.Int32, algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic) //expression field
                .WithField(p => p.MedicalRecords, bsonType: BsonType.Int32, algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic)  //expression field with array
                .WithNestedField(p => p.Insurance, insurance => insurance
                    .WithField(i => i.PolicyNumber)) //nested field
                .WithNestedField<Insurance>("insurance", insurance => insurance
                    .WithField(i => i.PolicyNumber)) //nested field with string
                .WithPattern("ins*", algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random); //with pattern

            var encryptionSchemaBuilder = new EncryptionSchemaBuilder()
                .WithType(CollectionNamespace.FromFullName("db.coll1"), typedBuilder)  //with builder
                .WithType<Patient>(CollectionNamespace.FromFullName("db.coll2"), builder => builder //with configure
                        .WithField("bloodType", bsonType: BsonType.String,
                            algorithm: CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random)
                );

            var schema = encryptionSchemaBuilder.Build();  //This can be passed to AutoEncryptionOptions
        }
    }

    internal enum CsfleEncyptionAlgorithm
    {
        AEAD_AES_256_CBC_HMAC_SHA_512_Random,
        AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic
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