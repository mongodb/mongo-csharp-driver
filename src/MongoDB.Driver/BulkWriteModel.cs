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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Model for performing writes in bulk.
    /// </summary>
    public sealed class BulkWriteModel<T>
    {
        // fields
        private bool _isOrdered;
        private readonly IReadOnlyList<WriteModel<T>> _requests;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteModel{T}"/> class.
        /// </summary>
        /// <param name="requests">The operations.</param>
        public BulkWriteModel(IEnumerable<WriteModel<T>> requests)
        {
            _requests = Ensure.IsNotNull(requests, "requests").ToList();
            _isOrdered = true;
        }

        // properties
        /// <summary>
        /// Gets or sets a value indicating whether the requests are run in order.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [is ordered]; otherwise, <c>false</c>.
        /// </value>
        public bool IsOrdered
        {
            get { return _isOrdered; }
            set { _isOrdered = value; }
        }

        /// <summary>
        /// Gets the requests.
        /// </summary>
        public IReadOnlyList<WriteModel<T>> Requests
        {
            get { return _requests; }
        }
    }
}
