/* Copyright 2010-2016 MongoDB Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents arguments for the Group command helper method.
    /// </summary>
    public class GroupArgs
    {
        // private fields
        private Collation _collation;
        private BsonJavaScript _finalizeFunction;
        private BsonDocument _initial;
        private IMongoGroupBy _keyFields;
        private BsonJavaScript _keyFunction;
        private TimeSpan? _maxTime;
        private IMongoQuery _query;
        private BsonJavaScript _reduceFunction;

        // public properties
        /// <summary>
        /// Gets or sets the collation.
        /// </summary>
        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        /// <summary>
        /// Gets or sets the finalize function.
        /// </summary>
        public BsonJavaScript FinalizeFunction
        {
            get { return _finalizeFunction; }
            set { _finalizeFunction = value; }
        }

        /// <summary>
        /// Gets or sets the initial value of the aggregation result document.
        /// </summary>
        public BsonDocument Initial
        {
            get { return _initial; }
            set { _initial = value; }
        }

        /// <summary>
        /// Gets or sets the key fields.
        /// </summary>
        public IMongoGroupBy KeyFields
        {
            get { return _keyFields; }
            set { _keyFields = value; }
        }

        /// <summary>
        /// Gets or sets the key function.
        /// </summary>
        public BsonJavaScript KeyFunction
        {
            get { return _keyFunction; }
            set { _keyFunction = value; }
        }

        /// <summary>
        /// Gets or sets the max time.
        /// </summary>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        public IMongoQuery Query
        {
            get { return _query; }
            set { _query = value; }
        }

        /// <summary>
        /// Gets or sets the reduce function.
        /// </summary>
        public BsonJavaScript ReduceFunction
        {
            get { return _reduceFunction; }
            set { _reduceFunction = value; }
        }
    }
}
