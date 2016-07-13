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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for the options used when creating an index.
    /// </summary>
    public static class IndexOptions
    {
        // public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoIndexOptions.
        /// </summary>
        public static IMongoIndexOptions Null
        {
            get { return null; }
        }

        // public static methods
        /// <summary>
        /// Sets whether to build the index in the background.
        /// </summary>
        /// <param name="value">Whether to build the index in the background.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder SetBackground(bool value)
        {
            return new IndexOptionsBuilder().SetBackground(value);
        }

        /// <summary>
        /// Sets the location precision bits.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder SetBits(int value)
        {
            return new IndexOptionsBuilder().SetBits(value);
        }

        /// <summary>
        /// Sets the bucket size for geospatial haystack indexes.
        /// </summary>
        /// <param name="value">The bucket size.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder SetBucketSize(double value)
        {
            return new IndexOptionsBuilder().SetBucketSize(value);
        }

        /// <summary>
        /// Sets whether duplicates should be dropped.
        /// </summary>
        /// <param name="value">Whether duplicates should be dropped.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder SetDropDups(bool value)
        {
            return new IndexOptionsBuilder().SetDropDups(value);
        }

        /// <summary>
        /// Sets the geospatial range.
        /// </summary>
        /// <param name="min">The min value of the range.</param>
        /// <param name="max">The max value of the range.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder SetGeoSpatialRange(double min, double max)
        {
            return new IndexOptionsBuilder().SetGeoSpatialRange(min, max);
        }

        /// <summary>
        /// Sets the name of the index.
        /// </summary>
        /// <param name="value">The name of the index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder SetName(string value)
        {
            return new IndexOptionsBuilder().SetName(value);
        }

        /// <summary>
        /// Sets the partial filter expression.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder SetPartialFilterExpression(IMongoQuery value)
        {
            return new IndexOptionsBuilder().SetPartialFilterExpression(value);
        }

        /// <summary>
        /// Sets whether the index is a sparse index.
        /// </summary>
        /// <param name="value">Whether the index is a sparse index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder SetSparse(bool value)
        {
            return new IndexOptionsBuilder().SetSparse(value);
        }

        /// <summary>
        /// Sets the storage engine options.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder SetStorageEngineOptions(BsonDocument value)
        {
            return new IndexOptionsBuilder().SetStorageEngineOptions(value);
        }

        /// <summary>
        /// Sets the default language for the text index.
        /// </summary>
        /// <param name="language">The default language.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder SetTextDefaultLanguage(string language)
        {
            return new IndexOptionsBuilder().SetTextDefaultLanguage(language);
        }

        /// <summary>
        /// Specifies the field name containing the language for the text index.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder SetTextLanguageOverride(string fieldName)
        {
            return new IndexOptionsBuilder().SetTextLanguageOverride(fieldName);
        }

        /// <summary>
        /// Sets the time to live value.
        /// </summary>
        /// <param name="timeToLive">The time to live.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder SetTimeToLive(TimeSpan timeToLive)
        {
            return new IndexOptionsBuilder().SetTimeToLive(timeToLive);
        }

        /// <summary>
        /// Sets whether the index enforces unique values.
        /// </summary>
        /// <param name="value">Whether the index enforces unique values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder SetUnique(bool value)
        {
            return new IndexOptionsBuilder().SetUnique(value);
        }

        /// <summary>
        /// Sets the weight of a field for the text index.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="value">The weight.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public static IndexOptionsBuilder SetWeight(string name, int value)
        {
            return new IndexOptionsBuilder().SetWeight(name, value);
        }
    }

    /// <summary>
    /// A builder for the options used when creating an index.
    /// </summary>
#if NET45
    [Serializable]
#endif
    [BsonSerializer(typeof(IndexOptionsBuilder.Serializer))]
    public class IndexOptionsBuilder : BuilderBase, IMongoIndexOptions
    {
        // private fields
        private BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the IndexOptionsBuilder class.
        /// </summary>
        public IndexOptionsBuilder()
        {
            _document = new BsonDocument();
        }

        // public methods
        /// <summary>
        /// Sets whether to build the index in the background.
        /// </summary>
        /// <param name="value">Whether to build the index in the background.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder SetBackground(bool value)
        {
            _document["background"] = value;
            return this;
        }

        /// <summary>
        /// Sets the location precision bits.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder SetBits(int value)
        {
            _document["bits"] = value;
            return this;
        }

        /// <summary>
        /// Sets the bucket size for geospatial haystack indexes.
        /// </summary>
        /// <param name="value">The bucket size.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder SetBucketSize(double value)
        {
            _document["bucketSize"] = value;
            return this;
        }

        /// <summary>
        /// Sets whether duplicates should be dropped.
        /// </summary>
        /// <param name="value">Whether duplicates should be dropped.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder SetDropDups(bool value)
        {
            _document["dropDups"] = value;
            return this;
        }

        /// <summary>
        /// Sets the geospatial range.
        /// </summary>
        /// <param name="min">The min value of the range.</param>
        /// <param name="max">The max value of the range.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder SetGeoSpatialRange(double min, double max)
        {
            _document["min"] = min;
            _document["max"] = max;
            return this;
        }

        /// <summary>
        /// Sets the name of the index.
        /// </summary>
        /// <param name="value">The name of the index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder SetName(string value)
        {
            _document["name"] = value;
            return this;
        }

        /// <summary>
        /// Sets the partial filter expression.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder SetPartialFilterExpression(IMongoQuery value)
        {
            _document["partialFilterExpression"] = value.ToBsonDocument();
            return this;
        }

        /// <summary>
        /// Sets whether the index is a sparse index.
        /// </summary>
        /// <param name="value">Whether the index is a sparse index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder SetSparse(bool value)
        {
            _document["sparse"] = value;
            return this;
        }

        /// <summary>
        /// Sets the storage engine options.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder SetStorageEngineOptions(BsonDocument value)
        {
            _document["storageEngine"] = value;
            return this;
        }

        /// <summary>
        /// Sets the default language for the text index.
        /// </summary>
        /// <param name="language">The default language.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder SetTextDefaultLanguage(string language)
        {
            _document["default_language"] = language;
            return this;
        }

        /// <summary>
        /// Specifies the field name containing the language for the text index.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder SetTextLanguageOverride(string fieldName)
        {
            _document["language_override"] = fieldName;
            return this;
        }

        /// <summary>
        /// Sets the time to live value.
        /// </summary>
        /// <param name="timeToLive">The time to live.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder SetTimeToLive(TimeSpan timeToLive)
        {
            _document["expireAfterSeconds"] = (int)timeToLive.TotalSeconds;
            return this;
        }

        /// <summary>
        /// Sets whether the index enforces unique values.
        /// </summary>
        /// <param name="value">Whether the index enforces unique values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder SetUnique(bool value)
        {
            _document["unique"] = value;
            return this;
        }

        /// <summary>
        /// Sets the weight of a field for the text index.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The weight.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public IndexOptionsBuilder SetWeight(string name, int value)
        {
            BsonValue weights;
            if (!_document.TryGetValue("weights", out weights))
            {
                weights = new BsonDocument();
                _document.Add("weights", weights);
            }
            weights[name] = value;
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
        new internal class Serializer : SerializerBase<IndexOptionsBuilder>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IndexOptionsBuilder value)
            {
                BsonDocumentSerializer.Instance.Serialize(context, value._document);
            }
        }
    }

    /// <summary>
    /// A builder for the options used when creating an index.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public static class IndexOptions<TDocument>
    {
        // public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoIndexOptions.
        /// </summary>
        public static IMongoIndexOptions Null
        {
            get { return null; }
        }

        // public static methods
        /// <summary>
        /// Sets whether to build the index in the background.
        /// </summary>
        /// <param name="value">Whether to build the index in the background.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder<TDocument> SetBackground(bool value)
        {
            return new IndexOptionsBuilder<TDocument>().SetBackground(value);
        }

        /// <summary>
        /// Sets the location precision bits.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder<TDocument> SetBits(int value)
        {
            return new IndexOptionsBuilder<TDocument>().SetBits(value);
        }

        /// <summary>
        /// Sets the bucket size for geospatial haystack indexes.
        /// </summary>
        /// <param name="value">The bucket size.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder<TDocument> SetBucketSize(double value)
        {
            return new IndexOptionsBuilder<TDocument>().SetBucketSize(value);
        }

        /// <summary>
        /// Sets whether duplicates should be dropped.
        /// </summary>
        /// <param name="value">Whether duplicates should be dropped.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder<TDocument> SetDropDups(bool value)
        {
            return new IndexOptionsBuilder<TDocument>().SetDropDups(value);
        }

        /// <summary>
        /// Sets the geospatial range.
        /// </summary>
        /// <param name="min">The min value of the range.</param>
        /// <param name="max">The max value of the range.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder<TDocument> SetGeoSpatialRange(double min, double max)
        {
            return new IndexOptionsBuilder<TDocument>().SetGeoSpatialRange(min, max);
        }

        /// <summary>
        /// Sets the name of the index.
        /// </summary>
        /// <param name="value">The name of the index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder<TDocument> SetName(string value)
        {
            return new IndexOptionsBuilder<TDocument>().SetName(value);
        }

        /// <summary>
        /// Sets the partial filter expression.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder<TDocument> SetPartialFilterExpression(IMongoQuery value)
        {
            return new IndexOptionsBuilder<TDocument>().SetPartialFilterExpression(value);
        }

        /// <summary>
        /// Sets whether the index is a sparse index.
        /// </summary>
        /// <param name="value">Whether the index is a sparse index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder<TDocument> SetSparse(bool value)
        {
            return new IndexOptionsBuilder<TDocument>().SetSparse(value);
        }

        /// <summary>
        /// Sets the storage engine options.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder<TDocument> SetStorageEngineOptions(BsonDocument value)
        {
            return new IndexOptionsBuilder<TDocument>().SetStorageEngineOptions(value);
        }

        /// <summary>
        /// Sets the default language for the text index.
        /// </summary>
        /// <param name="language">The default language.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder<TDocument> SetTextDefaultLanguage(string language)
        {
            return new IndexOptionsBuilder<TDocument>().SetTextDefaultLanguage(language);
        }

        /// <summary>
        /// Specifies a member expression for the field name containing the language for the text index.
        /// </summary>
        /// <param name="memberExpression">The member expression indicating the language field name.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public static IndexOptionsBuilder<TDocument> SetTextLanguageOverride(Expression<Func<TDocument, string>> memberExpression)
        {
            return new IndexOptionsBuilder<TDocument>().SetTextLanguageOverride(memberExpression);
        }

        /// <summary>
        /// Sets the time to live value.
        /// </summary>
        /// <param name="timeToLive">The time to live.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder<TDocument> SetTimeToLive(TimeSpan timeToLive)
        {
            return new IndexOptionsBuilder<TDocument>().SetTimeToLive(timeToLive);
        }

        /// <summary>
        /// Sets whether the index enforces unique values.
        /// </summary>
        /// <param name="value">Whether the index enforces unique values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexOptionsBuilder<TDocument> SetUnique(bool value)
        {
            return new IndexOptionsBuilder<TDocument>().SetUnique(value);
        }

        /// <summary>
        /// Sets the weight of a field for the text index.
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public static IndexOptionsBuilder<TDocument> SetWeight<TMember>(Expression<Func<TDocument, TMember>> memberExpression, int value)
        {
            return new IndexOptionsBuilder<TDocument>().SetWeight(memberExpression, value);
        }
    }

    /// <summary>
    /// A builder for the options used when creating an index.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
#if NET45
    [Serializable]
#endif
    [BsonSerializer(typeof(IndexOptionsBuilder<>.Serializer))]
    public class IndexOptionsBuilder<TDocument> : BuilderBase, IMongoIndexOptions
    {
        // private fields
        private readonly BsonSerializationInfoHelper _serializationInfoHelper;
        private IndexOptionsBuilder _indexOptionsBuilder;

        // constructors
        /// <summary>
        /// Initializes a new instance of the IndexOptionsBuilder class.
        /// </summary>
        public IndexOptionsBuilder()
        {
            _serializationInfoHelper = new BsonSerializationInfoHelper();
            _indexOptionsBuilder = new IndexOptionsBuilder();
        }

        // public methods
        /// <summary>
        /// Sets whether to build the index in the background.
        /// </summary>
        /// <param name="value">Whether to build the index in the background.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder<TDocument> SetBackground(bool value)
        {
            _indexOptionsBuilder.SetBackground(value);
            return this;
        }

        /// <summary>
        /// Sets the location precision bits.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder<TDocument> SetBits(int value)
        {
            _indexOptionsBuilder.SetBits(value);
            return this;
        }

        /// <summary>
        /// Sets the bucket size for geospatial haystack indexes.
        /// </summary>
        /// <param name="value">The bucket size.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder<TDocument> SetBucketSize(double value)
        {
            _indexOptionsBuilder.SetBucketSize(value);
            return this;
        }

        /// <summary>
        /// Sets whether duplicates should be dropped.
        /// </summary>
        /// <param name="value">Whether duplicates should be dropped.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder<TDocument> SetDropDups(bool value)
        {
            _indexOptionsBuilder.SetDropDups(value);
            return this;
        }

        /// <summary>
        /// Sets the geospatial range.
        /// </summary>
        /// <param name="min">The min value of the range.</param>
        /// <param name="max">The max value of the range.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder<TDocument> SetGeoSpatialRange(double min, double max)
        {
            _indexOptionsBuilder.SetGeoSpatialRange(min, max);
            return this;
        }

        /// <summary>
        /// Sets the name of the index.
        /// </summary>
        /// <param name="value">The name of the index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder<TDocument> SetName(string value)
        {
            _indexOptionsBuilder.SetName(value);
            return this;
        }

        /// <summary>
        /// Sets the partial filter expression.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder<TDocument> SetPartialFilterExpression(IMongoQuery value)
        {
            _indexOptionsBuilder.SetPartialFilterExpression(value);
            return this;
        }

        /// <summary>
        /// Sets whether the index is a sparse index.
        /// </summary>
        /// <param name="value">Whether the index is a sparse index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder<TDocument> SetSparse(bool value)
        {
            _indexOptionsBuilder.SetSparse(value);
            return this;
        }

        /// <summary>
        /// Sets the storage engine options.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder<TDocument> SetStorageEngineOptions(BsonDocument value)
        {
            _indexOptionsBuilder.SetStorageEngineOptions(value);
            return this;
        }

        /// <summary>
        /// Sets the default language for the text index.
        /// </summary>
        /// <param name="language">The default language.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder<TDocument> SetTextDefaultLanguage(string language)
        {
            _indexOptionsBuilder.SetTextDefaultLanguage(language);
            return this;
        }

        /// <summary>
        /// Specifies a member expression for the field name containing the language for the text index.
        /// </summary>
        /// <param name="memberExpression">The member expression indicating the language field name.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public IndexOptionsBuilder<TDocument> SetTextLanguageOverride(Expression<Func<TDocument, string>> memberExpression)
        {
            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            _indexOptionsBuilder.SetTextLanguageOverride(serializationInfo.ElementName);
            return this;
        }

        /// <summary>
        /// Sets the time to live value.
        /// </summary>
        /// <param name="timeToLive">The time to live.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder<TDocument> SetTimeToLive(TimeSpan timeToLive)
        {
            _indexOptionsBuilder.SetTimeToLive(timeToLive);
            return this;
        }

        /// <summary>
        /// Sets whether the index enforces unique values.
        /// </summary>
        /// <param name="value">Whether the index enforces unique values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public IndexOptionsBuilder<TDocument> SetUnique(bool value)
        {
            _indexOptionsBuilder.SetUnique(value);
            return this;
        }

        /// <summary>
        /// Sets the weight of a field for the text index.
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The builder (so method calls can be chained).
        /// </returns>
        public IndexOptionsBuilder<TDocument> SetWeight<TMember>(Expression<Func<TDocument, TMember>> memberExpression, int value)
        {
            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            _indexOptionsBuilder = _indexOptionsBuilder.SetWeight(serializationInfo.ElementName, value);
            return this;
        }

        /// <summary>
        /// Converts this object to a BsonDocument.
        /// </summary>
        /// <returns>
        /// A BsonDocument.
        /// </returns>
        public override BsonDocument ToBsonDocument()
        {
            return _indexOptionsBuilder.ToBsonDocument();
        }

        // nested classes
        new internal class Serializer : SerializerBase<IndexOptionsBuilder<TDocument>>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IndexOptionsBuilder<TDocument> value)
            {
                BsonDocumentSerializer.Instance.Serialize(context, value._indexOptionsBuilder.ToBsonDocument());
            }
        }
    }
}
