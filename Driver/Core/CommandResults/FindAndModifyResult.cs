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
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver {
    /// <summary>
    /// Represents the result of a FindAndModify command.
    /// </summary>
    [Serializable]
    public class FindAndModifyResult : CommandResult {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the FindAndModifyResult class.
        /// </summary>
        public FindAndModifyResult() {
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the modified document.
        /// </summary>
        public BsonDocument ModifiedDocument {
            get { return response["value"].AsBsonDocument; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Gets the modified document as a TDocument.
        /// </summary>
        /// <typeparam name="TDocument">The type of the modified document.</typeparam>
        /// <returns>The modified document.</returns>
        public TDocument GetModifiedDocumentAs<TDocument>() {
            return BsonSerializer.Deserialize<TDocument>(ModifiedDocument);
        }
        #endregion
    }
}
