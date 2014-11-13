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

using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a request to delete one or more documents.
    /// </summary>
    public sealed class DeleteRequest : WriteRequest
    {
        // fields
        private readonly BsonDocument _criteria;
        private int _limit;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteRequest" /> class.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        public DeleteRequest(BsonDocument criteria)
            : base(WriteRequestType.Delete)
        {
            _criteria = Ensure.IsNotNull(criteria, "criteria");
            _limit = 1;
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
        /// Gets or sets the limit.
        /// </summary>
        /// <value>
        /// The limit.
        /// </value>
        public int Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }
    }
}
