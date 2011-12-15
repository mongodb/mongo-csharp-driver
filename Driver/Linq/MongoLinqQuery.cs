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

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Represents a LINQ query that has been translated to a MongoDB query.
    /// </summary>
    public abstract class MongoLinqQuery
    {
        // protected fields
        /// <summary>
        /// The collection being queried.
        /// </summary>
        protected MongoCollection collection;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoLinqQuery class.
        /// </summary>
        /// <param name="collection">The collection being queried.</param>
        protected MongoLinqQuery(MongoCollection collection)
        {
            this.collection = collection;
        }

        // public methods
        /// <summary>
        /// Executes a query that returns a single result (overridden by subclasses).
        /// </summary>
        /// <returns>The result of executing the query.</returns>
        public virtual object Execute()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets an enumerator for the results of a query that returns multiple results (overridden by subclasses).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual IEnumerator<T> GetEnumerator<T>()
        {
            throw new NotSupportedException();
        }
    }
}
