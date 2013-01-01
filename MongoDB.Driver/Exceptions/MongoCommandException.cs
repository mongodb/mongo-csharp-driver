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
using System.Runtime.Serialization;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB command exception.
    /// </summary>
    [Serializable]
    public class MongoCommandException : MongoException
    {
        // private fields
        private CommandResult _commandResult;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoCommandException class.
        /// </summary>
        /// <param name="commandResult">The command result (an error message will be constructed using the result).</param>
        public MongoCommandException(CommandResult commandResult)
            : this(FormatErrorMessage(commandResult), commandResult)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoCommandException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public MongoCommandException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoCommandException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MongoCommandException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoCommandException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="commandResult">The command result.</param>
        public MongoCommandException(string message, CommandResult commandResult)
            : this(message)
        {
            _commandResult = commandResult;
        }

        /// <summary>
        /// Initializes a new instance of the MongoCommandException class (this overload supports deserialization).
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        public MongoCommandException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        // public properties
        /// <summary>
        /// Gets the command result.
        /// </summary>
        public CommandResult CommandResult
        {
            get { return _commandResult; }
        }

        // private static methods
        private static string FormatErrorMessage(CommandResult commandResult)
        {
            return string.Format("Command '{0}' failed: {1} (response: {2})", commandResult.CommandName, commandResult.ErrorMessage, commandResult.Response.ToJson());
        }
    }
}
