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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// A builder class for creating Client-Side Field Level Encryption (CSFLE) schemas.
    /// </summary>
    public class CsfleSchemaBuilder
    {
        private readonly Dictionary<string, BsonDocument> _schemas = new();

        private CsfleSchemaBuilder()
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CsfleSchemaBuilder"/> and configures it using the provided action.
        /// </summary>
        /// <param name="configure">An action to configure the schema builder.</param>
        public static CsfleSchemaBuilder Create(Action<CsfleSchemaBuilder> configure)
        {
            var builder = new CsfleSchemaBuilder();
            configure(builder);
            return builder;
        }

        /// <summary>
        /// Adds an encrypted collection schema for a specific collection namespace.
        /// </summary>
        /// <typeparam name="T">The type of the document in the collection.</typeparam>
        /// <param name="collectionNamespace">The namespace of the collection.</param>
        /// <param name="configure">An action to configure the encrypted collection builder.</param>
        /// <returns>The current <see cref="CsfleSchemaBuilder"/> instance.</returns>
        public CsfleSchemaBuilder Encrypt<T>(CollectionNamespace collectionNamespace, Action<EncryptedCollectionBuilder<T>> configure)
        {
            var builder = new EncryptedCollectionBuilder<T>();
            configure(builder);
            _schemas.Add(collectionNamespace.FullName, builder.Build());
            return this;
        }

        /// <summary>
        /// Builds and returns the resulting CSFLE schema.
        /// </summary>
        public Dictionary<string, BsonDocument> Build()
        {
            if (!_schemas.Any())
            {
                throw new InvalidOperationException("No schemas were added. Use Encrypt<T> to add a schema.");
            }

            return _schemas;
        }
    }

    /// <summary>
    /// A builder class for creating encrypted collection schemas.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document in the collection.</typeparam>
    public class EncryptedCollectionBuilder<TDocument>
    {
        private readonly BsonDocument _schema = new("bsonType", "object");
        private readonly RenderArgs<TDocument> _args = new(BsonSerializer.LookupSerializer<TDocument>(), BsonSerializer.SerializerRegistry);

        internal EncryptedCollectionBuilder()
        {
        }

        /// <summary>
        /// Configures encryption metadata for the collection.
        /// </summary>
        /// <param name="keyId">The key ID to use for encryption.</param>
        /// <param name="algorithm">The encryption algorithm to use.</param>
        /// <returns>The current <see cref="EncryptedCollectionBuilder{TDocument}"/> instance.</returns>
        public EncryptedCollectionBuilder<TDocument> EncryptMetadata(Guid? keyId = null, EncryptionAlgorithm? algorithm = null)
        {
            if (keyId is null && algorithm is null)
            {
                throw new ArgumentException("At least one of keyId or algorithm must be specified.");
            }

            _schema["encryptMetadata"] = new BsonDocument
            {
                { "keyId", () => new BsonArray { new BsonBinaryData(keyId!.Value, GuidRepresentation.Standard) }, keyId is not null },
                { "algorithm", () => MapCsfleEncryptionAlgorithmToString(algorithm!.Value), algorithm is not null }
            };
            return this;
        }

        /// <summary>
        /// Adds a pattern property to the schema with encryption settings.
        /// </summary>
        /// <param name="pattern">The regex pattern for the property.</param>
        /// <param name="bsonType">The BSON type of the property.</param>
        /// <param name="algorithm">The encryption algorithm to use.</param>
        /// <param name="keyId">The key ID to use for encryption.</param>
        /// <returns>The current <see cref="EncryptedCollectionBuilder{TDocument}"/> instance.</returns>
        public EncryptedCollectionBuilder<TDocument> PatternProperty(
            string pattern,
            BsonType bsonType,
            EncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
            => PatternProperty(pattern, [bsonType], algorithm, keyId);

        /// <summary>
        /// Adds a pattern property to the schema with encryption settings.
        /// </summary>
        /// <param name="pattern">The regex pattern for the property.</param>
        /// <param name="bsonTypes">The BSON types of the property.</param>
        /// <param name="algorithm">The encryption algorithm to use.</param>
        /// <param name="keyId">The key ID to use for encryption.</param>
        /// <returns>The current <see cref="EncryptedCollectionBuilder{TDocument}"/> instance.</returns>
        public EncryptedCollectionBuilder<TDocument> PatternProperty(
            string pattern,
            IEnumerable<BsonType> bsonTypes = null,
            EncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
        {
            AddToPatternProperties(pattern, CreateEncryptDocument(bsonTypes, algorithm, keyId));
            return this;
        }

        /// <summary>
        /// Adds a nested pattern property to the schema.
        /// </summary>
        /// <typeparam name="TField">The type of the nested field.</typeparam>
        /// <param name="path">The field.</param>
        /// <param name="configure">An action to configure the nested builder.</param>
        /// <returns>The current <see cref="EncryptedCollectionBuilder{TDocument}"/> instance.</returns>
        public EncryptedCollectionBuilder<TDocument> PatternProperty<TField>(
            Expression<Func<TDocument, TField>> path,
            Action<EncryptedCollectionBuilder<TField>> configure)
            => PatternProperty(new ExpressionFieldDefinition<TDocument, TField>(path), configure);

        /// <summary>
        /// Adds a nested pattern property to the schema.
        /// </summary>
        /// <typeparam name="TField">The type of the nested field.</typeparam>
        /// <param name="path">The field.</param>
        /// <param name="configure">An action to configure the nested builder.</param>
        /// <returns>The current <see cref="EncryptedCollectionBuilder{TDocument}"/> instance.</returns>
        public EncryptedCollectionBuilder<TDocument> PatternProperty<TField>(
            FieldDefinition<TDocument> path,
            Action<EncryptedCollectionBuilder<TField>> configure)
        {
            var nestedBuilder = new EncryptedCollectionBuilder<TField>();
            configure(nestedBuilder);

            var fieldName = path.Render(_args).FieldName;

            AddToPatternProperties(fieldName, nestedBuilder.Build());
            return this;
        }

        /// <summary>
        /// Adds a property to the schema with encryption settings.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The field.</param>
        /// <param name="bsonType">The BSON type of the property.</param>
        /// <param name="algorithm">The encryption algorithm to use.</param>
        /// <param name="keyId">The key ID to use for encryption.</param>
        /// <returns>The current <see cref="EncryptedCollectionBuilder{TDocument}"/> instance.</returns>
        public EncryptedCollectionBuilder<TDocument> Property<TField>(
            Expression<Func<TDocument, TField>> path,
            BsonType bsonType,
            EncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
            => Property(path, [bsonType], algorithm, keyId);

        /// <summary>
        /// Adds a property to the schema with encryption settings.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The field.</param>
        /// <param name="bsonTypes">The BSON types of the property.</param>
        /// <param name="algorithm">The encryption algorithm to use.</param>
        /// <param name="keyId">The key ID to use for encryption.</param>
        /// <returns>The current <see cref="EncryptedCollectionBuilder{TDocument}"/> instance.</returns>
        public EncryptedCollectionBuilder<TDocument> Property<TField>(
            Expression<Func<TDocument, TField>> path,
            IEnumerable<BsonType> bsonTypes = null,
            EncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
            => Property(new ExpressionFieldDefinition<TDocument, TField>(path), bsonTypes, algorithm, keyId);

        /// <summary>
        /// Adds a property to the schema with encryption settings.
        /// </summary>
        /// <param name="path">The field.</param>
        /// <param name="bsonType">The BSON type of the property.</param>
        /// <param name="algorithm">The encryption algorithm to use.</param>
        /// <param name="keyId">The key ID to use for encryption.</param>
        /// <returns>The current <see cref="EncryptedCollectionBuilder{TDocument}"/> instance.</returns>
        public EncryptedCollectionBuilder<TDocument> Property(
            FieldDefinition<TDocument> path,
            BsonType bsonType,
            EncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
            => Property(path, [bsonType], algorithm, keyId);

        /// <summary>
        /// Adds a property to the schema with encryption settings.
        /// </summary>
        /// <param name="path">The field.</param>
        /// <param name="bsonTypes">The BSON types of the property.</param>
        /// <param name="algorithm">The encryption algorithm to use.</param>
        /// <param name="keyId">The key ID to use for encryption.</param>
        /// <returns>The current <see cref="EncryptedCollectionBuilder{TDocument}"/> instance.</returns>
        public EncryptedCollectionBuilder<TDocument> Property(
            FieldDefinition<TDocument> path,
            IEnumerable<BsonType> bsonTypes = null,
            EncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
        {
            var fieldName = path.Render(_args).FieldName;
            AddToProperties(fieldName, CreateEncryptDocument(bsonTypes, algorithm, keyId));
            return this;
        }

        /// <summary>
        /// Adds a nested property to the schema.
        /// </summary>
        /// <typeparam name="TField">The type of the nested field.</typeparam>
        /// <param name="path">The field.</param>
        /// <param name="configure">An action to configure the nested builder.</param>
        /// <returns>The current <see cref="EncryptedCollectionBuilder{TDocument}"/> instance.</returns>
        public EncryptedCollectionBuilder<TDocument> Property<TField>(
            Expression<Func<TDocument, TField>> path,
            Action<EncryptedCollectionBuilder<TField>> configure)
            => Property(new ExpressionFieldDefinition<TDocument, TField>(path), configure);


        /// <summary>
        /// Adds a nested property to the schema.
        /// </summary>
        /// <typeparam name="TField">The type of the nested field.</typeparam>
        /// <param name="path">The field.</param>
        /// <param name="configure">An action to configure the nested builder.</param>
        /// <returns>The current <see cref="EncryptedCollectionBuilder{TDocument}"/> instance.</returns>
        public EncryptedCollectionBuilder<TDocument> Property<TField>(
            FieldDefinition<TDocument> path,
            Action<EncryptedCollectionBuilder<TField>> configure)
        {
            var nestedBuilder = new EncryptedCollectionBuilder<TField>();
            configure(nestedBuilder);

            var fieldName = path.Render(_args).FieldName;
            AddToProperties(fieldName, nestedBuilder.Build());
            return this;
        }

        internal BsonDocument Build() => _schema;

        private static BsonDocument CreateEncryptDocument(
            IEnumerable<BsonType> bsonTypes = null,
            EncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
        {
            BsonValue bsonTypeVal = null;

            if (bsonTypes != null)
            {
                var convertedBsonTypes = bsonTypes.Select(type => type.ToServerString()).ToList();

                if (convertedBsonTypes.Count == 0)
                {
                    throw new ArgumentException("At least one BSON type must be specified.", nameof(bsonTypes));
                }

                bsonTypeVal = convertedBsonTypes.Count == 1
                    ? convertedBsonTypes[0]
                    : new BsonArray(convertedBsonTypes);
            }

            return new BsonDocument
            {
                { "encrypt", new BsonDocument
                    {
                        { "bsonType", () => bsonTypeVal, bsonTypeVal is not null },
                        { "algorithm", () => MapCsfleEncryptionAlgorithmToString(algorithm!.Value), algorithm is not null },
                        {
                            "keyId",
                            () => new BsonArray(new[] { new BsonBinaryData(keyId!.Value, GuidRepresentation.Standard) }),
                            keyId is not null
                        },
                    }
                }
            };
        }

        private void AddToPatternProperties(string field, BsonDocument document)
        {
            if (!_schema.TryGetValue("patternProperties", out var value))
            {
                value = new BsonDocument();
                _schema["patternProperties"] = value;
            }
            var patternProperties = value.AsBsonDocument;
            patternProperties[field] = document;
        }

        private void AddToProperties(string field, BsonDocument document)
        {
            if (!_schema.TryGetValue("properties", out var value))
            {
                value = new BsonDocument();
                _schema["properties"] = value;
            }
            var properties = value.AsBsonDocument;
            properties[field] = document;
        }

        private static string MapCsfleEncryptionAlgorithmToString(EncryptionAlgorithm algorithm)
        {
            return algorithm switch
            {
                EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random => "AEAD_AES_256_CBC_HMAC_SHA_512-Random",
                EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic => "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic",
                _ => throw new ArgumentException($"Unexpected algorithm type: {algorithm}.", nameof(algorithm))
            };
        }
    }
}