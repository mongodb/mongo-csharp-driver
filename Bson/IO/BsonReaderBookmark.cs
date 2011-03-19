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

namespace MongoDB.Bson.IO {
    /// <summary>
    /// Represents a bookmark that can be used to return a reader to the current position and state.
    /// </summary>
    public abstract class BsonReaderBookmark {
        #region protected fields
        /// <summary>
        /// The state of the reader.
        /// </summary>
        protected BsonReaderState state;
        /// <summary>
        /// The current BSON type.
        /// </summary>
        protected BsonType currentBsonType;
        /// <summary>
        /// The name of the current element.
        /// </summary>
        protected string currentName;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonReaderBookmark class.
        /// </summary>
        /// <param name="state">The state of the reader.</param>
        /// <param name="currentBsonType">The current BSON type.</param>
        /// <param name="currentName">The name of the current element.</param>
        protected BsonReaderBookmark(
            BsonReaderState state,
            BsonType currentBsonType,
            string currentName
        ) {
            this.state = state;
            this.currentBsonType = currentBsonType;
            this.currentName = currentName;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the current state of the reader.
        /// </summary>
        public BsonReaderState State {
            get { return state; }
        }

        /// <summary>
        /// Gets the current BsonType;
        /// </summary>
        public BsonType CurrentBsonType {
            get { return currentBsonType; }
        }

        /// <summary>
        /// Gets the name of the current element.
        /// </summary>
        public string CurrentName {
            get { return currentName; }
        }
        #endregion
    }
}
