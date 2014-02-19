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
using System.Collections.ObjectModel;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Support;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the details of a write concern error.
    /// </summary>
    public class WriteConcernError
    {
        // private fields
        private readonly int _code;
        private readonly BsonDocument _details;
        private readonly string _message;

        // constructors
        internal WriteConcernError(int code, string message, BsonDocument details)
        {
            _code = code;
            _details = details;
            _message = message;
        }

        // public properties
        /// <summary>
        /// Gets the error code.
        /// </summary>
        /// <value>
        /// The error code.
        /// </value>
        public int Code
        {
            get { return _code; }
        }

        /// <summary>
        /// Gets the error information.
        /// </summary>
        /// <value>
        /// The error information.
        /// </value>
        public BsonDocument Details
        {
            get { return _details; }
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>
        /// The error message.
        /// </value>
        public string Message
        {
            get { return _message; }
        }
    }
}
