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
using System.IO;
using System.Linq;
using System.Text;

namespace MongoDB.BsonLibrary.IO {
    internal class BsonJsonWriterContext {
        #region private fields
        private BsonJsonWriterContext parentContext;
        private BsonWriteState writeState;
        private string indentation;
        private bool hasElements = false;
        #endregion

        #region constructors
        internal BsonJsonWriterContext(
            BsonJsonWriterContext parentContext,
            BsonWriteState writeState,
            string indentation
        ) {
            this.parentContext = parentContext;
            this.writeState = writeState;
            this.indentation = indentation;
        }
        #endregion

        #region internal properties
        internal BsonJsonWriterContext ParentContext {
            get { return parentContext; }
        }

        internal BsonWriteState WriteState {
            get { return writeState; }
            set { writeState = value; }
        }

        internal string Indentation {
            get { return indentation; }
        }

        internal bool HasElements {
            get { return hasElements; }
            set { hasElements = value; }
        }
        #endregion
    }
}
