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
    /// //TODO
    /// </summary>
    public class CsfleSchemaBuilder
    {
        private readonly Dictionary<string, BsonDocument> _schemas = new();

        private CsfleSchemaBuilder()
        {
        }

        /// <summary>
        /// //TODO
        /// </summary>
        public static CsfleSchemaBuilder Create(Action<CsfleSchemaBuilder> configure)
        {
            var builder = new CsfleSchemaBuilder();
            configure(builder);
            return builder;
        }

        /// <summary>
        /// //TODO
        /// </summary>
        public CsfleSchemaBuilder Encrypt<T>(string collectionNamespace, Action<EncryptedCollectionBuilder<T>> configure)
        {
            var builder = new EncryptedCollectionBuilder<T>();
            configure(builder);
            _schemas.Add(collectionNamespace, builder.Build());
            return this;
        }

        /// <summary>
        /// Builds and returns the resulting CSFLE schema.
        /// </summary>
        public IDictionary<string, BsonDocument> Build() => _schemas;
    }

    /// <summary>
    /// //TODO
    /// </summary>
    public class EncryptedCollectionBuilder<TDocument>
    {
        private readonly BsonDocument _schema = new("bsonType", "object");
        private readonly RenderArgs<TDocument> _args = new(BsonSerializer.LookupSerializer<TDocument>(), BsonSerializer.SerializerRegistry);

        /// <summary>
        /// //TODO
        /// </summary>
        internal EncryptedCollectionBuilder()
        {
        }

        /// <summary>
        /// //TODO
        /// </summary>
        public EncryptedCollectionBuilder<TDocument> EncryptMetadata(Guid? keyId = null, CsfleEncryptionAlgorithm? algorithm = null)
        {
            _schema["encryptMetadata"] = new BsonDocument
            {
                { "keyId", () => new BsonArray { new BsonBinaryData(keyId!.Value, GuidRepresentation.Standard) }, keyId is not null },
                { "algorithm", () => MapCsfleEncyptionAlgorithmToString(algorithm!.Value), algorithm is not null }
            };
            return this;
        }

        /// <summary>
        /// //TODO
        /// </summary>
        public EncryptedCollectionBuilder<TDocument> PatternProperty(
            string pattern,
            BsonType bsonType,
            CsfleEncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
            => PatternProperty(pattern, [bsonType], algorithm, keyId);

        /// <summary>
        /// //TODO
        /// </summary>
        public EncryptedCollectionBuilder<TDocument> PatternProperty(
            string pattern,
            IEnumerable<BsonType> bsonTypes,
            CsfleEncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
        {
            AddToPatternProperties(pattern, CreateEncryptDocument(bsonTypes, algorithm, keyId));
            return this;
        }

        /// <summary>
        /// //TODO
        /// </summary>
        public EncryptedCollectionBuilder<TDocument> PatternProperty<TField>(
            Expression<Func<TDocument, TField>> path,
            Action<EncryptedCollectionBuilder<TField>> configure)
            => PatternProperty(new ExpressionFieldDefinition<TDocument, TField>(path), configure);

        /// <summary>
        /// //TODO
        /// </summary>
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
        /// //TODO
        /// </summary>
        public EncryptedCollectionBuilder<TDocument> Property<TField>(
            Expression<Func<TDocument, TField>> path,
            BsonType bsonType,
            CsfleEncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
            => Property(path, [bsonType], algorithm, keyId);

        /// <summary>
        /// //TODO
        /// </summary>
        public EncryptedCollectionBuilder<TDocument> Property<TField>(
            Expression<Func<TDocument, TField>> path,
            IEnumerable<BsonType> bsonTypes,
            CsfleEncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
            => Property(new ExpressionFieldDefinition<TDocument, TField>(path), bsonTypes, algorithm, keyId);

        /// <summary>
        /// //TODO
        /// </summary>
        public EncryptedCollectionBuilder<TDocument> Property(
            FieldDefinition<TDocument> path,
            BsonType bsonType,
            CsfleEncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
            => Property(path, [bsonType], algorithm, keyId);

        /// <summary>
        /// //TODO
        /// </summary>
        public EncryptedCollectionBuilder<TDocument> Property(
            FieldDefinition<TDocument> path,
            IEnumerable<BsonType> bsonTypes,
            CsfleEncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
        {
            var fieldName = path.Render(_args).FieldName;
            AddToProperties(fieldName, CreateEncryptDocument(bsonTypes, algorithm, keyId));
            return this;
        }

        /// <summary>
        /// //TODO
        /// </summary>
        public EncryptedCollectionBuilder<TDocument> Property<TField>(
            Expression<Func<TDocument, TField>> path,
            Action<EncryptedCollectionBuilder<TField>> configure)
            => Property(new ExpressionFieldDefinition<TDocument, TField>(path), configure);

        /// <summary>
        /// //TODO
        /// </summary>
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
            IEnumerable<BsonType> bsonTypes,
            CsfleEncryptionAlgorithm? algorithm = null,
            Guid? keyId = null)
        {
            var convertedBsonTypes = bsonTypes.Select(MapBsonTypeToString).ToList();
            BsonValue bsonTypeVal = convertedBsonTypes.Count == 1
                ? convertedBsonTypes[0]
                : new BsonArray(convertedBsonTypes);

            return new BsonDocument
            {
                { "encrypt", new BsonDocument
                    {
                        { "bsonType", bsonTypeVal },
                        { "algorithm", () => MapCsfleEncyptionAlgorithmToString(algorithm!.Value), algorithm is not null },
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

        private static string MapBsonTypeToString(BsonType type)  //TODO Taken from AstTypeFilterOperation, do we have a common place where this could go?
        {
            return type switch
            {
                BsonType.Array => "array",
                BsonType.Binary => "binData",
                BsonType.Boolean => "bool",
                BsonType.DateTime => "date",
                BsonType.Decimal128 => "decimal",
                BsonType.Document => "object",
                BsonType.Double => "double",
                BsonType.Int32 => "int",
                BsonType.Int64 => "long",
                BsonType.JavaScript => "javascript",
                BsonType.JavaScriptWithScope => "javascriptWithScope",
                BsonType.MaxKey => "maxKey",
                BsonType.MinKey => "minKey",
                BsonType.Null => "null",
                BsonType.ObjectId => "objectId",
                BsonType.RegularExpression => "regex",
                BsonType.String => "string",
                BsonType.Symbol => "symbol",
                BsonType.Timestamp => "timestamp",
                BsonType.Undefined => "undefined",
                _ => throw new ArgumentException($"Unexpected BSON type: {type}.", nameof(type))
            };
        }

        private static string MapCsfleEncyptionAlgorithmToString(CsfleEncryptionAlgorithm algorithm)
        {
            return algorithm switch
            {
                CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random => "AEAD_AES_256_CBC_HMAC_SHA_512-Random",
                CsfleEncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic => "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic",
                _ => throw new ArgumentException($"Unexpected algorithm type: {algorithm}.", nameof(algorithm))
            };
        }
    }

    /// <summary>
    /// The type of possible encryption algorithms.  //TODO Maybe we need a more generic name?
    /// </summary>
    public enum CsfleEncryptionAlgorithm
    {
        /// <summary>
        /// Randomized encryption algorithm.
        /// </summary>
        AEAD_AES_256_CBC_HMAC_SHA_512_Random,

        /// <summary>
        /// Deterministic encryption algorithm.
        /// </summary>
        AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic
    }
}