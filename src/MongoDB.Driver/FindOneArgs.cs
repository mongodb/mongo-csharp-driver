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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents arguments to the FindOne method.
    /// </summary>
    public class FindOneArgs
    {
        // private fields
        private IMongoFields _fields;
        private BsonDocument _hint;
        private TimeSpan? _maxTime;
        private IMongoQuery _query;
        private ReadPreference _readPreference;
        private IBsonSerializer _serializer;
        private int? _skip;
        private IMongoSortBy _sortBy;

        // public properties
        /// <summary>
        /// Gets or sets the fields.
        /// </summary>
        /// <value>
        /// The fields.
        /// </value>
        public IMongoFields Fields
        {
            get { return _fields; }
            set { _fields = value; }
        }

        /// <summary>
        /// Gets or sets the hint.
        /// </summary>
        /// <value>
        /// The hint.
        /// </value>
        public BsonDocument Hint
        {
            get { return _hint; }
            set { _hint = value; }
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
        /// Gets or sets the read preference.
        /// </summary>
        /// <value>
        /// The read preference.
        /// </value>
        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
            set { _readPreference = value; }
        }

        /// <summary>
        /// Gets or sets the serializer.
        /// </summary>
        /// <value>
        /// The serializer.
        /// </value>
        public IBsonSerializer Serializer
        {
            get { return _serializer; }
            set { _serializer = value; }
        }

        /// <summary>
        /// Gets or sets the skip.
        /// </summary>
        /// <value>
        /// The skip.
        /// </value>
        public int? Skip
        {
            get { return _skip; }
            set { _skip = value; }
        }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <value>
        /// The sort order.
        /// </value>
        public IMongoSortBy SortBy
        {
            get { return _sortBy; }
            set { _sortBy = value; }
        }
    }
}
