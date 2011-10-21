/* Copyright 2010-2011 10gen Inc.
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
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson;

namespace MongoDB.Driver {
    /// <summary>
    /// Represents the result of a command (there are also subclasses for various commands).
    /// </summary>
    [Serializable]
    public class CommandResult {
        #region protected fields
        /// <summary>
        /// The command.
        /// </summary>
        protected IMongoCommand command;
        /// <summary>
        /// The response.
        /// </summary>
        protected BsonDocument response;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the CommandResult class.
        /// </summary>
        // since we often create instances of CommandResult using a generic type parameter
        // we need a constructor with no arguments (see also the Initialize method below)
        public CommandResult() {
        }

        /// <summary>
        /// Initializes a new instance of the CommandResult class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="response">The response.</param>
        public CommandResult(
            IMongoCommand command,
            BsonDocument response
        ) {
            this.command = command;
            this.response = response;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the command.
        /// </summary>
        public IMongoCommand Command {
            get { return command; }
        }

        /// <summary>
        /// Gets the command name.
        /// </summary>
        public string CommandName {
            get { return command.ToBsonDocument().GetElement(0).Name; }
        }

        /// <summary>
        /// Gets the response.
        /// </summary>
        public BsonDocument Response {
            get { return response; }
        }

        /// <summary>
        /// Gets the error message (null if none).
        /// </summary>
        public string ErrorMessage {
            get {
                BsonValue ok;
                if (response.TryGetValue("ok", out ok) && ok.ToBoolean()) {
                    return null;
                } else {
                    BsonValue errmsg;
                    if (response.TryGetValue("errmsg", out errmsg) && !errmsg.IsBsonNull) {
                        return errmsg.ToString();
                    } else {
                        return "Unknown error";
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Ok value from the response.
        /// </summary>
        public bool Ok {
            get {
                BsonValue ok;
                if (response.TryGetValue("ok", out ok)) {
                    return ok.ToBoolean();
                } else {
                    var message = string.Format("Command '{0}' failed. Response has no ok element (response was {1}).", CommandName, response.ToJson());
                    throw new MongoCommandException(message, this);
                }
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Initializes an existing instance of the CommandResult class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="response">The response.</param>
		// used after a constructor with no arguments (when creating a CommandResult from a generic type parameter)
        public void Initialize(
            IMongoCommand command,
            BsonDocument response
        ) {
            if (this.command != null || this.response != null) {
                var message = string.Format("{0} has already been initialized.", this.GetType().Name);
                throw new InvalidOperationException(message);
            }
            this.command = command;
            this.response = response;
        }
        #endregion
    }
}
