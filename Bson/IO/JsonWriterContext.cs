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
    internal class JsonWriterContext {
        #region private fields
        private JsonWriterContext parentContext;
        private ContextType contextType;
        private string indentation;
        private bool hasElements = false;
        #endregion

        #region constructors
        internal JsonWriterContext(
            JsonWriterContext parentContext,
            ContextType contextType,
            string indentChars
        ) {
            this.parentContext = parentContext;
            this.contextType = contextType;
            this.indentation = (parentContext == null) ? indentChars : parentContext.Indentation + indentChars;
        }
        #endregion

        #region internal properties
        internal JsonWriterContext ParentContext {
            get { return parentContext; }
        }

        internal ContextType ContextType {
            get { return contextType; }
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
