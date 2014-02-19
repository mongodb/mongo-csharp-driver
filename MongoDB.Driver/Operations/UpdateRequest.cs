/* Copyright 2010-2014 MongoDB Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a request to update one or more documents.
    /// </summary>
    public class UpdateRequest : WriteRequest
    {
        // private fields
        private bool? _isMultiUpdate;
        private bool? _isUpsert;
        private IMongoQuery _query;
        private IMongoUpdate _update;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateRequest"/> class.
        /// </summary>
        public UpdateRequest()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateRequest"/> class.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="update">The update.</param>
        public UpdateRequest(IMongoQuery query, IMongoUpdate update)
            : base(WriteRequestType.Update)
        {
            _query = query;
            _update = update;
        }

        // public properties
        /// <summary>
        /// Gets or sets a value indicating whether this update request should affect multiple documents.
        /// </summary>
        /// <value>
        /// <c>true</c> if this request should affect multiple documents; otherwise, <c>false</c>.
        /// </value>
        public bool? IsMultiUpdate
        {
            get { return _isMultiUpdate; }
            set { _isMultiUpdate = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this update request should insert the record if it doesn't already exist.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this update request should insert the record if it doesn't already exis; otherwise, <c>false</c>.
        /// </value>
        public bool? IsUpsert
        {
            get { return _isUpsert; }
            set { _isUpsert = value; }
        }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        /// <value>
        /// The query.
        /// </value>
        public IMongoQuery Query
        {
            get { return _query; }
            set { _query = value; }
        }

        /// <summary>
        /// Gets or sets the update.
        /// </summary>
        /// <value>
        /// The update.
        /// </value>
        public IMongoUpdate Update
        {
            get { return _update; }
            set { _update = value; }
        }
    }
}
