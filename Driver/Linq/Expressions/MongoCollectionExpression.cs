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

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// A MongoExpression that represents a collection.
    /// </summary>
    public class MongoCollectionExpression : MongoExpression
    {
        private MongoCollection _collection;
        private Type _documentType;

        /// <summary>
        /// Initializes an instance of the MongoCollectionExpression class.
        /// </summary>
        /// <param name="collection">The collection referenced.</param>
        /// <param name="documentType">The document type.</param>
        public MongoCollectionExpression(MongoCollection collection, Type documentType)
            : base(MongoExpressionType.Collection, typeof(void))
        {
            _collection = collection;
            _documentType = documentType;
        }

        /// <summary>
        /// Gets the collection.
        /// </summary>
        public MongoCollection Collection
        {
            get { return _collection; }
        }

        /// <summary>
        /// Gets the document type.
        /// </summary>
        public Type DocumentType
        {
            get { return _documentType; }
        }
    }
}
