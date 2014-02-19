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
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the result of a command (there are also subclasses for various commands).
    /// </summary>
    [Serializable]
    [BsonSerializer(typeof(DistinctCommandResultSerializer<>))]
    public class DistinctCommandResult<TValue> : CommandResult
    {
        // private fields
        private readonly IEnumerable<TValue> _values;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DistinctCommandResult{TValue}" /> class.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="values">The values.</param>
        internal DistinctCommandResult(BsonDocument response, IEnumerable<TValue> values)
            : base(response)
        {
            _values = values;
        }

        // public properties
        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>
        /// The values.
        /// </value>
        public IEnumerable<TValue> Values
        {
            get { return _values; }
        }
    }
}
