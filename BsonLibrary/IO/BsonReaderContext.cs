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

namespace MongoDB.BsonLibrary.IO {
    internal class BsonReaderContext {
        #region private fields
        private BsonReaderContext parentContext;
        private int startPosition;
        private int size;
        private BsonReaderDocumentType documentType;
        private BsonReadState readState;
        #endregion

        #region constructors
        internal BsonReaderContext(
            BsonReaderContext parentContext,
            int startPosition,
            int size,
            BsonReaderDocumentType documentType,
            BsonReadState readState
        ) {
            this.parentContext = parentContext;
            this.startPosition = startPosition;
            this.size = size;
            this.documentType = documentType;
            this.readState = readState;
        }
        #endregion

        #region internal properties
        internal BsonReaderContext ParentContext {
            get { return parentContext; }
        }

        internal int StartPosition {
            get { return startPosition; }
        }

        internal int Size {
            get { return size; }
        }

        internal BsonReaderDocumentType DocumentType {
            get { return documentType; }
        }

        internal BsonReadState ReadState {
            get { return readState; }
            set { readState = value; }
        }
        #endregion
    }
}
