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
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Core
{
    /// <summary>
    /// Represents a get last error exception.
    /// </summary>
    [Serializable]
    public class WriteConcernException : Exception
    {
        // fields
        private readonly WriteConcernResult _writeConcernResult;

        // constructors
        /// <summary>
        /// Initializes a new instance of the WriteConcernException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="writeConcernResult">The command result.</param>
        public WriteConcernException(string message, WriteConcernResult writeConcernResult)
            : base(message)
        {
            _writeConcernResult = Ensure.IsNotNull(writeConcernResult, "writeConcernResult");
        }

        /// <summary>
        /// Initializes a new instance of the WriteConcernException class (this overload supports deserialization).
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        public WriteConcernException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _writeConcernResult = new WriteConcernResult(BsonSerializer.Deserialize<BsonDocument>((byte[])info.GetValue("_query", typeof(byte[]))));
        }

        // properties
        /// <summary>
        /// Gets the write concern result.
        /// </summary>
        /// <value>
        /// The write concern result.
        /// </value>
        public WriteConcernResult WriteConcernResult
        {
            get { return _writeConcernResult; }
        }

        // methods
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_writeConcernResult", _writeConcernResult.Wrapped.ToBson());
        }
    }
}
