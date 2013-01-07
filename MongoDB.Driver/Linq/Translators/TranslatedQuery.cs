/* Copyright 2010-2013 10gen Inc.
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

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Represents a LINQ query that has been translated to a MongoDB query.
    /// </summary>
    public abstract class TranslatedQuery
    {
        // private fields
        private MongoCollection _collection;
        private Type _documentType;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoLinqQuery class.
        /// </summary>
        /// <param name="collection">The collection being queried.</param>
        /// <param name="documentType">The document type being queried.</param>
        protected TranslatedQuery(MongoCollection collection, Type documentType)
        {
            _collection = collection;
            _documentType = documentType;
        }

        // public properties
        /// <summary>
        /// Gets the collection being queried.
        /// </summary>
        public MongoCollection Collection
        {
            get { return _collection; }
        }

        /// <summary>
        /// Get the document type being queried.
        /// </summary>
        public Type DocumentType
        {
            get { return _documentType; }
        }

        // public methods
        /// <summary>
        /// Executes a query that returns a single result (overridden by subclasses).
        /// </summary>
        /// <returns>The result of executing the query.</returns>
        public virtual object Execute()
        {
            throw new NotImplementedException();
        }
    }
}
