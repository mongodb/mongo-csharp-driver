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
    [Serializable]
    public class BsonBinaryWriterSettings {
        #region private static fields
        private static BsonBinaryWriterSettings defaults = new BsonBinaryWriterSettings();
        #endregion

        #region private fields
        private bool closeOutput = false;
        private bool fixOldBinarySubTypeOnOutput = true;
        private int maxDocumentSize = BsonDefaults.MaxDocumentSize;
        #endregion

        #region constructors
        public BsonBinaryWriterSettings() {
        }
        #endregion

        #region public static properties
        public static BsonBinaryWriterSettings Defaults {
            get { return defaults; }
        }
        #endregion

        #region public properties
        public bool CloseOutput {
            get { return closeOutput; }
            set { closeOutput = value; }
        }

        public bool FixOldBinarySubTypeOnOutput {
            get { return fixOldBinarySubTypeOnOutput; }
            set { fixOldBinarySubTypeOnOutput = value; }
        }

        public int MaxDocumentSize {
            get { return maxDocumentSize; }
            set { maxDocumentSize = value; }
        }
        #endregion
    }
}
