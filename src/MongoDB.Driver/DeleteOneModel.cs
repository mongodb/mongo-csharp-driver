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

using System;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Model for deleting a single document.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
#if NET452
    [Serializable]
#endif
    public sealed class DeleteOneModel<TDocument> : WriteModel<TDocument>
    {
        // fields
        private Collation _collation;
        private readonly FilterDefinition<TDocument> _filter;
        private BsonValue _hint;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="filter">The filter.</param>
        public DeleteOneModel(FilterDefinition<TDocument> filter)
        {
            _filter = Ensure.IsNotNull(filter, nameof(filter));
        }

        // properties
        /// <summary>
        /// Gets or sets the collation.
        /// </summary>
        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        /// <summary>
        /// Gets the filter.
        /// </summary>
        public FilterDefinition<TDocument> Filter
        {
            get { return _filter; }
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
        /// Gets the type of the model.
        /// </summary>
        public override WriteModelType ModelType
        {
            get { return WriteModelType.DeleteOne; }
        }
    }
}
