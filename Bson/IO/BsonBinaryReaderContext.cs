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

namespace MongoDB.Bson.IO {
    internal class BsonBinaryReaderContext {
        #region private fields
        private BsonBinaryReaderContext parentContext;
        private int startPosition;
        private int size;
        private BsonReadState readState;
        private bool isBookmark;
        private int bookmarkPosition;
        #endregion

        #region constructors
        internal BsonBinaryReaderContext(
            BsonBinaryReaderContext parentContext,
            BsonReadState readState
        ) {
            this.parentContext = parentContext;
            this.readState = readState;
        }
        #endregion

        #region internal properties
        internal BsonBinaryReaderContext ParentContext {
            get {
                if (isBookmark) {
                    throw new InvalidOperationException("PushBookmark called without matching PopBookmark");
                }
                return parentContext;
            }
        }

        internal BsonBinaryReaderContext BookmarkParentContext {
            get {
                if (!isBookmark) {
                    throw new InvalidOperationException("Context is not a bookmark");
                }
                return parentContext;
            }
        }

        internal int StartPosition {
            get { return startPosition; }
            set { startPosition = value; }
        }

        internal int Size {
            get { return size; }
            set { size = value; }
        }

        internal int BookmarkPosition {
            get { return bookmarkPosition; }
            set { bookmarkPosition = value; }
        }

        internal BsonReadState ReadState {
            get { return readState; }
        }
        #endregion

        #region internal methods
        internal BsonBinaryReaderContext GetBookmark() {
            for (var context = this; context != null; context = context.parentContext) {
                if (context.isBookmark) {
                    return context;
                }
            }
            throw new InvalidOperationException("PopBookmark called without matching PushBookmark");
        }

        internal BsonBinaryReaderContext CreateBookmark() {
            var context = new BsonBinaryReaderContext(this, readState);
            context.startPosition = startPosition;
            context.size = size;
            context.isBookmark = true;
            return context;
        }
        #endregion
    }
}
