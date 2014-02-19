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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents arguments for the Count command helper method.
    /// </summary>
    public class CountArgs
    {
        // private fields
        private long? _limit;
        private TimeSpan? _maxTime;
        private IMongoQuery _query;
        private long? _skip;

        // public properties
        /// <summary>
        /// Gets or sets the maximum number of matching documents to count.
        /// </summary>
        /// <value>
        /// The maximum number of matching documents to count.
        /// </value>
        public long? Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        /// <summary>
        /// Gets or sets the max time.
        /// </summary>
        /// <value>
        /// The max time.
        /// </value>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
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
        /// Gets or sets the number of matching documents to skip before starting to count matching documents.
        /// </summary>
        /// <value>
        /// The number of matching documents to skip before starting to count matching documents.
        /// </value>
        public long? Skip
        {
            get { return _skip; }
            set { _skip = value; }
        }
    }
}
