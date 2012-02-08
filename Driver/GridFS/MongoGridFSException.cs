﻿/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents a MongoDB GridFS exception.
    /// </summary>
    [Serializable]
    public class MongoGridFSException : MongoException
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoGridFSException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public MongoGridFSException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoGridFSException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MongoGridFSException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoGridFSException class (this overload supports deserialization).
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        public MongoGridFSException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
