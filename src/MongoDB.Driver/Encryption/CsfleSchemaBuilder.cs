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
    ///
    /// </summary>
    public class CsfleSchemaBuilder
    {
        private readonly Dictionary<string, TypedBuilder> _typedBuilders = [];
        private CsfleSchemaBuilder()
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static CsfleSchemaBuilder Create(Action<CsfleSchemaBuilder> configure)
        {
            var builder = new CsfleSchemaBuilder();
            configure(builder);
            return builder;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="collectionNamespace"></param>
        /// <param name="configure"></param>
        /// <typeparam name="TDocument"></typeparam>
        public void Encrypt<TDocument>(CollectionNamespace collectionNamespace, Action<TypedBuilder<TDocument>> configure)
        {
            var typedBuilder = new TypedBuilder<TDocument>();
            configure(typedBuilder);
            _typedBuilders.Add(collectionNamespace.FullName, typedBuilder);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<string, BsonDocument> Build() => _typedBuilders.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build());
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TSelf"></typeparam>
    public class ElementBuilder<TSelf> where TSelf : ElementBuilder<TSelf>
    {
        private protected CsfleEncryptionAlgorithm? _algorithm;
        private protected Guid? _keyId;

        /// <summary>
        ///
        /// </summary>
        /// <param name="keyId"></param>
        /// <returns></returns>
        public TSelf WithKeyId(Guid keyId)
        {
            _keyId = keyId;
            return (TSelf)this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public TSelf WithAlgorithm(CsfleEncryptionAlgorithm algorithm)
        {
            _algorithm = algorithm;
            return (TSelf)this;
        }

        internal static BsonDocument GetEncryptBsonDocument(Guid? keyId, CsfleEncryptionAlgorithm? algorithm, List<BsonType> bsonTypes)
        {
            var bsonType = bsonTypes?.First(); //TODO need to support multiple types

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
    ///
    /// </summary>
    public class EncryptMetadataBuilder : ElementBuilder<EncryptMetadataBuilder>
    {
        internal BsonDocument Build() => new("encryptMetadata", GetEncryptBsonDocument(_keyId, _algorithm, null));
    }



    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TSelf"></typeparam>
    /// <typeparam name="TDocument"></typeparam>
    public abstract class SinglePropertyBuilder<TSelf, TDocument> : ElementBuilder<SinglePropertyBuilder<TSelf, TDocument>> where TSelf : SinglePropertyBuilder<TSelf, TDocument>
    {
        private protected List<BsonType> _bsonTypes;

        /// <summary>
        ///
        /// </summary>
        /// <param name="bsonType"></param>
        /// <returns></returns>
        public TSelf WithBsonType(BsonType bsonType)
        {
            _bsonTypes = [bsonType];
            return (TSelf)this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="bsonTypes"></param>
        /// <returns></returns>
        public TSelf WithBsonTypes(IEnumerable<BsonType> bsonTypes)
        {
            _bsonTypes = [..bsonTypes];
            return (TSelf)this;
        }

        internal abstract BsonDocument Build(RenderArgs<TDocument> args);
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    public class PropertyBuilder<TDocument> : SinglePropertyBuilder<PropertyBuilder<TDocument>, TDocument>
    {
        private readonly FieldDefinition<TDocument> _path;

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        public PropertyBuilder(FieldDefinition<TDocument> path)
        {
            _path = path;
        }

        internal override BsonDocument Build(RenderArgs<TDocument> args)
        {
            return new BsonDocument(_path.Render(args).FieldName, new BsonDocument("encrypt", GetEncryptBsonDocument(_keyId, _algorithm, _bsonTypes)));
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    public class PatternPropertyBuilder<TDocument> : SinglePropertyBuilder<PatternPropertyBuilder<TDocument>, TDocument>
    {
        private readonly string _pattern;

        /// <summary>
        ///
        /// </summary>
        /// <param name="pattern"></param>
        public PatternPropertyBuilder(string pattern)
        {
            _pattern = pattern;
        }

        internal override BsonDocument Build(RenderArgs<TDocument> args)
        {
            return new BsonDocument(_pattern, new BsonDocument("encrypt", GetEncryptBsonDocument(_keyId, _algorithm, _bsonTypes)));
        }
    }

    /// <summary>
    ///
    /// </summary>
    public abstract class SubdocumentPropertyBuilder<TDocument>
    {
        internal abstract BsonDocument Build(RenderArgs<TDocument> args);
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    /// <typeparam name="TField"></typeparam>
    public class NestedPropertyBuilder<TDocument, TField> : SubdocumentPropertyBuilder<TDocument>
    {
        private readonly FieldDefinition<TDocument> _path;
        private readonly Action<TypedBuilder<TField>> _configure;

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <param name="configure"></param>
        public NestedPropertyBuilder(FieldDefinition<TDocument> path, Action<TypedBuilder<TField>> configure)
        {
            _path = path;
            _configure = configure;
        }

        internal override BsonDocument Build(RenderArgs<TDocument> args)
        {
            var fieldBuilder = new TypedBuilder<TField>();
            _configure(fieldBuilder);
            return new BsonDocument(_path.Render(args).FieldName, fieldBuilder.Build());
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    /// <typeparam name="TField"></typeparam>
    public class NestedPatternPropertyBuilder<TDocument, TField> : SubdocumentPropertyBuilder<TDocument>
    {
        private readonly FieldDefinition<TDocument> _path;
        private readonly Action<TypedBuilder<TField>> _configure;

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <param name="configure"></param>
        public NestedPatternPropertyBuilder(FieldDefinition<TDocument> path, Action<TypedBuilder<TField>> configure)
        {
            _path = path;
            _configure = configure;
        }

        internal override BsonDocument Build(RenderArgs<TDocument> args)
        {
            var fieldBuilder = new TypedBuilder<TField>();
            _configure(fieldBuilder);
            return new BsonDocument(_path.Render(args).FieldName, fieldBuilder.Build());
        }
    }

    /// <summary>
    ///
    /// </summary>
    public abstract class TypedBuilder
    {
        internal abstract BsonDocument Build();
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    public class TypedBuilder<TDocument> : TypedBuilder
    {
        private readonly List<SubdocumentPropertyBuilder<TDocument>> _subdocumentProperties = [];
        private readonly List<PropertyBuilder<TDocument>> _properties = [];
        private EncryptMetadataBuilder _metadata;

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public EncryptMetadataBuilder EncryptMetadata()
        {
            _metadata = new EncryptMetadataBuilder();
            return _metadata;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public PropertyBuilder<TDocument> Property(FieldDefinition<TDocument> path)
        {
            var property = new PropertyBuilder<TDocument>(path);
            _properties.Add(property);
            return property;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public PropertyBuilder<TDocument> Property<TField>(Expression<Func<TDocument, TField>> path)
        {
            return Property(new ExpressionFieldDefinition<TDocument, TField>(path));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public PatternPropertyBuilder<TDocument> PatternProperty(string pattern)
        {
            var property = new PatternPropertyBuilder<TDocument>(pattern);
            _properties.Add(property);
            return property;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public NestedPropertyBuilder<TDocument, TField> NestedProperty<TField>(FieldDefinition<TDocument> path, Action<TypedBuilder<TField>> configure)
        {
            var nestedProperty = new NestedPropertyBuilder<TDocument,TField>(path, configure);
            _subdocumentProperties.Add(nestedProperty);
            return nestedProperty;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public NestedPropertyBuilder<TDocument, TField> NestedProperty<TField>(Expression<Func<TDocument, TField>> path, Action<TypedBuilder<TField>> configure)
        {
            return NestedProperty(new ExpressionFieldDefinition<TDocument, TField>(path), configure);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public NestedPatternPropertyBuilder<TDocument, TField> NestedPatternProperty<TField>(string pattern, Action<TypedBuilder<TField>> configure)
        {
            var nestedProperty = new NestedPatternPropertyBuilder<TDocument,TField>(pattern, configure);
            _subdocumentProperties.Add(nestedProperty);
            return nestedProperty;
        }

        internal override BsonDocument Build()
        {
            var schema = new BsonDocument("bsonType", "object");

            if (_metadata is not null)
            {
                schema.Merge(_metadata.Build());
            }

            var args = new RenderArgs<TDocument>(BsonSerializer.LookupSerializer<TDocument>(), BsonSerializer.SerializerRegistry);


            BsonDocument properties = null;

            if (_subdocumentProperties.Any())
            {
                properties = new BsonDocument();

                foreach (var nestedProperty in _subdocumentProperties)
                {
                    properties.Merge(nestedProperty.Build(args));
                }
            }

            if (_properties.Any())
            {
                properties ??= new BsonDocument();

                foreach (var property in _properties)
                {
                    properties.Merge(property.Build(args));
                }
            }

            if (properties != null)
            {
                schema.Add("properties", properties);
            }

            return schema;
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