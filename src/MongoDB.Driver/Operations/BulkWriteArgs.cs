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

using System;
using System.Collections.Generic;
using System.Linq;
namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the arguments to a BulkWrite method (BulkDelete, BulkInsert, BulkUpdate or BulkWrite).
    /// </summary>
    internal class BulkWriteArgs
    {
        // private fields
        private Action<InsertRequest> _assignId;
        private bool? _checkElementNames;
        private bool? _isOrdered;
        private int? _maxBatchCount;
        private int? _maxBatchLength;
        private IEnumerable<WriteRequest> _requests;
        private WriteConcern _writeConcern;

        // public properties
        /// <summary>
        /// Gets or sets a delegate that is called before a document is inserted to assign an id if the id is empty.
        /// </summary>
        public Action<InsertRequest> AssignId
        {
            get { return _assignId; }
            set { _assignId = value; }
        }

        /// <summary>
        /// Gets or sets whether to check element names.
        /// </summary>
        /// <value>
        ///   <c>true</c> if element names should be checked; otherwise, <c>false</c>.
        /// </value>
        public bool? CheckElementNames
        {
            get { return _checkElementNames; }
            set { _checkElementNames = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server should process the requests in order.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the server should process the requests in order; otherwise, <c>false</c>.
        /// </value>
        public bool? IsOrdered
        {
            get { return _isOrdered; }
            set { _isOrdered = value; }
        }

        /// <summary>
        /// Gets or sets the maximum batch count.
        /// </summary>
        /// <value>
        /// The maximum batch count.
        /// </value>
        public int? MaxBatchCount
        {
            get { return _maxBatchCount; }
            set { _maxBatchCount = value; }
        }

        /// <summary>
        /// Gets or sets the maximum batch length (in bytes).
        /// </summary>
        /// <value>
        /// The maximum batch length.
        /// </value>
        public int? MaxBatchLength
        {
            get { return _maxBatchLength; }
            set { _maxBatchLength = value; }
        }

        /// <summary>
        /// Gets or sets the write requests.
        /// </summary>
        public IEnumerable<WriteRequest> Requests
        {
            get { return _requests; }
            set { _requests = value; }
        }

        /// <summary>
        /// Gets or sets the write concern.
        /// </summary>
        /// <value>
        /// The write concern.
        /// </value>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }
    }
}
