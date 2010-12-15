﻿/* Copyright 2010 10gen Inc.
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

using MongoDB.Bson;

namespace MongoDB.Driver {
    [Serializable]
    public class MongoCommandException : MongoException {
        #region private fields
        private CommandResult commandResult;
        #endregion

        #region constructors
        public MongoCommandException(
            CommandResult commandResult
        )
            : this(FormatErrorMessage(commandResult), commandResult) {
        }

        public MongoCommandException(
            string message
        )
            : base(message) {
        }

        public MongoCommandException(
            string message,
            Exception innerException
        )
            : base(message, innerException) {
        }

        public MongoCommandException(
            string message,
            CommandResult commandResult
        )
            : this(message) {
                this.commandResult = commandResult;
        }

        // this constructor needed to support deserialization
        public MongoCommandException(
            SerializationInfo info,
            StreamingContext context
        )
            : base(info, context) {
        }
        #endregion

        #region public properties
        public CommandResult CommandResult {
            get { return commandResult; }
        }
        #endregion

        #region private static methods
        private static string FormatErrorMessage(
            CommandResult commandResult
        ) {
            return string.Format("Command '{0}' failed: {1} (response: {2})", commandResult.CommandName, commandResult.ErrorMessage, commandResult.Response.ToJson());
        }
        #endregion
    }
}
