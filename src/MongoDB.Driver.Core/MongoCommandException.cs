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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    [Serializable]
    public class MongoCommandException : MongoServerException
    {
        // fields
        private readonly BsonDocument _command;
        private readonly BsonDocument _result;

        // constructors
        public MongoCommandException(ConnectionId connectionId, string message, BsonDocument command)
            : this(connectionId, message, command, null)
        {
        }

        public MongoCommandException(ConnectionId connectionId, string message, BsonDocument command, BsonDocument result)
            : base(connectionId, message)
        {
            _command = command;
            _result = result; // can be null
        }

        protected MongoCommandException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _command = (BsonDocument)info.GetValue("_command", typeof(BsonDocument));
            _result = (BsonDocument)info.GetValue("_result", typeof(BsonDocument));
        }

        // properties
        public int Code
        {
            get { return _result.GetValue("code", -1).ToInt32(); }
        }

        public BsonDocument Command
        {
            get { return _command; }
        }

        public string ErrorMessage
        {
            get { return _result.GetValue("errmsg", "Unknown error.").AsString; }
        }

        public BsonDocument Result
        {
            get { return _result; }
        }

        // methods
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_command", _command);
            info.AddValue("_result", _result);
        }
    }
}
