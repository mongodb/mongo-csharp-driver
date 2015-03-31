﻿/* Copyright 2010-2014 MongoDB Inc.
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
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    /// <summary>
    /// Model for updating many documents.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    [Serializable]
    public sealed class UpdateManyModel<TDocument> : WriteModel<TDocument>
    {
        // fields
        private readonly FilterDefinition<TDocument> _filter;
        private bool _isUpsert;
        private readonly UpdateDefinition<TDocument> _update;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateManyModel{TDocument}"/> class.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update.</param>
        public UpdateManyModel(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update)
        {
            _filter = Ensure.IsNotNull(filter, "filter");
            _update = Ensure.IsNotNull(update, "update");
        }

        // properties
        /// <summary>
        /// Gets the filter.
        /// </summary>
        public FilterDefinition<TDocument> Filter
        {
            get { return _filter; }
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
        /// Gets the update.
        /// </summary>
        public UpdateDefinition<TDocument> Update
        {
            get { return _update; }
        }

        /// <inheritdoc/>
        public override WriteModelType ModelType
        {
            get { return WriteModelType.UpdateMany; }
        }
    }
}
