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
using System.IO;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.IO {
    internal class BsonBinaryReaderContext {
        #region private fields
        private BsonBinaryReaderContext parentContext;
        private ContextType contextType;
        private int startPosition;
        private int size;
        #endregion

        #region constructors
        // used by Clone
        private BsonBinaryReaderContext() {
        }

        internal BsonBinaryReaderContext(
            BsonBinaryReaderContext parentContext,
            ContextType contextType,
            int startPosition,
            int size
        ) {
            this.parentContext = parentContext;
            this.contextType = contextType;
            this.startPosition = startPosition;
            this.size = size;
        }
        #endregion

        #region internal properties
        internal ContextType ContextType {
            get { return contextType; }
        }
        #endregion

        #region public methods
        public BsonBinaryReaderContext Clone() {
            var clone = new BsonBinaryReaderContext();
            clone.parentContext = this.parentContext;
            clone.contextType = this.contextType;
            clone.startPosition = this.startPosition;
            clone.size = this.size;
            return clone;
        }

        public BsonBinaryReaderContext PopContext(
            int position
        ) {
            int actualSize = position - startPosition;
            if (actualSize != size) {
                var message = string.Format("{0} size is incorrect", contextType);
                throw new FileFormatException(message);
            }
            return parentContext;
        }
        #endregion
    }
}
