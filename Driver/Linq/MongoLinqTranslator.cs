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
using System.Threading;

namespace MongoDB.Driver.Linq {
    /// <summary>
    /// A translator from LINQ expression queries to Mongo queries.
    /// </summary>
    public static class MongoLinqTranslator {
        #region public static methods
        /// <summary>
        /// A translator from LINQ queries to MongoDB queries.
        /// </summary>
        /// <param name="collection">The collection being queried.</param>
        /// <param name="expression">The LINQ query.</param>
        /// <returns>An instance of MongoLinqQuery.</returns>
        public static MongoLinqQuery Translate(
            MongoCollection collection,
            Expression expression
        ) {
            // total hack just to test the initial LINQ framework
            var query = MongoDB.Driver.Builders.Query.EQ("X", 1);
            return new MongoLinqFindQuery(collection, query);
        }
        #endregion
    }
}
