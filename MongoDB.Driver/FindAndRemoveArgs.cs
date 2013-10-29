﻿/* Copyright 2010-2013 10gen Inc.
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
    /// Represents options for the FindAndRemove command.
    /// </summary>
    public class FindAndRemoveArgs
    {
        // private fields
        private IMongoFields _fields;
        private TimeSpan? _maxTime;
        private IMongoQuery _query;
        private IMongoSortBy _sort;

        // public properties
        /// <summary>
        /// Gets or sets the fields specification.
        /// </summary>
        /// <value>
        /// The fields specification.
        /// </value>
        public IMongoFields Fields
        {
            get { return _fields; }
            set { _fields = value; }
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
        /// Gets or sets the sort specification.
        /// </summary>
        /// <value>
        /// The sort specification.
        /// </value>
        public IMongoSortBy SortBy
        {
            get { return _sort; }
            set { _sort = value; }
        }
    }
}
