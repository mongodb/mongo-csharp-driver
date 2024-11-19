/* Copyright 2010-present MongoDB Inc.
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

using System.Collections.Generic;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for updating a single document.
    /// </summary>
    public class UpdateOptions
    {
        // fields
        private IEnumerable<ArrayFilterDefinition> _arrayFilters;
        private bool? _bypassDocumentValidation;
        private Collation _collation;
        private BsonValue _comment;
        private BsonValue _hint;
        private bool _isUpsert;
        private BsonDocument _let;

        // properties
        /// <summary>
        /// Gets or sets the array filters.
        /// </summary>
        /// <value>
        /// The array filters.
        /// </value>
        public IEnumerable<ArrayFilterDefinition> ArrayFilters
        {
            get { return _arrayFilters; }
            set { _arrayFilters = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to bypass document validation.
        /// </summary>
        public bool? BypassDocumentValidation
        {
            get { return _bypassDocumentValidation; }
            set { _bypassDocumentValidation = value; }
        }

        /// <summary>
        /// Gets or sets the collation.
        /// </summary>
        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        /// <summary>
        /// Gets or sets the hint.
        /// </summary>
        public BsonValue Hint
        {
            get { return _hint; }
            set { _hint = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to insert the document if it doesn't already exist.
        /// </summary>
        public bool IsUpsert
        {
            get { return _isUpsert; }
            set { _isUpsert = value; }
        }

        /// <summary>
        /// Gets or sets the let document.
        /// </summary>
        public BsonDocument Let
        {
            get { return _let; }
            set { _let = value; }
        }
    }

    /// <summary>
    /// Options for updating a single document and specifying a sort order.
    /// </summary>
    public sealed class UpdateOptions<T> : UpdateOptions
    {
        private SortDefinition<T> _sort;

        /// <summary>
        /// Gets or sets the sort definition.
        /// </summary>
        public SortDefinition<T> Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }
    }}
