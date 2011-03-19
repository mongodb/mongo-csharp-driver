﻿/* Copyright 2010-2011 10gen Inc.
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
    /// Represents the results of a GetLastError command.
    /// </summary>
    [Serializable]
    public class GetLastErrorResult : CommandResult {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the GetLastErrorResult class.
        /// </summary>
        public GetLastErrorResult() {
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the number of documents affected.
        /// </summary>
        public int DocumentsAffected {
            get { return response["n"].ToInt32(); }
        }

        /// <summary>
        /// Gets whether the result has a LastErrorMessage.
        /// </summary>
        public bool HasLastErrorMessage {
            get { return response["err", false].ToBoolean(); }
        }

        /// <summary>
        /// Gets the last error message (null if none).
        /// </summary>
        public string LastErrorMessage {
            get { 
                var err = response["err", false];
                return (err.ToBoolean()) ? err.ToString() : null;
            }
        }

        /// <summary>
        /// Gets whether the last command updated an existing document.
        /// </summary>
        public bool UpdatedExisting {
            get {
                var updatedExisting = response["updatedExisting", false];
                return updatedExisting.ToBoolean();
            }
        }
        #endregion
    }
}
