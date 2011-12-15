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
    /// A helper class for creating sort by specifiers.
    /// </summary>
    public static class SortBy {
        #region public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoSortBy.
        /// </summary>
        public static IMongoSortBy Null {
            get { return null; }
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a sort order specifier from an object.
        /// </summary>
        /// <param name="sortBy">The wrapped object.</param>
        /// <returns>A new instance of SortByWrapper or null.</returns>
        public static SortByWrapper Create(
            object sortBy
        ) {
            return SortByWrapper.Create(sortBy);
        }

        /// <summary>
        /// Adds keys to be sorted by in ascending order.
        /// </summary>
        /// <param name="keys">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static SortByBuilder Ascending(
            params string[] keys
        ) {
            return new SortByBuilder().Ascending(keys);
        }

        /// <summary>
        /// Adds keys to be sorted by in ascending order.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpressions">One or more lambda expressions specifying the members.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static SortByBuilder Ascending<TDocument>(
            params Expression<Func<TDocument, object>>[] memberExpressions
        ) {
            return new SortByBuilder().Ascending(memberExpressions);
        }

        /// <summary>
        /// Adds keys to be sorted by in descending order.
        /// </summary>
        /// <param name="keys">One or more key names.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static SortByBuilder Descending(
            params string[] keys
        ) {
            return new SortByBuilder().Descending(keys);
        }

        /// <summary>
        /// Adds keys to be sorted by in descending order.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpressions">One or more lambda expressions specifying the members.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static SortByBuilder Descending<TDocument>(
            params Expression<Func<TDocument, object>>[] memberExpressions
        ) {
            return new SortByBuilder().Descending(memberExpressions);
        }
        #endregion
    }
}
