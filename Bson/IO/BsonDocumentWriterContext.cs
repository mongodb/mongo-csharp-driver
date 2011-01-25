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
    internal class BsonDocumentWriterContext {
        #region private fields
        private BsonDocumentWriterContext parentContext;
        private ContextType contextType;
        private BsonDocument document;
        private BsonArray array;
        private string code;
        private string name;
        #endregion

        #region constructors
        internal BsonDocumentWriterContext(
            BsonDocumentWriterContext parentContext,
            ContextType contextType,
            BsonDocument document
        ) {
            this.parentContext = parentContext;
            this.contextType = contextType;
            this.document = document;
        }

        internal BsonDocumentWriterContext(
            BsonDocumentWriterContext parentContext,
            ContextType contextType,
            BsonArray array
        ) {
            this.parentContext = parentContext;
            this.contextType = contextType;
            this.array = array;
        }

        internal BsonDocumentWriterContext(
            BsonDocumentWriterContext parentContext,
            ContextType contextType,
            string code
        ) {
            this.parentContext = parentContext;
            this.contextType = contextType;
            this.code = code;
        }
        #endregion

        #region internal properties
        internal BsonDocumentWriterContext ParentContext {
            get { return parentContext; }
        }

        internal string Name {
            get { return name; }
            set { name = value; }
        }

        internal ContextType ContextType {
            get { return contextType; }
        }

        internal BsonDocument Document {
            get { return document; }
        }

        internal BsonArray Array {
            get { return array; }
        }

        internal string Code {
            get { return code; }
        }
        #endregion
    }
}
