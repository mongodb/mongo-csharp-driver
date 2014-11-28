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

namespace MongoDB.Driver
{
    [Serializable]
    public class MongoQueryException : MongoServerException
    {
        // fields
        private readonly BsonDocument _query;
        private readonly BsonDocument _queryResult;

        // constructors
        public MongoQueryException(ConnectionId connectionId, string message, BsonDocument query, BsonDocument queryResult)
            : base(connectionId, message, null)
        {
            _query = query;
            _queryResult = queryResult;
        }

        protected MongoQueryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _query = (BsonDocument)info.GetValue("_query", typeof(BsonDocument));
            _queryResult = (BsonDocument)info.GetValue("_queryResult", typeof(BsonDocument));
        }

        // properties
        public BsonDocument Query
        {
            get { return _query; }
        }

        public BsonDocument QueryResult
        {
            get { return _queryResult; }
        }

        // methods
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_query", _query);
            info.AddValue("_queryResult", _queryResult);
        }
    }
}
