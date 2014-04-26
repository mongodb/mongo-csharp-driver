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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the results of a Aggregate command.
    /// </summary>
    [Serializable]
    [BsonSerializer(typeof(CommandResultSerializer<AggregateResult>))]
    public class AggregateResult : CommandResult
    {
        // private fields
        private readonly long _cursorId;
        private readonly IEnumerable<BsonDocument> _resultDocuments;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateResult" /> class.
        /// </summary>
        /// <param name="response">The response.</param>
        public AggregateResult(BsonDocument response)
            : base(response)
        {
            if (response.Contains("cursor"))
            {
                var cursorDocument = response["cursor"];
                _cursorId = cursorDocument["id"].ToInt64();
                _resultDocuments = cursorDocument["firstBatch"].AsBsonArray.Select(v => v.AsBsonDocument);
            }
            if (response.Contains("result"))
            {
                _resultDocuments = response["result"].AsBsonArray.Select(v => v.AsBsonDocument);
            }
        }

        // public properties
        /// <summary>
        /// Gets the cursor id.
        /// </summary>
        /// <value>
        /// The cursor id.
        /// </value>
        public long CursorId
        {
            get { return _cursorId; }
        }

        /// <summary>
        /// Gets the result documents (either the Inline results or the first batch if a cursor was used).
        /// </summary>
        public IEnumerable<BsonDocument> ResultDocuments
        {
            get { return _resultDocuments; }
        }
    }
}
