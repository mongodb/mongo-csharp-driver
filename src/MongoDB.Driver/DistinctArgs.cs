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
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents arguments for the Distinct command helper method.
    /// </summary>
    public class DistinctArgs
    {
        // private fields
        private string _key;
        private TimeSpan? _maxTime;
        private IMongoQuery _query;
        private IBsonSerializer _valueSerializer;

        // public properties
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public string Key
        {
            get { return _key; }
            set { _key = value; }
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
        /// Gets or sets the value serializer.
        /// </summary>
        /// <value>
        /// The value serializer.
        /// </value>
        public IBsonSerializer ValueSerializer
        {
            get { return _valueSerializer; }
            set { _valueSerializer = value; }
        }
    }
}
