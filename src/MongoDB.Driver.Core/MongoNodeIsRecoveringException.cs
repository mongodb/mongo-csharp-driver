﻿/* Copyright 2013-2014 MongoDB Inc.
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
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB node is recovering exception.
    /// </summary>
    [Serializable]
    public class MongoNodeIsRecoveringException : MongoServerException
    {
        // fields
        private readonly BsonDocument _result;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoNodeIsRecoveringException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="result">The result.</param>
        public MongoNodeIsRecoveringException(ConnectionId connectionId, BsonDocument result)
            : base(connectionId, "Server returned node is recovering error.")
        {
            _result = Ensure.IsNotNull(result, "result");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoNodeIsRecoveringException"/> class.
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        protected MongoNodeIsRecoveringException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _result = (BsonDocument)info.GetValue("_result", typeof(BsonDocument));
        }

        // properties
        /// <summary>
        /// Gets the result from the server.
        /// </summary>
        /// <value>
        /// The result from the server.
        /// </value>
        public BsonDocument Result
        {
            get { return _result; }
        }

        // methods
        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_result", _result);
        }
    }
}
