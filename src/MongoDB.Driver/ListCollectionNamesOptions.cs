﻿/* Copyright 2010-present MongoDB Inc.
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
    /// Options for a list collection names operation.
    /// </summary>
    public sealed class ListCollectionNamesOptions
    {
        // fields
        private bool? authorizedCollections;
        private BsonValue _comment;
        private FilterDefinition<BsonDocument> _filter;
        private TimeSpan? _timeout;

        // properties
        /// <summary>
        /// Gets or sets the AuthorizedCollections flag.
        /// </summary>
        public bool? AuthorizedCollections
        {
            get { return authorizedCollections; }
            set { authorizedCollections = value; }
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
        /// Gets or sets the filter.
        /// </summary>
        public FilterDefinition<BsonDocument> Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        /// <summary>
        /// Gets or sets the operation timeout.
        /// </summary>
        // TODO: CSOT: Make it public when CSOT will be ready for GA
        internal TimeSpan? Timeout
        {
            get => _timeout;
            set => _timeout = Ensure.IsNullOrValidTimeout(value, nameof(Timeout));
        }
    }
}
