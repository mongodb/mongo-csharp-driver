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
    /// Represents the results of a validate collection command.
    /// </summary>
    [Serializable]
    public class ValidateCollectionResult : CommandResult {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the ValidateCollectionResult class.
        /// </summary>
        public ValidateCollectionResult() {
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the namespace.
        /// </summary>
        public string Namespace {
            get { return response["ns"].AsString; }
        }

        /// <summary>
        /// Gets the result string.
        /// </summary>
        public string ResultString {
            get { return response["result"].AsString; }
        }
        #endregion
    }
}
