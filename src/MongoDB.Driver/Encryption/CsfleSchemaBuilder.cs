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
        public static CsfleTypeSchemaBuilder<T> GetTypeBuilder<T>() => new();

        /// <summary>
        ///
        /// </summary>
        /// <param name="collectionNamespace"></param>
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
        /// <param name="collectionNamespace"></param>
        /// <param name="configure"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public CsfleSchemaBuilder WithType<T>(CollectionNamespace collectionNamespace, Action<CsfleTypeSchemaBuilder<T>> configure)
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
        private readonly List<SchemaNestedField> _nestedFields = [];
        private readonly List<SchemaPattern> _patterns = [];
        private SchemaMetadata _metadata;

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <param name="keyId"></param>
        /// <param name="algorithm"></param>
        /// <param name="bsonType"></param>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> Encrypt(FieldDefinition<TDocument> path, Guid? keyId = null, CsfleEncryptionAlgorithm? algorithm = null, BsonType? bsonType = null)
        {
            _fields.Add(new SchemaField(path, keyId, algorithm, bsonType));
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <param name="keyId"></param>
        /// <param name="algorithm"></param>
        /// <param name="bsonType"></param>
        /// <typeparam name="TField"></typeparam>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> Encrypt<TField>(Expression<Func<TDocument, TField>> path, Guid? keyId = null, CsfleEncryptionAlgorithm? algorithm = null, BsonType? bsonType = null)
        {
            return Encrypt(new ExpressionFieldDefinition<TDocument, TField>(path), keyId, algorithm, bsonType);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <param name="configure"></param>
        /// <typeparam name="TField"></typeparam>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> Encrypt<TField>(FieldDefinition<TDocument> path, Action<CsfleTypeSchemaBuilder<TField>> configure)
        {
            _nestedFields.Add(new SchemaNestedField<TField>(path, configure));
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <param name="configure"></param>
        /// <typeparam name="TField"></typeparam>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> Encrypt<TField>(Expression<Func<TDocument, TField>> path, Action<CsfleTypeSchemaBuilder<TField>> configure)
        {
            return Encrypt(new ExpressionFieldDefinition<TDocument, TField>(path), configure);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="keyId"></param>
        /// <param name="algorithm"></param>
        /// <param name="bsonType"></param>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> PatternProperties(string pattern, Guid? keyId = null, CsfleEncryptionAlgorithm? algorithm = null, BsonType? bsonType = null)  //TODO This is not correct,
        {
            _patterns.Add(new SchemaPattern(pattern, keyId, algorithm, bsonType));
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="keyId"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> EncryptMetadata(Guid? keyId = null, CsfleEncryptionAlgorithm? algorithm = null )
        {
            _metadata = new SchemaMetadata(keyId, algorithm);
            return this;
        }

        /// <inheritdoc />
        public override BsonDocument Build()
        {
            var schema = new BsonDocument { { "bsonType", "object" } };
            var args = new RenderArgs<TDocument>(BsonSerializer.LookupSerializer<TDocument>(), BsonSerializer.SerializerRegistry);
            var properties = new BsonDocument();

            if (_metadata is not null)
            {
                schema.Merge(_metadata.Build());
            }

            foreach (var nestedField in _nestedFields)
            {
                properties.Merge(nestedField.Build(args));
            }

            foreach (var field in _fields)
            {
                properties.Merge(field.Build(args));
            }

            if (properties.Any())
            {
                schema.Add("properties", properties);
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

        private static string MapBsonTypeToString(BsonType type)  //TODO Taken from AstTypeFilterOperation
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

        private record SchemaField(FieldDefinition<TDocument> Path, Guid? KeyId, CsfleEncryptionAlgorithm? Algorithm, BsonType? BsonType)
        {
            public BsonDocument Build(RenderArgs<TDocument> args)
            {
                return new BsonDocument
                {
                    {
                        Path.Render(args).FieldName, new BsonDocument
                        {
                            { "encrypt", GetEncryptBsonDocument(KeyId, Algorithm, BsonType) }
                        }
                    }
                };
            }
        }

        private abstract record SchemaNestedField
        {
            public abstract BsonDocument Build(RenderArgs<TDocument> args);
        }

        private record SchemaNestedField<TField>(FieldDefinition<TDocument> Path, Action<CsfleTypeSchemaBuilder<TField>> Configure) : SchemaNestedField
        {
            public override BsonDocument Build(RenderArgs<TDocument> args)
            {
                var fieldBuilder = new CsfleTypeSchemaBuilder<TField>();
                Configure(fieldBuilder);
                var builtInternalSchema = fieldBuilder.Build();

                return new BsonDocument
                {
                    { Path.Render(args).FieldName, builtInternalSchema }
                };
            }
        }

        private record SchemaPattern(
            string Pattern,
            Guid? KeyId,
            CsfleEncryptionAlgorithm? Algorithm,
            BsonType? BsonType)
        {
            public BsonDocument Build()
            {
                return new BsonDocument
                {
                    {
                        "pattern", new BsonDocument
                        {
                            { "encrypt", GetEncryptBsonDocument(KeyId, Algorithm, BsonType) }
                        }
                    }
                };
            }
        }

        private record SchemaMetadata(Guid? KeyId, CsfleEncryptionAlgorithm? Algorithm)
        {
            public BsonDocument Build()
            {
                return new BsonDocument
                {
                    { "encryptMetadata", GetEncryptBsonDocument(KeyId, Algorithm, null)}
                };
            }
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
    ///
    /// </summary>
    public enum CsfleEncryptionAlgorithm
    {
        /// <summary>
        ///
        /// </summary>
        AEAD_AES_256_CBC_HMAC_SHA_512_Random,
        /// <summary>
        ///
        /// </summary>
        AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic
    }
}