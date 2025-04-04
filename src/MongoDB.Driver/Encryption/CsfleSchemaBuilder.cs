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
    //TODO Add docs

    /// <summary>
    ///
    /// </summary>
    public class CsfleSchemaBuilder
    {
        private readonly Dictionary<string, CsfleTypeSchemaBuilder> _typeSchemaBuilders = new();

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static CsfleTypeSchemaBuilder<T> GetTypeBuilder<T>() => new();  //TODO Do we need this?

        /// <summary>
        ///
        /// </summary>
        /// <param name="collectionNamespace">The namespace to which the encryption schema applies.</param>
        /// <param name="typedBuilder"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public CsfleSchemaBuilder WithType<T>(CollectionNamespace collectionNamespace, CsfleTypeSchemaBuilder<T> typedBuilder)
        {
            _typeSchemaBuilders.Add(collectionNamespace.FullName, typedBuilder);
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="collectionNamespace">The namespace to which the encryption schema applies.</param>
        /// <param name="configure"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public CsfleSchemaBuilder WithType<T>(CollectionNamespace collectionNamespace, Action<CsfleTypeSchemaBuilder<T>> configure)  //TODO Do we want to keep this?
        {
            var typedBuilder = new CsfleTypeSchemaBuilder<T>();
            configure(typedBuilder);
            _typeSchemaBuilders.Add(collectionNamespace.FullName, typedBuilder);
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<string, BsonDocument> Build() => _typeSchemaBuilders.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build());
    }

    /// <summary>
    ///
    /// </summary>
    public abstract class CsfleTypeSchemaBuilder
    {
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public abstract BsonDocument Build();
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    public class CsfleTypeSchemaBuilder<TDocument> : CsfleTypeSchemaBuilder
    {
        private readonly List<SchemaField> _fields = [];
        private readonly List<SchemaPattern> _patterns = [];
        private SchemaMetadata _metadata;

        /// <summary>
        ///
        /// </summary>
        /// <param name="path">The field to be encrypted.</param>
        /// <param name="keyId">The id of the Data Encryption Key to use for encrypting.</param>
        /// <param name="algorithm">The encryption algorithm to use.</param>
        /// <param name="bsonType">The BSON type of the field being encrypted.</param>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> Property(FieldDefinition<TDocument> path, Guid? keyId = null, CsfleEncryptionAlgorithm? algorithm = null, BsonType? bsonType = null)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            _fields.Add(new SchemaSimpleField(path, keyId, algorithm, bsonType));
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path">The field to be encrypted.</param>
        /// <param name="keyId">The id of the Data Encryption Key to use for encrypting.</param>
        /// <param name="algorithm">The encryption algorithm to use.</param>
        /// <param name="bsonType">The BSON type of the field being encrypted.</param>
        /// <typeparam name="TField"></typeparam>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> Property<TField>(Expression<Func<TDocument, TField>> path, Guid? keyId = null, CsfleEncryptionAlgorithm? algorithm = null, BsonType? bsonType = null)
        {
            return Property(new ExpressionFieldDefinition<TDocument, TField>(path), keyId, algorithm, bsonType);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path">The field to use for the nested property.</param>
        /// <param name="configure"></param>
        /// <typeparam name="TField"></typeparam>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> Property<TField>(FieldDefinition<TDocument> path, Action<CsfleTypeSchemaBuilder<TField>> configure)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _fields.Add(new SchemaNestedField<TField>(path, configure));
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path">The field to be encrypted.</param>
        /// <param name="configure"></param>
        /// <typeparam name="TField"></typeparam>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> Property<TField>(Expression<Func<TDocument, TField>> path, Action<CsfleTypeSchemaBuilder<TField>> configure)
        {
            return Property(new ExpressionFieldDefinition<TDocument, TField>(path), configure);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="pattern">The pattern to use.</param>
        /// <param name="keyId">The id of the Data Encryption Key to use for encrypting.</param>
        /// <param name="algorithm">The encryption algorithm to use.</param>
        /// <param name="bsonType">The BSON type of the field being encrypted.</param>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> PatternProperty(string pattern, Guid? keyId = null, CsfleEncryptionAlgorithm? algorithm = null, BsonType? bsonType = null)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                throw new ArgumentException("Input pattern cannot be empty or null", nameof(pattern));
            }

            _patterns.Add(new SchemaSimplePattern(pattern, keyId, algorithm, bsonType));
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path">The field to use for the nested pattern property.</param>
        /// <param name="configure"></param>
        /// <typeparam name="TField"></typeparam>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> PatternProperty<TField>(FieldDefinition<TDocument> path, Action<CsfleTypeSchemaBuilder<TField>> configure)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _patterns.Add(new SchemaNestedPattern<TField>(path, configure));
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path">The field to use for the nested pattern property.</param>
        /// <param name="configure"></param>
        /// <typeparam name="TField"></typeparam>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> PatternProperty<TField>(Expression<Func<TDocument, TField>> path, Action<CsfleTypeSchemaBuilder<TField>> configure)
        {
            return PatternProperty(new ExpressionFieldDefinition<TDocument, TField>(path), configure);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="keyId">The id of the Data Encryption Key to use for encrypting.</param>
        /// <param name="algorithm">The encryption algorithm to use.</param>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> EncryptMetadata(Guid? keyId = null, CsfleEncryptionAlgorithm? algorithm = null )
        {
            _metadata = new SchemaMetadata(keyId, algorithm);
            return this;
        }

        /// <inheritdoc />
        public override BsonDocument Build()
        {
            var schema = new BsonDocument("bsonType", "object");

            if (_metadata is not null)
            {
                schema.Merge(_metadata.Build());
            }

            var args = new RenderArgs<TDocument>(BsonSerializer.LookupSerializer<TDocument>(), BsonSerializer.SerializerRegistry);

            if (_fields.Any())
            {
                var properties = new BsonDocument();

                foreach (var field in _fields)
                {
                    properties.Merge(field.Build(args));
                }

                schema.Add("properties", properties);
            }

            if (_patterns.Any())
            {
                var patternProperties = new BsonDocument();

                foreach (var pattern in _patterns)
                {
                    patternProperties.Merge(pattern.Build(args));
                }

                schema.Add("patternProperties", patternProperties);
            }

            return schema;
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

        private abstract record SchemaField
        {
            public abstract BsonDocument Build(RenderArgs<TDocument> args);
        }

        private record SchemaSimpleField(FieldDefinition<TDocument> Path, Guid? KeyId, CsfleEncryptionAlgorithm? Algorithm, BsonType? BsonType) : SchemaField
        {
            public override BsonDocument Build(RenderArgs<TDocument> args) =>
                new(Path.Render(args).FieldName, new BsonDocument("encrypt", GetEncryptBsonDocument(KeyId, Algorithm, BsonType)));
        }

        private record SchemaNestedField<TField>(FieldDefinition<TDocument> Path, Action<CsfleTypeSchemaBuilder<TField>> Configure) : SchemaField
        {
            public override BsonDocument Build(RenderArgs<TDocument> args)
            {
                var fieldBuilder = new CsfleTypeSchemaBuilder<TField>();
                Configure(fieldBuilder);
                return new BsonDocument(Path.Render(args).FieldName, fieldBuilder.Build());
            }
        }

        private abstract record SchemaPattern
        {
            public abstract BsonDocument Build(RenderArgs<TDocument> args);
        }

        private record SchemaSimplePattern(
            string Pattern,
            Guid? KeyId,
            CsfleEncryptionAlgorithm? Algorithm,
            BsonType? BsonType) : SchemaPattern
        {
            public override BsonDocument Build(RenderArgs<TDocument> args) => new(Pattern, new BsonDocument("encrypt", GetEncryptBsonDocument(KeyId, Algorithm, BsonType)));
        }

        private record SchemaNestedPattern<TField>(
            FieldDefinition<TDocument> Path,
            Action<CsfleTypeSchemaBuilder<TField>> Configure) : SchemaPattern
        {
            public override BsonDocument Build(RenderArgs<TDocument> args)
            {
                var fieldBuilder = new CsfleTypeSchemaBuilder<TField>();
                Configure(fieldBuilder);
                return new BsonDocument(Path.Render(args).FieldName, fieldBuilder.Build());
            }
        }

        private record SchemaMetadata(Guid? KeyId, CsfleEncryptionAlgorithm? Algorithm)
        {
            public BsonDocument Build() => new("encryptMetadata", GetEncryptBsonDocument(KeyId, Algorithm, null));
        }

        private static BsonDocument GetEncryptBsonDocument(Guid? keyId, CsfleEncryptionAlgorithm? algorithm, BsonType? bsonType)
        {
            return new BsonDocument
            {
                { "bsonType", () => MapBsonTypeToString(bsonType!.Value), bsonType is not null },
                { "algorithm", () => MapCsfleEncyptionAlgorithmToString(algorithm!.Value), algorithm is not null },
                {
                    "keyId",
                    () => new BsonArray(new[] { new BsonBinaryData(keyId!.Value, GuidRepresentation.Standard) }),
                    keyId is not null
                },
            };
        }
    }

    /// <summary>
    /// The type of possible encryption algorithms.
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