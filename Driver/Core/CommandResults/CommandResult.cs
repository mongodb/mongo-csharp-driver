/* Copyright 2010 10gen Inc.
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
    [Serializable]
    public class CommandResult {
        #region protected fields
        protected BsonDocument response;
        #endregion

        #region constructors
        // since we are creating instances of CommandResult using a generic type parameter
        // our constructor cannot have any arguments (see the Initialize method below)
        public CommandResult() {
        }
        #endregion

        #region public properties
        public BsonDocument Response {
            get { return response; }
        }

        public string ErrorMessage {
            get {
                BsonValue err;
                if (response.TryGetValue("errmsg", out err)) {
                    return err.ToString();
                } else {
                    BsonValue ok;
                    if (response.TryGetValue("ok", out ok) && ok.ToBoolean()) {
                        return null;
                    } else {
                        return "Unknown error";
                    }
                }
            }
        }

        public bool Ok {
            get {
                BsonValue ok;
                if (response.TryGetValue("ok", out ok)) {
                    return ok.ToBoolean();
                } else {
                    throw new MongoCommandException("CommandResult is missing an ok element.");
                }
            }
        }
        #endregion

        #region public methods
		// used in place of a constructor with arguments (since we can't have arguments to our constructor)
        public void Initialize(
            BsonDocument response
        ) {
            if (this.response != null) {
                var message = string.Format("{0} already has a document", this.GetType().Name);
                throw new InvalidOperationException(message);
            }
            this.response = response;
        }
        #endregion
    }
}
