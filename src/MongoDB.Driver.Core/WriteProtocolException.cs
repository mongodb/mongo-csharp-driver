/* Copyright 2013-2014 MongoDB Inc.
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

namespace MongoDB.Driver
{
    [Serializable]
    public class WriteProtocolException : MongoServerException
    {
        // fields
        private readonly BsonDocument _result;

        // constructors
        public WriteProtocolException(ConnectionId connectionId, string message)
            : this(connectionId, message, null)
        {
        }

        public WriteProtocolException(ConnectionId connectionId, string message, BsonDocument result)
            : base(connectionId, message)
        {
            _result = result; // can be null
        }

        protected WriteProtocolException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _result = BsonSerializer.Deserialize<BsonDocument>((byte[])info.GetValue("_result", typeof(byte[])));
        }

       // properties
        public BsonDocument Result
        {
            get { return _result; }
        }

        // methods
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_result", _result.ToBson());
        }
    }
}
