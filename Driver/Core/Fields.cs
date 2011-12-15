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
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;

namespace MongoDB.Driver {
    /// <summary>
    /// A builder for specifying which fields of a document the server should return.
    /// </summary>
    public static class Fields {
        #region public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoFields.
        /// </summary>
        public static IMongoFields Null {
            get { return null; }
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a new instance of the FieldsWrapper class.
        /// </summary>
        /// <param name="fields">The wrapped object.</param>
        /// <returns>A new instance of FieldsWrapper or null.</returns>
        public static FieldsWrapper Create(
            object fields
        ) {
            if (fields == null) {
                return null;
            } else {
                return new FieldsWrapper(fields);
            }
        }

        /// <summary>
        /// Adds one or more field names to be excluded from the results.
        /// </summary>
        /// <param name="names">One or more field names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder Exclude(
            params string[] names
        ) {
            return new FieldsBuilder().Exclude(names);
        }

        /// <summary>
        /// Adds one or more field names to be excluded from the results.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpressions">The member expressions specifying the fields.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder Exclude<TDocument>(
            params Expression<Func<TDocument, object>>[] memberExpressions
        ) {
            return new FieldsBuilder().Exclude(memberExpressions);
        }

        /// <summary>
        /// Adds one or more field names to be included in the results.
        /// </summary>
        /// <param name="names">One or more field names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder Include(
            params string[] names
        ) {
            return new FieldsBuilder().Include(names);
        }

        /// <summary>
        /// Adds one or more field names to be included in the results.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpressions">The member expressions specifying the fields.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder Include<TDocument>(
            params Expression<Func<TDocument, object>>[] memberExpressions
        ) {
            return new FieldsBuilder().Include(memberExpressions);
        }
        
        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <param name="name">The name of the field to slice.</param>
        /// <param name="size">The size of the slice (negative sizes are taken from the end).</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder Slice(
            string name,
            int size // negative sizes are from the end
        ) {
            return new FieldsBuilder().Slice(name, size);
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the field.</param>
        /// <param name="size">The size of the slice (negative sizes are taken from the end).</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder Slice<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            int size // negative sizes are from the end
        ) {
            return new FieldsBuilder().Slice(memberExpression, size);
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <param name="name">The name of the field to slice.</param>
        /// <param name="skip">The number of values to skip.</param>
        /// <param name="limit">The number of values to extract.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder Slice(
            string name,
            int skip,
            int limit
        ) {
            return new FieldsBuilder().Slice(name, skip, limit);
        }

        /// <summary>
        /// Adds a slice to be included in the results.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the field.</param>
        /// <param name="skip">The number of values to skip.</param>
        /// <param name="limit">The number of values to extract.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static FieldsBuilder Slice<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            int skip,
            int limit
        ) {
            return new FieldsBuilder().Slice(memberExpression, skip, limit);
        }
        #endregion
    }
}
