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
        private Dictionary<string, CsfleTypeSchemaBuilder> _typeSchemaBuilders = new();

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static CsfleTypeSchemaBuilder<T> GetTypeBuilder<T>()  //TODO Maybe we should remove this...?
        {
            return new CsfleTypeSchemaBuilder<T>();
        }

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
        public IReadOnlyDictionary<string, BsonDocument> Build()
        {
            return _typeSchemaBuilders.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build());
        }
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
        private List<SchemaField> _fields;
        private List<SchemaNestedField> _nestedFields;
        private List<SchemaPattern> _patterns;
        private SchemaMetadata _metadata;

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <param name="keyId"></param>
        /// <param name="algorithm"></param>
        /// <param name="bsonType"></param>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> Encrypt(FieldDefinition<TDocument> path, Guid? keyId = null, CsfleEncyptionAlgorithm? algorithm = null, BsonType? bsonType = null)
        {
            _fields ??= [];
            _fields.Add(new SchemaField(path, keyId, algorithm, bsonType));
            return this;
        }

        //TODO We need an overload that accepts an array of bsonTypes (it's supported)

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <param name="keyId"></param>
        /// <param name="algorithm"></param>
        /// <param name="bsonType"></param>
        /// <typeparam name="TField"></typeparam>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> Encrypt<TField>(Expression<Func<TDocument, TField>> path, Guid? keyId = null, CsfleEncyptionAlgorithm? algorithm = null, BsonType? bsonType = null)
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
            _nestedFields ??= [];
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
        public CsfleTypeSchemaBuilder<TDocument> PatternProperties(string pattern, Guid? keyId = null, CsfleEncyptionAlgorithm? algorithm = null, BsonType? bsonType = null)  //TODO This is not correct,
        {
            _patterns ??= [];
            _patterns.Add(new SchemaPattern(pattern, keyId, algorithm, bsonType));
            return this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="keyId"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public CsfleTypeSchemaBuilder<TDocument> EncryptMetadata(Guid? keyId = null, CsfleEncyptionAlgorithm? algorithm = null )
        {
            _metadata = new SchemaMetadata(keyId, algorithm);
            return this;
        }

        /// <inheritdoc />
        public override BsonDocument Build()
        {
            var schema = new BsonDocument();
            var args = new RenderArgs<TDocument>(BsonSerializer.LookupSerializer<TDocument>(), BsonSerializer.SerializerRegistry);

            schema.Add("bsonType", "object");

            if (_metadata is not null)
            {
                schema.Merge(_metadata.Build(args));
            }

            var properties = new BsonDocument();

            if (_nestedFields is not null)
            {
                foreach (var nestedFields in _nestedFields)
                {
                    properties.Merge(nestedFields.Build(args));
                }
            }

            if (_fields is not null)
            {
                foreach (var field in _fields)
                {
                    properties.Merge(field.Build(args));
                }
            }

            if (properties.Any())
            {
                schema.Add("properties", properties);
            }

            return schema;
        }

        private static string MapCsfleEncyptionAlgorithmToString(CsfleEncyptionAlgorithm algorithm)
        {
            return algorithm switch
            {
                CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random => "AEAD_AES_256_CBC_HMAC_SHA_512-Random",
                CsfleEncyptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic => "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic",
                _ => throw new InvalidOperationException()
            };
        }

        private static string MapBsonTypeToString(BsonType type)  //TODO Taken from AstTypeFilterOperation
        {
            switch (type)
            {
                case BsonType.Array: return "array";
                case BsonType.Binary: return "binData";
                case BsonType.Boolean: return "bool";
                case BsonType.DateTime: return "date";
                case BsonType.Decimal128: return "decimal";
                case BsonType.Document: return "object";
                case BsonType.Double: return "double";
                case BsonType.Int32: return "int";
                case BsonType.Int64: return "long";
                case BsonType.JavaScript: return "javascript";
                case BsonType.JavaScriptWithScope: return "javascriptWithScope";
                case BsonType.MaxKey: return "maxKey";
                case BsonType.MinKey: return "minKey";
                case BsonType.Null: return "null";
                case BsonType.ObjectId: return "objectId";
                case BsonType.RegularExpression: return "regex";
                case BsonType.String: return "string";
                case BsonType.Symbol: return "symbol";
                case BsonType.Timestamp: return "timestamp";
                case BsonType.Undefined: return "undefined";
                default: throw new ArgumentException($"Unexpected BSON type: {type}.", nameof(type));
            }
        }

        private class SchemaField
        {
            private FieldDefinition<TDocument> Path { get; }  //TODO These could all be private properties
            private Guid? KeyId { get; }
            private CsfleEncyptionAlgorithm? Algorithm { get; }
            private BsonType? BsonType { get; }

            public SchemaField(FieldDefinition<TDocument> path, Guid? keyId, CsfleEncyptionAlgorithm? algorithm, BsonType? bsonType)
            {
                Path = path;
                KeyId = keyId;
                Algorithm = algorithm;
                BsonType = bsonType;
            }

            public BsonDocument Build(RenderArgs<TDocument> args)
            {
                return new BsonDocument
                {
                    {
                        Path.Render(args).FieldName, new BsonDocument
                        {
                            {
                                "encrypt", new BsonDocument
                                {
                                    { "bsonType", () => MapBsonTypeToString(BsonType!.Value), BsonType is not null },
                                    { "algorithm", () => MapCsfleEncyptionAlgorithmToString(Algorithm!.Value), Algorithm is not null },
                                    { "keyId", () => new BsonArray( new [] {new BsonBinaryData(KeyId!.Value, GuidRepresentation.Standard) }), KeyId is not null },
                                }
                            }
                        }
                    }
                };
            }
        }

        private abstract class SchemaNestedField
        {
            public abstract BsonDocument Build(RenderArgs<TDocument> args);
        }

        private class SchemaNestedField<TField> : SchemaNestedField
        {
            public FieldDefinition<TDocument> Path { get; }
            public Action<CsfleTypeSchemaBuilder<TField>> Configure { get; }

            public SchemaNestedField(FieldDefinition<TDocument> path, Action<CsfleTypeSchemaBuilder<TField>> configure)
            {
                Path = path;
                Configure = configure;
            }

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

        private class SchemaPattern
        {
            public string Pattern { get; }
            public Guid? KeyId { get; }
            public CsfleEncyptionAlgorithm? Algorithm { get; }
            public BsonType? BsonType { get; }

            public SchemaPattern(string pattern, Guid? keyId, CsfleEncyptionAlgorithm? algorithm, BsonType? bsonType)
            {
                Pattern = pattern;
                KeyId = keyId;
                Algorithm = algorithm;
                BsonType = bsonType;
            }
        }

        private class SchemaMetadata
        {
            public Guid? KeyId { get; }
            public CsfleEncyptionAlgorithm? Algorithm { get; }

            public SchemaMetadata(Guid? keyId, CsfleEncyptionAlgorithm? algorithm)
            {
                KeyId = keyId;
                Algorithm = algorithm;
            }

            public BsonDocument Build(RenderArgs<TDocument> args)
            {
                return new BsonDocument
                {
                    {
                        "encryptMetadata", new BsonDocument
                        {
                            { "algorithm", () => MapCsfleEncyptionAlgorithmToString(Algorithm!.Value), Algorithm is not null },
                            { "keyId", () => new BsonArray( new [] {new BsonBinaryData(KeyId!.Value, GuidRepresentation.Standard) }), KeyId is not null },
                        }
                    }
                };
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    public enum CsfleEncyptionAlgorithm
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