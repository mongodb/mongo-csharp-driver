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
    internal class BsonDocumentReaderContext {
        #region private fields
        private BsonDocumentReaderContext parentContext;
        private ContextType contextType;
        private BsonDocument document;
        private BsonArray array;
        private int index;
        #endregion

        #region constructors
        internal BsonDocumentReaderContext(
            BsonDocumentReaderContext parentContext,
            ContextType contextType,
            BsonArray array
        ) {
            this.parentContext = parentContext;
            this.contextType = contextType;
            this.array = array;
        }

        internal BsonDocumentReaderContext(
            BsonDocumentReaderContext parentContext,
            ContextType contextType,
            BsonDocument document
        ) {
            this.parentContext = parentContext;
            this.contextType = contextType;
            this.document = document;
        }

        // used by Clone
        private BsonDocumentReaderContext(
            BsonDocumentReaderContext parentContext,
            ContextType contextType,
            BsonDocument document,
            BsonArray array,
            int index
        ) {
            this.parentContext = parentContext;
            this.contextType = contextType;
            this.document = document;
            this.array = array;
            this.index = index;
        }
        #endregion

        #region internal properties
        internal BsonArray Array {
            get { return array; }
        }

        internal ContextType ContextType {
            get { return contextType; }
        }

        internal BsonDocument Document {
            get { return document; }
        }

        internal int Index {
            get { return index; }
            set { index = value; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Creates a clone of the context.
        /// </summary>
        /// <returns>A clone of the context.</returns>
        public BsonDocumentReaderContext Clone() {
            return new BsonDocumentReaderContext(
                parentContext,
                contextType,
                document,
                array,
                index
            );
        }

        public BsonElement GetNextElement() {
            if (index < document.ElementCount) {
                return document.GetElement(index++);
            } else {
                return null;
            }
        }

        public BsonValue GetNextValue() {
            if (index < array.Count) {
                return array[index++];
            } else {
                return null;
            }
        }

        public BsonDocumentReaderContext PopContext() {
            return parentContext;
        }
        #endregion
    }
}
