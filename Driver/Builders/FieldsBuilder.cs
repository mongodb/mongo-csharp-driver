/* Copyright 2010-2011 10gen Inc.
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
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for specifying which fields of a document the server should return.
    /// </summary>
    [Serializable]
    public class FieldsBuilder : BuilderBase, IMongoFields
    {
        // private fields
        private BsonDocument document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the FieldsBuilder class.
        /// </summary>
        public FieldsBuilder()
        {
            document = new BsonDocument();
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
                document.Add(name, 0);
            }
            return this;
        }

        /// <summary>
        /// Adds one or more field names to be excluded from the results.
        /// </summary>
        /// <param name="memberExpressions">The member expressions specifying the fields.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public FieldsBuilder Exclude<TDocument>(
            params Expression<Func<TDocument, object>>[] memberExpressions)
        {
            return this.Exclude(memberExpressions.GetElementNames());
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
                document.Add(name, 1);
            }
            return this;
        }

        /// <summary>
        /// Adds one or more field names to be included in the results.
        /// </summary>
        /// <param name="memberExpressions">The member expressions specifying the fields.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public FieldsBuilder Include<TDocument>(
            params Expression<Func<TDocument, object>>[] memberExpressions)
        {
            return this.Include(memberExpressions.GetElementNames());
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <param name="name">The name of the field to slice.</param>
        /// <param name="size">The size of the slice (negative sizes are taken from the end).</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public FieldsBuilder Slice(string name, int size)
        {
            document.Add(name, new BsonDocument("$slice", size));
            return this;
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the field.</param>
        /// <param name="size">The size of the slice (negative sizes are taken from the end).</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public FieldsBuilder Slice<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            int size)
        {
            return this.Slice(memberExpression.GetElementName(), size);
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
            document.Add(name, new BsonDocument("$slice", new BsonArray { skip, limit }));
            return this;
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the field.</param>
        /// <param name="skip">The number of values to skip.</param>
        /// <param name="limit">The number of values to extract.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public FieldsBuilder Slice<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            int skip,
            int limit)
        {
            return this.Slice(memberExpression.GetElementName(), skip, limit);
        }

        /// <summary>
        /// Returns the result of the builder as a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public override BsonDocument ToBsonDocument()
        {
            return document;
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
            document.Serialize(bsonWriter, nominalType, options);
        }
    }
}
