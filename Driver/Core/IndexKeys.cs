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
    /// A builder for specifying the keys for an index.
    /// </summary>
    public static class IndexKeys {
        #region public static methods
        /// <summary>
        /// Creates a new instance of the IndexKeysWrapper class.
        /// </summary>
        /// <param name="keys">The wrapped object.</param>
        /// <returns>A new instance of IndexKeysWrapper or null.</returns>
        public static IndexKeysWrapper Create(
            object keys
        ) {
            return IndexKeysWrapper.Create(keys);
        }

        /// <summary>
        /// Sets one or more key names to index in ascending order.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder Ascending(
            params string[] names
        ) {
            return new IndexKeysBuilder().Ascending(names);
        }

        /// <summary>
        /// Sets one or more key names to index in ascending order.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpressions">One or more lambda expressions specifying the members.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder Ascending<TDocument>(
            params Expression<Func<TDocument, object>>[] memberExpressions
        ) {
            return new IndexKeysBuilder().Ascending(memberExpressions);
        }

        /// <summary>
        /// Sets one or more key names to index in descending order.
        /// </summary>
        /// <param name="names">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder Descending(
            params string[] names
        ) {
            return new IndexKeysBuilder().Descending(names);
        }

        /// <summary>
        /// Sets one or more key names to index in descending order.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpressions">One or more lambda expressions specifying the members.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder Descending<TDocument>(
            params Expression<Func<TDocument, object>>[] memberExpressions
        ) {
            return new IndexKeysBuilder().Descending(memberExpressions);
        }

        /// <summary>
        /// Sets the key name to create a geospatial index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder GeoSpatial(
            string name
        ) {
            return new IndexKeysBuilder().GeoSpatial(name);
        }

        /// <summary>
        /// Sets the key name to create a geospatial index on.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder GeoSpatial<TDocument>(
            Expression<Func<TDocument, object>> memberExpression
        ) {
            return new IndexKeysBuilder().GeoSpatial(memberExpression);
        }

        /// <summary>
        /// Sets the key name to create a geospatial haystack index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder GeoSpatialHaystack(
            string name
        ) {
            return new IndexKeysBuilder().GeoSpatialHaystack(name);
        }

        /// <summary>
        /// Sets the key name to create a geospatial haystack index on.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member to index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder GeoSpatialHaystack<TDocument>(
            Expression<Func<TDocument, object>> memberExpression
        ) {
            return new IndexKeysBuilder().GeoSpatialHaystack(memberExpression);
        }

        /// <summary>
        /// Sets the key name and additional field name to create a geospatial haystack index on.
        /// </summary>
        /// <param name="name">The key name.</param>
        /// <param name="additionalName">The name of an additional field to index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder GeoSpatialHaystack(
            string name,
            string additionalName
        ) {
            return new IndexKeysBuilder().GeoSpatialHaystack(name, additionalName);
        }

        /// <summary>
        /// Sets the key name and additional field name to create a geospatial haystack index on.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member to index.</param>
        /// <param name="additionalMemberLambda">A additional lambda expression specifying the member to index.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static IndexKeysBuilder GeoSpatialHaystack<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            Expression<Func<TDocument, object>> additionalMemberLambda
        ) {
            return new IndexKeysBuilder().GeoSpatialHaystack(memberExpression, additionalMemberLambda);
        }
        #endregion
    }
}
