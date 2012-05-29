/* Copyright 2010-2012 10gen Inc.
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
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for specifying which fields of a document the server should return.
    /// </summary>
    public static class Fields
    {
        // public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoFields.
        /// </summary>
        public static IMongoFields Null
        {
            get { return null; }
        }

        // public static methods
        /// <summary>
        /// Adds one or more field names to be excluded from the results.
        /// </summary>
        /// <param name="names">One or more field names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder Exclude(params string[] names)
        {
            return new FieldsBuilder().Exclude(names);
        }

        /// <summary>
        /// Adds one or more field names to be included in the results.
        /// </summary>
        /// <param name="names">One or more field names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder Include(params string[] names)
        {
            return new FieldsBuilder().Include(names);
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <param name="name">The name of the field to slice.</param>
        /// <param name="size">The size of the slice (negative sizes are taken from the end).</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder Slice(string name, int size)
        {
            return new FieldsBuilder().Slice(name, size);
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <param name="name">The name of the field to slice.</param>
        /// <param name="skip">The number of values to skip.</param>
        /// <param name="limit">The number of values to extract.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder Slice(string name, int skip, int limit)
        {
            return new FieldsBuilder().Slice(name, skip, limit);
        }
    }

    /// <summary>
    /// A builder for specifying which fields of a document the server should return.
    /// </summary>
    [Serializable]
    public class FieldsBuilder : BuilderBase, IMongoFields
    {
        // private fields
        private BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the FieldsBuilder class.
        /// </summary>
        public FieldsBuilder()
        {
            _document = new BsonDocument();
        }

        // public methods
        /// <summary>
        /// Adds one or more field names to be excluded from the results.
        /// </summary>
        /// <param name="names">One or more field names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public FieldsBuilder Exclude(params string[] names)
        {
            foreach (var name in names)
            {
                _document.Add(name, 0);
            }
            return this;
        }

        /// <summary>
        /// Adds one or more field names to be included in the results.
        /// </summary>
        /// <param name="names">One or more field names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public FieldsBuilder Include(params string[] names)
        {
            foreach (var name in names)
            {
                _document.Add(name, 1);
            }
            return this;
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <param name="name">The name of the field to slice.</param>
        /// <param name="size">The size of the slice (negative sizes are taken from the end).</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public FieldsBuilder Slice(string name, int size)
        {
            _document.Add(name, new BsonDocument("$slice", size));
            return this;
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <param name="name">The name of the field to slice.</param>
        /// <param name="skip">The number of values to skip.</param>
        /// <param name="limit">The number of values to extract.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public FieldsBuilder Slice(string name, int skip, int limit)
        {
            _document.Add(name, new BsonDocument("$slice", new BsonArray { skip, limit }));
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

        // protected methods
        /// <summary>
        /// Serializes the result of the builder to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The writer.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="options">The serialization options.</param>
        protected override void Serialize(BsonWriter bsonWriter, Type nominalType, IBsonSerializationOptions options)
        {
            ((IBsonSerializable)_document).Serialize(bsonWriter, nominalType, options);
        }
    }

    /// <summary>
    /// A builder for specifying which fields of a document the server should return.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public static class Fields<TDocument>
    {
        // public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoFields.
        /// </summary>
        public static IMongoFields Null
        {
            get { return null; }
        }

        // public static methods
        /// <summary>
        /// Adds one or more field names to be excluded from the results.
        /// </summary>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder<TDocument> Exclude(params Expression<Func<TDocument, object>>[] memberExpressions)
        {
            return new FieldsBuilder<TDocument>().Exclude(memberExpressions);
        }

        /// <summary>
        /// Adds one or more field names to be included in the results.
        /// </summary>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder<TDocument> Include(params Expression<Func<TDocument, object>>[] memberExpressions)
        {
            return new FieldsBuilder<TDocument>().Include(memberExpressions);
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="size">The size of the slice (negative sizes are taken from the end).</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder<TDocument> Slice<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, int size)
        {
            return new FieldsBuilder<TDocument>().Slice(memberExpression, size);
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="skip">The number of values to skip.</param>
        /// <param name="limit">The number of values to extract.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder<TDocument> Slice<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, int skip, int limit)
        {
            return new FieldsBuilder<TDocument>().Slice(memberExpression, skip, limit);
        }
    }

    /// <summary>
    /// A builder for specifying which fields of a document the server should return.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    [Serializable]
    public class FieldsBuilder<TDocument> : BuilderBase, IMongoFields
    {
        // private fields
        private readonly BsonSerializationInfoHelper _serializationInfoHelper;
        private FieldsBuilder _fieldsBuilder;

        // constructors
        /// <summary>
        /// Initializes a new instance of the FieldsBuilder class.
        /// </summary>
        public FieldsBuilder()
        {
            _serializationInfoHelper = new BsonSerializationInfoHelper();
            _fieldsBuilder = new FieldsBuilder();
        }

        // public methods
        /// <summary>
        /// Adds one or more field names to be excluded from the results.
        /// </summary>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public FieldsBuilder<TDocument> Exclude(params Expression<Func<TDocument, object>>[] memberExpressions)
        {
            var elementNames = GetElementNames(memberExpressions);
            _fieldsBuilder = _fieldsBuilder.Exclude(elementNames.ToArray());
            return this;
        }

        /// <summary>
        /// Adds one or more field names to be included in the results.
        /// </summary>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public FieldsBuilder<TDocument> Include(params Expression<Func<TDocument, object>>[] memberExpressions)
        {
            var elementNames = GetElementNames(memberExpressions);
            _fieldsBuilder = _fieldsBuilder.Include(elementNames.ToArray());
            return this;
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="size">The size of the slice (negative sizes are taken from the end).</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public FieldsBuilder<TDocument> Slice<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, int size)
        {
            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            _fieldsBuilder = _fieldsBuilder.Slice(serializationInfo.ElementName, size);
            return this;
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="skip">The number of values to skip.</param>
        /// <param name="limit">The number of values to extract.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public FieldsBuilder<TDocument> Slice<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, int skip, int limit)
        {
            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            _fieldsBuilder = _fieldsBuilder.Slice(serializationInfo.ElementName, skip, limit);
            return this;
        }

        /// <summary>
        /// Converts this object to a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public override BsonDocument ToBsonDocument()
        {
            return _fieldsBuilder.ToBsonDocument();
        }

        // protected methods
        /// <summary>
        /// Serializes the result of the builder to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The writer.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="options">The serialization options.</param>
        protected override void Serialize(BsonWriter bsonWriter, Type nominalType, IBsonSerializationOptions options)
        {
            ((IBsonSerializable)_fieldsBuilder).Serialize(bsonWriter, nominalType, options);
        }

        // private methods
        private IEnumerable<string> GetElementNames(IEnumerable<Expression<Func<TDocument, object>>> memberExpressions)
        {
            var elementNames = memberExpressions
                .Select(x => _serializationInfoHelper.GetSerializationInfo(x))
                .Select(x => x.ElementName);
            return elementNames;
        }
    }
}
