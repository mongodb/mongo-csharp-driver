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
    public class BsonBinaryReaderSettings {
        #region private static fields
        private static BsonBinaryReaderSettings defaults = new BsonBinaryReaderSettings();
        #endregion

        #region private fields
        private bool closeInput = false;
        private bool fixOldBinarySubTypeOnInput = true;
        private int maxDocumentSize = BsonDefaults.MaxDocumentSize;
        private bool isFrozen;
        #endregion

        #region constructors
        public BsonBinaryReaderSettings() {
        }
        #endregion

        #region public static properties
        public static BsonBinaryReaderSettings Defaults {
            get { return defaults; }
            set { defaults = value; }
        }
        #endregion

        #region public properties
        public bool CloseInput {
            get { return closeInput; }
            set {
                if (isFrozen) { throw new InvalidOperationException("BsonBinaryReaderSettings is frozen"); }
                closeInput = value;
            }
        }

        public bool FixOldBinarySubTypeOnInput {
            get { return fixOldBinarySubTypeOnInput; }
            set {
                if (isFrozen) { throw new InvalidOperationException("BsonBinaryReaderSettings is frozen"); }
                fixOldBinarySubTypeOnInput = value;
            }
        }

        public bool IsFrozen {
            get { return isFrozen; }
        }

        public int MaxDocumentSize {
            get { return maxDocumentSize; }
            set {
                if (isFrozen) { throw new InvalidOperationException("BsonBinaryReaderSettings is frozen"); }
                maxDocumentSize = value;
            }
        }
        #endregion

        #region public methods
        public BsonBinaryReaderSettings Freeze() {
            isFrozen = true;
            return this;
        }
        #endregion
    }
}
