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

using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a request to update one or more documents.
    /// </summary>
    public sealed class UpdateRequest : WriteRequest
    {
        // fields
        private readonly BsonDocument _criteria;
        private bool _isMulti;
        private bool _isUpsert;
        private readonly BsonDocument _update;
        private UpdateType _updateType;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateRequest" /> class.
        /// </summary>
        /// <param name="updateType">The type.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="update">The update.</param>
        public UpdateRequest(UpdateType updateType, BsonDocument criteria, BsonDocument update)
            : base(WriteRequestType.Update)
        {
            _updateType = updateType;
            _criteria = Ensure.IsNotNull(criteria, "criteria");
            _update = Ensure.IsNotNull(update, "update");
        }

        // properties
        /// <summary>
        /// Gets or sets the criteria.
        /// </summary>
        public BsonDocument Criteria
        {
            get { return _criteria; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this update request should affect multiple documents.
        /// </summary>
        /// <value>
        /// <c>true</c> if this request should affect multiple documents; otherwise, <c>false</c>.
        /// </value>
        public bool IsMulti
        {
            get { return _isMulti; }
            set { _isMulti = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this update request should insert the record if it doesn't already exist.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this update request should insert the record if it doesn't already exis; otherwise, <c>false</c>.
        /// </value>
        public bool IsUpsert
        {
            get { return _isUpsert; }
            set { _isUpsert = value; }
        }

        /// <summary>
        /// Gets or sets the update.
        /// </summary>
        public BsonDocument Update
        {
            get { return _update; }
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        public UpdateType UpdateType
        {
            get { return _updateType; }
        }
    }
}
