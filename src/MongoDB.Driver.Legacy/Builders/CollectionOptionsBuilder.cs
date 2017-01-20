/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for the options used when creating a collection.
    /// </summary>
    public static class CollectionOptions
    {
        // public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoCollectionOptions.
        /// </summary>
        public static IMongoCollectionOptions Null
        {
            get { return null; }
        }

        // public static methods
        /// <summary>
        /// Sets whether to automatically create an index on the _id element.
        /// </summary>
        /// <param name="value">Whether to automatically create an index on the _id element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetAutoIndexId(bool value)
        {
            return new CollectionOptionsBuilder().SetAutoIndexId(value);
        }

        /// <summary>
        /// Sets whether the collection is capped.
        /// </summary>
        /// <param name="value">Whether the collection is capped.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetCapped(bool value)
        {
            return new CollectionOptionsBuilder().SetCapped(value);
        }

        /// <summary>
        /// Sets the collation.
        /// </summary>
        /// <param name="value">The collation.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetCollation(Collation value)
        {
            return new CollectionOptionsBuilder().SetCollation(value);
        }

        /// <summary>
        /// Sets the index options defaults.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetIndexOptionDefaults(IndexOptionDefaults value)
        {
            return new CollectionOptionsBuilder().SetIndexOptionDefaults(value);
        }

        /// <summary>
        /// Sets the max number of documents in a capped collection.
        /// </summary>
        /// <param name="value">The max number of documents.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetMaxDocuments(long value)
        {
            return new CollectionOptionsBuilder().SetMaxDocuments(value);
        }

        /// <summary>
        /// Sets the max size of a capped collection.
        /// </summary>
        /// <param name="value">The max size.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetMaxSize(long value)
        {
            return new CollectionOptionsBuilder().SetMaxSize(value);
        }

        /// <summary>
        /// Sets the storage engine options.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetStorageEngineOptions(BsonDocument value)
        {
            return new CollectionOptionsBuilder().SetStorageEngineOptions(value);
        }
        /// <summary>
        /// Sets the validation action.
        /// </summary>
        /// <param name="validationAction">The validation action.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetValidationAction(DocumentValidationAction validationAction)
        {
            return new CollectionOptionsBuilder().SetValidationAction(validationAction);
        }

        /// <summary>
        /// Sets the validation level.
        /// </summary>
        /// <param name="validationLevel">The validation level.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetValidationLevel(DocumentValidationLevel validationLevel)
        {
            return new CollectionOptionsBuilder().SetValidationLevel(validationLevel);
        }

        /// <summary>
        /// Sets the validator.
        /// </summary>
        /// <param name="validator">The validator.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static CollectionOptionsBuilder SetValidator(IMongoQuery validator)
        {
            return new CollectionOptionsBuilder().SetValidator(validator);
        }
    }

    /// <summary>
    /// A builder for the options used when creating a collection.
    /// </summary>
#if NET45
    [Serializable]
#endif
    [BsonSerializer(typeof(CollectionOptionsBuilder.Serializer))]
    public class CollectionOptionsBuilder : BuilderBase, IMongoCollectionOptions
    {
        // private fields
        private BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the CollectionOptionsBuilder class.
        /// </summary>
        public CollectionOptionsBuilder()
        {
            _document = new BsonDocument();
        }

        // public methods
        /// <summary>
        /// Sets whether to automatically create an index on the _id element.
        /// </summary>
        /// <param name="value">Whether to automatically create an index on the _id element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public CollectionOptionsBuilder SetAutoIndexId(bool value)
        {
            _document["autoIndexId"] = value;
            return this;
        }

        /// <summary>
        /// Sets whether the collection is capped.
        /// </summary>
        /// <param name="value">Whether the collection is capped.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public CollectionOptionsBuilder SetCapped(bool value)
        {
            _document["capped"] = value;
            return this;
        }

        /// <summary>
        /// Sets the collation.
        /// </summary>
        /// <param name="value">The collation.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public CollectionOptionsBuilder SetCollation(Collation value)
        {
            _document["collation"] = value.ToBsonDocument();
            return this;
        }

        /// <summary>
        /// Sets the index options defaults.
        /// </summary>
        /// <param name="value">The index options defaults.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public CollectionOptionsBuilder SetIndexOptionDefaults(IndexOptionDefaults value)
        {
            _document["indexOptionDefaults"] = value.ToBsonDocument();
            return this;
        }

        /// <summary>
        /// Sets the max number of documents in a capped collection.
        /// </summary>
        /// <param name="value">The max number of documents.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public CollectionOptionsBuilder SetMaxDocuments(long value)
        {
            _document["max"] = value;
            return this;
        }

        /// <summary>
        /// Sets the max size of a capped collection.
        /// </summary>
        /// <param name="value">The max size.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public CollectionOptionsBuilder SetMaxSize(long value)
        {
            _document["size"] = value;
            return this;
        }

        /// <summary>
        /// Sets the storage engine options.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public CollectionOptionsBuilder SetStorageEngineOptions(BsonDocument value)
        {
            _document["storageEngine"] = value;
            return this;
        }

        /// <summary>
        /// Sets the validation action.
        /// </summary>
        /// <param name="validationAction">The validation action.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public CollectionOptionsBuilder SetValidationAction(DocumentValidationAction validationAction)
        {
            _document["validationAction"] = validationAction.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the validation level.
        /// </summary>
        /// <param name="validationLevel">The validation level.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public CollectionOptionsBuilder SetValidationLevel(DocumentValidationLevel validationLevel)
        {
            _document["validationLevel"] = validationLevel.ToString().ToLowerInvariant();
            return this;
        }

        /// <summary>
        /// Sets the validator.
        /// </summary>
        /// <param name="validator">The validator.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public CollectionOptionsBuilder SetValidator(IMongoQuery validator)
        {
            _document["validator"] = validator.ToBsonDocument();
            return this;
        }

        /// <summary>
        /// Returns the result of the builder as a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public override BsonDocument ToBsonDocument()
        {
            return _document;
        }

        // nested classes
        new internal class Serializer : SerializerBase<CollectionOptionsBuilder>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, CollectionOptionsBuilder value)
            {
                BsonDocumentSerializer.Instance.Serialize(context, value._document);
            }
        }
    }
}
