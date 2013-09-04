/* Copyright 2010-2013 10gen Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the results of a Aggregate command.
    /// </summary>
    [Serializable]
    [BsonSerializer(typeof(CommandResultSerializer))]
    public class AggregateResult : CommandResult
    {
        // private fields
        private readonly IEnumerable<BsonDocument> _resultDocuments;
        private MongoServer _server;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateResult" /> class.
        /// </summary>
        /// <param name="response">The response.</param>
        internal AggregateResult(BsonDocument response)
            : base(response)
        {
            if (response.Contains("cursor"))
            {
                var cursorDocument = response["cursor"];
                var cursorId = cursorDocument["id"].ToInt64();
                var firstBatch = cursorDocument["firstBatch"].AsBsonArray.Select(v => v.AsBsonDocument);

                // TODO: create a real cursor enumerator passing it the cursorId and the firstBatch
                var fakeCursorEnumerator = firstBatch.GetEnumerator();
                var errorMessage = "The ResultDocuments of an Aggregate command that are returned using a cursor can only be enumerated once.";
                _resultDocuments = new EnumerableOneTimeWrapper<BsonDocument>(fakeCursorEnumerator, errorMessage);
            }
            else if (response.Contains("outputNs"))
            {
                var ns = response["outputNs"].AsString;
                var firstDot = ns.IndexOf('.');
                var databaseName = ns.Substring(0, firstDot);
                var collectionName = ns.Substring(firstDot + 1);
                var database = _server.GetDatabase(databaseName);
                var collection = database.GetCollection<BsonDocument>(collectionName);
                var cursor = collection.FindAll();
                _resultDocuments = cursor; // TODO: wrap it in EnumerableOneTimeWrapper?
            }
            else if (response.Contains("result"))
            {
                _resultDocuments = response["result"].AsBsonArray.Select(v => v.AsBsonDocument);
            }
            else
            {
                var exception = new FormatException("The response to the aggregate command doesn't have any of the expected fields: result, cursor or outputNs.");
                exception.Data["Response"] = response;
                throw exception;
            }
        }

        // public properties
        /// <summary>
        /// Gets the results of the aggregation.
        /// </summary>
        public IEnumerable<BsonDocument> ResultDocuments
        {
            get { return _resultDocuments; }
        }

        // internal methods
        internal void SetServer(MongoServer server)
        {
            _server = server;
        }
    }
}
